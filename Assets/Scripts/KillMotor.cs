using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class KillMotor : UnityPublisher<MessageTypes.Std.Bool>
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


        void Update()
        {
            if(Input.GetKeyDown("k"))
                killSpot();
        }

        public void killSpot()
        {
            message.data = true;
            Publish(message);
        }

    }
}
