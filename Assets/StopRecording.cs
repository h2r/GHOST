using UnityEngine;
using System;
using System.Collections;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class StopRecording : UIOption
{
    public SpotMode spot;
    public string spot_name = "Spot 1"; //may or not need these; grabbed from StowArmsButton 
    public RecordAction recordAction;

    public override void DoAction(ScoutModeManager modeManager)
    {
        if (recordAction != null && recordAction.timerActive && recordAction.elapsedTime > 0)
        {   Debug.Log($"Elapsed time: {recordAction.elapsedTime}");
            Debug.Log("Stop Recording Button Pressed! Requesting ROS Bag Stop...");
            recordAction.StopRecording();
            Debug.Log($"Reset Time: {recordAction.elapsedTime}");
        }
        else
        {
            Debug.Log("Elapsed time is not being updated :(");
        }
    }

    public override string GetName()
    {
        return $"Stop Recording";
    }

    public override Color GetSelectedColor()
    {
        return Color.white;
    }
}

