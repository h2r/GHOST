using UnityEngine;

// Abstract base class for perspective modes like "Cloud" or "Arm Camera"
public abstract class PerspectiveMode : NamedMode
{
    // Called when the mode is selected
    public virtual void PerspectiveStart() { }

    // Called when switching away from this perspective mode
    public virtual void PerspectiveEnd() { }

    // Display name shown in the UI
    public abstract override string GetName();
}
