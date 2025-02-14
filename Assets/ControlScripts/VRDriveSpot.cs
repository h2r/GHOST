using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;

public class VRDriveSpot : MonoBehaviour
{
    public OVRInput.RawAxis2D MoveTranslation;
    public OVRInput.RawAxis2D RAx;
    public OVRInput.RawButton rightPress;
    public OVRInput.RawButton leftPress;
    public OVRInput.RawButton RT1;
    //public OVRInput.RawButton RB;

    public RosSharp.RosBridgeClient.MoveSpot drive;
    //public RosSharp.RosBridgeClient.MoveSpot drive2;

    public RawImageSubscriber[] depthSubscribers;
    public JPEGImageSubscriber[] colorSubscribers; // Must be in the same order as depthSubscribers
    public OdometrySubscriber odometrySubscriber;
    public DrawMeshInstanced[] pointClouds;

    private Vector3 lastOdomPos;
    private Quaternion lastOdomRot;
    private Tuple<Vector3, Quaternion>[] origCloudTransforms; // Original location of each point cloud
    private bool[] depthsTempChanged;

    public bool defaultLow;

    private float height;
    private const float HEIGHT_INC = 0.005f;
    private const float HEIGHT_MIN = -0.1f;
    private const float HEIGHT_MAX = 0.3f;
    //private int drive_mode = 0;

    void Start()
    {
        if (defaultLow)
        {
            height = HEIGHT_MIN;
        }
        else
        {
            height = 0f;
        }
        origCloudTransforms = new Tuple<Vector3, Quaternion>[pointClouds.Length];
        for (int i = 0; i < origCloudTransforms.Length; i++ )
        {
            origCloudTransforms[i] = new Tuple<Vector3, Quaternion>(pointClouds[i].transform.localPosition, pointClouds[i].transform.localRotation);
        }

        depthsTempChanged = new bool[pointClouds.Length];
    }

    void Update()
    {

        Vector2 leftMove;
        Vector2 rightMove;
        Vector3 relativePos;
        Quaternion relativeRot;
        Vector3 newPos;
        Quaternion newRot;
        bool heightChanged = false;

        //if (OVRInput.GetDown(RB))
        //{
        //    // loop through 0-1-2
        //    drive_mode = (drive_mode + 1) % 3;
        //}

        // Detect joystick press values
        if (OVRInput.Get(leftPress) && (height - HEIGHT_INC) > HEIGHT_MIN)
        {
            height -= HEIGHT_INC;
            heightChanged = true;
        }
        // No else so that if both are pressed, nothing happens
        if (OVRInput.Get(rightPress) && (height + HEIGHT_INC) < HEIGHT_MAX)
        {
            height += HEIGHT_INC;
            heightChanged = true;
        }


        // Read base movement values, adjust speeds
        rightMove = OVRInput.Get(RAx) * 0.75f;
        leftMove = OVRInput.Get(MoveTranslation) * 0.5f;
        leftMove.x *= 0.5f;

        // Move the robot if any adjustments have been made
        if (rightMove.x != 0f || leftMove.magnitude != 0f || heightChanged)
        {
            // Set movement so only one direction is moved with the left stick at a time
            if (Mathf.Abs(leftMove.x) > Mathf.Abs(leftMove.y)) { leftMove.y = 0; }
            else if (Mathf.Abs(leftMove.y) > Mathf.Abs(leftMove.x)) { leftMove.x = 0; }

            drive.drive(leftMove, rightMove.x, height);

            //// move spot 1
            //if (drive_mode == 0)
            //{
            //    drive.drive(leftMove, rightMove.x, height);
            //}
            //// move spot 2
            //if (drive_mode == 1)
            //{
            //    drive2.drive(leftMove, rightMove.x, height);
            //}
            //// move together
            //if (drive_mode == 2)
            //{
            //    drive.drive(leftMove, rightMove.x, height);
            //    drive2.drive(leftMove, rightMove.x, height);
            //}

            // Pause depth history for 1.5 seconds
            foreach (RawImageSubscriber ds in depthSubscribers)
            {
                ds.pauseDepthHistory(1.5f);
            }
        }
    }


    private bool vectorEqual(Vector3 a, Vector3 b)
    {
        Vector3 diff;
        float thresh;
        thresh = 0.001f;
        diff = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        diff.x = Math.Abs(diff.x);
        diff.y = Math.Abs(diff.y);
        diff.z = Math.Abs(diff.z);
        if (diff.x > thresh || diff.y > thresh || diff.z > thresh)
        {
            return false;
        }
        return true;
    }

    private bool quatEqual(Quaternion a, Quaternion b)
    {
        Quaternion diff;
        float thresh;
        thresh = 0.001f;
        diff = new Quaternion(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        diff.x = Math.Abs(diff.x);  
        diff.y = Math.Abs(diff.y);
        diff.z = Math.Abs(diff.z);
        diff.w = Math.Abs(diff.w);

        if (diff.x > thresh || diff.y > thresh || diff.z > thresh) // || diff.w > 0.001)
        {
            return false;
        }
        return true;
    }
}
