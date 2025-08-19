using UnityEngine;

public class UITabOption : UIOption
{
    public string name;
    public SuperMode superMode;
        
    public override string GetName()
    {
        return name;
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
