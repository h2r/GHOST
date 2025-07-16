using System;
using UnityEngine;

// One controller mode
public class DriveJoystickMode : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, GameObject controller, bool isLeft, Action<string[]> SetLabels)
    {
        SetLabels(new[] {
            "",
            "",
            "",
            "Drive",
            "",
            ""
        });

        Vector2 joystick;
        if (isLeft)
            joystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        else
            joystick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (joystick.magnitude > 0.1)
            spot.Drive(joystick);
    }

    public override string GetName()
    {
        return "Drive (Joystick)";
    }
}