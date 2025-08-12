using UnityEngine;

public class StowArmsButton : UIOption
{
    public SpotMode spotOne, spotTwo;

    public override void DoAction(ScoutModeManager modeManager)
    {
        spotOne.StowArm();
        spotTwo.StowArm();
    }

    public override string GetName()
    {
        return "Stow Arms";
    }

    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}