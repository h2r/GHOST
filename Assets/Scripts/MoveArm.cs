using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Oculus.Interaction.OVR.Input;
using RosSharp.RosBridgeClient;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class MoveArm : MonoBehaviour
{
    public bool isSpot2Arm;
    public DriveControlAll driveControlAll;
    public RosSharp.RosBridgeClient.PoseStampedRelativePublisher armPublisher; // Reference to RosConnnector's arm publisher
    public GameObject CurrentController; // Reference to right controller object
    public Transform dummyHandTransform; // Reference to dummy hand object
    public Transform realHandTransform; // Reference to real hand

    public OVRInput.RawButton HandTracking; // Changing how input actions are received
    //public OVRInput.RawButton LT1; // Toggle for slow open and close
    public OVRInput.RawAxis2D LAx; // Left joystick controls slow close and open with LT1 toggle
    //public OVRInput.RawButton bButton;
    public Transform spotBody;
    public DrawMeshInstanced[] cloudsToFreeze;
    public TransformUpdater handExtUpdater;
    public JPEGImageSubscriber handImageSubscriber;
    public SetGripper gripper;
    public VRGeneralControls generalControls;
    public RawImageSubscriber[] depthSubscribers; // all depth subscribers except back, because if hand could move in front of a camera, depth history should be off

    // private MessageTypes.Geometry.Twist message;
    private bool triggerWasPressed = false;
    private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
    private Quaternion initialHandRotation = Quaternion.identity;
    private Quaternion initialDummyRotation = Quaternion.identity;
    private Vector3 defaultDummyPos = new Vector3(-1f, -1f, -1f);
    private Quaternion defaultDummyRot = new Quaternion(-1f, -1f, -1f, -1f);

    // Used to change ghost gripper color when it goes out of reach - Are Oelsner
    public Transform armBase;
    public GameObject ghostArm;
    public GameObject ghostFinger;
    public Material greenMaterial;
    public Material redMaterial;
    private bool isGreen = true;
    private float maxArmLength = .73f;


    private bool showSpotBody = false;
    public Material translucentSpotMaterial;
    public Material opaqueSpotMaterial;
    public Material opaqueSpotArmMaterial;
    public Material translucentSpotArmMaterial;

    public Material spot1_indicator;


    public DrawMeshInstanced[] pointClouds;
    public OVRInput.RawButton two3d;
    private float point_cloud_t = 1f;

    /*public bool curDrive = true;
    public DepthManager depthManager;*/

    public VRDriveSpot driveScript;

    // Double-click detection for LT1
    private float lastLT1PressTime = 0f;
    private const float doubleClickInterval = 0.5f; // Adjust as needed
    private bool clickPending = false;

    private bool pointCloudSpotActive;


    private void OnEnable()
    {
        // Set the dummy hand to the same location as the real hand
        dummyHandTransform.position = realHandTransform.position;
        dummyHandTransform.rotation = realHandTransform.rotation;
        showSpotBody = false;
        setSpotVisible(spotBody, showSpotBody);

        pointCloudSpotActive = driveScript.curDrive;
    }

    void Update()
    {
        Vector3 locationChange;
        Quaternion rotationChange;

        if (OVRInput.GetDown(two3d))
        {
            float timeSinceLastClick = Time.time - lastLT1PressTime;

            if (timeSinceLastClick <= doubleClickInterval)
            {
                // Double click detected
                clickPending = false;

                // DOUBLE CLICK ACTION
                point_cloud_t = point_cloud_t == 1f ? 0f : 1f;
                foreach (DrawMeshInstanced cloud in pointClouds)
                {
                    cloud.t = point_cloud_t;
                }
            }
            else
            {
                // Maybe a single click — set flag and wait for possible second click next frame
                clickPending = true;
            }

            lastLT1PressTime = Time.time;
        }

        // Execute single-click if no second click came within interval
        if (clickPending && Time.time - lastLT1PressTime > doubleClickInterval)
        {
            clickPending = false;

            // SINGLE CLICK ACTION
            //pointCloudSpotActive = !pointCloudSpotActive;
            driveControlAll.updateState(isSpot2Arm);
            pointCloudSpotActive = driveControlAll.getState_Bool(isSpot2Arm);
        }
        driveScript.depthManager.show_spot = pointCloudSpotActive;


        // If the trigger is pressed, we want to start tracking the position of the arm and sending it to Spot
        if (OVRInput.Get(HandTracking))
        {
            // If trigger is just now getting pressed, save the location but don't send a command
            // Also turn on the publisher to track the dummy hand
            if (!triggerWasPressed)
            {
                triggerWasPressed = true;
                armPublisher.enabled = true;
                initialHandRotation = CurrentController.transform.rotation;
                initialDummyRotation = dummyHandTransform.rotation;
            }
            else
            {
                // Change the location of the hand the same way
                locationChange = (CurrentController.transform.position - lastHandLocation);
                rotationChange = CurrentController.transform.rotation * Quaternion.Inverse(initialHandRotation);
                dummyHandTransform.position += locationChange;
                dummyHandTransform.rotation = rotationChange * initialDummyRotation;
            }
            lastHandLocation = CurrentController.transform.position;
            // Change ghost gripper color if it is out of bounds            
            if ((dummyHandTransform.position - armBase.position).magnitude > maxArmLength)
            {
                if (isGreen)
                {
                    // Set ghost gripper color to red
                    ghostArm.GetComponent<MeshRenderer>().material = redMaterial;
                    ghostFinger.GetComponent<MeshRenderer>().material = redMaterial;
                    isGreen = false;
                }
            }
            else // Maybe check if color is already green / red before going to swap them
            {
                if (!isGreen)
                {
                    // Set ghost gripper color to green
                    ghostArm.GetComponent<MeshRenderer>().material = greenMaterial;
                    ghostFinger.GetComponent<MeshRenderer>().material = greenMaterial;
                    isGreen = true;
                }
            }

            //// Pause depth history for 1.5 seconds
            //foreach (RawImageSubscriber ds in depthSubscribers)
            //{
            //    ds.pauseDepthHistory(1.5f);
            //}

            foreach (DrawMeshInstanced ds in pointClouds)
            {
                ds.continue_update();
            }
        }
       //// Change the gripper percentage
       // else if (OVRInput.Get(LT1))
       // {
       //     Vector2 leftMove = OVRInput.Get(LAx);
       //     if (leftMove.y < 0)
       //     {
       //         if (generalControls.gripperPercentage > 0)
       //         {
       //             generalControls.gripperPercentage -= 0.25f;
       //             gripper.setGripperPercentage(generalControls.gripperPercentage);
       //             generalControls.gripperOpen = false;
       //         }

       //     }
       //     if (leftMove.y > 0)
       //     {
       //         if (generalControls.gripperPercentage < 100.0f)
       //         {
       //             generalControls.gripperPercentage += 0.25f;
       //             gripper.setGripperPercentage(generalControls.gripperPercentage);
       //             generalControls.gripperOpen = true;
       //         }
       //     }

       // }
        else
        {
            // trigger is not pressed
            triggerWasPressed = false;

            // turn off dummy hand tracking
            armPublisher.enabled = false;
        }

        // Freeze or unfreeze the hand point cloud
        //if (OVRInput.GetDown(bButton))
        //{
        //    //// Switch visibility
        //    //showSpotBody = !showSpotBody;

        //    //// Set invisible or visible
        //    //setSpotVisible(spotBody, showSpotBody);

        //    foreach(DrawMeshInstanced cloud in cloudsToFreeze)
        //    {
        //        cloud.toggleFreezeCloud();
        //    }
        //}
    }

    // Recursive function to get all children of the parent that have the name "unnamed" and are children of "Visuals"
    // Ignores children of arm0.link_wr0 and dummy_arm0.link_wr1
    // and set them active or inactive
    private void setSpotVisible(Transform parent, bool visible)
    {
        foreach (Transform child in parent)
        {
            if (parent.gameObject.name == "unnamed")
            {
                if (child.gameObject.GetComponent<MeshRenderer>() != null)
                {
                    if (!visible)
                    {
                        if (child.gameObject.name.Contains("arm"))
                        {
                            child.gameObject.GetComponent<MeshRenderer>().material = translucentSpotArmMaterial;
                        }
                        else
                        {
                            child.gameObject.GetComponent<MeshRenderer>().material = translucentSpotMaterial;
                        }
                    }
                    else
                    {
                        if (child.gameObject.name.Contains("arm"))
                        {

                            if (child.gameObject.name == "arm0_link_el1" || child.gameObject.name == "arm0_link_wr0")
                            {
                                child.gameObject.GetComponent<MeshRenderer>().material = spot1_indicator;
                            }
                            else
                            {
                                child.gameObject.GetComponent<MeshRenderer>().material = opaqueSpotArmMaterial;
                            }
                        }
                        else
                        {
                            child.gameObject.GetComponent<MeshRenderer>().material = opaqueSpotMaterial;
                        }
                    }
                }
            }
            else if (child.gameObject.name == "arm0.link_wr1" || child.gameObject.name == "dummy_arm0.link_wr1")
            {
                return;
            }
            // child.gameObject.SetActive(visible);

            else
            {
                setSpotVisible(child, visible);
            }
        }
    }

    private void OnDisable()
    {
        armPublisher.enabled = false;
        showSpotBody = false;
        setSpotVisible(spotBody, true);

        driveScript.curDrive = pointCloudSpotActive;

        foreach (DrawMeshInstanced cloud in pointClouds)
        {
            cloud.t = 1f;
        }
    }
}

