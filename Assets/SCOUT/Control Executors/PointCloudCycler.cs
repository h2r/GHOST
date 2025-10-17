using UnityEngine;
using SCOUT;

public class PointCloudCycler : MonoBehaviour
{
    public DepthManager[] depthManagers;
    public MessageBadge messageManager;

    private int currentIndex = -1;

    public void CyclePointClouds()
    {
        if (depthManagers == null || depthManagers.Length == 0)
            return;
        currentIndex = (currentIndex + 1) % (depthManagers.Length + 1);

        for (int i = 0; i < depthManagers.Length; i++)
        {
            if (currentIndex == 0)
            {
                // all on
                depthManagers[i].show_spot = true;
            }
            else
            {
                depthManagers[i].show_spot = (i + 1 == currentIndex);
                if (messageManager != null)
                {
                    messageManager.ShowMessage("PointCloud: Robot " + (i + 1 == currentIndex ? "On" : "Off"));
                }
            }
        }
        if (messageManager != null)
        {
            if (currentIndex == 0)
                messageManager.ShowMessage("PointCloud: All On");
            else
                messageManager.ShowMessage($"PointCloud: {currentIndex} On");
        }
    }
}
