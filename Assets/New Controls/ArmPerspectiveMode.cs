using UnityEngine;

public class ArmPerspectiveMode : PerspectiveMode
{
    public GameObject armCameraView;

    public override void PerspectiveStart()
    {
        armCameraView.SetActive(true);
    }

    public override string GetName()
    {
        return "Arm Camera";
    }
}