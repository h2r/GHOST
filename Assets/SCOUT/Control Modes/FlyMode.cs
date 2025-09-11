using UnityEngine;

public class FlyMode : TwoControllerMode
{
    public LocomotionJoystickMode locomotionJoystickMode;

    public override void ControlUpdate(SpotMode spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        locomotionJoystickMode.ControlUpdate(spot, leftModel);
        locomotionJoystickMode.ControlUpdate(spot, rightModel);
    }

    public override void AssignDefaultLabels(ControllerModel leftExampleModel, ControllerModel rightExampleModel)
    {
        locomotionJoystickMode.AssignDefaultLabels(leftExampleModel);
        locomotionJoystickMode.AssignDefaultLabels(rightExampleModel);
    }

    public override string GetName()
    {
        return "Fly";
    }

    public override int ModeIndex => 3;
    public override bool ControlsSpot => false;
}