using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class StowArm : UnityPublisher<MessageTypes.Std.Bool>
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


        //Press 'R' to stow
        public void Update()
        {
            if (Input.GetKeyDown("r"))
                Stow();
        }

        // Call this function from other scripts to stow the arm
        //WARNING: if recently sent an incomplete command, it will not Stow
            //eg: if you close the gripper with an object in the gripper (gripper doesn't close all of the way)
            //the arm won't stow because it's still trying to finish closing the gripper
        public void Stow()
        {
            Debug.Log("Stowing arm");
            message.data = true;
            Publish(message);
        }
    }
}
