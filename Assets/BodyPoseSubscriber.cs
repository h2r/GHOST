using Oculus.Interaction.Body.PoseDetection;
using RosSharp.RosBridgeClient.MessageTypes.Tf2;
using System;
using UnityEngine;
using static UnityEditor.Recorder.OutputPath;

namespace RosSharp.RosBridgeClient
{
    public class BodyPoseSubscriber : UnitySubscriber<MessageTypes.Tf2.TFMessage>
    {
        private bool isMessageReceived;
        private MessageTypes.Tf2.TFMessage message;
        //private Vector3 position;
        //private Quaternion rotation;
        private Matrix4x4 body_pose;


        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        public Matrix4x4 getBodyPose()
        {
            return body_pose;
        }

        protected override void ReceiveMessage(MessageTypes.Tf2.TFMessage _message)
        {
            message = _message;
            isMessageReceived = true;
        }

        private Matrix4x4 CalculatePoseMatrix(Vector3 pos, Quaternion rot)
        {
            Matrix4x4 translationMatrix = Matrix4x4.Translate(pos);
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rot);
            Matrix4x4 poseMatrix = translationMatrix * rotationMatrix;

            return poseMatrix;
        }

        private void ProcessMessage()
        {
            foreach (var transformStamped in message.transforms)
            {
                if (transformStamped.header.frame_id == "spot/body" &&
                    transformStamped.child_frame_id == "spot/odom")
                {
                    body_pose = CalculatePoseMatrix(
                        GetPosition(transformStamped).Ros2Unity(),
                        GetRotation(transformStamped).Ros2Unity()
                        );
                }
            }
        }

        private Vector3 GetPosition(MessageTypes.Geometry.TransformStamped message)
        {
            return new Vector3(
                (float)message.transform.translation.x,
                (float)message.transform.translation.y,
                (float)message.transform.translation.z);
        }

        private Quaternion GetRotation(MessageTypes.Geometry.TransformStamped message)
        {
            return new Quaternion(
                (float)message.transform.rotation.x,
                (float)message.transform.rotation.y,
                (float)message.transform.rotation.z,
                (float)message.transform.rotation.w);
        }
    }
}
