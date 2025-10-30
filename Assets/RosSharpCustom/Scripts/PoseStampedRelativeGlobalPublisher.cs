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
    public class PoseStampedRelativeGlobalPublisher : UnityPublisher<MessageTypes.Geometry.PoseStamped>
    {
        public Transform PublishedTransform;
        public Transform BaseTransform;
        public string FrameId = "Unity";
        public Vector3 RotationOffsetEulerZXY = new Vector3(-90, 0, 0);

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

        public void SendUpdate()
        {
            message.header.Update();

            // get the PublishedTransform's position relative to the BaseTransform
            Matrix4x4 publishedMatrix = Matrix4x4.TRS(PublishedTransform.position, PublishedTransform.rotation, Vector3.one);
            Matrix4x4 baseMatrix = Matrix4x4.TRS(BaseTransform.position, BaseTransform.rotation, Vector3.one);
            Matrix4x4 publishedMatrixWithOffset = baseMatrix.inverse * publishedMatrix;
            // hack -- rotate -90 degrees around X to account for the fact that the end effector in the URDF in Unity is upright ?!!!
            publishedMatrixWithOffset = publishedMatrixWithOffset * Matrix4x4.Rotate(Quaternion.Euler(RotationOffsetEulerZXY));

            Vector3 newLocation = publishedMatrixWithOffset.GetPosition();
            Quaternion newRotation = publishedMatrixWithOffset.rotation; 
            // if the motion is too small, don't publish it
            if ((newLocation - lastPublishedPosition).magnitude < 0.005f &&
                Quaternion.Angle(newRotation, lastPublishedRotation) < 0.1f)
            {
                Debug.Log("skipping small motion!");
                return;
            }

            // publish the new location if it's sufficiently different
            lastPublishedPosition = newLocation;
            lastPublishedRotation = newRotation;
            GetGeometryPoint(newLocation.Unity2Ros(), message.pose.position);
            GetGeometryQuaternion(newRotation.Unity2Ros(), message.pose.orientation);

            Publish(message);
        }

        private void UpdateMessage()
        {
            Debug.Log("PoseStampedRelativePublisher: UpdateMessage called. enabled? " + enabled);
            // Only set location if enabled -- controlled by MoveArm script
            if (enabled)
            {
                SendUpdate();
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
