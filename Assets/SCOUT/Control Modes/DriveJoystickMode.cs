using UnityEngine;

public class DriveJoystickMode : OneControllerMode
{
    public PositionPresetController positionPresetController;

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        var doRotate = OVRInput.Get(model.gripButton);
        var doHeight = OVRInput.Get(model.indexButton);
        var joystick = OVRInput.Get(model.joystick);

        if (OVRInput.GetDown(model.indexButton))
            spot.SetHeight(0);

        if (doRotate)
        {
            model.joystickLabel = "Rotate";
            if (Mathf.Abs(joystick.x) > 0.1)
                spot.Rotate(joystick.x * 0.5f);
        }
        else if (doHeight)
        {
            model.joystickLabel = "Adjust Height";
            if (Mathf.Abs(joystick.y) > 0.1)
                spot.AdjustHeight(joystick.y * 0.005f);
        }
        else
        {
            model.joystickLabel = "Drive";
            if (joystick.magnitude > 0.1)
                spot.Drive(joystick * 0.5f);
        }

        model.indexLabel = (!doRotate && !doHeight) ? "Hold: Adjust Height" : "";
        model.gripLabel = (!doRotate && !doHeight) ? "Hold: Rotate" : "";

        if (OVRInput.GetDown(model.axButton))
            positionPresetController.CyclePresets();

        model.axLabel = "Cycle Views";
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