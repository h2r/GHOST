using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InstructionSwitch : MonoBehaviour
{
    public OVRInput.RawButton UserInstruction;

    // Tracking the current mode of each Spot
    public ModeManager Spot1Manager;
    public ModeManager Spot2Manager;

    // Show/hide the instruction panels as needed
    public GameObject Spot1InstructPanel;
    public GameObject FixInstruction;

    // Instruction text shown in VR
    public TextMeshProUGUI spot1CurModeInstruct;

    private int previousSpot1Mode = -999; // Track last known mode
    private int previousSpot2Mode = -999; // Track last known mode

    private int spot1Mode;
    private int spot2Mode;

    public Transform userCamera;

    // Mode instructions
    private readonly string[] instructContentRelatedToMode =
    {
        "Press A to Switch/Sync Spot to Drive\n\nMove L Thumbstick to Translate Spot\n\nMove R Thumbstick to Rotate Spot\n\nPress L Thumbstick to Lower Height\n\nPress R Thumbstick to Raise Height",
        "Hold Top Trigger and Drag your hand to Move arm.\n\nPress Hand Trigger to Open/Close Gripper\n\nPress A To Switch PointCloud or Double Click to show RGB Camera\n\nCamera Transform\nL Thumbstick → Translate\nR Thumbstick → Rotate\nPress L/R Thumbstick → Adjust Height",
        //"Move Thumbstick → Translate Spot\n\nHold Index Trigger + Move Thumbstick → Rotate Spot\n\nPress Thumbstick → Lower Spot Height\n\nHold Index Trigger + Press Thumbstick → Raise Spot Height",
        //"Press Hand Trigger → Stow Arm\n\nDouble Click Index Trigger → Switch Thumbstick Mode: \n- Gripper Translation\n- Gripper Nod\n- Gripper Rotate (180° max)\n\nPress Thumbstick → Lower Gripper\n\nHold Index Trigger + Press Thumbstick → Raise Gripper"
    };

    private readonly string[] defaultContent =
    {
        "Press X to Switch Mode\n\nPress Menu (Left Controller) to Show Instruction\n\nPress Y (Spot 1) / B (Spot 2) to Stow Arm",
        "Press X → Switch Mode"
    };

    private bool isInstructionVisible = true; // Track instruction panel visibility

    // Update is called once per frame
    void Update()
    {
        spot1Mode = Spot1Manager.currMode;
        spot2Mode = Spot2Manager.currMode;
        
        if (OVRInput.GetDown(UserInstruction))
        {
            ToggleInstructions();
        }


        // button for demo to reset the user camera
        if (Input.GetKeyDown(KeyCode.R)){
            Debug.LogWarning("keycode R");

            userCamera.position = new Vector3(-0.0417f, -1.7499f, 3.124f);
            userCamera.rotation = Quaternion.Euler(new Vector3(0, 8, 0));
        }

        CheckForModeChange();
    }

    void CheckForModeChange()
    {
        // Check if either mode has changed
        if (spot1Mode != previousSpot1Mode || spot2Mode != previousSpot2Mode)
        {
            previousSpot1Mode = spot1Mode;
            previousSpot2Mode = spot2Mode;
            updateInstructPanel();
        }
    }

    void ToggleInstructions()
    {
        isInstructionVisible = !isInstructionVisible;
        updateInstructPanel();
    }


    void updateInstructPanel()
    {
        if (isInstructionVisible)
        {
            if (spot1Mode == -1 && spot2Mode == -1)
            {
                // Both spots are in default mode, show their respective default content
                //Spot1InstructPanel.SetActive(true);
                //spot1CurModeInstruct.text = defaultContent[0];

                // not showing the mode instruction at start
                Spot1InstructPanel.SetActive(false);
            }
            else if (spot1Mode != -1 && spot2Mode != -1 && spot1Mode == spot2Mode)
            {
                // Both Spots in the same valid mode
                Spot1InstructPanel.SetActive(true);
                spot1CurModeInstruct.text = instructContentRelatedToMode[spot1Mode];
            }
            else
            {
                // Handle cases where either Spot1 or Spot2 is in an invalid state (-1)
                Spot1InstructPanel.SetActive(true);
                spot1CurModeInstruct.text = spot1Mode != -1 ? instructContentRelatedToMode[spot1Mode] : defaultContent[0];
            }

            FixInstruction.SetActive(true);
        }
        else
        {
            Spot1InstructPanel.SetActive(false);
            FixInstruction.SetActive(false);
        }
    }

}
