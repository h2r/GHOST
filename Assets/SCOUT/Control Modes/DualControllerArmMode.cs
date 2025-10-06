using UnityEngine;

// Two controller mode for controlling two separate robot arms and VR locomotion.
public class DualControllerArmMode : TwoControllerMode
{
    public enum ArmControlMode { Absolute, Relative }

    [Header("Robot References")]
    public SpotMode spot2; // The primary spot (spot1) is passed into ControlUpdate

    [Header("Arm Control")]
    public ArmControlMode armControlMode = ArmControlMode.Relative;

    [Header("Locomotion")]
    public GameObject cameraRig;
    public RigPositioner rigPositioner;
    public Transform headTransform;
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 120f;
    public bool useHeadRelativeMovement = true;

    [Header("Utility References")]
    public DepthManager depthManager1;
    public DepthManager depthManager2;

    // --- Internal State ---
    private Vector3 initialControllerPositionLeft, initialControllerPositionRight;
    private Quaternion initialControllerRotationLeft, initialControllerRotationRight;
    private Vector3 initialGripperPositionLeft, initialGripperPositionRight;
    private Quaternion initialGripperRotationLeft, initialGripperRotationRight;
    private bool isRelativeModeActiveLeft = false;
    private bool isRelativeModeActiveRight = false;
    private int showSpotState = 0;

    public override void ControlUpdate(SpotMode spot1, ControllerModel leftModel, ControllerModel rightModel)
    {
        // --- Get Inputs ---
        bool leftTriggerHeld = OVRInput.Get(leftModel.indexButton);
        bool rightTriggerHeld = OVRInput.Get(rightModel.indexButton);
        bool leftGripDown = OVRInput.GetDown(leftModel.gripButton);
        bool rightGripDown = OVRInput.GetDown(rightModel.gripButton);
        Vector2 leftJoy = OVRInput.Get(leftModel.joystick);
        Vector2 rightJoy = OVRInput.Get(rightModel.joystick);
        bool isAnyJoyInUse = leftJoy.magnitude > 0.1f || rightJoy.magnitude > 0.1f;

        // --- Locomotion ---
        if (cameraRig != null && rigPositioner != null && headTransform != null)
        {
            // Left Joystick: Movement (Fly)
            if (leftJoy.magnitude > 0.1f)
            {
                Vector3 move;
                if (useHeadRelativeMovement)
                {
                    Quaternion headYaw = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);
                    Vector3 forward = headYaw * Vector3.forward;
                    Vector3 right = headYaw * Vector3.right;
                    move = (forward * leftJoy.y + right * leftJoy.x);
                }
                else
                {
                    Quaternion rigRotation = cameraRig.transform.rotation;
                    Vector3 forward = rigRotation * Vector3.forward; forward.y = 0; forward.Normalize();
                    Vector3 right = rigRotation * Vector3.right; right.y = 0; right.Normalize();
                    move = (forward * leftJoy.y + right * leftJoy.x);
                }
                rigPositioner.pos += move * moveSpeed * Time.deltaTime;
            }

            // Right Joystick: Rotation
            if (Mathf.Abs(rightJoy.x) > 0.1f)
            {
                Vector3 rotationAxis = Vector3.up;
                Vector3 rotationCenter = headTransform.position;
                float angle = rotationSpeed * rightJoy.x * Time.deltaTime;
                cameraRig.transform.RotateAround(rotationCenter, rotationAxis, angle);
                rigPositioner.pos = cameraRig.transform.position;
            }
        }
        leftModel.joystickLabel = "Fly";
        rightModel.joystickLabel = "Rotate View";

        // --- Right Hand (Spot 1) ---
        HandleArmAndHeight(spot1, rightModel, rightTriggerHeld, rightGripDown, isAnyJoyInUse, ref isRelativeModeActiveRight,
                           ref initialControllerPositionRight, ref initialControllerRotationRight,
                           ref initialGripperPositionRight, ref initialGripperRotationRight);
        
        // --- Left Hand (Spot 2) ---
        if (spot2 != null)
        {
            HandleArmAndHeight(spot2, leftModel, leftTriggerHeld, leftGripDown, isAnyJoyInUse, ref isRelativeModeActiveLeft,
                               ref initialControllerPositionLeft, ref initialControllerRotationLeft,
                               ref initialGripperPositionLeft, ref initialGripperRotationLeft);
        }
        else
        {
            leftModel.indexLabel = "Spot 2 Not Set";
        }

        // --- Utility Buttons (Right Hand, while controlling arm) ---
        if (rightTriggerHeld && !isAnyJoyInUse)
        {
            if (OVRInput.GetDown(rightModel.axButton))
            {
                HandleShowSpotToggle();
            }
            if (OVRInput.GetDown(rightModel.byButton))
            {
                ToggleArmCamera();
            }
            
            rightModel.axLabel = "Toggle Point Cloud";
            rightModel.byLabel = "Toggle Arm Cam";
        }
    }

    private void HandleArmAndHeight(SpotMode spot, ControllerModel model, bool triggerHeld, bool gripDown, bool isJoyInUse,
                                    ref bool isRelativeModeActive, ref Vector3 initialControllerPosition,
                                    ref Quaternion initialControllerRotation, ref Vector3 initialGripperPosition,
                                    ref Quaternion initialGripperRotation)
    {
        if (gripDown)
        {
            spot.SetGripperOpen(!spot.GetGripperOpen());
        }

        if (triggerHeld && !isJoyInUse)
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
                        if(spot.GetGripperPos() != null)
                        {
                            initialControllerPosition = model.anchor.transform.position;
                            initialControllerRotation = model.anchor.transform.rotation;
                            initialGripperPosition = spot.GetGripperPos().position;
                            initialGripperRotation = spot.GetGripperPos().rotation;
                            isRelativeModeActive = true;
                        }
                    }
                    
                    if(isRelativeModeActive)
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
            model.indexLabel = isJoyInUse ? "Release Joystick to Control Arm" : "Hold: Control Arm";

            // Height Control
            if (OVRInput.Get(model.byButton))
                spot.AdjustHeight(0.02f);
            if (OVRInput.Get(model.axButton))
                spot.AdjustHeight(-0.02f);

            model.axLabel = "Lower Body";
            model.byLabel = "Raise Body";
        }

        model.gripLabel = spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper";
    }

    private void HandleShowSpotToggle()
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

    public override string GetName() => "Dual Arm & Fly";

    public override int ModeIndex => 8;
    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;

    public override void AssignDefaultLabels(ControllerModel left, ControllerModel right)
    {
        // Left
        left.joystickLabel = "Fly";
        left.indexLabel = "Hold: Control Arm 2";
        left.gripLabel = "Toggle Gripper 2";
        left.axLabel = "Lower Body 2";
        left.byLabel = "Raise Body 2";

        // Right
        right.joystickLabel = "Rotate View";
        right.indexLabel = "Hold: Control Arm 1";
        right.gripLabel = "Toggle Gripper 1";
        right.axLabel = "Lower Body 1";
        right.byLabel = "Raise Body 1";
    }
}
