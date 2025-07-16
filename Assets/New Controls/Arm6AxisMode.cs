using UnityEngine;

public class Arm6AxisMode : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, GameObject controller)
    {
        spot.SetGripperPos(controller.transform);
    }

    public override string GetName()
    {
        return "Arm (6 Axis)";
    }
}