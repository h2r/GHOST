/*
� Siemens AG, 2017-2018
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

// Added allocation free alternatives
// UoK , 2019, Odysseas Doumas (od79@kent.ac.uk / odydoum@gmail.com)

// Modified by Brown University 2023-2024
// for relative pose publishing of SCOUT's arm end effector
// in VR controller use.

using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class PoseStampedRelativePublisher : UnityPublisher<MessageTypes.Geometry.PoseStamped>
    {
        public Transform PublishedTransform;
        public Transform Offset;
        public string FrameId = "Unity";

        private MessageTypes.Geometry.PoseStamped message;
        private Vector3 lastPublishedPosition = Vector3.zero;
        private Quaternion lastPublishedRotation = Quaternion.identity;

        protected override void Start()
        {
            base.Start();
            InitializeMessage();

            // Call update at 5 hz
            InvokeRepeating("UpdateMessage", 0, 0.2f);
        }

        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.PoseStamped
            {
                header = new MessageTypes.Std.Header()
                {
                    frame_id = FrameId
                }
            };
        }

        private void UpdateMessage()
        {
            // Only set location if enabled -- controlled by MoveArm script
            if (enabled)
            {
                message.header.Update();

                // Go to the dummy finger's position, plus the approximate offset of the end effector
                Vector3 offsetPosition = PublishedTransform.localRotation * Offset.localPosition;
                Vector3 newLocation = PublishedTransform.localPosition + offsetPosition;

                // if the motion is too small, don't publish it
                if ((newLocation - lastPublishedPosition).magnitude < 0.005f &&
                    Quaternion.Angle(PublishedTransform.localRotation, lastPublishedRotation) < 0.1f)
                {
                    Debug.Log("skipping small motion!");
                    return;
                }

                // publish the new location if it's sufficiently different
                lastPublishedPosition = newLocation;
                lastPublishedRotation = PublishedTransform.localRotation;
                GetGeometryPoint(newLocation.Unity2Ros(), message.pose.position);
                GetGeometryQuaternion(PublishedTransform.localRotation.Unity2Ros(), message.pose.orientation);

                Publish(message);
            }
        }

        private static void GetGeometryPoint(Vector3 position, MessageTypes.Geometry.Point geometryPoint)
        {
            geometryPoint.x = position.x;
            geometryPoint.y = position.y;
            geometryPoint.z = position.z;
        }

        private static void GetGeometryQuaternion(Quaternion quaternion, MessageTypes.Geometry.Quaternion geometryQuaternion)
        {
            geometryQuaternion.x = quaternion.x;
            geometryQuaternion.y = quaternion.y;
            geometryQuaternion.z = quaternion.z;
            geometryQuaternion.w = quaternion.w;
        }

    }
}
