using System;
using UnityEngine;

public class Arm6AxisMode : OneControllerMode
{
    public enum ArmControlMode
    {
        Absolute,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.Relative;

    [Header("Locomotion")]
    public GameObject cameraRig;
    public RigPositioner rigPositioner;
    public Transform headTransform;
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 120f;

    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    public bool IsJoystickInUse { get; private set; }

    public bool armCamera = true;

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        try
        {
            // --- Input ---
            bool handDown = OVRInput.GetDown(model.gripButton);
            bool triggerHeld = OVRInput.Get(model.indexButton);

            if (OVRInput.GetDown(model.byButton))
            {
                var uiManager = GameObject.FindObjectOfType<UIManager>();
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

            // Toggle gripper on trigger press
            if (handDown)
            {
                spot.SetGripperOpen(!spot.GetGripperOpen());
            }

            bool gripperOpen = spot.GetGripperOpen();

            // --- VR Navigation (Locomotion & Rotation) ---
            Vector2 thumbstick = OVRInput.Get(model.joystick);
            bool thumbstickPressed = OVRInput.Get(model.joystickButton);
            IsJoystickInUse = thumbstick.magnitude > 0.1f || thumbstickPressed;

            // --- Arm Positioning Logic ---
            bool canControlArm = model.anchor != null;

            // Check for joystick use on EITHER controller
            bool isOtherJoystickInUse = false;
            OneControllerMode otherMode = (this == ScoutModeManager.Instance.singleDrive.leftControl) ? ScoutModeManager.Instance.singleDrive.rightControl : ScoutModeManager.Instance.singleDrive.leftControl;
            if (otherMode is LocomotionJoystickMode locoMode)
            {
                isOtherJoystickInUse = locoMode.IsJoystickInUse;
            }
            else if (otherMode is Arm6AxisMode armMode)
            {
                isOtherJoystickInUse = armMode.IsJoystickInUse;
            }
            bool anyJoystickInUse = IsJoystickInUse || isOtherJoystickInUse;

            if (thumbstickPressed)
            {
                // VR Rotation
                if (cameraRig != null && headTransform != null && Mathf.Abs(thumbstick.x) > 0.1f)
                {
                    Vector3 rotationAxis = Vector3.up;
                    Vector3 rotationCenter = headTransform.position;
                    float angle = rotationSpeed * thumbstick.x * Time.deltaTime;
                    cameraRig.transform.RotateAround(rotationCenter, rotationAxis, angle);
                    rigPositioner.pos = cameraRig.transform.position;
                }
            }
            else
            {
                // VR Locomotion
                if (cameraRig != null && thumbstick.magnitude > 0.1f)
                {
                    Quaternion rigRotation = cameraRig.transform.rotation;

                    Vector3 forward = rigRotation * Vector3.forward;
                    forward.y = 0;
                    forward.Normalize();

                    Vector3 right = rigRotation * Vector3.right;
                    right.y = 0;
                    right.Normalize();

                    Vector3 horizontalMove = forward * thumbstick.y + right * thumbstick.x;
                    rigPositioner.pos += horizontalMove * moveSpeed * Time.deltaTime;
                }
            }

            switch (armControlMode)
            {
                case ArmControlMode.Absolute:
                    if (canControlArm && triggerHeld && !anyJoystickInUse)
                    {
                        spot.SetGripperTf(model.anchor.transform);
                    }
                    break;

                case ArmControlMode.Relative:
                    if (canControlArm && triggerHeld && !anyJoystickInUse)
                    {
                        if (!isRelativeModeActive)
                        {
                            if (spot.GetGripperPos() != null)
                            {
                                // Start of a new relative movement
                                initialControllerPosition = model.anchor.transform.position;
                                initialControllerRotation = model.anchor.transform.rotation;
                                initialGripperPosition = spot.GetGripperPos().position;
                                initialGripperRotation = spot.GetGripperPos().rotation;
                                isRelativeModeActive = true;
                            }
                        }

                        if (isRelativeModeActive)
                        {
                            // Apply relative movement
                            Vector3 deltaPos = model.anchor.transform.position - initialControllerPosition;
                            Quaternion deltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

                            Vector3 newPos = initialGripperPosition + deltaPos;
                            Quaternion newRot = deltaRot * initialGripperRotation;

                            spot.SetGripperWorldPose(newPos, newRot);
                        }
                    }
                    else
                    {
                        // Stop relative movement if trigger is released or any joystick is used
                        isRelativeModeActive = false;
                    }
                    break;
            }

            // --- UI Labels ---
            model.joystickLabel = thumbstickPressed ? "Rotate" : "Move";
            if (canControlArm)
            {
                model.indexLabel = triggerHeld ? (isRelativeModeActive ? "Controlling Arm" : "") : "Hold: Control Arm";
            }
            else
            {
                model.indexLabel = "Error: Anchor not assigned";
            }
            model.gripLabel = gripperOpen ? "Close Gripper" : "Open Gripper";
            model.byLabel = armCamera ? "Arm Cam: On" : "Arm Cam: Off";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Arm6AxisMode: {e.Message}\n{e.StackTrace}");
            model.indexLabel = "Error";
        }
    }

    public override string GetName() => "Arm (6 Axis)";
    public override int ModeIndex => 2;
    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => armCamera;

    public override void AssignDefaultLabels(ControllerModel exampleModel)
    {
        exampleModel.joystickLabel = "Move";
        exampleModel.indexLabel = "Hold: Control Arm";
        exampleModel.gripLabel = "Toggle Gripper";
        exampleModel.byLabel = "Toggle Arm Cam";
    }
}
