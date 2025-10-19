using UnityEngine;

public abstract class NamedOption : MonoBehaviour
{
    public abstract string GetName();
    public abstract Color GetSelectedColor();

    public virtual void OnModeExit() { }
}
