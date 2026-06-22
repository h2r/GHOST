using UnityEngine;
using System;
using System.Collections;

public class RecordAction : UIOption
{
    public SpotMode spot;
    public string spot_name = "Spot 1"; //may or not need these; grabbed from StowArmsButton
    private bool timerActive;
    private float elapsedTime;
    private const float maxRecordingTime = 900f; //15 min to complete task


    public override void DoAction(ScoutModeManager modeManager)
    {
       timerActive = true;
       elapsedTime = 0f;
       DebugLog("Button Pressed!"); //remove once actual logic is here
    }

    private void Update()
    {
        if (!timerActive) return; 

        elapsedTime += Time.deltaTime;

        //put ROS logic here later
        DebugLog("Pretend I'm recording..."); //remove for actual logic
        if (elapsedTime >= maxRecordingTime) 
        {
            StopRecording();
        }
    }

    private void StopRecording()
    {
        timerActive = false;
        elapsedTime = 0f;
        
        //put ending logic here
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
