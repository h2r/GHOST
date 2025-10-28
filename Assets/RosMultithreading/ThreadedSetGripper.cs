using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;

namespace RosSharp.RosBridgeClient
{

    public class ThreadedSetGripper : UnityPublisher<MessageTypes.Std.Float32>
    {
        protected override void Start()
        {
            base.Start();
        }

        public void OpenGripper()
        {
            SetGripperAngle(90f); // Open gripper to 90 degrees
        }

        public void CloseGripper()
        {
            SetGripperAngle(0f); // Close gripper to 0 degrees
        }

        public void SetGripperAngle(float angle)
        {
            float clampedAngle = Mathf.Clamp(angle, 0f, 90f);

            MessageTypes.Std.Float32 message = new MessageTypes.Std.Float32();
            message.data = clampedAngle;

            Publish(message);

            Debug.Log($"Published gripper angle: {clampedAngle}");
        }
    }
}
