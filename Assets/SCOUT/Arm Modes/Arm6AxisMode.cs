// using System;
// using UnityEngine;
// using RosSharp.RosBridgeClient;

// public class Arm6AxisMode : NewControlMode
// {
//     public enum ArmControlMode
//     {
//         Absolute,
//         Relative
//     }

//     [Header("Arm Control Mode")]
//     public ArmControlMode armControlMode = ArmControlMode.Absolute;

//     private Vector3 initialControllerPosition;
//     private Quaternion initialControllerRotation;
//     private Vector3 initialGripperPosition;
//     private Quaternion initialGripperRotation;
//     private bool wasInArmModeLastFrame = false;

//     public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
//     {
//         model.SetLabels(new[] {
//             "",
//             "",
//             "",
//             "",
//             spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper",
//             "",
//             "B/Y: Stow"
//         });

//         // Detect B (Button.Two) or Y (Button.Three)
//         bool stowPressed = OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Three);
//         if (stowPressed)
//         {
//             spot.StowArm();
//         }

//         if (armControlMode == ArmControlMode.Absolute)
//         {
//             // Snap gripper to controller position and rotation
//             spot.SetGripperPos(model.anchor.transform);
//         }
//         else
//         {
//             if (!wasInArmModeLastFrame)
//             {
//                 initialControllerPosition = model.anchor.transform.position;
//                 initialControllerRotation = model.anchor.transform.rotation;
//                 initialGripperPosition = spot.GetGripperPos().position;
//                 initialGripperRotation = spot.GetGripperPos().rotation;
//             }

//             Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
//             Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

//             Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
//             Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

//             spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
//         }

//         bool indexTrigger;
//         if (model.isLeft)
//             indexTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
//         else
//             indexTrigger = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);

//         if (indexTrigger)
//         {
//             spot.SetGripperOpen(!spot.GetGripperOpen());
//         }

//         wasInArmModeLastFrame = true;
//     }

//     public override string GetName()
//     {
//         return "Arm (6 Axis)";
//     }

//     public override int ModeIndex => 2;
// }



using System;
using UnityEngine;
using RosSharp.RosBridgeClient;

public class Arm6AxisMode : NewControlMode
{
    public enum ArmControlMode
    {
        Absolute,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.Absolute;

    [Header("Ready Pose (Optional)")]
    public Transform readyPoseReference;

    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;

    private Vector3 readyPosition;
    private Quaternion readyRotation;

    private bool wasInArmModeLastFrame = false;
    private bool isInitialized = false;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        // NEW: Reset state if we are no longer in this mode
        if (spot.CurrentModeIndex != ModeIndex)
        {
            wasInArmModeLastFrame = false;
            return;
        }

        // Initialize the ready pose once
        if (!isInitialized)
        {
            if (readyPoseReference != null)
            {
                readyPosition = readyPoseReference.position;
                readyRotation = readyPoseReference.rotation;
            }
            else
            {
                Transform gripper = spot.GetGripperPos();
                readyPosition = gripper.position;
                readyRotation = gripper.rotation;
            }

            isInitialized = true;
        }

        // When entering the mode
        if (!wasInArmModeLastFrame)
        {
            // Move to ready pose
            spot.SetGripperWorldPose(readyPosition, readyRotation);

            // For relative mode, reset reference
            initialControllerPosition = model.anchor.transform.position;
            initialControllerRotation = model.anchor.transform.rotation;
            initialGripperPosition = readyPosition;
            initialGripperRotation = readyRotation;
        }

        // UI labels
        model.SetLabels(new[] {
            "",
            "",
            "",
            "",
            spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper",
            "",
            "B/Y: Stow"
        });

        // Stow the arm
        bool stowPressed = OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Three);
        if (stowPressed)
        {
            spot.StowArm();
        }

        // Arm movement
        if (armControlMode == ArmControlMode.Absolute)
        {
            spot.SetGripperPos(model.anchor.transform);
        }
        else
        {
            Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
            Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

            Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
            Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

            spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
        }

        // Toggle gripper open/close
        bool indexTrigger = model.isLeft
            ? OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)
            : OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);

        if (indexTrigger)
        {
            spot.SetGripperOpen(!spot.GetGripperOpen());
        }

        wasInArmModeLastFrame = true;
    }

    public override string GetName()
    {
        return "Arm (6 Axis)";
    }

    public override int ModeIndex => 2;
    public override bool ControlsSpot => true;
}
