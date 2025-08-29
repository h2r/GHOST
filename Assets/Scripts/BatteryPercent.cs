using UnityEngine;
using TMPro;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    public class FloatSubscriber : UnitySubscriber<Float64>
    {
        [Header("Spot Identifier")]
        [Tooltip("Label for the robot or device this subscriber is monitoring.")]
        public string spotIdentifier = "Red";

        [Header("Battery UI")]
        public TextMeshProUGUI batteryText;
        public double batteryLevel = -1; // Set default value to -1 (indicating no data)

        protected override void Start()
        {
            base.Start();
        }

        protected override void ReceiveMessage(Float64 message)
        {
            batteryLevel = message.data;
            Debug.Log($"{spotIdentifier} Spot Battery: {batteryLevel}%");
        }

        private void Update()
        {
            if (batteryText != null)
            {
                // Check if batteryLevel is -1 (no data)
                if (batteryLevel == -1)
                {
                    batteryText.text = $"{spotIdentifier} Spot Battery: No Data";
                }
                else
                {
                    batteryText.text = $"{spotIdentifier} Spot Battery: {batteryLevel}%";
                }
            }
        }
    }
}