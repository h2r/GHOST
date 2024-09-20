using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Author: Ezra Odole - Summer Undergraduate 7/26/23

namespace RosSharp.RosBridgeClient
{
    public class ArmInputPublisher : UnityPublisher<MessageTypes.Std.Float64MultiArray>
    {
        private MessageTypes.Std.Float64MultiArray message;
        public double[] published;


        protected override void Start()
        {
            base.Start();
            InitializeMessage();
            published = new double[] { 0, 0, 0, 0, 0, 0 };
        }

        private void InitializeMessage()
        {
            message = new MessageTypes.Std.Float64MultiArray
            {
                data = { }
            };
        }






        /* This function publishes an array of radians to spot, from a set of 6 angles to represent the Ghost Arm's 6 joints 
         * 
         *Instructions:
         *Input the robots local euler angles from each joint
         *order of axis: YXXZXZ
         * 
         * Min/Max angles for each joint
         * sh0: -180, 150
         * sh1: -180, 30
         * el0: 0, 180
         * el1: -160, 160
         * wr0: -105, 105
         * wr1: -165, 165
         * 
         * Warning:
         * If any of the inputs are outside of these bounds
         * Spot will consider it an invalid input
         * and won't move until a valid input is received
         */
        public void publishArmDegrees(double[] inputArmDegrees)
        {
            //do transformations to have a valid output
            double[] robotDegrees = Unity2RobotRotationAxis(inputArmDegrees);
                //TODO:add a 'ClampInputAngles' function
            double[] robotRadians = arrayDeg2Rad(robotDegrees);

            //update the published array variable (to view in inspector)
            for (int i = 0; i < robotRadians.Length; i++)
                published[i] = robotRadians[i];

            //publish
            message.data = published;
            Publish(message);
            
        }




        //Spot and Unity use slightly differnt coordinate systems
        //This function transforms unity's arm angles into spot's arm angles
        public double[] Unity2RobotRotationAxis(double[] inputArmDegrees)
        {
            double[] robotDegrees = new double[6];

            robotDegrees[0] = inputArmDegrees[0] * -1.0;
            robotDegrees[1] = inputArmDegrees[1];
            robotDegrees[2] = inputArmDegrees[2];
            robotDegrees[3] = inputArmDegrees[3] * -1.0;
            robotDegrees[4] = inputArmDegrees[4];
            robotDegrees[5] = inputArmDegrees[5] * -1.0;

            return robotDegrees;
        }




        //Turns degrees into radians
        //Spot's inputs require radians
        public double[] arrayDeg2Rad(double[] input)
        {
            double[] output = new double[input.Length];

            for (int i = 0; i < output.Length; i++)
                output[i] = input[i] * Mathf.Deg2Rad;

            return output;
        }
    }
}

