using System;
using UnityEngine;

public class Arm6AxisMode : NewControlMode
{
    public enum ArmControlMode
    {
        Absolute,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.Absolute;

    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        // --- Input ---
        bool handDown = model.isLeft
            ? OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger)
            : OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger);

        bool triggerHeld = model.isLeft
            ? OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)
            : OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);

        // Toggle gripper on trigger press (works regardless of mode or held state)
        if (handDown)
            spot.SetGripperOpen(!spot.GetGripperOpen());

        bool gripperOpen = spot.GetGripperOpen();

        // --- Arm Positioning Logic ---
        switch (armControlMode)
        {
            case ArmControlMode.Absolute:
                spot.SetGripperPos(model.anchor.transform);
                break;

            case ArmControlMode.Relative:
                if (triggerHeld)
                {
                    if (!isRelativeModeActive)
                    {
                        initialControllerPosition = model.anchor.transform.position;
                        initialControllerRotation = model.anchor.transform.rotation;
                        initialGripperPosition = spot.GetGripperPos().position;
                        initialGripperRotation = spot.GetGripperPos().rotation;
                        isRelativeModeActive = true;
                    }

                    Vector3 deltaPos = model.anchor.transform.position - initialControllerPosition;
                    Quaternion deltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

                    Vector3 newPos = initialGripperPosition + deltaPos;
                    Quaternion newRot = deltaRot * initialGripperRotation;

                    spot.SetGripperWorldPose(newPos, newRot);
                }
                else
                {
                    isRelativeModeActive = false;
                }
                break;
        }

        // --- UI Labels ---
        string thumbstickLabel = "Arm Mode";
        string triggerLabel = triggerHeld ? "" : "Hold: Control Arm";
        string gripLabel = gripperOpen ? "Close Gripper" : "Open Gripper";

        model.SetLabels(new[]
        {
            "", "", "",
            thumbstickLabel,
            triggerLabel,
            gripLabel,
            ""
        });
    }

    public override string GetName() => "Arm (6 Axis)";
    public override int ModeIndex => 2;
    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;
}
