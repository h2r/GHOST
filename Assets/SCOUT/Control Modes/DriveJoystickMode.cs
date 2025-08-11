using System;
using System.Diagnostics.Contracts;
using UnityEngine;

// One controller mode
public class DriveJoystickMode : OneControllerMode
{
    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        bool doRotate;
        if (model.isLeft)
            doRotate = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        else
            doRotate = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);

        model.joystickLabel = doRotate ? "Rotate" : "Drive";
        model.indexLabel = doRotate ? "" : "Hold: Rotate";

        Vector2 joystick;
        if (model.isLeft)
            joystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        else
            joystick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (doRotate && Mathf.Abs(joystick.x) > 0.1)
            spot.Rotate(joystick.x * 0.5f);
        else if (!doRotate && joystick.magnitude > 0.1)
            spot.Drive(joystick * 0.5f);
    }

    public override string GetName()
    {
        return "Drive"; // "Drive (Joystick)"
    }

    public override int ModeIndex => 1;
    public override bool ControlsSpot => true;

    public override void AssignDefaultLabels(ControllerModel exampleModel)
    {
        exampleModel.joystickLabel = "Drive";
        exampleModel.indexLabel = "Rotate";
    }
}