using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.XR;

public class VRDriveSpot : MonoBehaviour
{
    public OVRInput.RawAxis2D MoveTranslation;
    public OVRInput.RawAxis2D RAx;
    public OVRInput.RawButton rightPress;
    public OVRInput.RawButton leftPress;
    public OVRInput.RawButton RT1;
    public DriveControlAll driveControlAll;
    

    public RosSharp.RosBridgeClient.MoveSpot drive;

    public RosSharp.RosBridgeClient.SyncMoveSpot syncDrive;

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
    public OVRInput.RawButton driveSwitch;
    public bool curDrive = true;
    public GameObject spotPointer;

    public Image spotPanel;
    public TextMeshProUGUI spotPanelText;
    public Material spotIndicatorMaterial;

    public DepthManager depthManager;
    
    public Transform childObject; 
    public Transform parentObject;
    public bool isSpot2Drive;

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
        if (OVRInput.GetDown(driveSwitch))
        {
            //curDrive = !curDrive;
            driveControlAll.updateState(isSpot2Drive);
            curDrive = driveControlAll.getState_Bool(isSpot2Drive);

            // in sync drive mode
            if(driveControlAll.getState_Int() == 2)
            {
                // only show spot1's pointcloud
                if(!isSpot2Drive) {
                    depthManager.show_spot = curDrive;
                }
                else
                {
                    depthManager.show_spot = false;
                }
            }
            else
            // show own pointcloud
            {
                depthManager.show_spot = curDrive;
            }
        }

        // control the pointer to spot according to selection.
        spotPointer.SetActive(curDrive);

        // control the spot panel's color according to selection.
        if (!curDrive)
        {
            spotPanel.color = new Color(0f, 0f, 0f, 87f / 255f);
            return;
        }
        Color baseColor = spotIndicatorMaterial.GetColor("_Color");
        baseColor.a = 87f / 255f; // Convert 87 to Unity’s 0–1 range
        spotPanel.color = baseColor;

        Vector2 leftMove;
        Vector2 rightMove;
        //Vector3 relativePos;
        //Quaternion relativeRot;
        //Vector3 newPos;
        //Quaternion newRot;
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

            if(isSpot2Drive)
            {
                childObject.SetParent(parentObject);
            }
            // Set movement so only one direction is moved with the left stick at a time
            if (Mathf.Abs(leftMove.x) > Mathf.Abs(leftMove.y)) { leftMove.y = 0; }
            else if (Mathf.Abs(leftMove.y) > Mathf.Abs(leftMove.x)) { leftMove.x = 0; }

            if (driveControlAll.getState_Int() == 2)
            {
                if (!isSpot2Drive)
                {
                    syncDrive.drive(leftMove, rightMove.x, height);
                }
                else
                {
                }
            }
            else
            {
                drive.drive(leftMove, rightMove.x, height);
            }
                
            // Pause depth history for 1.5 seconds
            //foreach (RawImageSubscriber ds in depthSubscribers)
            //{
            //    ds.pauseDepthHistory(1.5f);
            //}
            foreach (DrawMeshInstanced ds in pointClouds)
            {
                ds.continue_update();
            }
        }
        
    }

    void OnDisable()
    {
        if (isSpot2Drive)
        {
            childObject.SetParent(null);
        }
        // when switch to the other mode, hide the spot pointer
        if (spotPointer != null)
            spotPointer.SetActive(false);

        Color baseColor = spotIndicatorMaterial.GetColor("_Color");
        baseColor.a = 87f / 255f; // Convert 87 to Unity’s 0–1 range
        spotPanel.color = baseColor;

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
