using UnityEngine;

public class RecordAction : UIOption
{
    public SpotMode spot;
    public string spot_name = "Spot 1"; //may or not need these; grabbed from StowArmsButton

    public override void DoAction(ScoutModeManager modeManager)
    {
       
    }

    public override string GetName()
    {
        return $"Record Action";
    }

    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}
