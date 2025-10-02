using UnityEngine;

public class StowArmsButton : UIOption
{
    public SpotMode spot;
    public string spot_name = "Spot 1";

    public override void DoAction(ScoutModeManager modeManager)
    {
        spot.StowArm();
    }

    public override string GetName()
    {
        return $"Stow {spot_name} Arm";
    }

    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}