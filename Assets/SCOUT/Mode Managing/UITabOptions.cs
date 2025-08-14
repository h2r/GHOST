using UnityEngine;

public class UITabOptions : UIOption
{
    public string name;
    public SuperMode superMode; 
    public override string GetName()
    {
        return name;
    }

    public override Color GetSelectedColor()
    {
        return Color.magenta; // Darker highlight color
    }
    public override void DoAction(ScoutModeManager modeManager)
    {
        modeManager.setActiveSuperMode(superMode);
        Debug.Log("MODE CHANGED TO " + name);
    }
}
