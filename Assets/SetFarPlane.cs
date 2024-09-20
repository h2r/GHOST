using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class SetFarPlane : UnityPublisher<MessageTypes.Std.Float32>
    {

        private MessageTypes.Std.Float32 message;

        private void InitializeMessage()
        {
            message = new MessageTypes.Std.Float32();
        }

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }

        public void Update()
        {

        }

        public void RequestFarPlane(float farPlane)
        {
            message.data = farPlane;
            Debug.Log("Set Far Plane: " + farPlane);
            Publish(message);
        }
    }
}
