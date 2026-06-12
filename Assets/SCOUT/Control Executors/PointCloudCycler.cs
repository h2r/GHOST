using UnityEngine;
using SCOUT;

public class PointCloudCycler : MonoBehaviour
{
    public DepthManager[] depthManagers;
    public MessageBadge messageManager;

    private int currentIndex = -1;
    private bool[] storedShowStates;

    // turn all point clouds off (e.g. while the video windows are up), remembering their state
    public void HideAllPointClouds()
    {
        if (depthManagers == null)
            return;

        storedShowStates = new bool[depthManagers.Length];
        for (int i = 0; i < depthManagers.Length; i++)
        {
            storedShowStates[i] = depthManagers[i].show_spot;
            depthManagers[i].show_spot = false;
        }

        if (messageManager != null)
            messageManager.ShowMessage("PointCloud: Off");
    }

    // restore whatever was showing before HideAllPointClouds
    public void RestorePointClouds()
    {
        if (depthManagers == null || storedShowStates == null)
            return;

        for (int i = 0; i < depthManagers.Length && i < storedShowStates.Length; i++)
            depthManagers[i].show_spot = storedShowStates[i];
        storedShowStates = null;
    }

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
