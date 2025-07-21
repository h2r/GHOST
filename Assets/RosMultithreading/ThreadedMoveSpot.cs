using UnityEngine;
using RosSharp;
using MessageTypes = RosSharp.RosBridgeClient.MessageTypes;

public class ThreadedMoveSpot : ThreadedUnityPublisher<MessageTypes.Geometry.Twist>
{
    public void Move(Vector2 drive, float rotate, float height)
    {
        if (drive.magnitude != 0 || rotate != 0 || height != 0)
        {
            MessageTypes.Geometry.Twist message = new()
            {
                linear = GetGeometryVector3(new Vector3(drive[0], height, drive[1]).Unity2Ros()),
                angular = GetGeometryVector3(new Vector3(0, rotate, 0).Unity2Ros())
            };
            LoopPublish(message);
        }
        else
        {
            LoopUnpublish();
        }
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