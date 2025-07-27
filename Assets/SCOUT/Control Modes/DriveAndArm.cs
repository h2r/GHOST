using System;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.InputSystem;

public class DriveAndArm : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        bool isLeft = model.isLeft;

        // Input mappings
        var gripButton = isLeft ? OVRInput.Button.PrimaryHandTrigger : OVRInput.Button.SecondaryHandTrigger;
        var indexTrigger = isLeft ? OVRInput.Button.PrimaryIndexTrigger : OVRInput.Button.SecondaryIndexTrigger;
        var joystickAxis = isLeft ? OVRInput.Axis2D.PrimaryThumbstick : OVRInput.Axis2D.SecondaryThumbstick;

        // Input states
        bool isGripHeld = OVRInput.Get(gripButton);
        bool gripPressed = OVRInput.GetDown(gripButton);
        bool isIndexHeld = OVRInput.Get(indexTrigger);
        Vector2 joystick = OVRInput.Get(joystickAxis);

        // UI labels
        string thumbstickLabel;
        string triggerLabel;
        string gripLabel;

        if (isIndexHeld)
        {
            // === Arm Mode ===
            spot.SetGripperPos(model.anchor.transform);

            if (gripPressed)
                spot.SetGripperOpen(!spot.GetGripperOpen());

            bool isGripperOpen = spot.GetGripperOpen();

            thumbstickLabel = "Arm Mode";
            triggerLabel = "Release to Drive";
            gripLabel = isGripperOpen ? "Hold to Close Gripper" : "Hold to Open Gripper";
        }
        else
        {
            // === Drive / Rotate Mode ===
            if (isGripHeld && Mathf.Abs(joystick.x) > 0.1f)
            {
                spot.Rotate(joystick.x * 0.5f);
            }
            else if (!isGripHeld && joystick.magnitude > 0.1f)
            {
                spot.Drive(joystick * 0.5f);
            }

            thumbstickLabel = isGripHeld ? "Rotate" : "Drive";
            gripLabel = "Hold to Rotate";
            triggerLabel = isGripHeld ? "Hold to Control Arm (Release Grip First)" : "Control Arm";
        }

        // Apply label order: "", "", "", thumbstick, trigger, grip
        model.SetLabels(new[] {
            "",
            "",
            "",
            thumbstickLabel,
            triggerLabel,
            gripLabel
        });
    }

    public override string GetName()
    {
        return "Drive and Arm (Joystick)";
    }
}
