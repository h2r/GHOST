using System;
using UnityEngine;

public enum SingleControl
{
    DYNAMIC,
    DRIVE,
    ARM,
    LOCOMOTION
}

public enum Perspective
{
    CLOUD,
    ARM
}

public enum SpotColor
{
    RED,
    BLUE
}

public class UIManager : MonoBehaviour
{
    public SingleControllerFlow leftFlow, rightFlow;
    public DualControllerFlow dualFlow;

    // Changed from single armCameraUIController to two controllers: Right and Left
    public ArmCameraUIController armCameraUIControllerRight;
    public ArmCameraUIController armCameraUIControllerLeft;

    public ButtonList[] singleControllerLists, dualControllerLists;

    public bool useDualController;

    public bool showSpotButtons;
    public Transform cameraRig;

    private ButtonList[] activeLists;
    private Action<NamedMode>[] actions;

    private bool isOpen = true;

    private Perspective currentPerspective = Perspective.CLOUD;

    private bool spotsSwapped = false;

    public void Start()
    {
        // Turn off both Arm Camera UIs by default
        if (armCameraUIControllerRight != null)
            armCameraUIControllerRight.gameObject.SetActive(false);

        if (armCameraUIControllerLeft != null)
            armCameraUIControllerLeft.gameObject.SetActive(false);

        if (useDualController)
        {
            activeLists = dualControllerLists;
            actions = new Action<NamedMode>[]
            {
                m => dualFlow.SetSpot((SpotMode)m),
                m => dualFlow.SetControl((NewControlMode)m),
                m => OnPerspectiveChange((PerspectiveMode)m)
            };
            foreach (var list in singleControllerLists)
                list.gameObject.SetActive(false);
        }
        else
        {
            activeLists = singleControllerLists;
            actions = new Action<NamedMode>[]
            {
                m => leftFlow.SetSpot((SpotMode)m),

                m => {
                    var control = (NewControlMode)m;
                    leftFlow.SetControl(control);
                },

                m => OnPerspectiveChange((PerspectiveMode)m),

                m => {
                    var control = (NewControlMode)m;
                    rightFlow.SetControl(control);
                },

                m => rightFlow.SetSpot((SpotMode)m),

                m => this.SwapSpots(),
            };

            SetSingleControlPresets();

            if (!showSpotButtons)
            {
                activeLists[0].gameObject.SetActive(false);
                activeLists[4].gameObject.SetActive(false);
            }

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

            UpdateArmCameraUIVisibility();
        }

        cameraRig.position = new Vector3(cameraRig.position.x, isOpen ? 100f : 0f, cameraRig.position.z);
    }

    private void OnPerspectiveChange(PerspectiveMode mode)
    {
        Debug.Log("OnPerspectiveChange called with mode: " + mode.GetName());

        if (mode is ArmPerspectiveMode)
        {
            currentPerspective = Perspective.ARM;
        }
        else if (mode is CloudPerspectiveMode)
        {
            currentPerspective = Perspective.CLOUD;
        }

        mode.PerspectiveStart();

        UpdateArmCameraUIVisibility();
    }

    private void UpdateArmCameraUIVisibility()
    {
        bool show = !isOpen && currentPerspective == Perspective.ARM;

        if (armCameraUIControllerRight == null || armCameraUIControllerLeft == null)
        {
            Debug.LogWarning("ArmCameraUIControllerRight or Left is null");
            return;
        }

        armCameraUIControllerRight.gameObject.SetActive(show);
        armCameraUIControllerLeft.gameObject.SetActive(show);

        Debug.Log($"UpdateArmCameraUIVisibility: isOpen={isOpen}, currentPerspective={currentPerspective}, show={show}");
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

    public bool isMenuOpen()
    {
        return isOpen;
    }

    private void SetSingleControlPresets()
    {
        activeLists[0].PressButtonIndex((int)SpotColor.BLUE, actions[0]);
        activeLists[1].PressButtonIndex((int)SingleControl.LOCOMOTION, actions[1]);
        activeLists[2].PressButtonIndex((int)Perspective.CLOUD, actions[2]);
        activeLists[3].PressButtonIndex((int)SingleControl.DRIVE, actions[3]);
        activeLists[4].PressButtonIndex((int)SpotColor.RED, actions[4]);
    }

    public void SwapSpots()
    {
        spotsSwapped = !spotsSwapped;

        SpotColor leftSpot = this.leftFlow.GetSpot().spotColor;
        SpotColor rightSpot = this.rightFlow.GetSpot().spotColor;
        activeLists[0].PressButtonIndex((int)rightSpot, actions[0]);
        activeLists[4].PressButtonIndex((int)leftSpot, actions[4]);
    }

    public bool AreSpotsSwapped()
    {
        return spotsSwapped;
    }

    public bool IsInArmPerspective()
    {
        return currentPerspective == Perspective.ARM;
    }

}
