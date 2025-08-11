using UnityEngine;

public class SwapModality : UIOption
{
    public override void DoAction(ScoutModeManager modeManager)
    {
        if (modeManager.activeSuperMode == SuperMode.DualDrive)
            modeManager.activeSuperMode = SuperMode.SingleDrive;
        else
            modeManager.activeSuperMode = SuperMode.DualDrive;
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