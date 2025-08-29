using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;
using MessageTypes = RosSharp.RosBridgeClient.MessageTypes;

public class ThreadedPoseStampedRelativePublisher : ThreadedUnityPublisher<MessageTypes.Geometry.PoseStamped>
{
    public Transform publishedTransform;
    public Transform offset;
    public string frameId = "Unity";

    private MessageTypes.Geometry.PoseStamped message;

    protected override void Start()
    {
        base.Start();
        message = new()
        {
            header = new()
            {
                frame_id = frameId
            }
        };
    }

    // Originally this was called every 0.2 secs but is now run every frame
    // This should be fine with new threading but should still check it doesn't cause slowdown
    public void Update()
    {
        message.header.Update();

        // Go to the dummy finger's position, plus the approximate offset of the end effector
        Vector3 offsetPosition = publishedTransform.localRotation * offset.localPosition;
        Vector3 newLocation = publishedTransform.localPosition + offsetPosition;
        message.pose.position = GetGeometryPoint(newLocation.Unity2Ros());
        message.pose.orientation = GetGeometryQuaternion(publishedTransform.localRotation.Unity2Ros());

        LoopPublish(message);
    }

    private MessageTypes.Geometry.Point GetGeometryPoint(Vector3 position)
    {
        return new()
        {
            x = position.x,
            y = position.y,
            z = position.z
        };
    }

    private MessageTypes.Geometry.Quaternion GetGeometryQuaternion(Quaternion quaternion)
    {
        return new()
        {
            x = quaternion.x,
            y = quaternion.y,
            z = quaternion.z,
            w = quaternion.w
        };
    }
}