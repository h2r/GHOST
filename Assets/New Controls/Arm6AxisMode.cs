using System;
using UnityEngine;

// One controller mode
public class Arm6AxisMode : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        model.SetLabels(new[] {
            "",
            "",
            "",
            "",
            spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper",
            "",
            ""
        });

        spot.SetGripperPos(model.anchor.transform);

        bool indexTrigger;
        if (model.isLeft)
            indexTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        else
            indexTrigger = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
        if (indexTrigger)
            spot.SetGripperOpen(!spot.GetGripperOpen());
    }

    public override string GetName()
    {
        return "Arm (6 Axis)";
    }
}