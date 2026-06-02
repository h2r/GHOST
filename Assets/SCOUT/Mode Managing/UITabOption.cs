using UnityEngine;
using UnityEngine.Serialization;

public class UITabOption : UIOption
{
    [FormerlySerializedAs("name")]
    public string displayName;
    public SuperMode superMode;
        
    public override string GetName()
    {
        return displayName;
    }

    public override Color GetSelectedColor()
    {
        return Color.magenta;
    }

    public override void DoAction(ScoutModeManager modeManager)
    {
        modeManager.SetUISuperMode(superMode);
    }
}
