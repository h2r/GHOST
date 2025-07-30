using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;

namespace RosSharp.RosBridgeClient
{
    public class ThreadedStowArm : ThreadedUnityPublisher<MessageTypes.Std.Bool>
    {
        private MessageTypes.Std.Bool message;

        protected override void Start()
        {
            base.Start();
            message = new MessageTypes.Std.Bool();
        }

        public void Stow()
        {
            Debug.Log("Stowing arm (Threaded)");
            message.data = true;
            LoopPublish(message);
        }
    }
}
