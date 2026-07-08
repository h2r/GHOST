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

        protected override void Start()
        {
            base.Start();
            Debug.Log($"[BatteryPercent] Subscription active for topic: {Topic}");
            Invoke("DelayedCheck", 2f);
        }

        private void DelayedCheck()
        {
            if (!hasReceivedMessage)
            {
                Debug.LogWarning($"[BatteryPercent] After 2 seconds, still no messages received on {Topic}.");
                Debug.LogWarning($"[BatteryPercent] Verify in ROS2: ros2 topic echo {Topic}");
            }
            else
            {
                Debug.Log("[BatteryPercent] SUCCESS: Messages are being received!");
            }
        }

        protected override void ReceiveMessage(BatteryStateArray message)
        {
            hasReceivedMessage = true;

            if (message?.battery_states == null || message.battery_states.Length == 0)
            {
                Debug.LogError("[BatteryPercent] battery_states array is null or empty!");
                return;
            }

            batteryLevel = message.battery_states[0].charge_percentage;
            Debug.Log($"[BatteryPercent] Charge percentage: {batteryLevel}, identifier: {message.battery_states[0].identifier}");
        }

        private void Update()
        {
            connectionCheckTimer += Time.deltaTime;
            if (connectionCheckTimer >= 5f)
            {
                connectionCheckTimer = 0f;
                if (!hasReceivedMessage)
                    Debug.LogWarning($"[BatteryPercent] No messages received yet. Topic: '{Topic}'");
            }

            if (batteryText != null)
            {
                batteryText.text = batteryLevel < 0
                    ? $"{spotIdentifier} Spot Battery: No Data"
                    : $"{spotIdentifier} Spot Battery: {batteryLevel:F1}%";
            }
        }
    }
}