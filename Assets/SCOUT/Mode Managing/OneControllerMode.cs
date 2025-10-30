using UnityEngine;

public abstract class OneControllerMode : NamedOption
{
    public abstract void ControlUpdate(SpotController spot, ControllerModel model);

    public abstract void AssignDefaultLabels(ControllerModel exampleModel);

    public override Color GetSelectedColor()
    {
        return Color.green;
    }

    public abstract int ModeIndex { get; }
    public abstract bool ControlsSpot { get; }
    public virtual bool RequiresArmCamera => false;
}