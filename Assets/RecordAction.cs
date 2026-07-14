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
    private bool canInteract = false;

    [SerializeField] private RosConnector rosConnector;

    // Added leading slash to match how Rosbridge indexes global services
    [SerializeField] private string rosServiceName = "/bag_trigger";

    private void Start()
    {
        // Automatically finds the active RosConnector component 
        if (rosConnector == null)
        {
            Debug.LogError($"RecordAction on {gameObject.name}: Please drag and drop the correct Spot's RosConnector into the inspector field!");
        }

        // Enable button clicks after 0.5 seconds (bypasses startup UI triggers)
        StartCoroutine(EnableInteractionDelay());
    }

    private IEnumerator EnableInteractionDelay()
    {
        yield return new WaitForSeconds(0.5f);
        canInteract = true;
    }

    public override void DoAction(ScoutModeManager modeManager)
    {
        // If we are still in the startup phase, block the action silently
        if (!canInteract) 
        {
            Debug.LogWarning($"RecordAction: Blocked automatic startup trigger from {gameObject.name}.");
            return;
        }

        if (timerActive) return;

        timerActive = true;
        elapsedTime = 0f;
        
        Debug.Log("Button Pressed! Requesting ROS Bag Start...");
        StartCoroutine(WaitAndCallRosBagService(true));
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

    // Coroutine that safely waits for ROS# to finish its network handshake
    private IEnumerator WaitAndCallRosBagService(bool startRecording)
    {
        if (rosConnector == null)
        {
            Debug.LogError("RecordAction: Missing RosConnector reference.");
            yield break;
        }

        // Wait here if the socket is not initialized or still connecting
        float timeout = 5f; // Prevent infinite loop if ROS is completely offline
        float elapsedWait = 0f;
        
        while ((rosConnector.RosSocket == null || !rosConnector.IsConnected.WaitOne(10)) && elapsedWait < timeout)
        {
            elapsedWait += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (rosConnector.RosSocket == null || !rosConnector.IsConnected.WaitOne(10))
        {
            Debug.LogError($"RecordAction: Timed out waiting to connect to ROS. Cannot call service '{rosServiceName}'.");
            timerActive = false; // Reset flag since call failed
            yield break;
        }

        // Socket is ready, proceed with the call
        SetBoolRequest request = new SetBoolRequest(startRecording);
        
        rosConnector.RosSocket.CallService<SetBoolRequest, SetBoolResponse>(
            rosServiceName, 
            OnServiceResponse, 
            request
        );
    }

    // Callback that prints what the Python script replies with
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
        StartCoroutine(WaitAndCallRosBagService(false));
    }

    public override string GetName()
    {
        return (!timerActive) ? $"Record Action" : $"Recording in Progress";
    }

    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}