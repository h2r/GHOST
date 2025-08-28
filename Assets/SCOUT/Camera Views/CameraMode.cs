using UnityEngine;

public abstract class CameraMode : NamedOption
{
    public GameObject controlledGameObject; // ADDED: Reference to the GameObject this CameraMode controls

    public virtual void ActivateView()
    {
        if (controlledGameObject != null)
            controlledGameObject.SetActive(true);
    }

    public virtual void DeactivateView()
    {
        if (controlledGameObject != null)
            controlledGameObject.SetActive(false);
    }

    public override Color GetSelectedColor()
    {
        return Color.green;
    }
}