using UnityEngine;

public class CloudPerspectiveMode : PerspectiveMode
{
    public GameObject armCameraView;

    public override void PerspectiveStart()
    {
        armCameraView.SetActive(false);
    }

    public override string GetName()
    {
        return "Cloud Only";
    }
}