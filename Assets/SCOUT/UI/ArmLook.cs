using UnityEngine;

public class ReadyArm : UIOption
{
    public override void DoAction(ScoutModeManager modeManager)
    {
        
    }
    public override string GetName()
    {
        return "Ready Arm";
    }
    
    public override Color GetSelectedColor()
    {
        return Color.red;
    }
}
