using UnityEngine;
using System;
using System.Collections;

// 1. Swapped namespaces to ROS# WebSocket handlers
using RosSharp.RosBridgeClient;
using SetBoolRequest = RosSharp.RosBridgeClient.MessageTypes.Std.SetBoolRequest;
using SetBoolResponse = RosSharp.RosBridgeClient.MessageTypes.Std.SetBoolResponse;

public class RecordAction : UIOption
{
    public SpotMode spot;
    public string spot_name = "Spot 1"; 
    public bool timerActive { get; private set; } = false;
    public float elapsedTime { get; private set; } = 0f;
    private const float maxRecordingTime = 600f; // 10 min to complete task
    
    // 2. Changed to RosConnector
    private RosConnector rosConnector;

    // Added leading slash to match how Rosbridge indexes global services
    [SerializeField] private string rosServiceName = "/bag_trigger";

    private void Start()
    {
        // Automatically finds the active RosConnector component in your Unity scene
        rosConnector = FindObjectOfType<RosConnector>();
        if (rosConnector == null)
        {
            Debug.LogError("RecordAction: Could not find RosConnector component in the scene!");
        }
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
        if (rosConnector == null || rosConnector.RosSocket == null)
        {
            Debug.LogError("RecordAction: Cannot call service, ROS# is not connected.");
            return;
        }

        SetBoolRequest request = new SetBoolRequest(startRecording);
        
        // 3. Updated to ROS# service calling execution structure
        rosConnector.RosSocket.CallService<SetBoolRequest, SetBoolResponse>(
            rosServiceName, 
            OnServiceResponse, 
            request
        );
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