using System;
using System.Collections.Generic;
using Meta.WitAi.Events.Editor;
using UnityEngine;


// enums used for default controller settings for readability 
public enum SingleControl
{
    DYNAMIC,
    DRIVE,
    ARM,
    FLY
}

public enum DualControl
{
    DRIVE,
    ARM, 
    FLY
}

public enum RecordingSettings
{
    RECORD,
    END_RECORDING
}

public enum SpotColor
{
    RED,
    BLUE
}


public class UIManager : MonoBehaviour
{
    public ScoutModeManager modeManager;

    public ButtonList[] singleControllerLists, dualControllerLists, recordingLists, tabSelectionLists;

    public RigPositioner rigPositioner;

    private Dictionary<SuperMode, ButtonList[]> superModeLists;

    private float robotWorldY = 0;


    public void Start()
    {

        rigPositioner.y = 1; // Initial Height change; 0 is ground level, 
        robotWorldY = 0;

        superModeLists = new()
        {
            { SuperMode.SingleDrive, singleControllerLists },
            { SuperMode.DualDrive, dualControllerLists },
            { SuperMode.Camera, recordingLists }, //change this
            { SuperMode.TabSelection, tabSelectionLists }
        };
        var superModeGetters = new Dictionary<SuperMode, Func<NamedOption>[]>()
        {
            { SuperMode.SingleDrive, new Func<NamedOption>[] {
                () => modeManager.singleDrive.leftSpot,
                () => modeManager.singleDrive.leftControl,
                () => modeManager.singleDrive.rightControl,
                () => modeManager.singleDrive.rightSpot,
                () => null,
                () => null
            } },
            { SuperMode.DualDrive, new Func<NamedOption>[] {
                () => modeManager.dualDrive.spot,
                () => modeManager.dualDrive.control,
                () => null,
                () => null,
            } },
            { SuperMode.Camera, new Func<NamedOption>[]{ //change this
                () => modeManager.cameraView.activeCameraMode,
                () => null,
                () => null,
            }},
            // MODIFIED: Getters for TabSelection
            { SuperMode.TabSelection, new Func<NamedOption>[]{
                null,
                () => null,
                () => null,
            }}
        };
        var superModeSetters = new Dictionary<SuperMode, Action<NamedOption>[]>()
        {
            { SuperMode.SingleDrive, new Action<NamedOption>[] {
                m => modeManager.singleDrive.leftSpot = (SpotMode)m,
                m => {
                    if (modeManager.singleDrive.leftControl != null) {
                        modeManager.leftModel.ClearLabels();
                    }
                    modeManager.singleDrive.leftControl = (OneControllerMode)m;
                    if (m is Arm6AxisMode) {
                        var armCameraView = FindCameraMode<ArmCameraView>();
                        if (armCameraView != null) {
                            modeManager.cameraView.SetActiveCameraMode(armCameraView);
                        }
                    }
                },
                m => {
                    if (modeManager.singleDrive.rightControl != null) {
                        modeManager.rightModel.ClearLabels();
                    }
                    modeManager.singleDrive.rightControl = (OneControllerMode)m;
                    if (m is Arm6AxisMode) {
                        var armCameraView = FindCameraMode<ArmCameraView>();
                        if (armCameraView != null) {
                            modeManager.cameraView.SetActiveCameraMode(armCameraView);
                        }
                    }
                },
                m => modeManager.singleDrive.rightSpot = (SpotMode)m,
                m => ((UIOption)m).DoAction(modeManager)
            } },
            { SuperMode.DualDrive, new Action<NamedOption>[] {
                m => modeManager.dualDrive.spot = (SpotMode)m,
                m => modeManager.dualDrive.control = (TwoControllerMode)m,
                m => ((UIOption)m).DoAction(modeManager)
            } },
            { SuperMode.Camera, new Action<NamedOption>[] { //change this 
                m => modeManager.cameraView.SetActiveCameraMode((CameraMode)m), //change to do action as well
                m => ((UIOption) m).DoAction(modeManager) 
            } },
            // ADDED: Setters for TabSelection
            { SuperMode.TabSelection, new Action<NamedOption>[] {
                m => ((UITabOption)m).DoAction(modeManager)
            }}
        };

        foreach (var kvp in superModeLists)
        {
            var lists = superModeLists[kvp.Key];
            var getters = superModeGetters[kvp.Key];
            var setters = superModeSetters[kvp.Key];

            for (int i = 0; i < lists.Length; i++)
            {
                lists[i].optionGetter = getters[i];
                lists[i].optionSetter = setters[i];
                lists[i].Reset();
            }
        }

        SetDefaultControls();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            modeManager.isMenuOpen = !modeManager.isMenuOpen;
            if (modeManager.isMenuOpen)
            {
                robotWorldY = rigPositioner.y;
                rigPositioner.y = 100;
            }
            else
            {
                rigPositioner.y = robotWorldY;
            }
        }
        transform.parent.gameObject.GetComponent<Canvas>().enabled = modeManager.isMenuOpen;

        // Always enable tabSelectionLists if menu is open
        foreach (var list in tabSelectionLists)
        {
            list.gameObject.SetActive(modeManager.isMenuOpen);
            list.UpdateTabSelections(modeManager);
        }

        foreach (var kvp in superModeLists)
        {
            // Only enable other lists if their SuperMode is active AND menu is open
            bool enableLists = kvp.Key == modeManager.uiSuperMode && modeManager.isMenuOpen;
            // Skip tabSelectionLists here as it's handled above
            if (kvp.Key == SuperMode.TabSelection) continue;

            foreach (var list in kvp.Value)
                list.gameObject.SetActive(enableLists);
        }
    }

    public bool TryRaycastHover(GameObject hit)
    {
        // MODIFIED: Always check tabSelectionLists first
        foreach (var list in tabSelectionLists)
        {
            if (list.TryHoverButton(hit))
                return true;
        }

        var activeLists = superModeLists[modeManager.uiSuperMode];
        foreach (var list in activeLists)
        {
            if (list.TryHoverButton(hit))
                return true;
        }
        return false;
    }

    public void RaycastPress(GameObject hit)
    {
        // MODIFIED: Always check tabSelectionLists first
        for (int i = 0; i < tabSelectionLists.Length; i++)
        {
            if (tabSelectionLists[i].PressButton(hit))
                return;
        }

        var activeLists = superModeLists[modeManager.uiSuperMode];
        for (int i = 0; i < activeLists.Length; i++)
        {
            if (activeLists[i].PressButton(hit))
                return;
        }
    }

    private void SetDefaultControls()
    {
        var activeLists = superModeLists[modeManager.uiSuperMode];
        switch (modeManager.uiSuperMode)
        {

            case SuperMode.SingleDrive:
                if (activeLists.Length > 0 && activeLists[0].options.Length > 0)
                {
                    // Set default left spot and control
                    activeLists[0].PressButtonIndex((int)SpotColor.RED);
                    activeLists[1].PressButtonIndex((int)SingleControl.FLY); // select fly as teh default for left and right control 
                    activeLists[2].PressButtonIndex((int)SingleControl.FLY);
                    activeLists[3].PressButtonIndex((int)SpotColor.BLUE);
                }
                break; 

            case SuperMode.DualDrive:
                if (activeLists.Length > 0 && activeLists[0].options.Length > 0)
                {
                    // Set default spot and control
                    modeManager.dualDrive.spot = (SpotMode)activeLists[0].options[(int) SpotColor.RED];
                    modeManager.dualDrive.control = (TwoControllerMode)activeLists[1].options[(int) DualControl.FLY];
                }
                break;
        }
    }
}