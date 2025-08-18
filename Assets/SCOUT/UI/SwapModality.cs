using UnityEngine;

public class SwapModality : UIOption
{
    public override void DoAction(ScoutModeManager modeManager)
    {
        if (modeManager.uiSuperMode == SuperMode.DualDrive)
            modeManager.uiSuperMode = SuperMode.SingleDrive;
        else
            modeManager.uiSuperMode = SuperMode.DualDrive;
    }

    public override string GetName()
    {
        return "Swap Modality";
    }

    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}