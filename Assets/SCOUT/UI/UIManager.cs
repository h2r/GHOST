using System;
using UnityEngine;

public enum SingleControl
{
DRIVE, 
ARM, 
LOCOMOTION
}

public enum Perspective
{
CLOUD, 
ARM
}

public class UIManager : MonoBehaviour
{
    public SingleControllerFlow leftFlow, rightFlow;
    public DualControllerFlow dualFlow;


    // Single controller lists: left spot, left control, perspective, right control, right spot
    // Dual controller lists: spot, control, perspective
    public ButtonList[] singleControllerLists, dualControllerLists;
    
    public bool useDualController;
    public Transform cameraRig;

    private ButtonList[] activeLists;
    private Action<NamedMode>[] actions;

    private bool isOpen = true;

    public void Start()
    {
        if (useDualController)
        {
            activeLists = dualControllerLists;
            actions = new Action<NamedMode>[] {
                m => dualFlow.SetSpot((SpotMode)m),
                m => dualFlow.SetControl((NewControlMode)m),
                m => ((PerspectiveMode)m).PerspectiveStart()
            };
            foreach (var list in singleControllerLists)
                list.gameObject.SetActive(false);
        }
        else
        {
            activeLists = singleControllerLists;
            actions = new Action<NamedMode>[] {
                m => leftFlow.SetSpot((SpotMode)m),
                m => leftFlow.SetControl((NewControlMode)m),
                m => ((PerspectiveMode)m).PerspectiveStart(),
                m => rightFlow.SetControl((NewControlMode)m),
                m => rightFlow.SetSpot((SpotMode)m), 
                m => this.SwapSpots(), 
            };
            SetSingleControlPresets(); 
            activeLists[0].gameObject.SetActive(false);
            activeLists[4].gameObject.SetActive(false);
            foreach (var list in dualControllerLists)
                list.gameObject.SetActive(false);
            
        }
        //manually preselect buttons
        
    }

    public void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            isOpen = !isOpen;
            transform.parent.gameObject.GetComponent<Canvas>().enabled = isOpen;
            leftFlow.SetPaused(isOpen);
            rightFlow.SetPaused(isOpen);
            dualFlow.SetPaused(isOpen);
        }
        cameraRig.position = new(cameraRig.position.x, isOpen ? 100 : 0, cameraRig.position.z);
    }

    public bool TryRaycastHover(GameObject hit)
    {
        foreach (var list in activeLists)
        {
            if (list.TryHoverButton(hit))
                return true;
        }

        return false;
    }

    public void RaycastPress(GameObject hit)
    {
        for (int i = 0; i < activeLists.Length; i++)
        {
            if (activeLists[i].PressButton(hit, actions[i]))
                return;
        }
    }

    public bool GetOpen()
    {
        return isOpen;
    }

    private void SetSingleControlPresets()
    {
        //sets left spot to spot one (red) 
        //sets right spot to spot two (blue) 
        activeLists[0].PressButtonIndex(1, actions[0]);
        activeLists[1].PressButtonIndex((int)SingleControl.LOCOMOTION, actions[1]);
        activeLists[2].PressButtonIndex((int)Perspective.CLOUD, actions[2]);
        activeLists[3].PressButtonIndex((int)SingleControl.DRIVE, actions[3]);
        activeLists[4].PressButtonIndex(0, actions[4]);
    }

    public void SwapSpots()
    {
        SpotMode leftSpot = this.leftFlow.GetSpot(); 
        SpotMode rightSpot = this.rightFlow.GetSpot();

        this.leftFlow.SetSpot(rightSpot);
        this.rightFlow.SetSpot(leftSpot); 
        
        //TODO add some UI toggle swap 
    }
}