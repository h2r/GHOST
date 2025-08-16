using UnityEngine;

public class ArmPerspectiveMode : PerspectiveMode
{
    public GameObject armCameraView;

    public override void PerspectiveStart()
    {
        if (armCameraView != null)
            armCameraView.SetActive(true);
    }

    public override void PerspectiveEnd()
    {
        if (armCameraView != null)
            armCameraView.SetActive(false);
    }

    public override string GetName()
    {
        return "Arm Camera";
    }
}