using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Diagnostics;
using RosSharp.RosBridgeClient;

public class ModeManager : MonoBehaviour
{
    [SerializeField]
    public int currMode; // currMode is set to value from 0-modes.Count      
    public List<ControlMode> modes;
    //public List<TextMeshProUGUI> UICanvases;
    public string UISpotID;
    public TextMeshProUGUI curModeTextMesh;
    public VRGeneralControls generalControlsScript;
    public SetFarPlane planePublisher;

    // Time tracking
    private Stopwatch[] stopwatches;
    private bool timeStarted;

    // Disable modes to at the beginning, user has to choose to enter into a mode
    // TODO: Default mode: LOOK THIS UP
    void Start()
    {
        //stopwatches = new Stopwatch[modes.Count];
        //for (int i = 0; i < modes.Count; i++)
        //{
        //    stopwatches[i] = new Stopwatch();
        //}
        //currMode = modes.Count - 1; // Second to last mode

        currMode = -1; // LOOK THIS UP

        for (int i = 0; i < modes.Count; i++) { 
            modes[i].disableMode();
        }

        //timeStarted = false;
        UnityEngine.Debug.Log("currentMode"+ currMode);
    }

    void Update()
    {
        if (Input.GetKeyDown("t"))
            nextMode();
    }


    public void switchToMode(int newMode)
    {
        if (currMode != -1) // only when the pre mode is a actual mode, then disable it.
        {
            modes[currMode].disableMode();
        }

        ////disable previous mode
        //ControlMode mode = modes[currMode];
        //mode.disableMode();

        currMode = newMode;
        modes[currMode].enableMode();

        if (curModeTextMesh != null)
        {
            //curModeTextMesh.text = UISpotID + " Mode: " + (currMode + 1).ToString() + " - " + modes[currMode].modeName + "\n";
            curModeTextMesh.text = UISpotID + " - " + modes[currMode].modeName + "\n";
        }

        // Send far plane request
        if (modes[currMode].name == "ControlMode - Dynamic Arm")
        {
            planePublisher.RequestFarPlane(2000f);
        }
        else
        {
            planePublisher.RequestFarPlane(6000f);
        }
    }

    public void nextMode()
    {
        //iterate mode number
        int newMode;

        //// Time tracking, end stopwatch for old mode
        //if (timeStarted)
        //{
        //    stopwatches[currMode].Stop();
        //}
        //else
        //{
        //    // First time a mode is activated, start other stopwatch
        //    generalControlsScript.beginTime();
        //    timeStarted = true;
        //}

        if (currMode == -1)
        {
            newMode = 0;
        }
        else
        {
            // normal 
            newMode = (currMode + 1) % modes.Count;
        }


        //// Begin stopwatch for next mode
        //stopwatches[newMode].Start();

        switchToMode(newMode);
    }

    public void UpdateUI(string text)
    {
        //for(int i = 0; i < UICanvases.Count; i++)
        //{
        //    UICanvases[i].text = text;
        //}
    }

    private void OnApplicationQuit()
    {
        //for (int i = 0; i < modes.Count; i++)
        //{
        //    UnityEngine.Debug.Log(modes[i].modeName + " mode time elapsed: " + System.Math.Round(stopwatches[i].Elapsed.TotalSeconds, 2) + " seconds");
        //}
    }
}
