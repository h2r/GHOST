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

    public void Start()
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

    public void SetArmPoseEnabled(bool armPoseEnabled)
    {
        armPose.enabled = armPoseEnabled;
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

    public void AdjustHeight(float deltaHeight)
    {
        if (rosConnector != null)
        {
            curHeight += deltaHeight;
            moveSpot.Move(Vector2.zero, 0, curHeight);
        }
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
            setGripper.OpenGripper();
        else
            setGripper.CloseGripper();
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
    
    public override Color GetSelectedColor()
    {
        return color;
    }
}