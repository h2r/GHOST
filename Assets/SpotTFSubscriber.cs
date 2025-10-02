/*
� Siemens AG, 2017-2019
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections.Generic;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Moveit;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class SpotTFSubscriber : UnitySubscriber<MessageTypes.Geometry.TransformStamped>
    {
        public string target;
        public string source;
        public GameObject targetObject;
        private bool isMessageReceived;
        private TransformStamped message;

        protected override void Start()
        {
            base.Start();
        }

        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        protected override void ReceiveMessage(TransformStamped transformStamped)
        {
            message = transformStamped;
            isMessageReceived = true;
        }

        private UnityEngine.Vector3 GetPosition(TransformStamped message)
        {
            return new UnityEngine.Vector3(
                (float)message.transform.translation.x,
                (float)message.transform.translation.y,
                (float)message.transform.translation.z);
        }

        private UnityEngine.Quaternion GetRotation(TransformStamped message)
        {
            return new UnityEngine.Quaternion(
                (float)message.transform.rotation.x,
                (float)message.transform.rotation.y,
                (float)message.transform.rotation.z,
                (float)message.transform.rotation.w);
        }


        private void ProcessMessage()
        {
            if (message == null || targetObject == null)
                return;
            Debug.Log("multi spot tf: " + message.header.frame_id + "  " + message.child_frame_id);
            if (message.header.frame_id == source && message.child_frame_id == target)
            {
                Debug.Log("xyz: " + message.transform.translation.x.ToString() + message.transform.translation.y.ToString() + message.transform.translation.z.ToString());
                UnityEngine.Vector3 position = GetPosition(message).Ros2Unity();
                UnityEngine.Quaternion rotation = GetRotation(message).Ros2Unity();
                Debug.Log("received spot tf");

                targetObject.transform.SetLocalPositionAndRotation(position, rotation);
            }
            isMessageReceived = false;
        }
    }
}