using System;
using UnityEngine;

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
                m => rightFlow.SetSpot((SpotMode)m)
            };
            foreach (var list in dualControllerLists)
                list.gameObject.SetActive(false);
        }
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
        cameraRig.position = new(0, isOpen ? 100 : 0, 0);
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
}