using RosSharp.RosBridgeClient;
using UnityEngine;

public class SpotMode : NamedMode
{
    public GameObject rosConnector, dummyGripper;
    public string modeName;
    public Color color;

    public SpotColor spotColor; 
    private ThreadedMoveSpot moveSpot;
    private SetGripper setGripper;
    private bool isGripperOpen = false;

    private ThreadedStowArm stowArm;
    public int CurrentModeIndex { get; private set; } = -1;

    public void Start()
    {
        if (rosConnector != null)
        {
            moveSpot = rosConnector.GetComponent<ThreadedMoveSpot>();
            setGripper = rosConnector.GetComponent<SetGripper>();
            stowArm = rosConnector.GetComponent<ThreadedStowArm>();
            setGripper.closeGripper();
        }
    }
    public void SetCurrentModeIndex(int index)
    {
        CurrentModeIndex = index;
    }

    public void Drive(Vector2 direction)
    {
        print(modeName + " drive: " + direction);
        if (rosConnector != null)
            moveSpot.Move(direction, 0, 0);
    }

    public void Rotate(float direction)
    {
        // print(modeName + " rotate: " + direction);
        if (rosConnector != null)
            moveSpot.Move(Vector2.zero, direction, 0);
    }

    public void SetGripperPos(Transform tf)
    {
        // print(modeName + " move gripper");
        if (dummyGripper == null) return;

        dummyGripper.transform.SetPositionAndRotation(tf.position, tf.rotation);
    }

    public void SetGripperWorldPose(Vector3 position, Quaternion rotation)
    {
        if (dummyGripper == null) return;
        dummyGripper.transform.SetPositionAndRotation(position, rotation);
     }

    public Transform GetGripperPos()
    {
        return dummyGripper.transform;
    }

    public bool GetGripperOpen()
    {
        return isGripperOpen;
    }

    public void SetGripperOpen(bool isGripperOpen)
    {
        this.isGripperOpen = isGripperOpen;
        if (isGripperOpen)
            setGripper.openGripper();
        else
            setGripper.closeGripper();
    }

    public void StowArm()
    {
        if (stowArm != null)
        {
            stowArm.Stow();
        }
        else
        {
            Debug.LogWarning("ThreadedStowArm not found on rosConnector!");
        }
    }
    


    public override string GetName()
    {
        return modeName;
    }
}