using System;
using UnityEngine;

public abstract class NewControlMode : NamedMode
{
    public abstract void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel otherModel);
}