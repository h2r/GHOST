using System;
using RosSharp.RosBridgeClient;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class SpotMode : NamedOption
{
    public GameObject rosConnector, dummyGripper;
    public string modeName;
    public Color color;

    private ThreadedMoveSpot moveSpot;
    private ThreadedStowArm stowArm;
    private ThreadedSetGripper setGripper;
    private PoseStampedRelativePublisher armPose;

    private float curHeight = 0f;
    private bool isGripperOpen = false;
    private Vector3 gripperReadyPos;
    private Quaternion gripperReadyRot; 

    public virtual void Start()
    {
        if (rosConnector != null)
        {
            moveSpot = rosConnector.GetComponent<ThreadedMoveSpot>();
            setGripper = rosConnector.GetComponent<ThreadedSetGripper>();
            stowArm = rosConnector.GetComponent<ThreadedStowArm>();
            armPose = rosConnector.GetComponent<PoseStampedRelativePublisher>();

            moveSpot.Move(Vector2.zero, 0, curHeight);
            setGripper.CloseGripper();

            gripperReadyPos = dummyGripper.transform.position;
            gripperReadyRot = dummyGripper.transform.rotation;
        }
    }

    public virtual void SetArmPoseEnabled(bool armPoseEnabled)
    {
        armPose.enabled = armPoseEnabled;
    }

    public virtual void Drive(Vector2 direction)
    {
        print(modeName + " drive: " + direction);
        if (rosConnector != null)
            moveSpot.Move(direction, 0, curHeight);
    }

    public virtual void Rotate(float direction)
    {
        // print(modeName + " rotate: " + direction);
        if (rosConnector != null)
            moveSpot.Move(Vector2.zero, direction, curHeight);
    }

    public virtual void SetHeight(float height)
    {
        curHeight = height;
        print(modeName + " set height: " + curHeight);
        if (rosConnector != null)
            moveSpot.SetHeight(curHeight);
    }

    public virtual void AdjustHeight(float deltaHeight)
    {
        SetHeight(Mathf.Clamp(curHeight + deltaHeight, -0.1f, 0.15f));
    }

    public virtual void SetGripperPos(Transform tf)
    {
        // print(modeName + " move gripper");
        if (dummyGripper == null) return;

        dummyGripper.transform.SetPositionAndRotation(tf.position, tf.rotation);
    }

    public virtual void SetGripperWorldPose(Vector3 position, Quaternion rotation)
    {
        if (dummyGripper == null) return;
        dummyGripper.transform.SetPositionAndRotation(position, rotation);
    }

    public virtual Transform GetGripperPos()
    {
        return dummyGripper.transform;
    }

    public virtual bool GetGripperOpen()
    {
        return isGripperOpen;
    }

    public virtual void SetGripperOpen(bool isGripperOpen)
    {
        this.isGripperOpen = isGripperOpen;
        if (isGripperOpen)
            setGripper.OpenGripper();
        else
            setGripper.CloseGripper();
    }

    public virtual void StowArm()
    {
        if (stowArm != null)
        {
            stowArm.Stow();
            dummyGripper.transform.position = gripperReadyPos;
            dummyGripper.transform.rotation = gripperReadyRot;
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
    
    public override Color GetSelectedColor()
    {
        return color;
    }
}