// using System;
// using UnityEngine;

// // One controller mode
// public class Arm6AxisMode : NewControlMode
// {
//     public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
//     {
//         model.SetLabels(new[] {
//             "",
//             "",
//             "",
//             "",
//             spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper",
//             "",
//             ""
//         });

//         spot.SetGripperPos(model.anchor.transform);

//         bool indexTrigger;
//         if (model.isLeft)
//             indexTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
//         else
//             indexTrigger = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
//         if (indexTrigger)
//             spot.SetGripperOpen(!spot.GetGripperOpen());
//     }

//     public override string GetName()
//     {
//         return "Arm (6 Axis)";
//     }
// }




using System;
using UnityEngine;
using RosSharp.RosBridgeClient;

/// <summary>
/// Arm control mode for 6-axis movement with one controller.
/// Supports Absolute and Relative control modes.
/// Press B (right) or Y (left) to stow the arm.
/// </summary>
public class Arm6AxisMode : NewControlMode
{
    public enum ArmControlMode
    {
        Absolute,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.Absolute;

    // For relative mode anchoring
    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool wasInArmModeLastFrame = false;

    // Reference to the StowArm publisher (assign in Inspector or find at runtime)
    public StowArm stowArmPublisher;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        // Set UI labels
        model.SetLabels(new[] {
            "",
            "",
            "",
            "",
            spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper",
            "",
            "B/Y: Stow"
        });

        // --- Stow Arm Button ---
        bool stowPressed = OVRInput.GetDown(OVRInput.Button.Two); // B (right) or Y (left)
        if (stowPressed)
        {
            if (stowArmPublisher == null)
            {
                // Try to find the publisher if not assigned
                stowArmPublisher = GameObject.FindFirstObjectByType<StowArm>();
            }
            if (stowArmPublisher != null)
            {
                stowArmPublisher.Stow();
            }
            else
            {
                Debug.LogWarning("StowArm publisher not found in scene!");
            }
        }

        // === Arm Control ===
        if (armControlMode == ArmControlMode.Absolute)
        {
            // Snap gripper to controller
            spot.SetGripperPos(model.anchor.transform);
        }
        else // Relative mode
        {
            // Only anchor when entering arm mode from a non-arm state
            if (!wasInArmModeLastFrame)
            {
                initialControllerPosition = model.anchor.transform.position;
                initialControllerRotation = model.anchor.transform.rotation;
                initialGripperPosition = spot.GetGripperPos().position;
                initialGripperRotation = spot.GetGripperPos().rotation;
            }

            // Compute controller delta from anchor
            Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
            Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

            // Apply delta to initial gripper pose
            Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
            Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

            spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
        }

        // Gripper open/close logic (index trigger press)
        bool indexTrigger;
        if (model.isLeft)
            indexTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        else
            indexTrigger = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);

        if (indexTrigger)
            spot.SetGripperOpen(!spot.GetGripperOpen());

        // Update last frame state
        wasInArmModeLastFrame = true;
    }

    public override string GetName()
    {
        return "Arm (6 Axis)";
    }
}