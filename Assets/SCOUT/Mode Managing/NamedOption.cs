using UnityEngine;

public abstract class NamedOption : MonoBehaviour
{
   // public virtual bool isToggle => false;
    public abstract string GetName();
    public abstract Color GetSelectedColor();

    public virtual void OnModeExit() { }
}
