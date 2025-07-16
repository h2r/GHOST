using System;
using UnityEngine;

// One controller mode
public class Arm6AxisMode : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        spot.SetGripperPos(model.anchor.transform);
    }

    public override string GetName()
    {
        return "Arm (6 Axis)";
    }
}