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

    public bool useWorldGripper = false;

    void Start()
    {
        dummyGripperInWorldFrame.transform.position=dummyGripperInLocalFrame.transform.position;
         dummyGripperInWorldFrame.transform.rotation=dummyGripperInLocalFrame.transform.rotation;
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
        else if (dummyGripperInWorldFrame == null || dummyGripperInLocalFrame == null)
            return;
        else if (!useWorldGripper)
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
    }
}
