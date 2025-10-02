using UnityEngine;
using RosSharp.RosBridgeClient.MessageTypes.Spot;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using System;

namespace RosSharp.RosBridgeClient
{
    public class SetHeight : MonoBehaviour
    {
        private RosConnector rosConnector;
        public string serviceName = "/set_stand_height";

        void Start()
        {
            rosConnector = GetComponent<RosConnector>();
        }

        public void SetHeightPercentage(float height)
        {
            Debug.Log($"Setting height to... {height}");
            SetStandHeightRequest request = new SetStandHeightRequest(height);
            Debug.Log($"Requested height percentage: {height}");
            Debug.Log("Service name: " + serviceName);

            rosConnector.RosSocket.CallService<SetStandHeightRequest, SetStandHeightResponse>(
                serviceName,
                response => Debug.Log($"Set Height Service response received: success={response.success}, message={response.message}"),
                request
            );

            Debug.Log($"Requested to set height to {height * 100}%");
        }
    }
}