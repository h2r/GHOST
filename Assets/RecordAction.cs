using UnityEngine;
using System;
using System.Collections;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class RecordAction : UIOption
{
    public SpotMode spot;
    public string spot_name = "Spot 1"; //may or not need these; grabbed from StowArmsButton
    public bool timerActive { get; private set; } = false;
    public float elapsedTime { get; private set; } = 0f;
    private const float maxRecordingTime = 600f; //10 min to complete task
    private ROSConnection ros;

    [SerializeField] private string rosServiceName = "bag_trigger";

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<SetBoolRequest, SetBoolResponse>(rosServiceName);
    }


    public override void DoAction(ScoutModeManager modeManager)
    {
        if (timerActive) return;

        timerActive = true;
        elapsedTime = 0f;
        
        Debug.Log("Button Pressed! Requesting ROS Bag Start...");
        CallRosBagService(true);
    }

    private void Update()
    {
        if (!timerActive) return; 

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= maxRecordingTime) 
        {
            StopRecording();
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

    public void StopRecording()
    {
        timerActive = false;
        elapsedTime = 0f;

        Debug.Log("Timeout or manual trigger! Requesting ROS Bag Stop...");
        CallRosBagService(false);
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

