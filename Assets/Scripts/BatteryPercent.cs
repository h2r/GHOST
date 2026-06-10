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

        private double batteryLevel = -1; // Default to "No Data"

        private BatteryState batteryState;

        public RosConnector rosConnector;

        protected override void Start()
        {            
            Debug.Log("[BatteryPercent] FloatSubscriber.Start() called");
            base.Start();
            Debug.Log("[BatteryPercent] Subscriber started with topic " + Topic);
        }


        protected override void ReceiveMessage(BatteryStateArray message)
        {
            Debug.Log("[BatteryPercent] ReceiveMessage() triggered!");

            // Just take first battery charge_percentage for now
            batteryLevel = message.battery_states[0].charge_percentage;
            Debug.Log($"[BatteryPercent] Updated batteryLevel: {batteryLevel}");
            Debug.Log("[BatteryPercent] " + message);

        }


        private void Update()
        {
           
            if (batteryText != null)
            {
                if (batteryLevel < 0)
                {
                    Debug.Log($"[{spotIdentifier}] BatteryLevel is invalid (<0) → Displaying 'No Data'");
                    batteryText.text = $"{spotIdentifier} Spot Battery: No Data";
                }
                else
                {
                    Debug.Log($"[{spotIdentifier}] BatteryLevel: {batteryLevel:F1}% → Displaying value");
                    batteryText.text = $"{spotIdentifier} Spot Battery: {batteryLevel:F1}%";
                }
            }
        }
    }
}
