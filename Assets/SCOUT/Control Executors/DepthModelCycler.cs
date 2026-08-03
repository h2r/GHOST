using UnityEngine;
using SCOUT;

// Parallel to PointCloudCycler and operates on SpotObserverClient references (not DepthManager)
public class DepthModelCycler : MonoBehaviour
{
    public SpotObserverClient[] spotObserverClients;
    public MessageBadge messageManager;

    public void CycleModels()
    {
        if (spotObserverClients == null || spotObserverClients.Length == 0)
            return;

        string activeModelName = "Unknown";
        bool anySuccess = false;

        for (int i = 0; i < spotObserverClients.Length; i++)
        {
            SpotObserverClient client = spotObserverClients[i];
            if (client == null)
                continue;

            bool success = client.CycleDepthModel();
            anySuccess |= success;
            activeModelName = client.GetCurrentDepthModelName();
        }

        if (messageManager != null)
        {
            messageManager.ShowMessage(anySuccess ? $"Depth Model: {activeModelName}" : "Depth Model: switch failed");
        }
    }
}