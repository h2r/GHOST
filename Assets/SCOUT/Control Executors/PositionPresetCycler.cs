using UnityEngine;
using SCOUT;

public class PositionPresetCycler : MonoBehaviour
{
    public RigPositioner rigPositioner;
    public GameObject spotOne, spotTwo;
    public MessageBadge messageManager;

    [Header("Spot Front-Camera Lock")]
    [Tooltip("Match rig yaw to the spot heading")]
    public bool matchSpotHeading = true;

    [Tooltip("Forward offset to the front camera (+Z is forward)")]
    [Range(-3f, 3f)] public float cameraForwardOffset = 0.45f;
    [Tooltip("Height offset to the front camera")]
    [Range(-2f, 2f)] public float cameraHeightOffset = 0.0f;
    [Tooltip("Sideways offset to the front camera (+X is right)")]
    [Range(-2f, 2f)] public float cameraLateralOffset = 0.0f;

    [Header("Video Window Mode")]
    [Tooltip("Cycle exo-fly/video+spots/video+no-spots instead of exo-fly/spot-lock presets")]
    public bool useVideoWindows = false;
    [Tooltip("Point clouds are hidden while the video windows are up")]
    public PointCloudCycler pointCloudCycler;
    [Tooltip("Height the rig is raised to in the no-robot-view state")]
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

    public enum RigAnchorPoints { World, SpotOne, SpotTwo }
    public RigAnchorPoints currentAnchorPoint = RigAnchorPoints.World;
    public bool IsLockedToSpot => currentAnchorPoint != RigAnchorPoints.World;

    // Default mode: 0 = fly (exo), 1 = lock SpotOne, 2 = lock SpotTwo
    // Video mode:   0 = fly (exo), 1 = video+spots, 2 = video+no-spots
    private int videoState = 0;
    private float storedRigY;

    private Vector3 CameraOffset => new(cameraLateralOffset, cameraHeightOffset, cameraForwardOffset);

    private void Update()
    {
        if (!useVideoWindows || videoState == 0)
            return;

        bool menuOpen = ScoutModeManager.Instance != null && ScoutModeManager.Instance.isMenuOpen;
        SetVideoWindowsActive(!menuOpen);
        if (!menuOpen)
            PositionVideoWindows();
    }

    private void LateUpdate()
    {
        if (!IsLockedToSpot)
            return;

        if (ScoutModeManager.Instance != null && ScoutModeManager.Instance.isMenuOpen)
            return;

        Transform anchor = GetCurrentAnchorTransform();
        if (anchor != null)
            ApplyLockedPose(anchor);
    }

    private void ApplyLockedPose(Transform anchor)
    {
        Vector3 pos = anchor.position + anchor.TransformDirection(CameraOffset);
        rigPositioner.x = pos.x;
        rigPositioner.y = pos.y;
        rigPositioner.z = pos.z;

        if (matchSpotHeading)
            rigPositioner.rotation = Quaternion.Euler(0, anchor.eulerAngles.y, 0);
    }

    private Transform GetCurrentAnchorTransform() => currentAnchorPoint switch
    {
        RigAnchorPoints.SpotOne => spotOne?.transform,
        RigAnchorPoints.SpotTwo => spotTwo?.transform,
        _ => null
    };

    // Both modes start at fly mode (state 0) and cycle back to it.
    // Default: fly → lock SpotOne → lock SpotTwo → fly
    // Video:   fly → front cams + spots → front cams + no spots → fly
    public void CyclePresets()
    {
        if (useVideoWindows)
        {
            CycleVideoStates();
            return;
        }

        currentAnchorPoint = currentAnchorPoint switch
        {
            RigAnchorPoints.World   => RigAnchorPoints.SpotOne,
            RigAnchorPoints.SpotOne => RigAnchorPoints.SpotTwo,
            _                       => RigAnchorPoints.World
        };

        switch (currentAnchorPoint)
        {
            case RigAnchorPoints.SpotOne:
                ApplyLockedPose(spotOne.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot One");
                break;
            case RigAnchorPoints.SpotTwo:
                ApplyLockedPose(spotTwo.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot Two");
                break;
            case RigAnchorPoints.World:
                Vector3 mid = (spotOne.transform.position + spotTwo.transform.position) / 2 - new Vector3(0, 0, .5f);
                rigPositioner.x = mid.x;
                rigPositioner.z = mid.z;
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Fly");
                break;
        }
    }

    private void CycleVideoStates()
    {
        videoState = (videoState + 1) % 3;

        switch (videoState)
        {
            case 0: // fly mode
                SetVideoWindowsActive(false);
                rigPositioner.y = storedRigY;
                if (pointCloudCycler != null) pointCloudCycler.RestorePointClouds();
                if (messageManager) messageManager.ShowMessage("Camera: Fly");
                break;

            case 1: // front cams, spots visible
                storedRigY = rigPositioner.y;
                SetVideoWindowsActive(true);
                PositionVideoWindows();
                if (pointCloudCycler != null) pointCloudCycler.HideAllPointClouds();
                if (messageManager) messageManager.ShowMessage("Video Windows: Front Cams");
                break;

            case 2: // front cams, no spots
                rigPositioner.y = noRobotViewHeight;
                PositionVideoWindows();
                if (messageManager) messageManager.ShowMessage("Video Windows: No Robots");
                break;
        }
    }

    public void SetInitialPreset()
    {
        if (useVideoWindows)
        {
            videoState = 0;
            SetVideoWindowsActive(false);
            return;
        }

        currentAnchorPoint = RigAnchorPoints.World;
    }

    private void PositionVideoWindows()
    {
        if (centerEyeAnchor == null) return;
        PositionVideoWindowPair(spotOneWindow, -windowSeparationAngle / 2f);
        PositionVideoWindowPair(spotTwoWindow, windowSeparationAngle / 2f);
    }

    private void PositionVideoWindowPair(VideoWindow window, float centerAngle)
    {
        if (window == null) return;
        PositionVideoScreen(window, window.frontRight, centerAngle - windowPairAngle / 2f);
        PositionVideoScreen(window, window.frontLeft, centerAngle + windowPairAngle / 2f);
    }

    private void PositionVideoScreen(VideoWindow window, VideoScreen screenSetting, float angle)
    {
        if (screenSetting?.screen == null) return;

        Transform screen = screenSetting.screen;
        Vector3 pos = centerEyeAnchor.position
            + Quaternion.Euler(0, centerEyeAnchor.eulerAngles.y + angle, 0) * Vector3.forward * windowDistance;
        pos.y = centerEyeAnchor.position.y + windowHeightOffset;

        screen.position = pos;
        screen.LookAt(centerEyeAnchor.position);
        screen.Rotate(0, 0, GetZRotationFromOption(screenSetting.rotation), Space.Self);
        screen.localScale = new Vector3(window.width, window.height, windowZScale);
    }

    private static float GetZRotationFromOption(AllCamsPositioner.RotationOption option) => option switch
    {
        AllCamsPositioner.RotationOption.Left90        => 90f,
        AllCamsPositioner.RotationOption.Right90       => -90f,
        AllCamsPositioner.RotationOption.UpsideDown180 => 180f,
        _ => 0f
    };

    private void SetVideoWindowsActive(bool isActive)
    {
        SetVideoWindowActive(spotOneWindow, isActive);
        SetVideoWindowActive(spotTwoWindow, isActive);
    }

    private void SetVideoWindowActive(VideoWindow window, bool isActive)
    {
        if (window == null) return;
        SetVideoScreenActive(window.frontRight, isActive);
        SetVideoScreenActive(window.frontLeft, isActive);
    }

    private void SetVideoScreenActive(VideoScreen s, bool isActive)
    {
        if (s?.screen != null && s.screen.gameObject.activeSelf != isActive)
            s.screen.gameObject.SetActive(isActive);
    }
}
