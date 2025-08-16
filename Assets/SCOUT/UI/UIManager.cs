using System;
using System.Collections.Generic;
using Meta.WitAi.Events.Editor;
using UnityEngine;

public enum SingleControl
{
    DYNAMIC,
    DRIVE,
    ARM,
    LOCOMOTION
}



public enum SpotColor
{
    RED,
    BLUE
}

public class UIManager : MonoBehaviour
{
    public ScoutModeManager modeManager;

    // Changed from single armCameraUIController to two controllers: Right and Left
    
    public ButtonList[] singleControllerLists, dualControllerLists, cameraLists;
    public ButtonList[] tabselectionList; // ADDED: New ButtonList for tab selection

    public bool showSpotButtons;
    public Transform cameraRig;

    private Dictionary<SuperMode, ButtonList[]> superModeLists;

    

    public void Start()
    {
        superModeLists = new()
        {
            { SuperMode.SingleDrive, singleControllerLists },
            { SuperMode.DualDrive, dualControllerLists },
            { SuperMode.Camera, cameraLists },
            { SuperMode.TabSelection, tabselectionList } // ADDED: Include tabselectionList
        };
        var superModeGetters = new Dictionary<SuperMode, Func<NamedOption>[]>()
        {
            { SuperMode.SingleDrive, new Func<NamedOption>[] {
                () => modeManager.singleDrive.leftSpot,
                () => modeManager.singleDrive.leftControl,
                // () => modeManager.activePerspectiveMode,
                () => modeManager.singleDrive.rightControl,
                () => modeManager.singleDrive.rightSpot,
                () => null,
                () => null
            } },
            { SuperMode.DualDrive, new Func<NamedOption>[] {
                () => modeManager.dualDrive.spot,
                () => modeManager.dualDrive.control,
                // () => modeManager.activePerspectiveMode,
                () => null,
                () => null,
            } },
            { SuperMode.Camera, new Func<NamedOption>[]{
                () => modeManager.cameraView.activeCameraMode, // MODIFIED: Return activeCameraMode
                () => null,
                () => null,
            }},
            // MODIFIED: Getters for TabSelection
            { SuperMode.TabSelection, new Func<NamedOption>[]{
                () => { // This getter needs to return the currently active UITabOptions based on activeSuperMode
                    // This is a placeholder. We need a way to map activeSuperMode back to a UITabOptions instance.
                    // This might require a new property in ScoutModeManager or a lookup.
                    // For now, let's assume we can get the current UITabOptions from the modeManager.
                    // This part is tricky because UITabOptions are not directly stored in ScoutModeManager.
                    // A better approach would be to have a way to get the currently selected UITabOptions from the UIManager itself.
                    // The ButtonList needs to know which of its *own* options is selected.
                    // The UIManager's role is to tell the ButtonList which option is active.
                    // The UITabOptions.superMode property is what we need to match.

                    // This is a more robust way to get the selected UITabOptions for the tabselectionList
                    if (tabselectionList.Length > 0 && tabselectionList[0] != null && tabselectionList[0].options != null)
                    {
                        foreach (var tabOption in tabselectionList[0].options)
                        {
                            if (tabOption is UITabOptions uiTabOption && uiTabOption.superMode == modeManager.uiSuperMode)
                            {
                                return uiTabOption;
                            }
                        }
                    }
                    return null;
                },
                () => null,
                () => null,
            }}
        };
        var superModeSetters = new Dictionary<SuperMode, Action<NamedOption>[]>()
        {
            { SuperMode.SingleDrive, new Action<NamedOption>[] {
                m => modeManager.singleDrive.leftSpot = (SpotMode)m,
                m => modeManager.singleDrive.leftControl = (OneControllerMode)m,
                // m => OnPerspectiveChange((PerspectiveMode)m),
                m => modeManager.singleDrive.rightControl = (OneControllerMode)m,
                m => modeManager.singleDrive.rightSpot = (SpotMode)m,
                m => ((UIOption)m).DoAction(modeManager),
                m => ((UITabOptions)m).DoAction(modeManager)
            } },
            { SuperMode.DualDrive, new Action<NamedOption>[] {
                m => modeManager.dualDrive.spot = (SpotMode)m,
                m => modeManager.dualDrive.control = (TwoControllerMode)m,
                // m => OnPerspectiveChange((PerspectiveMode)m),
                m => ((UIOption)m).DoAction(modeManager),
                m => ((UITabOptions)m).DoAction(modeManager)
            } },
            { SuperMode.Camera, new Action<NamedOption>[] {
                m => modeManager.cameraView.SetActiveCameraMode((CameraMode)m), // MODIFIED: Call SetActiveCameraMode
                m => ((UIOption) m).DoAction(modeManager),
                m => ((UITabOptions)m).DoAction(modeManager)
            } },
            // ADDED: Setters for TabSelection
            { SuperMode.TabSelection, new Action<NamedOption>[] {
                m => ((UITabOptions)m).DoAction(modeManager), // This will call DoAction on the selected tab option
                m => ((UIOption)m).DoAction(modeManager),
                m => ((UITabOptions)m).DoAction(modeManager)
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

        // ADDED: Default selection for Camera SuperMode
        if (modeManager.uiSuperMode == SuperMode.Camera && cameraLists.Length > 0 && cameraLists[0].options.Length > 0)
        {
            modeManager.cameraView.SetActiveCameraMode((CameraMode)cameraLists[0].options[0]);
        }
        //SetDefaultControls();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            modeManager.isMenuOpen = !modeManager.isMenuOpen;

        }
        transform.parent.gameObject.GetComponent<Canvas>().enabled = modeManager.isMenuOpen;
        cameraRig.position = new(cameraRig.position.x, modeManager.isMenuOpen ? 100 : 0, cameraRig.position.z);

        // MODIFIED: Always enable tabselectionList if menu is open
        foreach (var list in tabselectionList)
        {
            list.gameObject.SetActive(modeManager.isMenuOpen);
            list.UpdateTabSelections(modeManager);
        }

        foreach (var kvp in superModeLists)
        {
            // Only enable other lists if their SuperMode is active AND menu is open
            bool enableLists = kvp.Key == modeManager.uiSuperMode && modeManager.isMenuOpen;
            // Skip tabselectionList here as it's handled above
            if (kvp.Key == SuperMode.TabSelection) continue;

            foreach (var list in kvp.Value)
                list.gameObject.SetActive(enableLists);
        }
    }

    

    

    public bool TryRaycastHover(GameObject hit)
    {
        // MODIFIED: Always check tabselectionList first
        foreach (var list in tabselectionList)
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
        // MODIFIED: Always check tabselectionList first
        for (int i = 0; i < tabselectionList.Length; i++)
        {
            if (tabselectionList[i].PressButton(hit))
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
            case SuperMode.Camera:

            case SuperMode.SingleDrive:

            case SuperMode.DualDrive:
                break;

        }
    }

}
