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
            

            if (Input.GetKey("w"))
        	{
            	print("move forward");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f,  0.5f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	            message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("s"))
        	{
            	print("move back");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, -0.5f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	            message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("d"))
        	{
            	print("move right");
            	Vector3 linearVelocity = new Vector3(0.5f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	            message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("a"))
        	{
            	print("move left");
            	Vector3 linearVelocity = new Vector3(-0.5f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	            message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("e"))
        	{
            	print("rotate right");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.5f, 0.0f);
	            message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

            	Publish(message);
        	}
       		if (Input.GetKey("q"))
        	{
            	print("rotate left");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, -0.5f, 0.0f);
	            message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

            	Publish(message);
        	}
            if (Input.GetKeyDown("i"))
            {
                Debug.Log("Saving");
                save = true;
            }
    	}


        public void drive(Vector2 wasd, float rotate, float height)
        {

            Vector3 linearVelocity = new Vector3(wasd[0], height, wasd[1]);
            Vector3 angularVelocity = new Vector3(0.0f, rotate, 0.0f);
            Debug.Log("in drive" + linearVelocity);
            Debug.Log("in drive" + linearVelocity.Unity2Ros());
            Debug.Log("message" + message);
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

