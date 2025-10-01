using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using System;

[RequireComponent(typeof(RosConnector))]
public class ThreadedSetGripper : MonoBehaviour
{
    private RosConnector rosConnector;
    public string serviceName = "/set_gripper_angle";

    void Start()
    {
        rosConnector = GetComponent<RosConnector>();
    }

    public void OpenGripper()
    {
        SetGripperAngle(90f); // Open gripper to 90 degrees
    }

    public void CloseGripper()
    {
        SetGripperAngle(0f); // Close gripper to 0 degrees
    }

    public void SetGripperAngle(float angle)
    {
        float clampedAngle = Mathf.Clamp(angle, 0f, 90f);
        var request = new Float32 { data = clampedAngle };
        Debug.Log($"Requested gripper angle: {request.data}");

        // rosConnector.RosSocket.CallService<Float32, RosSharp.RosBridgeClient.MessageTypes.Std.SetBoolResponse>(
        //     serviceName,
        //     response => Debug.Log($"Gripper Service response received: success={response.success}, message={response.message}"),
        //     request
        // );

        rosConnector.RosSocket.CallService<Float32, Tuple<bool, string>>( // reponse type bool success; string message
            serviceName,
            response => Debug.Log($"Gripper Service response received: success={response.Item1}, message={response.Item2}"),
            request
        );

        Debug.Log($"Requested to set gripper angle to {clampedAngle} degrees");
    }
}


// public class ThreadedSetGripper : ThreadedUnityPublisher<MessageTypes.Geometry.Twist>
// {
//     private MessageTypes.Geometry.Twist message = new();

//     protected override void Start()
//     {
//         base.Start();
//         message = new()
//         {
//             linear = new(),
//             angular = new()
//         };
//     }

//     public void OpenGripper()
//     {
//         SetGripperPercentage(100);
//     }

//     public void CloseGripper()
//     {
//         SetGripperPercentage(-10);
//     }

//     public void SetGripperPercentage(float percent)
//     {
//         message.linear = new(percent, 0.0f, 0.0f);
//         message.angular = GetGeometryVector3(-Vector3.zero.Unity2Ros());
//         LoopPublish(message, 3);
//     }

//     private MessageTypes.Geometry.Vector3 GetGeometryVector3(Vector3 vec)
//     {

//         return new MessageTypes.Geometry.Vector3()
//         {
//             x = vec.x,
//             y = vec.y,
//             z = vec.z
//         };
//     }
// }