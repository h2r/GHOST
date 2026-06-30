using UnityEngine;
using System;
using System.Collections;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class StopRecording : UIOption
{
    public SpotMode spot;
    public string spot_name = "Spot 1"; //may or not need these; grabbed from StowArmsButton    private ROSConnection ros;

    [SerializeField] private string rosServiceName = "bag_trigger";

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<SetBoolRequest, SetBoolResponse>(rosServiceName);
    }


    public override void DoAction(ScoutModeManager modeManager)
    {
       if (RecordAction.timerActive)
        {
            RecordAction.timerActive = false;
            RecordAction.elapsedTime = 0f;
            Debug.Log("Manual trigger! Requesting ROS Bag Stop...");
            CallRosBagService(false);
        }
    }

    

    // Helper method to dispatch the async service call
    private void CallRosBagService(bool startRecording)
    {
        SetBoolRequest request = new SetBoolRequest(startRecording);
        ros.SendServiceMessage<SetBoolResponse>(rosServiceName, request, OnServiceResponse);
    }

    // Callback that prints what your Python script replies with
    private void OnServiceResponse(SetBoolResponse response)
    {
        if (response.success)
        {
            Debug.Log($"[ROS Success]: {response.message}");
        }
        else
        {
            Debug.LogError($"[ROS Failure]: {response.message}");
        }
    }

    public override string GetName()
    {
        return $"Record Action";
    }

    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}

