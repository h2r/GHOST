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

        // Call this function from other scripts to stow the arm
        // WARNING: if recently sent an incomplete command, it will not Stow
        // eg: if you close the gripper with an object in the gripper (gripper doesn't close all of the way)
        // the arm won't stow because it's still trying to finish closing the gripper
        public void Stow()
        {
            Debug.Log("Stowing arm (Threaded)");
            message.data = true;
            LoopPublish(message);
        }
    }
}
