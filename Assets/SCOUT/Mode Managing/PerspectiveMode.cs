using UnityEngine;

// Abstract base class for perspective modes like "Cloud" or "Arm Camera"
public abstract class PerspectiveMode : NamedOption
{
    // Called when the mode is selected
    public virtual void PerspectiveStart() { }

    // Called when switching away from this perspective mode
    public virtual void PerspectiveEnd() { }

    // Display name shown in the UI
    public abstract override string GetName();

    public override Color GetSelectedColor()
    {
        return Color.green;
    }
}
