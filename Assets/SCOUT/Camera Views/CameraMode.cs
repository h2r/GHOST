using UnityEngine;

public abstract class CameraMode : NamedOption
{
    public GameObject controlledGameObject; // ADDED: Reference to the GameObject this CameraMode controls

    public override Color GetSelectedColor()
    {
        return Color.green;
    }
}