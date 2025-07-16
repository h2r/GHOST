using UnityEngine;

public class DriveJoystickMode : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, GameObject controller)
    {
        var joystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (joystick.magnitude > 0.1)
            spot.Drive(joystick);
    }

    public override string GetName()
    {
        return "Drive (Joystick)";
    }
}