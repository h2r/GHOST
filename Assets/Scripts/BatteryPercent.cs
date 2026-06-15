using UnityEngine;
using TMPro;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Spot;

namespace RosSharp.RosBridgeClient
{
    public class FloatSubscriber : UnitySubscriber<BatteryStateArray>
    {
        [Tooltip("Label for the robot or device this subscriber is monitoring.")]
        public string spotIdentifier = "Red"; 
        public TextMeshProUGUI batteryText;
        
        private float connectionCheckTimer = 0f;
        private bool hasReceivedMessage = false;

        private double batteryLevel = -1; // Default to "No Data"

        private BatteryState batteryState;

        public RosConnector rosConnector;

        protected override void Start()
        {            
            Debug.Log("[BatteryPercent] FloatSubscriber.Start() called");
            
            // Log current configuration
            Debug.Log($"[BatteryPercent] Topic: '{Topic}'");
            Debug.Log($"[BatteryPercent] RosConnector assigned: {rosConnector != null}");
            
            if (rosConnector != null)
            {
                Debug.Log($"[BatteryPercent] RosConnector URL: {rosConnector.RosBridgeServerUrl}");
                Debug.Log($"[BatteryPercent] RosConnector Protocol: {rosConnector.protocol}");
            }
            
            base.Start();
            Debug.Log("[BatteryPercent] Subscriber started with topic " + Topic);
        }


        protected override void ReceiveMessage(BatteryStateArray message)
        {
            hasReceivedMessage = true;
            Debug.Log("[BatteryPercent] ReceiveMessage() triggered!");
            
            if (message == null)
            {
                Debug.LogError("[BatteryPercent] Received null message!");
                return;
            }
            
            if (message.battery_states == null || message.battery_states.Length == 0)
            {
                Debug.LogError("[BatteryPercent] battery_states array is null or empty!");
                return;
            }
            
            Debug.Log($"[BatteryPercent] Received {message.battery_states.Length} battery states");

            // Just take first battery charge_percentage for now
            batteryLevel = message.battery_states[0].charge_percentage;
            Debug.Log($"[BatteryPercent] Updated batteryLevel: {batteryLevel}");
            Debug.Log($"[BatteryPercent] Battery identifier: {message.battery_states[0].identifier}");
            Debug.Log($"[BatteryPercent] Charge percentage: {message.battery_states[0].charge_percentage}");
            Debug.Log($"[BatteryPercent] Estimated runtime: {message.battery_states[0].estimated_runtime}");
        }


        private void Update()
        {
            // Periodic connection status check
            connectionCheckTimer += Time.deltaTime;
            if (connectionCheckTimer >= 5f)  // Check every 5 seconds
            {
                connectionCheckTimer = 0f;
                if (!hasReceivedMessage)
                {
                    Debug.LogWarning($"[BatteryPercent] No messages received yet. Topic: '{Topic}', RosConnector: {rosConnector != null}");
                    if (rosConnector != null)
                    {
                        Debug.LogWarning($"[BatteryPercent] Check if topic '/battery_states' is being published in ROS2");
                    }
                }
            }
           
            if (batteryText != null)
            {
                if (batteryLevel < 0)
                {
                    //Debug.Log($"[{spotIdentifier}] BatteryLevel is invalid (<0) → Displaying 'No Data'");
                    batteryText.text = $"{spotIdentifier} Spot Battery: No Data";
                }
                else
                {
                    //Debug.Log($"[{spotIdentifier}] BatteryLevel: {batteryLevel:F1}% → Displaying value");
                    batteryText.text = $"{spotIdentifier} Spot Battery: {batteryLevel:F1}%";
                }
            }
        }
    }
}
