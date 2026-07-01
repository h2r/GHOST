using UnityEngine;

public class ReadyArmsButton : UIOption
{
    public SpotMode spot;
    public string spot_name;
    public override void DoAction(ScoutModeManager modeManager)
    {
        //spot.StowArm();
    }
    public override string GetName()
    {
        return $"Ready {spot_name} Arm";
    }
    
    public override Color GetSelectedColor()
    {
        return Color.red;
    }
}
