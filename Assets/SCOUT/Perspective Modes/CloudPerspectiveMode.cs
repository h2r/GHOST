using UnityEngine;

public class CloudPerspectiveMode : PerspectiveMode
{
    public GameObject armCameraView;

    public override void PerspectiveStart()
    {
        if (armCameraView != null)
            armCameraView.SetActive(false);
    }

    public override void PerspectiveEnd()
    {
        // Nothing needed
    }

    public override string GetName()
    {
        return "Cloud Only";
    }
}
