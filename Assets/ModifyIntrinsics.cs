using System.Collections;
using System.Collections.Generic;
using RosSharp.RosBridgeClient;
using UnityEngine;

public class ModifyIntrinsics : MonoBehaviour
{
    public IntrinsicsSubscriber intrinsicsSubscriber;
    public GetIntrinsics getIntrinsicsScript;
    

    // Point clouds -- must be in the same order of the images in the ROS script
    public List<DrawMeshInstanced> clouds;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("getIntrinsics", 3);
        InvokeRepeating("modifyCloudIntrinsics", 4, 2);
        Debug.Log("Intrinsics Subscriber: " + intrinsicsSubscriber != null);
        Debug.Log("Get Intrinsics Script: " + getIntrinsicsScript != null);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Tell RosConnector to publish a request for intrinsics
    /// </summary>
    private void getIntrinsics()
    {
        getIntrinsicsScript.RequestIntrinsics();
    }

    /// <summary>
    /// Iterates through the intrinsics array and updates the point clouds.
    /// In the intrinsics array, the first four entries correspond to the hand cloud, the next four frontleft, in the order specified in spot_wrapper.py
    /// </summary>
    /// <param name="intrinsics">All cloud intrinsics. The first four entries correspond to the hand cloud, the next four frontleft, in the order specified in spot_wrapper.py</param>
    public void modifyCloudIntrinsics()
    {
        int ind;
        float[] intrinsics;

        if (intrinsicsSubscriber.messageReceived)
        {
            intrinsics = intrinsicsSubscriber.intrinsics;
            ind = 0;
            foreach (DrawMeshInstanced cloud in clouds)
            {
                cloud.FX = intrinsics[ind++];
                cloud.FY = intrinsics[ind++];
                cloud.CX = Mathf.RoundToInt(intrinsics[ind++]);
                cloud.CY = Mathf.RoundToInt(intrinsics[ind++]);
            }

            Debug.Log("Modified Intrinsics");
            intrinsicsSubscriber.messageReceived = false;
        }
    }
}
