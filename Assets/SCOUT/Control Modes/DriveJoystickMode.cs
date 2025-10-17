using UnityEngine;

public class DriveJoystickMode : OneControllerMode
{
    public GameObject ViewOptionConfigurer;

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        var doRotate = OVRInput.Get(model.indexButton);
        var doBodyHeightAdjust = OVRInput.Get(model.gripButton);
        var joystick = OVRInput.Get(model.joystick);

        if (doRotate)
        {
            model.joystickLabel = "Rotate";
            if (Mathf.Abs(joystick.x) > 0.1)
                spot.Rotate(joystick.x * 0.5f);
        }
        else if (doBodyHeightAdjust)
        {
            model.joystickLabel = "Adjust Body Height";
            if (Mathf.Abs(joystick.y) > 0.1)
                spot.AdjustHeight(joystick.y * 0.02f);
        }
        else
        {
            model.joystickLabel = "Drive";
            if (joystick.magnitude > 0.1)
                spot.Drive(joystick * 0.5f);
        }

        if (OVRInput.GetDown(model.axButton))
            ViewOptionConfigurer.GetComponent<PointCloudCycler>().CyclePointClouds();
        if (OVRInput.GetDown(model.byButton))
            ViewOptionConfigurer.GetComponent<PositionPresetCycler>().CyclePresets();

        // if (OVRInput.Get(model.byButton))
        //     spot.AdjustHeight(0.02f);

        // if (OVRInput.Get(model.axButton))
        //     spot.AdjustHeight(-0.02f);

        model.indexLabel = !doRotate ? "Hold: Rotate" : "";
        model.gripLabel = !doBodyHeightAdjust ? "Hold: Adjust Body Height" : "";
        model.axLabel = "Cycle PointClouds";
        model.byLabel = "Cycle Views";
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