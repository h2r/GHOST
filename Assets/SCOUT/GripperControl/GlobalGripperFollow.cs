using UnityEngine;

public class WorldLocalGripperSync : MonoBehaviour
{
    /// <summary>
    /// Sometimes we issue gripper commands in the world frame, sometimes in the local frame.
    /// Therefore, it is often useful to have two dummy grippers, one in each frame, and sync them.
    /// This script does exactly that.
    /// </summary>
    public GameObject dummyGripperInWorldFrame;
    public GameObject dummyGripperInLocalFrame;
    public GameObject robotObject;

    private Transform lastRobotTransform;

    // generally there are three use cases:
    // 1. useWorldGripper = false: 
    //          you use the gripper and never change body pose while moving the gripper.
    // 2. useWorldGripper = true, worldGripperFollowNavigation = false: 
    //          you use the gripper and the body navigates to follow the trajectory of the gripper.
    // 3. useWorldGripper = true, worldGripperFollowNavigation = true:
    //          1. When you use the gripper, the body don't move to follow the gripper, 
    //              only pitches / adjusts height to ensure the max range of gripper reachability.
    //          2. When the body navigates, the grippers will follow the body movement.

    // whether to use the world frame gripper as reference for gripper commands.
    // the body-reference gripper will follow the world-reference gripper when this is true, and vice versa.
    [HideInInspector]
    public bool useWorldGripper = false;

    // whether the world frame gripper should follow the robot navigation (i.e., move with the robot body)
    [HideInInspector]
    public bool worldGripperFollowBody = false;

    void Start()
    {
        if (robotObject == null)
        {
            Debug.LogError("[GripperFollow] Robot object is not assigned.");
            return;
        }
        lastRobotTransform = robotObject.transform;
    }

    void SwitchGripperReference(string reference)
    {
        if (reference == "world")
            useWorldGripper = true;
        else if (reference == "local")
            useWorldGripper = false;
        else
            Debug.LogError("[GripperFollow] Invalid reference frame specified. Use 'world' or 'local'.");
    }

    void Update()
    {
        if (robotObject == null)
            return;

        // If using world gripper and it should follow navigation, update its position based on robot movement
        if (dummyGripperInWorldFrame == null || dummyGripperInLocalFrame == null)
            return;
        else if (!useWorldGripper || worldGripperFollowBody)
        {
            // Update the world frame gripper position and rotation
            dummyGripperInWorldFrame.transform.position = dummyGripperInLocalFrame.transform.position;
            dummyGripperInWorldFrame.transform.rotation = dummyGripperInLocalFrame.transform.rotation;
        }
        else
        {
            // Update the local frame gripper position and rotation
            dummyGripperInLocalFrame.transform.position = dummyGripperInWorldFrame.transform.position;
            dummyGripperInLocalFrame.transform.rotation = dummyGripperInWorldFrame.transform.rotation;
        }
        lastRobotTransform = robotObject.transform;
    }
}
