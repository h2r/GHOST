using System;
using UnityEngine;

// One controller mode
public class LocomotionJoystickMode : NewControlMode
{
    public GameObject cameraRig;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        model.SetLabels(new[] {
            "",
            "",
            "",
            "Locomote",
            "",
            ""
        });

        Vector2 joystick;
        if (model.isLeft)
            joystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        else
            joystick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (joystick.magnitude > 0.1)
            cameraRig.transform.position += new Vector3(joystick.x, 0, joystick.y) * 0.05f;
    }

    public override string GetName()
    {
        return "Locomotion (Joystick)";
    }
}