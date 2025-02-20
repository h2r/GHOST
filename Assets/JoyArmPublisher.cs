
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class JoyArmPublisher : UnityPublisher<MessageTypes.Geometry.PoseStamped>
    {

        public string FrameId = "Unity";

        private MessageTypes.Geometry.PoseStamped message;

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

        protected override void Start()
        {
            base.Start();
            InitializeMessage();

            // Call update at 5 hz
            //InvokeRepeating("UpdateMessage", 0, 0.2f);
        }

        //Press 'O' to open
        //Press 'P' to close
        private void Update()
        {

        }




        public void setCoordinate(float x, float y, float z, Quaternion rotationChange)
        {
            //Vector3 angularVelocity = new Vector3(0.0f, qx, 0.0f);
            //message.linear = new MessageTypes.Geometry.Vector3(x, y, z);
            //message.angular = new MessageTypes.Geometry.Vector3(qx, 0.0f, 0.0f);

            //message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
            //Publish(message);


            message.header.Update();

            //Vector3 newLocation = new Vector3(x, y, z);
            //GetGeometryPoint(newLocation, message.pose.position);
            message.pose.position.x = x;
            message.pose.position.y = y;
            message.pose.position.z = z;

            //GetGeometryQuaternion(rotationChange.Unity2Ros(), message.pose.orientation);
            message.pose.orientation.x = rotationChange.x;
            message.pose.orientation.y = rotationChange.y;
            message.pose.orientation.z = rotationChange.z;
            message.pose.orientation.w = rotationChange.w;

            Publish(message);
        }

        public static MessageTypes.Geometry.Vector3 GetGeometryVector3(Vector3 vector3)
        {
            MessageTypes.Geometry.Vector3 geometryVector3 = new MessageTypes.Geometry.Vector3();
            geometryVector3.x = vector3.x;
            geometryVector3.y = vector3.y;
            geometryVector3.z = vector3.z;
            return geometryVector3;
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
