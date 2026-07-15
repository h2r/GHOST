using UnityEngine;

public abstract class NamedOption : MonoBehaviour
{
    public virtual bool IsToggle => false;
    public virtual bool IsActive => false;
    public abstract string GetName();
    public abstract Color GetSelectedColor();

    public virtual void OnModeExit() { }
}
