using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;

namespace RosSharp.RosBridgeClient
{
    public class ThreadedStowArm : MonoBehaviour
    {
        private RosConnector connector;
        private bool ready = false;
        public string serviceName = "/spot/arm_stow";

        protected void Start()
        {
            connector = GetComponent<RosConnector>();
            ready = connector != null && connector.RosSocket != null;
        }

        // Call this function from other scripts to stow the arm
        // WARNING: if recently sent an incomplete command, it will not Stow
        // eg: if you close the gripper with an object in the gripper (gripper doesn't close all of the way)
        // the arm won't stow because it's still trying to finish closing the gripper
        public void Stow()
        {
            ready = connector != null && connector.RosSocket != null;
            if (ready)
            {
                Debug.Log("Stowing arm (Threaded)");
                connector.RosSocket.CallService<MessageTypes.Std.Empty, MessageTypes.Std.Bool>(
                    serviceName,
                    response =>
                    {
                        Debug.Log("Stow arm service response received: " + response.data);
                    },
                    new MessageTypes.Std.Empty()
                );
            }
            else
            {
                Debug.Log("ThreadedStowArm is not ready!");
            }
        }
    }
}
