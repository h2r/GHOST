using System;
using UnityEngine;

// One controller mode
public class DriveJoystickMode : NewControlMode
{
    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        bool doRotate;
        if (model.isLeft)
            doRotate = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        else
            doRotate = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);

        model.SetLabels(new[] {
            "",
            "",
            "",
            doRotate ? "Rotate" : "Drive",
            "Do Rotate",
            ""
        });

        Vector2 joystick;
        if (model.isLeft)
            joystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        else
            joystick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (doRotate && Mathf.Abs(joystick.x) > 0.1)
            spot.Rotate(joystick.x);
        else if (!doRotate && joystick.magnitude > 0.1)
            spot.Drive(joystick);
    }

    public override string GetName()
    {
        return "Drive (Joystick)";
    }
}