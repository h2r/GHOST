using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
	public class MoveSpot : UnityPublisher<MessageTypes.Geometry.Twist>
	{
        private MessageTypes.Geometry.Twist message;
        public bool save;

 	    protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }

        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.Twist();
            message.linear = new MessageTypes.Geometry.Vector3();
            message.angular = new MessageTypes.Geometry.Vector3();
        }

    	// Update is called once per frame
    	void Update()
    	{
            bool moved = false;
            Vector3 linearVelocity  = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);

            if (Input.GetKey("w"))
        	{
            	print("move forward");
                linearVelocity += new Vector3(0.0f, 0.0f, 0.5f);
                moved = true;
        	}
        	if (Input.GetKey("s"))
        	{
            	print("move back");
            	linearVelocity -= new Vector3(0.0f, 0.0f, 0.5f);
                moved = true;
            }
        	if (Input.GetKey("d"))
        	{
            	print("move right");
            	linearVelocity += new Vector3(0.5f, 0.0f, 0.0f);
                moved = true;
            }
        	if (Input.GetKey("a"))
        	{
            	print("move left");
            	linearVelocity -= new Vector3(0.5f, 0.0f, 0.0f);
                moved = true;
            }
        	if (Input.GetKey("e"))
        	{
            	print("rotate right");
            	angularVelocity += new Vector3(0.0f, 0.5f, 0.0f);
                moved = true;
            }
       		if (Input.GetKey("q"))
        	{
            	print("rotate left");
            	angularVelocity -= new Vector3(0.0f, 0.5f, 0.0f);
                moved = true;
            }            

            if (Input.GetKeyDown("i"))
            {
                Debug.Log("Saving");
                save = true;
            }

            if (moved)
            {
                message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
                message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

                Publish(message);
            }
        }

        public void drive(float forward, float strafe, float turn, float look, float height)
        {
            Vector3 linearVelocity = new Vector3(strafe, height, forward);
            Vector3 angularVelocity = new Vector3(look, turn, 0.0f);

            Debug.Log("Driving with linear velocity: " + linearVelocity + " (Unity2Ros: " + linearVelocity.Unity2Ros() + ")");
            Debug.Log("Driving with angular velocity: " + angularVelocity + " (Unity2Ros: " + (-angularVelocity.Unity2Ros()) + ")");

            message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
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

