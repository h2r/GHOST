using RosSharp.RosBridgeClient;
using UnityEngine;

public class SpotMode : NamedMode
{
    public GameObject rosConnector, dummyGripper;
    public string modeName;
    public Color color;

    private ThreadedMoveSpot moveSpot;
    private SetGripper setGripper;

    private bool isGripperOpen = false;

    public void Start()
    {
        if (rosConnector != null)
        {
            moveSpot = rosConnector.GetComponent<ThreadedMoveSpot>();
            setGripper = rosConnector.GetComponent<SetGripper>();
            setGripper.closeGripper();
        }
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
        if (dummyGripper != null)
            dummyGripper.transform.SetPositionAndRotation(tf.position, tf.rotation);
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

    public override string GetName()
    {
        return modeName;
    }
}