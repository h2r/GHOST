using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.RclInterfaces;
using System.Collections.Generic;

namespace RosSharp.RosBridgeClient
{
    public class SpotHostNameGetter : MonoBehaviour
    {
        public string hostname = "";
        public string spot_prefix = "/spot";

        private RosConnector rosConnector;

        void Start()
        {
            rosConnector = GetComponent<RosConnector>();
            UpdateHostName();
        }

        private void UpdateHostName()
        {
            string service_topic = spot_prefix + "/spot_ros2/get_parameters";
            string[] names = new string[] { "hostname" };
            GetParametersRequest request_params = new GetParametersRequest(names);
            rosConnector.RosSocket.CallService<GetParametersRequest, GetParametersResponse>(
                service_topic,
                response =>
                {
                    hostname = response.values[0].string_value;
                    Debug.Log("Got " + spot_prefix + " Hostname: " + hostname);
                },
                request_params
            );
        }

    }
}
