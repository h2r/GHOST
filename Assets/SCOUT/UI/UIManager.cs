using System;
using System.Collections.Generic;
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
    public ScoutModeManager modeManager;

    // Changed from single armCameraUIController to two controllers: Right and Left
    public ArmCameraUIController armCameraUIControllerRight;
    public ArmCameraUIController armCameraUIControllerLeft;

    public ButtonList[] singleControllerLists, dualControllerLists;

    public bool useDualController;

    public bool showSpotButtons;
    public Transform cameraRig;

    private Dictionary<SuperMode, ButtonList[]> superModeLists;

    private Perspective currentPerspective = Perspective.CLOUD;

    private bool spotsSwapped = false;

    public void Start()
    {
        superModeLists = new()
        {
            { SuperMode.SingleDrive, singleControllerLists },
            { SuperMode.DualDrive, dualControllerLists }
        };
        var superModeGetters = new Dictionary<SuperMode, Func<NamedOption>[]>()
        {
            { SuperMode.SingleDrive, new Func<NamedOption>[] {
                () => modeManager.singleDrive.leftSpot,
                () => modeManager.singleDrive.leftControl,
                () => null,
                () => modeManager.singleDrive.rightControl,
                () => modeManager.singleDrive.rightSpot,
                () => null
            } },
            { SuperMode.DualDrive, new Func<NamedOption>[] {
                () => modeManager.dualDrive.spot,
                () => modeManager.dualDrive.control,
                () => null,
                () => null
            } }
        };
        var superModeSetters = new Dictionary<SuperMode, Action<NamedOption>[]>()
        {
            { SuperMode.SingleDrive, new Action<NamedOption>[] {
                m => modeManager.singleDrive.leftSpot = (SpotMode)m,
                m => modeManager.singleDrive.leftControl = (OneControllerMode)m,
                m => OnPerspectiveChange((PerspectiveMode)m),
                m => modeManager.singleDrive.rightControl = (OneControllerMode)m,
                m => modeManager.singleDrive.rightSpot = (SpotMode)m,
                m => ((UIOption)m).DoAction(modeManager)
            } },
            { SuperMode.DualDrive, new Action<NamedOption>[] {
                m => modeManager.dualDrive.spot = (SpotMode)m,
                m => modeManager.dualDrive.control = (TwoControllerMode)m,
                m => OnPerspectiveChange((PerspectiveMode)m),
                m => ((UIOption)m).DoAction(modeManager)
            } }
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
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            modeManager.isMenuOpen = !modeManager.isMenuOpen;
            UpdateArmCameraUIVisibility();
        }
        transform.parent.gameObject.GetComponent<Canvas>().enabled = modeManager.isMenuOpen;
        cameraRig.position = new(cameraRig.position.x, modeManager.isMenuOpen ? 100 : 0, cameraRig.position.z);

        foreach (var kvp in superModeLists)
        {
            bool enableLists = kvp.Key == modeManager.activeSuperMode;
            foreach (var list in kvp.Value)
                list.gameObject.SetActive(enableLists);
        }
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
        bool show = !modeManager.isMenuOpen && currentPerspective == Perspective.ARM;

        if (armCameraUIControllerRight == null || armCameraUIControllerLeft == null)
        {
            Debug.LogWarning("ArmCameraUIControllerRight or Left is null");
            return;
        }

        armCameraUIControllerRight.gameObject.SetActive(show);
        armCameraUIControllerLeft.gameObject.SetActive(show);

        Debug.Log($"UpdateArmCameraUIVisibility: isOpen={modeManager.isMenuOpen}, currentPerspective={currentPerspective}, show={show}");
    }

    public bool TryRaycastHover(GameObject hit)
    {
        foreach (var list in superModeLists[modeManager.activeSuperMode])
        {
            if (list.TryHoverButton(hit))
                return true;
        }

        return false;
    }

    public void RaycastPress(GameObject hit)
    {
        var activeLists = superModeLists[modeManager.activeSuperMode];
        for (int i = 0; i < activeLists.Length; i++)
        {
            if (activeLists[i].PressButton(hit))
                return;
        }
    }

    public bool IsInArmPerspective()
    {
        return currentPerspective == Perspective.ARM;
    }

}
