using System;
using UnityEngine;

public class Arm6AxisMode : NewControlMode
{
    public enum ArmControlMode
    {
        Absolute,  // This mode will behave as it did before (gripper directly follows controller)
        Relative   // This mode will move the gripper relative to the controller's movement
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.Absolute;  // Default mode is Absolute

    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        string thumbstickLabel = "";
        string triggerLabel = "";
        string gripLabel = "";

        model.SetLabels(new[] {
            "",
            "",
            "",
            thumbstickLabel,
            triggerLabel,
            gripLabel,
            ""
        });

        // Toggle gripper open/close based on the trigger
        bool indexTrigger;
        if (model.isLeft)
            indexTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        else
            indexTrigger = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);

        if (indexTrigger)
            spot.SetGripperOpen(!spot.GetGripperOpen());

        // Handle the Arm Control Modes
        switch (armControlMode)
        {
            case ArmControlMode.Absolute:
                // In absolute mode, the gripper follows the controller directly
                spot.SetGripperPos(model.anchor.transform);
                
                // Set UI labels for absolute mode
                thumbstickLabel = "Arm Mode";  // Just display "Arm Mode" for the thumbstick
                break;

            case ArmControlMode.Relative:
                // Only enter relative mode when the index trigger is held
                bool isIndexHeld = OVRInput.Get(model.isLeft ? OVRInput.Button.PrimaryIndexTrigger : OVRInput.Button.SecondaryIndexTrigger);

                if (isIndexHeld)
                {
                    if (!isRelativeModeActive)
                    {
                        // Save the initial state when relative mode is first activated
                        initialControllerPosition = model.anchor.transform.position;
                        initialControllerRotation = model.anchor.transform.rotation;
                        initialGripperPosition = spot.GetGripperPos().position;
                        initialGripperRotation = spot.GetGripperPos().rotation;
                        isRelativeModeActive = true;
                    }

                    // Calculate the difference (delta) between the controller's current and initial position
                    Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
                    Quaternion controllerDeltaRotation = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

                    // Update the gripper's position and rotation based on the controller's movement
                    Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
                    Quaternion newGripperRotation = controllerDeltaRotation * initialGripperRotation;

                    // Apply the relative movement to the gripper
                    spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);

                    // Set UI labels for relative mode
                    triggerLabel = "Hold: Move Arm";  // Trigger to hold the arm's relative movement
                    gripLabel = spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper";  // Toggle gripper state
                }
                else
                {
                    // Reset the relative mode when the trigger is released
                    isRelativeModeActive = false;

                    // Set UI labels for relative mode when trigger is not held
                    triggerLabel = "Hold: Move Arm";
                    gripLabel = "Open/Close Gripper";
                }
                break;
        }

        model.SetLabels(new[] {
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




// Simplest version --
// using System;
// using UnityEngine;

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


//     public override string GetName() => "Arm (6 Axis)";
//     public override int ModeIndex => 2;
//     public override bool ControlsSpot => true;
//     public override bool RequiresArmCamera => true;

// }
