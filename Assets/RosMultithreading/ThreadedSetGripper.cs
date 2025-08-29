using UnityEngine;
using RosSharp;
using MessageTypes = RosSharp.RosBridgeClient.MessageTypes;

public class ThreadedSetGripper : ThreadedUnityPublisher<MessageTypes.Geometry.Twist>
{
    private MessageTypes.Geometry.Twist message = new();

    protected override void Start()
    {
        base.Start();
        message = new()
        {
            linear = new(),
            angular = new()
        };
    }

    public void OpenGripper()
    {
        SetGripperPercentage(100);
    }

    public void CloseGripper()
    {
        SetGripperPercentage(-10);
    }

    public void SetGripperPercentage(float percent)
    {
        message.linear = new(percent, 0.0f, 0.0f);
        message.angular = GetGeometryVector3(-Vector3.zero.Unity2Ros());
        LoopPublish(message, 3);
    }

    private MessageTypes.Geometry.Vector3 GetGeometryVector3(Vector3 vec)
    {

        return new MessageTypes.Geometry.Vector3()
        {
            x = vec.x,
            y = vec.y,
            z = vec.z
        };
    }
}