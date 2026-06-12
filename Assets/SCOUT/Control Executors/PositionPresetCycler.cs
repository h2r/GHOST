using UnityEngine;
using SCOUT;

public class PositionPresetCycler : MonoBehaviour
{
    public RigPositioner rigPositioner;
    public GameObject spotOne, spotTwo, spotOneArm, spotTwoArm;
    public MessageBadge messageManager;

    // Current control flow leaves preset cycling disabled, but keeping it reachable avoids dead code.
    public bool enablePresetCycling;

    [Header("Spot Front-Camera Lock")]
    [Tooltip("Match rig yaw to the spot heading")]
    public bool matchSpotHeading = true;

    // offset from the spot anchor to the front-camera viewpoint, in the spot's local frame
    [Tooltip("Forward offset to the front camera (+Z is forward)")]
    [Range(-3f, 3f)] public float cameraForwardOffset = 0.45f;
    [Tooltip("Height offset to the front camera")]
    [Range(-2f, 2f)] public float cameraHeightOffset = 0.0f;
    [Tooltip("Sideways offset to the front camera (+X is right)")]
    [Range(-2f, 2f)] public float cameraLateralOffset = 0.0f;

    [Header("Video Window Mode")]
    [Tooltip("Cycle between default view and a 2-window front-cam grid, instead of the spot lock presets")]
    public bool useVideoWindows = false;
    [Tooltip("Point clouds are hidden while the video windows are up")]
    public PointCloudCycler pointCloudCycler;
    [Tooltip("Raise the rig while the video windows are up, like the menu does, so the spots aren't in view")]
    public bool noRobotView = false;
    [Tooltip("Height the rig is raised to while No Robot View is active")]
    public float noRobotViewHeight = 100f;
    public Transform centerEyeAnchor;
    public VideoWindow spotOneWindow, spotTwoWindow;
    [Tooltip("Distance of the windows from the player")]
    public float windowDistance = 2f;
    [Tooltip("Angle in degrees separating the centers of the two spots' window pairs")]
    public float windowSeparationAngle = 65f;
    [Tooltip("Angle in degrees separating the two front-cam screens within one spot's pair")]
    public float windowPairAngle = 30f;
    [Tooltip("Vertical offset of the windows from the headset")]
    public float windowHeightOffset = 0f;
    public float windowZScale = 0.001f;

    [System.Serializable]
    public class VideoScreen
    {
        public Transform screen;
        public AllCamsPositioner.RotationOption rotation = AllCamsPositioner.RotationOption.None;
    }

    [System.Serializable]
    public class VideoWindow
    {
        // the front cams criss-cross: FRONTRIGHT sees the left side, FRONTLEFT the right
        [Tooltip("FRONTRIGHT cam quad - shown on the left of the pair")]
        public VideoScreen frontRight;
        [Tooltip("FRONTLEFT cam quad - shown on the right of the pair")]
        public VideoScreen frontLeft;
        public float width = 1f;
        public float height = 0.75f;
    }

    private bool videoWindowsVisible = false;
    private float storedRigY;

    public enum RigAnchorPoints
    {
        World,
        SpotOne,
        SpotTwo
    }
    public RigAnchorPoints currentAnchorPoint = RigAnchorPoints.World;

    // true while the rig is locked to a spot
    public bool IsLockedToSpot => currentAnchorPoint != RigAnchorPoints.World;

    enum Preset
    {
        BehindSpotOne,
        BehindSpotTwo,
        BetweenSpots,
        ArmSpotOne,
        ArmSpotTwo
    }

    private readonly Preset[] presetOrder = {
        Preset.BetweenSpots,
        Preset.BehindSpotOne,
        Preset.BehindSpotTwo,
        // Preset.ArmSpotOne,
        // Preset.ArmSpotTwo
    };
    private int curPresetIndex = -1;

    private Vector3 CameraOffset => new(cameraLateralOffset, cameraHeightOffset, cameraForwardOffset);

    private void Update()
    {
        if (!useVideoWindows || !videoWindowsVisible)
            return;

        // hide the windows while the menu is open, like the other cam positioners
        bool menuOpen = ScoutModeManager.Instance != null && ScoutModeManager.Instance.isMenuOpen;
        SetVideoWindowsActive(!menuOpen);
        if (!menuOpen)
            PositionVideoWindows();
    }

    private void PositionVideoWindows()
    {
        if (centerEyeAnchor == null)
            return;

        PositionVideoWindowPair(spotOneWindow, -windowSeparationAngle / 2f);
        PositionVideoWindowPair(spotTwoWindow, windowSeparationAngle / 2f);
    }

    private void PositionVideoWindowPair(VideoWindow window, float centerAngle)
    {
        if (window == null)
            return;

        PositionVideoScreen(window, window.frontRight, centerAngle - windowPairAngle / 2f);
        PositionVideoScreen(window, window.frontLeft, centerAngle + windowPairAngle / 2f);
    }

    private void PositionVideoScreen(VideoWindow window, VideoScreen screenSetting, float angle)
    {
        if (screenSetting == null || screenSetting.screen == null)
            return;

        Transform screen = screenSetting.screen;
        Vector3 position = centerEyeAnchor.position
            + Quaternion.Euler(0, centerEyeAnchor.eulerAngles.y + angle, 0) * Vector3.forward * windowDistance;
        position.y = centerEyeAnchor.position.y + windowHeightOffset;

        screen.position = position;
        screen.LookAt(centerEyeAnchor.position);
        screen.Rotate(0, 0, GetZRotationFromOption(screenSetting.rotation), Space.Self);
        screen.localScale = new Vector3(window.width, window.height, windowZScale);
    }

    private static float GetZRotationFromOption(AllCamsPositioner.RotationOption option)
    {
        switch (option)
        {
            case AllCamsPositioner.RotationOption.Left90: return 90f;
            case AllCamsPositioner.RotationOption.Right90: return -90f;
            case AllCamsPositioner.RotationOption.UpsideDown180: return 180f;
            default: return 0f;
        }
    }

    private void SetVideoWindowsActive(bool isActive)
    {
        SetVideoWindowActive(spotOneWindow, isActive);
        SetVideoWindowActive(spotTwoWindow, isActive);
    }

    private void SetVideoWindowActive(VideoWindow window, bool isActive)
    {
        if (window == null)
            return;

        SetVideoScreenActive(window.frontRight, isActive);
        SetVideoScreenActive(window.frontLeft, isActive);
    }

    private void SetVideoScreenActive(VideoScreen screenSetting, bool isActive)
    {
        if (screenSetting != null && screenSetting.screen != null && screenSetting.screen.gameObject.activeSelf != isActive)
            screenSetting.screen.gameObject.SetActive(isActive);
    }

    // re-pin the rig to the spot after the control modes run, so the joystick can't move it
    private void LateUpdate()
    {
        if (!IsLockedToSpot)
            return;

        // while the menu is open, let UIManager lift the rig instead of re-pinning to the spot
        if (ScoutModeManager.Instance != null && ScoutModeManager.Instance.isMenuOpen)
            return;

        Transform anchorTransform = GetCurrentAnchorTransform();
        if (anchorTransform == null)
            return;

        ApplyLockedPose(anchorTransform);
    }

    private void ApplyLockedPose(Transform anchorTransform)
    {
        Vector3 cameraPosition = anchorTransform.position + anchorTransform.TransformDirection(CameraOffset);

        rigPositioner.x = cameraPosition.x;
        rigPositioner.y = cameraPosition.y;
        rigPositioner.z = cameraPosition.z;

        if (matchSpotHeading)
        {
            // face where the robot faces; head tracking still lets you look around freely
            rigPositioner.rotation = Quaternion.Euler(0, anchorTransform.eulerAngles.y, 0);
        }
    }

    private Transform GetCurrentAnchorTransform()
    {
        switch (currentAnchorPoint)
        {
            case RigAnchorPoints.SpotOne:
                return spotOne?.transform;
            case RigAnchorPoints.SpotTwo:
                return spotTwo?.transform;
            default:
                return null;
        }
    }

    public void CyclePresets()
    {
        if (!enablePresetCycling)
            return;

        if (useVideoWindows)
        {
            CycleVideoWindows();
            return;
        }

        curPresetIndex = (curPresetIndex + 1) % presetOrder.Length;

        switch (presetOrder[curPresetIndex])
        {
            case Preset.BehindSpotOne:
                currentAnchorPoint = RigAnchorPoints.SpotOne;
                ApplyLockedPose(spotOne.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot One");
                break;

            case Preset.BehindSpotTwo:
                currentAnchorPoint = RigAnchorPoints.SpotTwo;
                ApplyLockedPose(spotTwo.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot Two");
                break;

            case Preset.BetweenSpots:
                currentAnchorPoint = RigAnchorPoints.World;
                Vector3 cameraPosition = (spotOne.transform.position + spotTwo.transform.position) / 2 - new Vector3(0, 0, .5f);
                rigPositioner.x = cameraPosition.x;
                rigPositioner.z = cameraPosition.z;
                if (messageManager) messageManager.ShowMessage("Camera Anchor: World");
                break;

            case Preset.ArmSpotOne:
                currentAnchorPoint = RigAnchorPoints.SpotOne;
                ApplyLockedPose(spotOneArm.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot One");
                break;

            case Preset.ArmSpotTwo:
                currentAnchorPoint = RigAnchorPoints.SpotTwo;
                ApplyLockedPose(spotTwoArm.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot Two");
                break;
        }
    }

    private void CycleVideoWindows()
    {
        videoWindowsVisible = !videoWindowsVisible;
        SetVideoWindowsActive(videoWindowsVisible);

        if (videoWindowsVisible)
        {
            if (noRobotView)
            {
                storedRigY = rigPositioner.y;
                rigPositioner.y = noRobotViewHeight;
            }
            PositionVideoWindows();
            if (pointCloudCycler != null) pointCloudCycler.HideAllPointClouds();
            if (messageManager) messageManager.ShowMessage("Video Windows: Front Cams");
        }
        else
        {
            if (noRobotView)
                rigPositioner.y = storedRigY;
            if (pointCloudCycler != null) pointCloudCycler.RestorePointClouds();
            if (messageManager) messageManager.ShowMessage("Video Windows: Off");
        }
    }

    public void SetInitialPreset()
    {
        if (useVideoWindows)
        {
            // start in the default view with the windows hidden
            videoWindowsVisible = false;
            SetVideoWindowsActive(false);
            return;
        }

        curPresetIndex = -1;
        CyclePresets();
    }
}
