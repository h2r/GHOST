using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class GetIntrinsics : UnityPublisher<MessageTypes.Std.Bool>
    {

        private MessageTypes.Std.Bool message;

        private void InitializeMessage()
        {
            message = new MessageTypes.Std.Bool();
        }

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }

        public void Update()
        {

        }

        public void RequestIntrinsics()
        {
            message.data = true;
            Publish(message);
        }
    }
}
