using UnityEngine;
using RosSharp.RosBridgeClient;

namespace RosSharp.RosBridgeClient
{
    [RequireComponent(typeof(ThreadedRosConnector))]
    public class ThreadedSetGripperService : MonoBehaviour
    {
        public string topic = "/spot/set_gripper_angle";

        private ThreadedRosConnector rosConnector;

        protected virtual void Start()
        {
            rosConnector = GetComponent<ThreadedRosConnector>();
        }

        public void OpenGripper()
        {
            SetGripperAngle(90.0f);
        }

        public void CloseGripper()
        {
            SetGripperAngle(0.0f);
        }

        public void SetGripperAngle(float angle)
        {
            if (angle < 0.0f || angle > 90.0f)
            {
                Debug.LogWarning($"Gripper angle {angle} is out of valid range [0.0, 90.0]");
                return;
            }
            // TODO it is unclear what this is used for. remove this class?
            // SetGripperRequest request = new SetGripperRequest(angle);
            // rosConnector.RosSocket.CallService<SetGripperRequest, SetGripperResponse>(
            //     topic,
            //     ServiceResponseHandler,
            //     request
            // );
        }

        private void ServiceResponseHandler(SetGripperResponse response)
        {
            if (response.success)
            {
                Debug.Log($"Gripper service call succeeded: {response.message}");
            }
            else
            {
                Debug.LogWarning($"Gripper service call failed: {response.message}");
            }
        }
    }

    // Define custom message types for the service
    public class SetGripperRequest : Message
    {
        public const string RosMessageName = "spot_msgs/SetGripperAngle";

        public float gripper_angle;

        public SetGripperRequest()
        {
            gripper_angle = 0.0f;
        }

        public SetGripperRequest(float angle)
        {
            gripper_angle = angle;
        }
    }

    public class SetGripperResponse : Message
    {
        public const string RosMessageName = "spot_msgs/SetGripperAngle";

        public bool success;
        public string message;

        public SetGripperResponse()
        {
            success = false;
            message = "";
        }
    }
}
