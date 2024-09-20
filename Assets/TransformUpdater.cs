using System.Collections;
using System.Collections.Generic;
using RosSharp.RosBridgeClient;
using UnityEngine;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using System;
using System.Drawing;

public class TransformUpdater : MonoBehaviour
{
    public UnityEngine.Transform target;
    public ExtrinsicsSubscriber subscriber;
    public bool[] neg = new bool[7];
    public int subscriberIndex;
    private bool freezeExt = false;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (subscriber != null && subscriber.pose != null && !freezeExt)
        {
            float[] negations = new float[7];
            for (int i = 0; i<7; i++)
            {
                if (neg[i]) { negations[i] = -1f; } else { negations[i] = 1f; }
            }
            RosSharp.RosBridgeClient.MessageTypes.Geometry.Point new_position = subscriber.pose.position;
            // Debug.Log(new_position.x + ", " +  new_position.y + ", " + new_position.z);
            UnityEngine.Vector3 unity_position = new UnityEngine.Vector3(negations[0]*(float)new_position.x, negations[1]*(float)new_position.y, negations[2]*(float)new_position.z);
            // Debug.Log(unity_position.x + ", " + unity_position.y + ", " + unity_position.z);
            RosSharp.RosBridgeClient.MessageTypes.Geometry.Quaternion new_orientation = subscriber.pose.orientation;
            //UnityEngine.Vector3 unity_rotation = new UnityEngine.Vector3(negations[3]*(float)new_orientation.x, negations[4]*(float)new_orientation.y, negations[5]*(float)new_orientation.z);
            UnityEngine.Quaternion unity_rotation = UnityEngine.Quaternion.Euler(negations[3] * (float)new_orientation.x, negations[4] * (float)new_orientation.y, negations[5] * (float)new_orientation.z);
            target.transform.localPosition = unity_position;
            target.transform.localRotation = unity_rotation;
        }
        
    }

    public void toggleFreeze()
    {
        freezeExt = !freezeExt;
    }    
}
