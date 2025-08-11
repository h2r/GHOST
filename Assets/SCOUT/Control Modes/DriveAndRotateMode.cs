using System;
using UnityEngine;

// Two controller mode
public class DriveAndRotateMode : TwoControllerMode
{
    public override void ControlUpdate(SpotMode spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        leftModel.joystickLabel = "Drive";
        rightModel.joystickLabel = "Rotate";

        var leftJoystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        var rightJoystick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (leftJoystick.magnitude > 0.1)
            spot.Drive(leftJoystick);

        if (Mathf.Abs(rightJoystick.x) > 0.1)
            spot.Rotate(rightJoystick.x);
    }

    public override string GetName()
    {
        return "Drive & Rotate";
    }

    public override int ModeIndex => 4;
    public override bool ControlsSpot => true;

    public override void AssignDefaultLabels(ControllerModel leftExampleModel, ControllerModel rightExampleModel)
    {
        leftExampleModel.joystickLabel = "Drive";
        rightExampleModel.joystickLabel = "Rotate";
    }
}