using UnityEngine;

public abstract class NewControlMode : NamedMode
{
    public abstract void ControlUpdate(SpotMode spot, GameObject controller, bool isLeft);
}