using UnityEngine;

public class SwapSpot : UIOption
{
    public override void DoAction(ScoutModeManager modeManager)
    {
        (modeManager.singleDrive.leftSpot, modeManager.singleDrive.rightSpot)
            = (modeManager.singleDrive.rightSpot, modeManager.singleDrive.leftSpot);
    }

    public override string GetName()
    {
        return "Swap Spots";
    }
    
    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}