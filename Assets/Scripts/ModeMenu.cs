using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ModeMenu : MonoBehaviour
{
    public TextMeshProUGUI LeftToggle;
    public TextMeshProUGUI RightToggle;
    public TextMeshProUGUI YButton;
    public TextMeshProUGUI AButton;
    public TextMeshProUGUI LeftTrigger;
    public TextMeshProUGUI RightTrigger;
    public TextMeshProUGUI LeftGripper;
    public TextMeshProUGUI RightGripper;

    public List<Image> Icons;
    public Image LeftTriggerIcon;
    public Image RightTriggerIcon;
    public Image LeftGripperIcon;
    public Image RightGripperIcon;
    public Image RightJoystickIcon;

    private bool ArmMode = false;
    private bool DriveMode = false;
    private bool textHidden = false;

    void Start()
    {

    }

    void Update()
    {
        // A button
        if (OVRInput.GetDown(OVRInput.RawButton.A)) // Toggle between arm and drive mode
        {
            SwitchText();

        }

        // Y button
        if (OVRInput.GetDown(OVRInput.RawButton.Y)) // Toggle UI text on/off
        {
            if (!textHidden)
            {
                Empty();

            }
            else
            {
                ShowText();
            }
        }
    }


    void ShowText()
    {
        textHidden = false;
        EnableText();
        DisplayIcons();
    }


    void SwitchText()
    {

        if (!DriveMode) //we start in drive mode
        {
            DriveMode = true;
            ArmMode = false;
            DisplayIcons();
            DriveText();
        }
        else // else we are in arm mode
        {
            ArmMode = true;
            DriveMode = false;
            DisplayIcons();
            ArmText();
        }
    }

    void Empty()
    {
        textHidden = true;
        foreach (Image g in Icons)
            g.enabled = false;
        RightToggle.enabled = false;
        LeftToggle.enabled = false;
        YButton.enabled = false;
        AButton.enabled = false;
        LeftTrigger.enabled = false;
        RightTrigger.enabled = false;
        LeftGripper.enabled = false;
        RightGripper.enabled = false;   
        DisplayIcons();
    }

    void DriveText()
    {
        LeftToggle.text = "Move Spot";
        RightToggle.text = "Rotate Spot";
        YButton.text = "Toggle UI";
        AButton.text = "Switch Modes";
        LeftTrigger.text = "";
        RightTrigger.text = "";
        LeftGripper.text = "";
        RightGripper.text = "";

    }

    void ArmText()
    {
        LeftToggle.text = "Adjust View";
        RightToggle.text = "";
        YButton.text = "Toggle UI";
        AButton.text = "Switch Modes";
        LeftTrigger.text = "Hold + move your arm" + "\n" + " to close/open hand" + "\n" + " slowly";
        RightTrigger.text = "Move your arm for" + "\n" + "new Spot arm position";
        LeftGripper.text = "Stow Arm";
        RightGripper.text = "Closes/Opens" + "\n" + " Spot hand";

    }
    void EnableText()
    {
        textHidden = false;
        foreach (Image i in Icons)
            i.enabled = true;
        RightToggle.enabled = true;
        LeftToggle.enabled = true;
        YButton.enabled = true;
        AButton.enabled = true;
        LeftTrigger.enabled = true;
        RightTrigger.enabled = true;
        LeftGripper.enabled = true;
        RightGripper.enabled = true;
    }

    void DisplayIcons()
    {
        if (textHidden)
        {
            LeftTriggerIcon.enabled = false;
            RightTriggerIcon.enabled = false;
            LeftGripperIcon.enabled = false;
            RightGripperIcon.enabled = false;
            RightJoystickIcon.enabled = false;
        }
        else
        {
            if (DriveMode)
            {
                LeftTriggerIcon.enabled = false;
                RightTriggerIcon.enabled = false;
                LeftGripperIcon.enabled = false;
                RightGripperIcon.enabled = false;
                RightJoystickIcon.enabled = true;
            }
            else
            {
                LeftTriggerIcon.enabled = true;
                RightTriggerIcon.enabled = true;
                LeftGripperIcon.enabled = true;
                RightGripperIcon.enabled = true;
                RightJoystickIcon.enabled = false;
            }
        }

    }
   

}
