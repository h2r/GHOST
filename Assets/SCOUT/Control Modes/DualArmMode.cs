using UnityEngine;
using System.Collections;

public class DualArmMode : TwoControllerMode
{
    public enum ArmControlMode { Absolute, Relative }

    [Header("Arm Control")]
    public ArmControlMode armControlMode = ArmControlMode.Absolute;

    [Header("Locomotion References")]
    public GameObject cameraRig;
    public RigPositioner rigPositioner;
    public Transform headTransform;
    public GameObject vignette;

    [Header("Locomotion Settings")]
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

    public bool IsJoystickInUse { get; private set; }

    // Internal state for arm control
    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    // Internal state for locomotion
    private float lastSnapTime = 0f;
    private float currentTurnVelocity = 0f;
    private float prevJoyX = 0f;
    private float initialY;
    private bool hasInitialY = false;

    public override void ControlUpdate(SpotMode spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        if (cameraRig == null || rigPositioner == null || headTransform == null) return;

        // --- Get All Inputs ---
        Vector2 leftJoy = OVRInput.Get(leftModel.joystick);
        bool leftTrigger = OVRInput.Get(leftModel.indexButton);
        bool leftGrip = OVRInput.Get(leftModel.gripButton);
        bool leftTriggerDown = OVRInput.GetDown(leftModel.indexButton);
        bool leftResetY = OVRInput.GetDown(leftModel.axButton);
        IsJoystickInUse = leftJoy.magnitude > 0.1f;

        bool rightTrigger = OVRInput.Get(rightModel.indexButton);
        bool rightGripDown = OVRInput.GetDown(rightModel.gripButton);

        // --- Right Controller: Arm Control Logic ---
        if (rightGripDown)
        {
            spot.SetGripperOpen(!spot.GetGripperOpen());
            spot.StartCoroutine(VibrateController(0.1f, 0.1f, 0.2f, OVRInput.Controller.RTouch));
        }

        if (IsJoystickInUse)
        {
            isRelativeModeActive = false;
        }
        else
        {
            if (rightTrigger)
            {
                switch (armControlMode)
                {
                    case ArmControlMode.Absolute:
                        spot.SetGripperTf(rightModel.anchor.transform);
                        break;
                    case ArmControlMode.Relative:
                        if (!isRelativeModeActive)
                        {
                            initialControllerPosition = rightModel.anchor.transform.position;
                            initialControllerRotation = rightModel.anchor.transform.rotation;
                            initialGripperPosition = spot.GetGripperPos().position;
                            initialGripperRotation = spot.GetGripperPos().rotation;
                            isRelativeModeActive = true;
                        }
                        Vector3 deltaPos = rightModel.anchor.transform.position - initialControllerPosition;
                        Quaternion deltaRot = rightModel.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);
                        spot.SetGripperWorldPose(initialGripperPosition + deltaPos, deltaRot * initialGripperRotation);
                        break;
                }
            }
            else
            {
                isRelativeModeActive = false;
            }
        }

        // --- Left Controller: Locomotion Logic ---
        if (leftTriggerDown)
        {
            initialY = rigPositioner.y;
            hasInitialY = true;
        }

        Vector3 rotationCenter = headTransform.position;
        if (leftTrigger)
        {
            if (Mathf.Abs(leftJoy.x) > 0.1f)
            {
                if (useSnapTurn)
                {
                    if (leftJoy.x < -snapTurnDeadzone && prevJoyX >= -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                    {
                        cameraRig.transform.RotateAround(rotationCenter, Vector3.up, -snapAngle);
                        lastSnapTime = Time.time;
                    }
                    else if (leftJoy.x > snapTurnDeadzone && prevJoyX <= snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                    {
                        cameraRig.transform.RotateAround(rotationCenter, Vector3.up, snapAngle);
                        lastSnapTime = Time.time;
                    }
                }
                else
                {
                    if (useRotationalInertia)
                    {
                        float targetTurnSpeed = rotationSpeed * leftJoy.x;
                        currentTurnVelocity = Mathf.MoveTowards(currentTurnVelocity, targetTurnSpeed, turnAcceleration * Time.deltaTime);
                        cameraRig.transform.RotateAround(rotationCenter, Vector3.up, currentTurnVelocity * Time.deltaTime);
                    }
                    else
                    {
                        float angle = rotationSpeed * leftJoy.x * Time.deltaTime;
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
        else if (leftGrip)
        {
            if (Mathf.Abs(leftJoy.y) > 0.1f)
            {
                rigPositioner.pos += leftJoy.y * moveSpeed * Time.deltaTime * Vector3.up;
            }
        }
        else
        {
            if (IsJoystickInUse)
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
                    Vector3 forward = cameraRig.transform.forward; forward.y = 0; forward.Normalize();
                    Vector3 right = cameraRig.transform.right; right.y = 0; right.Normalize();
                    move = (forward * leftJoy.y + right * leftJoy.x);
                }
                rigPositioner.pos += move * moveSpeed * Time.deltaTime;
            }
        }
        prevJoyX = leftJoy.x;

        if (leftResetY && hasInitialY)
        {
            Vector3 pos = rigPositioner.pos;
            pos.y = initialY;
            rigPositioner.pos = pos;
        }

        // --- Update Labels ---
        // Right Controller
        rightModel.joystickLabel = "";
        rightModel.indexLabel = rightTrigger ? (isRelativeModeActive ? "Controlling Arm" : "") : "Hold: Control Arm";
        rightModel.gripLabel = spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper";

        // Left Controller
        if (leftTrigger) { leftModel.joystickLabel = "Rotate"; }
        else if (leftGrip) { leftModel.joystickLabel = "Up/Down"; }
        else { leftModel.joystickLabel = "Fly"; }
        leftModel.indexLabel = leftTrigger ? "" : "Hold: Rotate";
        leftModel.gripLabel = leftGrip ? "" : "Hold: Up/Down";
        leftModel.axLabel = hasInitialY ? "Reset Y (X)" : "";
    }

    public override void AssignDefaultLabels(ControllerModel left, ControllerModel right)
    {
        left.joystickLabel = "Fly";
        left.indexLabel = "Hold: Rotate";
        left.gripLabel = "Hold: Up/Down";
        left.axLabel = "Reset Y";

        right.joystickLabel = "";
        right.indexLabel = "Hold: Control Arm";
        right.gripLabel = "Toggle Gripper";
    }

    private IEnumerator VibrateController(float duration, float frequency, float amplitude, OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controller);
    }

    public override string GetName() => "Arm & Fly";
    public override int ModeIndex => 7;
    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;
}
