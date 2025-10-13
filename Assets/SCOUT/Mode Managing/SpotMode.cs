using System;
using RosSharp.RosBridgeClient;
using UnityEngine;

public class SpotMode : NamedOption
{
    public GameObject rosConnector, dummyGripper, readyDummyGripper, worldDummyGripper, armBase;
    public Material greenMaterial, redMaterial;
    public string modeName;
    public Color color;

    public bool useWorldDummyGripper = false;

    private ThreadedMoveSpot moveSpot;
    private ThreadedStowArm stowArm;
    private ThreadedSetGripper setGripper;

    private SetHeight setHeight;

    protected float curHeight = 0f;
    private bool isGripperOpen = false;
    private float lastHeightChangeTime = -Mathf.Infinity;
    private const float MIN_HEIGHT_CHANGE_INTERVAL = 0.5f;
    private WorldLocalGripperSync worldLocalGripperSync;
    private PoseStampedRelativePublisher enableLocalGripperCmd;
    private PoseStampedRelativeGlobalPublisher enableWorldGripperCmd;
    public virtual void Start()
    {
        if (rosConnector != null)
        {
            moveSpot = rosConnector.GetComponent<ThreadedMoveSpot>();
            setGripper = rosConnector.GetComponent<ThreadedSetGripper>();
            stowArm = rosConnector.GetComponent<ThreadedStowArm>();
            setHeight = rosConnector.GetComponent<SetHeight>();
            worldLocalGripperSync = rosConnector.GetComponent<WorldLocalGripperSync>();
            enableLocalGripperCmd = rosConnector.GetComponent<PoseStampedRelativePublisher>();
            enableWorldGripperCmd = rosConnector.GetComponent<PoseStampedRelativeGlobalPublisher>();

            moveSpot.Move(Vector2.zero, 0, curHeight);
            setGripper.CloseGripper();
        }
    }

    public virtual void SetArmPoseEnabled(bool armPoseEnabled)
    {
        print(modeName + " arm pose " + armPoseEnabled);

        // disable publishing arm pose when arm pose is not enabled
        if (!armPoseEnabled)
        {
            worldLocalGripperSync.useWorldGripper = false; // disable world gripper when arm pose is not enabled
            enableWorldGripperCmd.enabled = false;
            enableLocalGripperCmd.enabled = false;
        }
        else if (useWorldDummyGripper)
        {
            enableWorldGripperCmd.enabled = armPoseEnabled;
            enableLocalGripperCmd.enabled = false;
        }
        else
        {
            enableLocalGripperCmd.enabled = armPoseEnabled;
            enableWorldGripperCmd.enabled = false;
        }
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
            Debug.Log("Height: rosconnector connected sending height to setheight.cs");
            setHeight.SetHeightPercentage(curHeight);
    }

    public virtual void AdjustHeight(float deltaHeight)
    {
        if (Time.time - lastHeightChangeTime < MIN_HEIGHT_CHANGE_INTERVAL) return;
        lastHeightChangeTime = Time.time;
        SetHeight(Mathf.Clamp(curHeight + deltaHeight, -0.15f, 0.15f));
    }

    public virtual void SetGripperTf(Transform tf)
    {
        SetGripperWorldPose(tf.position, tf.rotation);
    }

    public virtual void SetGripperWorldPose(Vector3 position, Quaternion rotation)
    {
        worldLocalGripperSync.useWorldGripper = useWorldDummyGripper;

        GameObject GripperToUse = useWorldDummyGripper ? worldDummyGripper : dummyGripper;
        GripperToUse.transform.SetPositionAndRotation(position, rotation);

        if (!useWorldDummyGripper)
        {
            var armLength = (GripperToUse.transform.position - armBase.transform.position).magnitude;
            var dummyMaterial = armLength > 0.73 ? redMaterial : greenMaterial;
            foreach (var mr in GripperToUse.GetComponentsInChildren<MeshRenderer>())
                mr.material = dummyMaterial;
        }
    }

    public virtual Transform GetGripperPos()
    {
        return worldDummyGripper.transform;
    }

    public virtual bool GetGripperOpen()
    {
        return isGripperOpen;
    }

    public virtual void SetGripperOpen(bool isGripperOpen)
    {
        bool changed = this.isGripperOpen != isGripperOpen;
        this.isGripperOpen = isGripperOpen;
        if (isGripperOpen && changed)
            setGripper.OpenGripper();
        else if (!isGripperOpen && changed)
            setGripper.CloseGripper();
    }

    public virtual void StowArm()
    {
        if (stowArm != null)
        {
            worldLocalGripperSync.useWorldGripper = false; // disable world gripper when stowing arm
            stowArm.Stow();
            dummyGripper.transform.position = readyDummyGripper.transform.position;
            dummyGripper.transform.rotation = readyDummyGripper.transform.rotation;
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