using UnityEngine;

public class StowArmsButton : UIOption
{
    public override void DoAction(ScoutModeManager modeManager)
    {
        print("stow now");
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