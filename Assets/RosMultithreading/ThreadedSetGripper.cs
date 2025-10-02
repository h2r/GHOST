using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Spot;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using System;

namespace RosSharp.RosBridgeClient
{

    public class ThreadedSetGripper : MonoBehaviour
    {
        private RosConnector rosConnector;
        public string serviceName = "/set_gripper_angle";

        void Start()
        {
            rosConnector = GetComponent<RosConnector>();
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
            SetGripperAngleRequest request = new SetGripperAngleRequest(clampedAngle);
            Debug.Log($"Requested gripper angle: {clampedAngle}");
            Debug.Log("Service name: " + serviceName);

            rosConnector.RosSocket.CallService<SetGripperAngleRequest, SetGripperAngleResponse>( // reponse type bool success; string message
                serviceName,
                response => Debug.Log($"Gripper Service response received: success={response.success}, message={response.message}"),
                request
            );

            Debug.Log($"Requested to set gripper angle to {clampedAngle} degrees");
        }
    }
}
