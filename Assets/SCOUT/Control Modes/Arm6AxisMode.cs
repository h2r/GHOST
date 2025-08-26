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
    public ArmControlMode armControlMode = ArmControlMode.Absolute;

    [Header("Locomotion")]
    public GameObject cameraRig;
    public Transform headTransform;
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 120f;

    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    private ScoutModeManager modeManager;

    private void Awake()
    {
        modeManager = ScoutModeManager.Instance;
    }

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        // --- Input ---
        bool handDown = OVRInput.GetDown(model.gripButton);
        bool triggerHeld = OVRInput.Get(model.indexButton);

        // Toggle gripper on trigger press
        if (handDown)
            spot.SetGripperOpen(!spot.GetGripperOpen());

        bool gripperOpen = spot.GetGripperOpen();

        // --- VR Navigation (Locomotion & Rotation) ---
        Vector2 thumbstick = OVRInput.Get(model.joystick);
        bool thumbstickPressed = OVRInput.Get(model.joystickButton);

        if (thumbstickPressed)
        {
            // VR Rotation
            if (cameraRig != null && headTransform != null && Mathf.Abs(thumbstick.x) > 0.1f)
            {
                Vector3 rotationAxis = Vector3.up;
                Vector3 rotationCenter = headTransform.position;
                float angle = rotationSpeed * thumbstick.x * Time.deltaTime;
                cameraRig.transform.RotateAround(rotationCenter, rotationAxis, angle);
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
                cameraRig.transform.position += horizontalMove * moveSpeed * Time.deltaTime;
            }
        }

        // --- Arm Positioning Logic ---
        
        // Check for joystick use on EITHER controller
        bool isOwnJoystickInUse = thumbstick != Vector2.zero || thumbstickPressed;
        bool isOtherJoystickInUse = false;
        OneControllerMode otherMode = (this == modeManager.singleDrive.leftControl) ? modeManager.singleDrive.rightControl : modeManager.singleDrive.leftControl;
        if (otherMode is LocomotionJoystickMode locoMode)
        {
            isOtherJoystickInUse = locoMode.IsJoystickInUse;
        }
        bool anyJoystickInUse = isOwnJoystickInUse || isOtherJoystickInUse;

        switch (armControlMode)
        {
            case ArmControlMode.Absolute:
                if (triggerHeld && !anyJoystickInUse)
                {
                    spot.SetGripperPos(model.anchor.transform);
                }
                break;

            case ArmControlMode.Relative:
                if (triggerHeld && !anyJoystickInUse)
                {
                    if (!isRelativeModeActive)
                    {
                        // Start of a new relative movement
                        initialControllerPosition = model.anchor.transform.position;
                        initialControllerRotation = model.anchor.transform.rotation;
                        initialGripperPosition = spot.GetGripperPos().position;
                        initialGripperRotation = spot.GetGripperPos().rotation;
                        isRelativeModeActive = true;
                    }

                    // Apply relative movement
                    Vector3 deltaPos = model.anchor.transform.position - initialControllerPosition;
                    Quaternion deltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

                    Vector3 newPos = initialGripperPosition + deltaPos;
                    Quaternion newRot = deltaRot * initialGripperRotation;

                    spot.SetGripperWorldPose(newPos, newRot);
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
        model.indexLabel = triggerHeld ? (isRelativeModeActive ? "Controlling Arm" : "") : "Hold: Control Arm";
        model.gripLabel = gripperOpen ? "Close Gripper" : "Open Gripper";
    }

    public override string GetName() => "Arm (6 Axis)";
    public override int ModeIndex => 2;
    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;

    public override void AssignDefaultLabels(ControllerModel exampleModel)
    {
        exampleModel.joystickLabel = "Move";
        exampleModel.indexLabel = "Hold: Control Arm";
        exampleModel.gripLabel = "Toggle Gripper";
    }
}