using System;
using RosSharp.RosBridgeClient;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MultiSpotMode : SpotMode
{
    public GameObject rosConnectorOne, rosConnectorTwo;

    private ThreadedSyncMoveSpot moveSpotOne, moveSpotTwo;

    private float curHeight;

    public override void Start()
    {
        if (rosConnectorOne != null)
        {
            moveSpotOne = rosConnectorOne.GetComponent<ThreadedSyncMoveSpot>();

            moveSpotOne.Move(Vector2.zero, 0, curHeight);
        }
        if (rosConnectorTwo != null)
        {
            moveSpotTwo = rosConnectorTwo.GetComponent<ThreadedSyncMoveSpot>();

            moveSpotTwo.Move(Vector2.zero, 0, curHeight);
        }
    }

    public override void SetArmPoseEnabled(bool armPoseEnabled)
    {}

    public override void Drive(Vector2 direction)
    {
        print(modeName + " drive: " + direction);
        if (rosConnectorOne != null)
            moveSpotOne.Move(direction, 0, curHeight);
        if (rosConnectorTwo != null)
            moveSpotTwo.Move(direction, 0, curHeight);
    }

    public override void Rotate(float direction)
    {
        // print(modeName + " rotate: " + direction);
        if (rosConnectorOne != null)
            moveSpotOne.Move(Vector2.zero, direction, curHeight);
        if (rosConnectorTwo != null)
            moveSpotTwo.Move(Vector2.zero, direction, curHeight);
    }

    public override void SetHeight(float height)
    {
        curHeight = height;
        print(modeName + " set height: " + curHeight);
        if (rosConnectorOne != null)
            moveSpotOne.SetHeight(curHeight);
        if (rosConnectorTwo != null)
            moveSpotTwo.SetHeight(curHeight);
    }

    public override void AdjustHeight(float deltaHeight)
    {
        SetHeight(Mathf.Clamp(curHeight + deltaHeight, -0.1f, 0.15f));
    }

    public override void SetGripperPos(Transform tf)
    {
        // not implemented on multi spot
    }

    public override void SetGripperWorldPose(Vector3 position, Quaternion rotation)
    {
        // not implemented on multi spot
    }

    public override Transform GetGripperPos()
    {
        // not implemented on multi spot
        return null;
    }

    public override bool GetGripperOpen()
    {
        // not implemented on multi spot
        return false;
    }

    public override void SetGripperOpen(bool isGripperOpen)
    {
        // not implemented on multi spot
    }

    public override void StowArm()
    {
        // not implemented on multi spot
    }

    public override string GetName()
    {
        return modeName;
    }
    
    public override Color GetSelectedColor()
    {
        return color;
    }
}