using UnityEngine;

public abstract class CameraMode : NamedOption
{
    public override Color GetSelectedColor()
    {
        return Color.green;
    }
}