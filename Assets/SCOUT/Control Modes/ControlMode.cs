using System;
using UnityEngine;

public abstract class NewControlMode : NamedMode
{
    public abstract void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel otherModel);

    // identify the control mode type for UI mapping
    public abstract int ModeIndex { get; }
    public abstract bool ControlsSpot { get; }
    public virtual bool RequiresArmCamera => false;

}