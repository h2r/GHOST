using System;
using UnityEngine;

public class DriveAndArm : OneControllerMode
{
    public enum ArmControlMode
    {
        AbsolutePos,
        AbsoluteController,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.AbsolutePos;

    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        bool isIndexHeld = OVRInput.Get(model.indexButton);
        bool indexPressed = OVRInput.GetDown(model.indexButton);
        bool isGripHeld = OVRInput.Get(model.gripButton);
        bool gripPressed = OVRInput.GetDown(model.gripButton);
        Vector2 joystick = OVRInput.Get(model.joystick);


        // UI Labels
        string thumbstickLabel = "";
        string triggerLabel = "";
        string gripLabel = "";

        if (isGripHeld && !isIndexHeld)
        {
            // === Rotate Mode ===
            isRelativeModeActive = false;

            if (Mathf.Abs(joystick.x) > 0.1f)
                spot.Rotate(joystick.x * 0.5f);

            thumbstickLabel = "Rotate";
            gripLabel = "Rotating";
            triggerLabel = ""; // Trigger disabled while rotating
        }
        else if (isIndexHeld)
        {
            // === Arm Control Mode ===
            switch (armControlMode)
            {
                case ArmControlMode.AbsolutePos:
                    spot.SetGripperTf(model.anchor.transform);
                    isRelativeModeActive = false;
                    break;

                case ArmControlMode.AbsoluteController:
                    model.anchor.transform.SetPositionAndRotation(
                        spot.GetGripperPos().position,
                        spot.GetGripperPos().rotation);
                    isRelativeModeActive = false;
                    break;

                case ArmControlMode.Relative:
                    if (!isRelativeModeActive)
                    {
                        initialControllerPosition = model.anchor.transform.position;
                        initialControllerRotation = model.anchor.transform.rotation;
                        initialGripperPosition = spot.GetGripperPos().position;
                        initialGripperRotation = spot.GetGripperPos().rotation;
                        isRelativeModeActive = true;
                    }

                    Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
                    Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

                    Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
                    Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

                    spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
                    break;
            }

            // Grip toggles gripper open/closed
            if (gripPressed)
                spot.SetGripperOpen(!spot.GetGripperOpen());

            bool isGripperOpen = spot.GetGripperOpen();

            thumbstickLabel = "Arm Mode";
            triggerLabel = "";
            gripLabel = isGripperOpen ? "Close Gripper" : " Open Gripper";
        }
        else
        {
            // === Drive Mode ===
            isRelativeModeActive = false;

            if (joystick.magnitude > 0.1f)
                spot.Drive(joystick * 0.5f);

            if (OVRInput.Get(model.byButton))
                spot.AdjustHeight(0.005f);

            if (OVRInput.Get(model.axButton))
                spot.AdjustHeight(-0.005f);

            thumbstickLabel = "Drive Spot";
            triggerLabel = "Hold: Control Arm";
            gripLabel = "Hold: Rotate";
            model.axLabel = "Lower Body";
            model.byLabel = "Raise Body";
        }

        model.joystickLabel = thumbstickLabel;
        model.indexLabel = triggerLabel;
        model.gripLabel = gripLabel;
    }

    public override string GetName() => "Dynamic Control";

    public override int ModeIndex => 0;

    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;

    public override void AssignDefaultLabels(ControllerModel exampleModel)
    {
        exampleModel.joystickLabel = "Drive";
        exampleModel.indexLabel = "Hold to Rotate";
        exampleModel.gripLabel = "Hold to Control Arm";
    }
}