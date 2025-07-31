// using System;
// using UnityEngine;

// public class DriveAndArm : NewControlMode
// {
//     public enum ArmControlMode
//     {
//         AbsolutePos,        // Dummy gripper follows controller position (current Absolute)
//         AbsoluteController, // Controller moves to dummy gripper position
//         Relative            // Relative movement as before
//     }

//     [Header("Arm Control Mode")]
//     public ArmControlMode armControlMode = ArmControlMode.AbsolutePos;

//     private Vector3 initialControllerPosition;
//     private Quaternion initialControllerRotation;
//     private Vector3 initialGripperPosition;
//     private Quaternion initialGripperRotation;
//     private bool isRelativeModeActive = false;

//     public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
//     {
//         bool isLeft = model.isLeft;

//         var indexTrigger = isLeft ? OVRInput.Button.PrimaryIndexTrigger : OVRInput.Button.SecondaryIndexTrigger;
//         var gripButton = isLeft ? OVRInput.Button.PrimaryHandTrigger : OVRInput.Button.SecondaryHandTrigger;
//         var joystickAxis = isLeft ? OVRInput.Axis2D.PrimaryThumbstick : OVRInput.Axis2D.SecondaryThumbstick;
//         var stowButton = isLeft ? OVRInput.Button.Two : OVRInput.Button.Four;  // Y for left, B for right

//         bool isIndexHeld = OVRInput.Get(indexTrigger);
//         bool indexPressed = OVRInput.GetDown(indexTrigger);
//         bool isGripHeld = OVRInput.Get(gripButton);
//         bool gripPressed = OVRInput.GetDown(gripButton);
//         bool stowPressed = OVRInput.GetDown(stowButton);
//         Vector2 joystick = OVRInput.Get(joystickAxis);

//         // Stow arm if B/Y pressed
//         if (stowPressed)
//         {
//             spot.StowArm();
//         }

//         string thumbstickLabel = "";
//         string triggerLabel = "";
//         string gripLabel = "";
//         string stowLabel = stowPressed ? "Stowing Arm..." : $"Press {(isLeft ? "Y" : "B")} to Stow Arm";

//         if (isIndexHeld)
//         {
//             // === Arm Mode ===

//             switch (armControlMode)
//             {
//                 case ArmControlMode.AbsolutePos:
//                     // Move dummy gripper to controller position (existing Absolute)
//                     spot.SetGripperPos(model.anchor.transform);
//                     isRelativeModeActive = false;
//                     break;

//                 case ArmControlMode.AbsoluteController:
//                     // Move controller to dummy gripper position and rotation
//                     model.anchor.transform.SetPositionAndRotation(
//                         spot.GetGripperPos().position,
//                         spot.GetGripperPos().rotation);
//                     isRelativeModeActive = false;
//                     break;

//                 case ArmControlMode.Relative:
//                     if (!isRelativeModeActive)
//                     {
//                         initialControllerPosition = model.anchor.transform.position;
//                         initialControllerRotation = model.anchor.transform.rotation;
//                         initialGripperPosition = spot.GetGripperPos().position;
//                         initialGripperRotation = spot.GetGripperPos().rotation;
//                         isRelativeModeActive = true;
//                     }

//                     Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
//                     Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

//                     Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
//                     Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

//                     spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
//                     break;
//             }

//             // Grip button toggles gripper open state
//             if (gripPressed)
//                 spot.SetGripperOpen(!spot.GetGripperOpen());

//             bool isGripperOpen = spot.GetGripperOpen();

//             thumbstickLabel = "Arm Mode";
//             triggerLabel = "Release to Drive";
//             gripLabel = isGripperOpen ? "Hold to Close Gripper" : "Hold to Open Gripper";
//         }
//         else
//         {
//             // === Drive / Rotate Mode ===
//             isRelativeModeActive = false;

//             if (isGripHeld && Mathf.Abs(joystick.x) > 0.1f)
//                 spot.Rotate(joystick.x * 0.5f);
//             else if (!isGripHeld && joystick.magnitude > 0.1f)
//                 spot.Drive(joystick * 0.5f);

//             thumbstickLabel = isGripHeld ? "Rotate" : "Drive";
//             gripLabel = "Hold to Rotate";
//             triggerLabel = isGripHeld ? "Hold to Control Arm (Release Grip First)" : "Control Arm";
//         }

//         model.SetLabels(new[] {
//             "",
//             "",
//             "",
//             thumbstickLabel,
//             triggerLabel,
//             gripLabel,
//             stowLabel
//         });
//     }

//     public override string GetName()
//     {
//         return "Dynamic Control";
//     }
    
//     public override int ModeIndex => 0;
// }




using System;
using UnityEngine;

public class DriveAndArm : NewControlMode
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

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        bool isLeft = model.isLeft;

        var indexTrigger = isLeft ? OVRInput.Button.PrimaryIndexTrigger : OVRInput.Button.SecondaryIndexTrigger;
        var gripButton = isLeft ? OVRInput.Button.PrimaryHandTrigger : OVRInput.Button.SecondaryHandTrigger;
        var joystickAxis = isLeft ? OVRInput.Axis2D.PrimaryThumbstick : OVRInput.Axis2D.SecondaryThumbstick;
        var stowButton = isLeft ? OVRInput.Button.Two : OVRInput.Button.Four;  // Y (left) or B (right)

        bool isIndexHeld = OVRInput.Get(indexTrigger);
        bool indexPressed = OVRInput.GetDown(indexTrigger);
        bool isGripHeld = OVRInput.Get(gripButton);
        bool gripPressed = OVRInput.GetDown(gripButton);
        bool stowPressed = OVRInput.GetDown(stowButton);
        Vector2 joystick = OVRInput.Get(joystickAxis);

        // Stow arm on Y/B press
        if (stowPressed)
            spot.StowArm();

        // UI Labels
        string thumbstickLabel = "";
        string triggerLabel = "";
        string gripLabel = "";
        string stowLabel = stowPressed ? "Stowing Arm..." : $"Press {(isLeft ? "Y" : "B")} to Stow Arm";

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
                    spot.SetGripperPos(model.anchor.transform);
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
            triggerLabel = "Release to Drive";
            gripLabel = isGripperOpen ? "Hold to Close Gripper" : "Hold to Open Gripper";
        }
        else
        {
            // === Drive Mode ===
            isRelativeModeActive = false;

            if (joystick.magnitude > 0.1f)
                spot.Drive(joystick * 0.5f);

            thumbstickLabel = "Drive Spot";
            triggerLabel = "Hold: Control Arm";
            gripLabel = "Hold: Rotate";
        }

        model.SetLabels(new[]
        {
            "", "", "", // button labels not used
            thumbstickLabel,
            triggerLabel,
            gripLabel,
            stowLabel
        });
    }

    public override string GetName() => "Dynamic Control";

    public override int ModeIndex => 0;
}
