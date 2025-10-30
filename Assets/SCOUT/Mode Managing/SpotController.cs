using System;
using RosSharp.RosBridgeClient;
using UnityEngine;

public class SpotController : NamedOption
{
    public GameObject rosConnector, dummyGripper, readyDummyGripper, worldDummyGripper, armBase;
    public GameObject actualGripper;
    public Material greenMaterial, redMaterial;
    public string displayName;
    public string robotName;
    public Color color;

    public enum GripperOperateMode
    {
        LOCAL,
        BODY_ASSIST,
        BODY_FOLLOW
    }
    public GripperOperateMode gripperOperateMode = GripperOperateMode.LOCAL;

    private ThreadedMoveSpot moveSpot;
    private ThreadedStowArm stowArm;
    private ThreadedSetGripper setGripper;

    private SetHeight setHeight;

    protected float curHeight = 0f;
    private bool isGripperOpen = false;
    private float lastHeightChangeTime = -Mathf.Infinity;
    private const float MIN_HEIGHT_CHANGE_INTERVAL = 0.5f;
    private WorldLocalGripperSync worldLocalGripperSync;
    private PoseStampedRelativePublisher localGripperCmd;
    private PoseStampedRelativeGlobalPublisher worldGripperCmd;

    // Keeps track of current actual gripper reference.
    // When using world gripper, the manipulation gripper reference is WORLD. 
    // It might be desirable to switch it back to BODY when switching back to navigation mode.
    enum GripperReference
    {
        BODY,
        WORLD
    }
    private GripperReference currentGripperReference = GripperReference.BODY;
    private bool lastArmPosePublisherEnabled = false;

    public virtual void Start()
    {
        if (rosConnector != null)
        {
            moveSpot = rosConnector.GetComponent<ThreadedMoveSpot>();
            setGripper = rosConnector.GetComponent<ThreadedSetGripper>();
            stowArm = rosConnector.GetComponent<ThreadedStowArm>();
            setHeight = rosConnector.GetComponent<SetHeight>();
            worldLocalGripperSync = rosConnector.GetComponent<WorldLocalGripperSync>();
            localGripperCmd = rosConnector.GetComponent<PoseStampedRelativePublisher>();
            worldGripperCmd = rosConnector.GetComponent<PoseStampedRelativeGlobalPublisher>();

            moveSpot.Move(Vector2.zero, 0, curHeight);
            setGripper.CloseGripper();

            // update the global gripper topic
            if (worldGripperCmd != null && gripperOperateMode == GripperOperateMode.BODY_ASSIST)
                worldGripperCmd.Topic = $"/{robotName}/arm_pose_body_assist_commands";
            else if (localGripperCmd != null && gripperOperateMode == GripperOperateMode.BODY_FOLLOW)
                localGripperCmd.Topic = $"/{robotName}/arm_pose_body_follow_commands";
        }
    }

    public virtual void SetArmPoseEnabled(bool armPoseEnabled)
    {
        // enable the arm pose publisher only when one of the control modes requires it
        print(displayName + " arm pose " + armPoseEnabled);
        if (lastArmPosePublisherEnabled == armPoseEnabled)
            return;

        // disable publishing arm pose when arm pose is not enabled
        if (gripperOperateMode == GripperOperateMode.LOCAL)
        {
            UpdateGripperReference(GripperReference.BODY);
            worldGripperCmd.enabled = false;
            localGripperCmd.enabled = armPoseEnabled;
        }
        else if (gripperOperateMode == GripperOperateMode.BODY_ASSIST)
        {
            UpdateGripperReference(armPoseEnabled ? GripperReference.WORLD : GripperReference.BODY);
            worldGripperCmd.enabled = armPoseEnabled;
            localGripperCmd.enabled = false;
        }
        else
        {
            UpdateGripperReference(armPoseEnabled ? GripperReference.WORLD : GripperReference.BODY);
            localGripperCmd.enabled = armPoseEnabled;
            worldGripperCmd.enabled = false;
        }
        lastArmPosePublisherEnabled = armPoseEnabled;
    }

    private void UpdateGripperReference(GripperReference newReference)
    {
        // change the gripper reference when the body becomes moving or manipulation starts
        if (currentGripperReference != newReference)
        {
            if (newReference == GripperReference.WORLD)
            {
                worldGripperCmd.enabled = true;
                worldGripperCmd.SendUpdate();
                localGripperCmd.enabled = false;
                worldLocalGripperSync.worldGripperFollowBody = false;
            }
            else
            {
                localGripperCmd.enabled = true;
                localGripperCmd.SendUpdate();
                worldGripperCmd.enabled = false;
                worldLocalGripperSync.worldGripperFollowBody = true;
            }
            currentGripperReference = newReference;
            Debug.Log("Gripper reference changed to: " + newReference.ToString());
        }
    }

    private void OnMoveUpdateGripperReference()
    {
        UpdateGripperReference(GripperReference.BODY);
    }

    private void OnManipulateUpdateGripperReference()
    {
        if (gripperOperateMode != GripperOperateMode.LOCAL)
            UpdateGripperReference(GripperReference.WORLD);
        else
            UpdateGripperReference(GripperReference.BODY);
    }

    public virtual void Drive(Vector2 direction)
    {
        OnMoveUpdateGripperReference();
        if (rosConnector != null)
            moveSpot.Move(direction, 0, curHeight);
    }

    public virtual void Rotate(float direction)
    {
        OnMoveUpdateGripperReference();
        if (rosConnector != null)
            moveSpot.Move(Vector2.zero, direction, curHeight);
    }

    public virtual void SetHeight(float height)
    {
        OnMoveUpdateGripperReference();
        curHeight = height;
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
        OnManipulateUpdateGripperReference();
        bool useWorldDummyGripper = gripperOperateMode != GripperOperateMode.LOCAL;
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
        OnMoveUpdateGripperReference(); // stow is considered a move action because it disengages the arm from manipulation
        if (stowArm != null)
        {
            worldLocalGripperSync.worldGripperFollowBody = true; // disable world gripper when stowing arm
            stowArm.Stow();
            dummyGripper.transform.position = readyDummyGripper.transform.position;
            dummyGripper.transform.rotation = readyDummyGripper.transform.rotation;
        }
        else
        {
            Debug.LogWarning("ThreadedStowArm not found on rosConnector!");
        }

        // change the last control type to navigation to avoid relocking the gripper to body (it's already locked to body)
    }

    public override string GetName()
    {
        return displayName;
    }
    
    public override Color GetSelectedColor()
    {
        return color;
    }
}