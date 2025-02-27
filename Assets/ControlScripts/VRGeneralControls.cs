using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using RosSharp.RosBridgeClient;
using Unity.VisualScripting;

public class VRGeneralControls : MonoBehaviour
{

    //public Canvas UI;
    //public Canvas hintUI;

    //public OVRInput.RawButton LX;
    //public OVRInput.RawButton LT1;
    public OVRInput.RawButton Stow;
    public OVRInput.RawButton ModeSwitch;
    public OVRInput.RawButton GripperControl;

    public Transform dummyHandTransform; // Reference to dummy hand object
    public Transform realHandTransform; // Reference to real hand

    /* To be accessed by MoveArm script */
    public bool gripperOpen;
    public float gripperPercentage;

    public RosSharp.RosBridgeClient.KillMotor killSpot;
    public RosSharp.RosBridgeClient.StowArm stow;
    public RosSharp.RosBridgeClient.SetGripper gripper;
    public ModeManager manager;

    /* Toggle Point Cloud - not using for multispot project 2/12/2025 */
    //public GameObject body;
    //public DrawMeshInstanced[] pointClouds;
    //public GameObject[] toggleObjects;
    //private float point_cloud_t;

    /* Raw Image Subscribers */
    public RawImageSubscriber[] depthSubscribers;

    /* Track time in 2D vs 3D fields */
    private Stopwatch threed_time;
    private Stopwatch twod_time;

    void Start()
    {
        gripperOpen = false;
        //gripper.closeGripper();
        //hintUI.enabled = true;
        //UI.enabled = false;
        //UIShowing = false;
        gripperPercentage = 0f;

        //point_cloud_t = 1;
        //threed_time = new Stopwatch();
        //twod_time = new Stopwatch();


        UnityEngine.Debug.Log("vr general Start");


    }

    void Update()
    {
        ///* LX commands */
        //if (OVRInput.GetDown(LX))
        //{
        //    /* Toggle every point cloud */
        //    point_cloud_t = point_cloud_t == 1f ? 0f : 1f;
        //    foreach (DrawMeshInstanced cloud in pointClouds)
        //    {
        //        cloud.t = point_cloud_t;
        //    }

        //    /* Toggle whether the left and right are enabled at all */
        //    foreach (GameObject gameObject in toggleObjects)
        //    {
        //        gameObject.SetActive(point_cloud_t == 0f);
        //    }

        //    if (point_cloud_t == 1f)
        //    {
        //        /* Turn on 3D stopwatch */
        //        twod_time.Stop();
        //        threed_time.Start();
        //    }
        //    else
        //    {
        //        /* Turn on 2D stopwatch */
        //        threed_time.Stop();
        //        twod_time.Start();
        //    }

        //    /* Kill left gripper (LT1) and left trigger (LT2) are also pressed */
        //    if (OVRInput.Get(LT1) && OVRInput.Get(LT2))
        //    {
        //        killSpot.killSpot();
        //    }
        //}

        /* Stow arm if left trigger (LT2) is pressed */
        if (OVRInput.GetDown(Stow))
        {

            stow.Stow();

            // Set the dummy hand to the same location as the real hand  (click again to reset dummyhand after stow)
            dummyHandTransform.position = realHandTransform.position;
            dummyHandTransform.rotation = realHandTransform.rotation;

            //// Pause depth history for 1.5 seconds
            //foreach (RawImageSubscriber ds in depthSubscribers)
            //{
            //    ds.pauseDepthHistory(1.5f);
            //}
        }

        /* Switch modes if A is pressed */
        if (OVRInput.GetDown(ModeSwitch))
        {
            manager.nextMode();
        }

        /* Fully open/close gripper if right trigger (RT2) is pressed */
        if (OVRInput.GetDown(GripperControl))
        {
            if (gripperOpen)
            {
                gripper.closeGripper();
                gripperPercentage = 0f;
                gripperOpen = false;
            }
            else
            {
                gripper.openGripper();
                gripperPercentage = 100f;
                gripperOpen = true;
            }
        }

    }

    public void toggleUI()
    {
        //UI.enabled = !(UI.enabled);
        //hintUI.enabled = !(hintUI.enabled);
        //UIShowing = !UIShowing;
    }

    /* Start the first stopwatch */
    public void beginTime()
    {
        //threed_time.Start();
    }

    private void OnApplicationQuit()
    {
        /* Log time */
        //UnityEngine.Debug.Log("2D mode time elapsed: " + System.Math.Round(twod_time.Elapsed.TotalSeconds, 2) + " seconds");
        //UnityEngine.Debug.Log("3D mode time elapsed: " + System.Math.Round(threed_time.Elapsed.TotalSeconds, 2) + " seconds");
    }
}
