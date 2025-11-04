using System;
using RosSharp.RosBridgeClient;
using UnityEngine;

public class SpotMode : NamedOption
{
    public GameObject rosConnector, dummyGripper, readyDummyGripper, worldDummyGripper, armBase;
    public GameObject actualGripper;
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
            stowArm.Stow();
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
    }

    public void ChangeGripperColorBasedOnDistance()
    {
        GameObject gripperToUse = dummyGripper; // in world gripper mode, dummy gripper will still follow the world gripper
        // if the actual gripper location deviates too far from the arm base, change its color to red, meaning it's not ready.
        // get distance from arm base to dummy gripper + rotation offset

        // Compute position difference in meters
        Vector3 positionDifference = dummyGripper.transform.position - actualGripper.transform.position;
        float distanceMeters = positionDifference.magnitude;

        // Compute orientation difference in angles (degrees)
        float angleDegrees = Quaternion.Angle(actualGripper.transform.rotation, dummyGripper.transform.rotation);

        Debug.Log($"Position difference: {distanceMeters:F3} meters, Orientation difference: {angleDegrees:F1} degrees");

        Material targetMaterial = distanceMeters > 0.01f || angleDegrees > 5f ? redMaterial : greenMaterial;

        foreach (var mr in gripperToUse.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.sharedMaterial != targetMaterial)
                mr.material = targetMaterial;
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