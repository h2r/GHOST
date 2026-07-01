using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;

namespace RosSharp.RosBridgeClient
{
    public class ThreadedReadyArm : MonoBehaviour
    {
        private RosConnector connector;
        private bool ready = false;
        public string serviceName = "/spot/arm_ready";

        protected void Start()
        {
            connector = GetComponent<RosConnector>();
            ready = connector != null && connector.RosSocket != null;
        }

        // Call this function from other scripts to ready the arm
        // WARNING: if recently sent an incomplete command, it will not ready
        // eg: if you close the gripper with an object in the gripper (gripper doesn't close all of the way)
        // the arm won't ready because it's still trying to finish closing the gripper
        public void Ready()
        {
            ready = connector != null && connector.RosSocket != null;
            if (ready)
            {
                Debug.Log("Readying arm (Threaded)");
                connector.RosSocket.CallService<MessageTypes.Std.Empty, MessageTypes.Std.Bool>(
                    serviceName,
                    response =>
                    {
                        Debug.Log("Ready arm service response received: " + response.data);
                    },
                    new MessageTypes.Std.Empty()
                );
            }
            else
            {
                Debug.Log("ThreadedReadyArm is not ready!");
            }
        }
    }
}
