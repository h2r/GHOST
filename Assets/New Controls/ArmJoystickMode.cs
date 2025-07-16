using UnityEngine;

// Two controller mode
public class ArmJoystickMode : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, GameObject controller, bool isLeft)
    {
        var joystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (joystick.magnitude > 0.1)
        {
            var gripperTf = spot.GetGripperPos();
            gripperTf.position += (Vector3)joystick;
            spot.SetGripperPos(gripperTf);
        }
    }

    public override string GetName()
    {
        return "Arm (Joystick)";
    }
}