using System;
using UnityEngine;

// Two controller mode
public class ArmJoystickMode : TwoControllerMode
{
    public override void ControlUpdate(SpotMode spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        var joystick = OVRInput.Get(leftModel.joystick);
        if (joystick.magnitude > 0.1)
        {
            var gripperTf = spot.GetGripperPos();
            gripperTf.position += (Vector3)joystick;
            spot.SetGripperTf(gripperTf);
        }
    }

    public override string GetName()
    {
        return "Arm (Joystick)";
    }

    public override int ModeIndex => 5;
    public override bool ControlsSpot => true;

    public override void AssignDefaultLabels(ControllerModel leftExampleModel, ControllerModel rightExampleModel)
    {
        leftExampleModel.joystickLabel = "Move Gripper";
    }
}