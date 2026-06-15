using UnityEngine;
using TMPro;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Spot;
using System.Collections;

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
            
            if (rosConnector == null)
            {
                rosConnector = GetComponent<RosConnector>();
                Debug.Log($"[BatteryPercent] Getting RosConnector from GameObject: {rosConnector != null}");
            }
            
            if (rosConnector != null)
            {
                Debug.Log($"[BatteryPercent] RosConnector URL: {rosConnector.RosBridgeServerUrl}");
                Debug.Log($"[BatteryPercent] RosConnector Protocol: {rosConnector.protocol}");
                Debug.Log($"[BatteryPercent] RosConnector IsConnected: {rosConnector.IsConnected}");
                
                // Check if websocket is actually connected
                if (!rosConnector.IsConnected)
                {
                    Debug.LogError("[BatteryPercent] RosConnector is NOT connected to ROS bridge!");
                }
            }
            else
            {
                Debug.LogError("[BatteryPercent] No RosConnector found! Subscription will fail.");
            }
            
            Debug.Log("[BatteryPercent] Calling base.Start() to create subscription...");
            base.Start();
            Debug.Log($"[BatteryPercent] base.Start() completed. Subscription should be active for topic: {Topic}");
            
            // Start coroutine to check subscription status
            StartCoroutine(CheckSubscriptionStatus());
        }
        
        private IEnumerator CheckSubscriptionStatus()
        {
            yield return new UnityEngine.WaitForSeconds(2f);
            
            if (!hasReceivedMessage)
            {
                Debug.LogWarning($"[BatteryPercent] After 2 seconds, still no messages received.");
                Debug.LogWarning($"[BatteryPercent] Verify in ROS2: ros2 topic echo {Topic}");
                Debug.LogWarning($"[BatteryPercent] Also check: ros2 topic info {Topic} -v");
                
                if (rosConnector != null && !rosConnector.IsConnected)
                {
                    Debug.LogError("[BatteryPercent] RosConnector lost connection!");
                }
            }
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
