using UnityEngine;

public class DriveJoystickMode : OneControllerMode
{
    public PositionPresetController positionPresetController;

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        var doRotate = OVRInput.Get(model.indexButton);
        var joystick = OVRInput.Get(model.joystick);

        if (doRotate)
        {
            model.joystickLabel = "Rotate";
            if (Mathf.Abs(joystick.x) > 0.1)
                spot.Rotate(joystick.x * 0.5f);
        }
        else
        {
            model.joystickLabel = "Drive";
            if (joystick.magnitude > 0.1)
                spot.Drive(joystick * 0.5f);
        }

        if (OVRInput.GetDown(model.gripButton))
            positionPresetController.CyclePresets();

        if (OVRInput.Get(model.byButton))
            spot.AdjustHeight(0.02f);

        if (OVRInput.Get(model.axButton))
            spot.AdjustHeight(-0.02f);

        model.indexLabel = !doRotate ? "Hold: Rotate" : "";
        model.gripLabel = "Cycle Views";
        model.axLabel = "Lower Body";
        model.byLabel = "Raise Body";
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
        exampleModel.indexLabel = "Hold: Rotate";
        exampleModel.gripLabel = "Cycle Views";
        exampleModel.axLabel = "Lower Body";
        exampleModel.byLabel = "Raise Body";
    }
}