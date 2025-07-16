using RosSharp.RosBridgeClient;
using UnityEngine;

public class SpotMode : NamedMode
{
    public GameObject rosConnector, dummyGripper;
    public string modeName;

    private MoveSpot moveSpot;

    public void Start()
    {
        if (rosConnector != null)
            moveSpot = rosConnector.GetComponent<MoveSpot>();
    }

    public void Drive(Vector2 direction)
    {
        print(modeName + " drive: " + direction);
        if (rosConnector != null)
            moveSpot.drive(direction, 0, 0);
    }

    public void SetGripperPos(Transform tf)
    {
        print(modeName + " move gripper");
        if (dummyGripper != null)
            dummyGripper.transform.SetPositionAndRotation(tf.position, tf.rotation);
    }

    public override string GetName()
    {
        return modeName;
    }
}