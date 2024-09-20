using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class SetGripper: UnityPublisher<MessageTypes.Geometry.Twist>
    {

        private MessageTypes.Geometry.Twist message;

        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.Twist();

        }

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }

        //Press 'O' to open
        //Press 'P' to close
        private void Update()
        {
            if (Input.GetKeyDown("o"))
                openGripper();
            else if (Input.GetKeyDown("p"))
                closeGripper();
        }


        public void openGripper()
        {
            Vector3 linearVelocity;
            Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);

            linearVelocity = new Vector3(1.0f, 0.0f, 0.0f);
            message.linear = new MessageTypes.Geometry.Vector3(100.0f, 0.0f, 0.0f);
            message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
            Debug.Log(message.linear);
            Publish(message);
            Debug.Log("Opening Gripper");

        }

        public void closeGripper()
        {
            Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            message.linear = new MessageTypes.Geometry.Vector3(-10.0f, 0.0f, 0.0f);
            message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
            Publish(message);
            Debug.Log("Closing Gripper");

        }

        public void setGripperPercentage(float percent)
        {
            Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            message.linear = new MessageTypes.Geometry.Vector3(percent, 0.0f, 0.0f);
            message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
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

    }
}
