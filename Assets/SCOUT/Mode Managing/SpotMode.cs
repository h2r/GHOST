using RosSharp.RosBridgeClient;
using UnityEngine;

public class SpotMode : NamedOption
{
    public GameObject rosConnector, dummyGripper, readyDummyGripper, armBase;
    public Material greenMaterial, redMaterial;
    public string modeName;
    public Color color;

    private ThreadedMoveSpot moveSpot;
    private ThreadedStowArm stowArm;
    private ThreadedSetGripper setGripper;
    private PoseStampedRelativePublisher armPose;

    protected float curHeight = 0f;
    private bool isGripperOpen = false;
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

    public virtual void SetGripperTf(Transform tf)
    {
        SetGripperWorldPose(tf.position, tf.rotation);
    }

    public virtual void SetGripperWorldPose(Vector3 position, Quaternion rotation)
    {
        dummyGripper.transform.SetPositionAndRotation(position, rotation);

        var armLength = (dummyGripper.transform.position - armBase.transform.position).magnitude;
        var dummyMaterial = armLength > 0.73 ? redMaterial : greenMaterial;
        foreach (var mr in dummyGripper.GetComponentsInChildren<MeshRenderer>())
            mr.material = dummyMaterial;
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