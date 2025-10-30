using System;
using UnityEngine;

public class DualArmMode : TwoControllerMode
{
    // --- Fields from Arm6AxisMode ---
    public enum ArmControlMode { Absolute, Relative }
    [Header("Arm Control")]
    public ArmControlMode armControlMode = ArmControlMode.Relative;
    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    // --- Fields from LocomotionJoystickMode ---
    [Header("Locomotion & References")]
    public GameObject cameraRig;
    public RigPositioner rigPositioner;
    public GameObject vignette;
    public Transform headTransform;
    public float moveSpeed = 2.0f;
    public bool useHeadRelativeMovement = true;
    [Header("Rotation Settings")]
    public float rotationSpeed = 120f;
    public bool useSnapTurn = false;
    public float snapAngle = 25f;
    public float snapTurnDeadzone = 0.5f;
    public float snapTurnCooldown = 0.4f;
    public bool useRotationalInertia = false;
    public float turnAcceleration = 120f;
    public float turnDamping = 5f;
    [Header("Utility References")]
    public DepthManager depthManager1;
    public DepthManager depthManager2;

    // --- Internal State ---
    private bool isLeftJoystickInUse = false;
    private float initialY;
    private bool hasInitialY = false;
    private bool isLeftGripHeld = false;
    private float prevLeftJoyX = 0f;
    private float lastSnapTime = 0f;
    private float currentTurnVelocity = 0f;
    private int showSpotState = 0;


    public override void ControlUpdate(SpotController spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        // --- LEFT CONTROLLER: LOCOMOTION (from LocomotionJoystickMode) ---
        HandleLocomotion(leftModel);

        // --- RIGHT CONTROLLER: ARM (from Arm6AxisMode) ---
        HandleArm(spot, rightModel);
    }

    private void HandleLocomotion(ControllerModel model)
    {
        if (cameraRig == null || rigPositioner == null || headTransform == null) return;

        if (vignette != null) vignette.SetActive(false); // Vignette logic can be added later if needed

        Vector2 joystick = OVRInput.Get(model.joystick);
        isLeftJoystickInUse = joystick.magnitude > 0.1f;

        bool trigger = OVRInput.Get(model.indexButton);
        bool grip = OVRInput.Get(model.gripButton);

        if (grip && !isLeftGripHeld)
        {
            initialY = rigPositioner.y;
            hasInitialY = true;
        }
        isLeftGripHeld = grip;

        Vector3 rotationCenter = headTransform.position;

        if (grip) // Rotate
        {
            if (Mathf.Abs(joystick.x) > 0.1f)
            {
                if (useSnapTurn)
                {
                    if (joystick.x < -snapTurnDeadzone && prevLeftJoyX >= -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                    {
                        cameraRig.transform.RotateAround(rotationCenter, Vector3.up, -snapAngle);
                        lastSnapTime = Time.time;
                    }
                    else if (joystick.x > snapTurnDeadzone && prevLeftJoyX <= snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                    {
                        cameraRig.transform.RotateAround(rotationCenter, Vector3.up, snapAngle);
                        lastSnapTime = Time.time;
                    }
                }
                else
                {
                    if (useRotationalInertia)
                    {
                        float targetTurnSpeed = rotationSpeed * joystick.x;
                        currentTurnVelocity = Mathf.MoveTowards(currentTurnVelocity, targetTurnSpeed, turnAcceleration * Time.deltaTime);
                        cameraRig.transform.RotateAround(rotationCenter, Vector3.up, currentTurnVelocity * Time.deltaTime);
                    }
                    else
                    {
                        float angle = rotationSpeed * joystick.x * Time.deltaTime;
                        cameraRig.transform.RotateAround(rotationCenter, Vector3.up, angle);
                    }
                }
            }
            else if (useRotationalInertia)
            {
                currentTurnVelocity = Mathf.Lerp(currentTurnVelocity, 0f, turnDamping * Time.deltaTime);
                cameraRig.transform.RotateAround(rotationCenter, Vector3.up, currentTurnVelocity * Time.deltaTime);
            }
        }
        else if (trigger) // Up/Down
        {
            if (Mathf.Abs(joystick.y) > 0.1f)
            {
                rigPositioner.pos += joystick.y * moveSpeed * Time.deltaTime * Vector3.up;
            }
        }
        else // Fly
        {
            if (isLeftJoystickInUse)
            {
                Vector3 move;
                if (useHeadRelativeMovement)
                {
                    Quaternion headYaw = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);
                    Vector3 forward = headYaw * Vector3.forward;
                    Vector3 right = headYaw * Vector3.right;
                    move = (forward * joystick.y + right * joystick.x);
                }
                else
                {
                    Vector3 forward = cameraRig.transform.forward; forward.y = 0; forward.Normalize();
                    Vector3 right = cameraRig.transform.right; right.y = 0; right.Normalize();
                    move = (forward * joystick.y + right * joystick.x);
                }
                rigPositioner.pos += move * moveSpeed * Time.deltaTime;
            }
        }
        prevLeftJoyX = joystick.x;

        if (OVRInput.GetDown(model.axButton) && hasInitialY)
        {
            Vector3 pos = rigPositioner.pos;
            pos.y = initialY;
            rigPositioner.pos = pos;
        }
        
        HandleShowSpotToggle(model); // B/Y button

        // Set Left Labels
        if (grip) model.joystickLabel = "Rotate";
        else if (trigger) model.joystickLabel = "Up/Down";
        else model.joystickLabel = "Fly";
        model.indexLabel = trigger ? "" : "Hold: Up/Down";
        model.gripLabel = grip ? "" : "Hold: Rotate";
        model.axLabel = hasInitialY ? "Reset Y" : "";
        // byLabel is set in HandleShowSpotToggle
    }

    private void HandleArm(SpotController spot, ControllerModel model)
    {
        bool triggerHeld = OVRInput.Get(model.indexButton);
        bool gripDown = OVRInput.GetDown(model.gripButton);

        // Toggle Arm Camera on B/Y
        if (OVRInput.GetDown(model.byButton))
        {
            ToggleArmCamera();
        }

        // Toggle gripper on grip press
        if (gripDown)
        {
            spot.SetGripperOpen(!spot.GetGripperOpen());
        }

        // Arm Positioning Logic
        if (triggerHeld && !isLeftJoystickInUse) // Use state from left controller
        {
            model.indexLabel = "Controlling Arm";
            switch (armControlMode)
            {
                case ArmControlMode.Absolute:
                    spot.SetGripperTf(model.anchor.transform);
                    isRelativeModeActive = false;
                    break;
                case ArmControlMode.Relative:
                    if (!isRelativeModeActive)
                    {
                        if (spot.GetGripperPos() != null)
                        {
                            initialControllerPosition = model.anchor.transform.position;
                            initialControllerRotation = model.anchor.transform.rotation;
                            initialGripperPosition = spot.GetGripperPos().position;
                            initialGripperRotation = spot.GetGripperPos().rotation;
                            isRelativeModeActive = true;
                        }
                    }
                    if (isRelativeModeActive)
                    {
                        Vector3 deltaPos = model.anchor.transform.position - initialControllerPosition;
                        Quaternion deltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);
                        spot.SetGripperWorldPose(initialGripperPosition + deltaPos, deltaRot * initialGripperRotation);
                    }
                    break;
            }
        }
        else
        {
            isRelativeModeActive = false;
            model.indexLabel = isLeftJoystickInUse ? "Release Left Stick" : "Hold: Control Arm";
        }

        spot.ChangeGripperColorBasedOnDistance();

        // Set Right Labels
        model.joystickLabel = ""; // Right joystick is unused for arm control
        model.gripLabel = spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper";
        model.axLabel = ""; // A/X is unused on right controller
        model.byLabel = "Toggle Arm Cam";
    }

    private void HandleShowSpotToggle(ControllerModel model)
    {
        if (OVRInput.GetDown(model.byButton))
        {
            showSpotState = (showSpotState + 1) % 3;
            if (depthManager1 != null)
            {
                depthManager1.show_spot = (showSpotState == 0 || showSpotState == 1);
            }
            if (depthManager2 != null)
            {
                depthManager2.show_spot = (showSpotState == 0 || showSpotState == 2);
            }
        }

        string byLabel = "Toggle Point Cloud";
        model.byLabel = byLabel;
    }

    private void ToggleArmCamera()
    {
        var uiManager = GameObject.FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            var armCameraView = uiManager.FindCameraMode<ArmCameraView>();
            var allOffView = uiManager.FindCameraMode<AllOffView>();
            if (ScoutModeManager.Instance.cameraView.activeCameraMode == armCameraView)
            {
                ScoutModeManager.Instance.cameraView.SetActiveCameraMode(allOffView);
            }
            else
            {
                ScoutModeManager.Instance.cameraView.SetActiveCameraMode(armCameraView);
            }
        }
    }

    public override string GetName() => "Fly & Arm";

    public override int ModeIndex => 7;
    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;

    public override void AssignDefaultLabels(ControllerModel left, ControllerModel right)
    {
        // Left
        left.joystickLabel = "Fly";
        left.indexLabel = "Hold: Up/Down";
        left.gripLabel = "Hold: Rotate";
        left.axLabel = "Reset Y";
        left.byLabel = "Toggle Point Cloud";

        // Right
        right.joystickLabel = "";
        right.indexLabel = "Hold: Control Arm";
        right.gripLabel = "Toggle Gripper";
        right.axLabel = "";
        right.byLabel = "Toggle Arm Cam";
    }
}