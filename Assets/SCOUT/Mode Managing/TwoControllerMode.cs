using UnityEngine;

public abstract class TwoControllerMode : NamedOption
{
    public abstract void ControlUpdate(SpotMode spot, ControllerModel leftModel, ControllerModel rightModel);

    public abstract void AssignDefaultLabels(ControllerModel leftExampleModel, ControllerModel rightExampleModel);

    public override Color GetSelectedColor()
    {
        return Color.green;
    }

    public abstract int ModeIndex { get; }
    public abstract bool ControlsSpot { get; }
    public virtual bool RequiresArmCamera => false;
}