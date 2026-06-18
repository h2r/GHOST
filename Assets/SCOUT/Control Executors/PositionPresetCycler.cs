using System.Collections.Generic;
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
    [Tooltip("Robot/spot roots whose renderers are hidden in the no-robot video view. Wire the spot roots; GameObjects stay active so drive/odometry keep running.")]
    public GameObject[] robotObjects;
    [Tooltip("In the no-robot view, keep showing only the ghost arm (these objects' renderers stay on while the rest of the robot is hidden).")]
    public bool showGhostArmInNoSpotView = false;
    [Tooltip("The ghost-arm objects to keep visible when 'Show Ghost Arm In No Spot View' is on (e.g. each spot's dummy gripper).")]
    public GameObject[] ghostArmObjects;
    [Tooltip("SpotMode for each spot, used to detect which spot's arm is active so the no-robot view faces that spot.")]
    public SpotMode spotOneMode, spotTwoMode;
    public Transform centerEyeAnchor;
    public VideoWindow spotOneWindow, spotTwoWindow;
    [Tooltip("Use one stitched front-camera screen per spot instead of the separate front-right/front-left screens.")]
    public bool useStitchedFrontWindows = false;
    [Tooltip("Distance of the windows from the player")]
    public float windowDistance = 2f;
    [Tooltip("Angle in degrees separating the centers of the two spots' window pairs")]
    public float windowSeparationAngle = 65f;
    [Tooltip("Angle in degrees separating the two front-cam screens within one spot's pair")]
    public float windowPairAngle = 30f;
    [Tooltip("Vertical offset of the windows from the headset")]
    public float windowHeightOffset = 0f;
    public float windowZScale = 0.001f;

    [Header("Startup Camera Facing")]
    [Tooltip("Rotate the camera rig on startup so the headset faces the stitched front-camera quads.")]
    public bool faceStitchedFrontCamsOnStart = true;
    [Tooltip("Optional explicit reference for the Spot 1 stitched front-camera quad. Falls back to the stitched window reference or GameObject.Find(\"Spot1StitchedFrontCams\").")]
    public Transform spotOneStartupFacingTarget;
    [Tooltip("Optional explicit reference for the Spot 2 stitched front-camera quad. Falls back to the stitched window reference or GameObject.Find(\"Spot2StitchedFrontCams\").")]
    public Transform spotTwoStartupFacingTarget;
    [Tooltip("Extra yaw offset in degrees after facing the midpoint of the stitched front-camera quads.")]
    public float startupFacingYawOffset = 0f;
    [Tooltip("Move the camera rig on startup so the headset is placed in front of the stitched front-camera midpoint.")]
    public bool moveToStitchedFrontCamsOnStart = true;
    [Tooltip("Distance in metres from the headset to the stitched front-camera midpoint after startup placement.")]
    [Min(0.1f)] public float startupFacingDistance = 3f;
    [Tooltip("Headset height offset from the stitched front-camera midpoint after startup placement.")]
    public float startupFacingHeightOffset = 0f;

    [Header("No-robot view panel adjust")]
    [Tooltip("Metres per second the panels pan (left/right/up/down) at full stick. Driven by the fly controller's joystick.")]
    public float panelMoveSpeed = 1.5f;
    [Tooltip("Metres per second the panels move in depth at full stick.")]
    public float panelDepthSpeed = 1.5f;
    public float minWindowDistance = 0.5f;
    public float maxWindowDistance = 6f;
    private float panelLateralOffset = 0f;

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
        [Tooltip("Optional stitched front-camera quad driven by SpotFrontStitcher.")]
        public VideoScreen stitchedFront;
        public float width = 1f;
        public float height = 0.75f;
    }

    public enum RigAnchorPoints { World, SpotOne, SpotTwo }
    public RigAnchorPoints currentAnchorPoint = RigAnchorPoints.World;
    // Locked while on a lock-spot preset, or in the no-robot video view (which faces the active arm spot).
    public bool IsLockedToSpot => currentAnchorPoint != RigAnchorPoints.World || (useVideoWindows && videoState == 2);

    // Default mode: 0 = fly (exo), 1 = lock SpotOne, 2 = lock SpotTwo
    // Video mode:   0 = fly (exo), 1 = video+spots, 2 = video+no-spots
    private int videoState = 0;

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

    public bool IsNoRobotVideoView => useVideoWindows && videoState == 2;

    // Called by the fly controller mode while in the no-robot view: pan the panels with the stick,
    // or push them closer/further while the depth modifier (trigger) is held.
    public void AdjustPanels(Vector2 stick, bool depthMode)
    {
        if (!IsNoRobotVideoView || stick.sqrMagnitude < 0.01f)
            return;

        if (depthMode)
        {
            windowDistance = Mathf.Clamp(windowDistance + stick.y * panelDepthSpeed * Time.deltaTime,
                                         minWindowDistance, maxWindowDistance);
        }
        else
        {
            panelLateralOffset += stick.x * panelMoveSpeed * Time.deltaTime;
            windowHeightOffset += stick.y * panelMoveSpeed * Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (ScoutModeManager.Instance != null && ScoutModeManager.Instance.isMenuOpen)
            return;

        Transform anchor;
        if (useVideoWindows && videoState == 2)
            anchor = GetActiveArmSpotAnchor();      // no-robot view: face the arm-controlled spot
        else if (currentAnchorPoint != RigAnchorPoints.World)
            anchor = GetCurrentAnchorTransform();   // lock-spot presets
        else
            return;

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

    // The spot body to face in the no-robot view: whichever spot's arm is currently being controlled.
    private Transform GetActiveArmSpotAnchor()
    {
        SpotMode active = ScoutModeManager.Instance != null ? ScoutModeManager.Instance.GetActiveArmSpot() : null;
        if (active == null) return null;
        if (active == spotOneMode) return spotOne != null ? spotOne.transform : null;
        if (active == spotTwoMode) return spotTwo != null ? spotTwo.transform : null;
        return null;
    }

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

        // The rig is never moved between states: video windows follow the head and
        // robots are hidden in place, so drive/arm controls behave exactly like fly mode.
        switch (videoState)
        {
            case 0: // fly mode
                SetVideoWindowsActive(false);
                SetRobotVisible(true);
                if (pointCloudCycler != null) pointCloudCycler.RestorePointClouds();
                if (messageManager) messageManager.ShowMessage("Camera: Fly");
                break;

            case 1: // front cams, robots visible
                SetVideoWindowsActive(true);
                SetRobotVisible(true);
                PositionVideoWindows();
                if (pointCloudCycler != null) pointCloudCycler.HideAllPointClouds();
                if (messageManager) messageManager.ShowMessage("Video Windows: Front Cams");
                break;

            case 2: // front cams, robots hidden in place
                SetRobotVisible(false);
                PositionVideoWindows();
                if (messageManager) messageManager.ShowMessage("Video Windows: No Robots");
                break;
        }
    }

    public void SetInitialPreset()
    {
        FaceStitchedFrontCamsOnStart();

        if (useVideoWindows)
        {
            videoState = 0;
            SetVideoWindowsActive(false);
            SetRobotVisible(true);
            return;
        }

        currentAnchorPoint = RigAnchorPoints.World;
    }

    private void FaceStitchedFrontCamsOnStart()
    {
        if (!faceStitchedFrontCamsOnStart || rigPositioner == null || centerEyeAnchor == null)
            return;

        if (!TryGetStartupFacingTarget(out Vector3 target))
            return;

        Vector3 direction = target - centerEyeAnchor.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;
        direction.Normalize();

        float targetWorldYaw = Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y + startupFacingYawOffset;
        float currentRigYaw = rigPositioner.transform.eulerAngles.y;
        float headsetYawRelativeToRig = Mathf.DeltaAngle(currentRigYaw, centerEyeAnchor.eulerAngles.y);
        Quaternion startupRotation = Quaternion.Euler(0f, targetWorldYaw - headsetYawRelativeToRig, 0f);

        if (moveToStitchedFrontCamsOnStart)
        {
            Vector3 desiredEyePosition = target - direction * Mathf.Max(0.1f, startupFacingDistance);
            desiredEyePosition.y = target.y + startupFacingHeightOffset;

            Vector3 localEyeOffset = rigPositioner.transform.InverseTransformPoint(centerEyeAnchor.position);
            Vector3 desiredRigPosition = desiredEyePosition - startupRotation * localEyeOffset;
            rigPositioner.pos = desiredRigPosition;
            rigPositioner.transform.position = desiredRigPosition;
        }

        rigPositioner.rotation = startupRotation;
        rigPositioner.transform.rotation = startupRotation;
    }

    private bool TryGetStartupFacingTarget(out Vector3 target)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        AddStartupFacingTarget(
            spotOneStartupFacingTarget != null
                ? spotOneStartupFacingTarget
                : spotOneWindow?.stitchedFront?.screen != null
                    ? spotOneWindow.stitchedFront.screen
                    : FindStartupFacingTarget("Spot1StitchedFrontCams"));

        AddStartupFacingTarget(
            spotTwoStartupFacingTarget != null
                ? spotTwoStartupFacingTarget
                : spotTwoWindow?.stitchedFront?.screen != null
                    ? spotTwoWindow.stitchedFront.screen
                    : FindStartupFacingTarget("Spot2StitchedFrontCams"));

        if (count > 0)
        {
            target = sum / count;
            return true;
        }

        target = default;
        return false;

        void AddStartupFacingTarget(Transform t)
        {
            if (t == null) return;
            sum += t.position;
            count++;
        }
    }

    private static Transform FindStartupFacingTarget(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
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
        if (ShouldUseStitchedFront(window))
        {
            PositionVideoScreen(window, window.stitchedFront, centerAngle);
            return;
        }

        PositionVideoScreen(window, window.frontRight, centerAngle - windowPairAngle / 2f);
        PositionVideoScreen(window, window.frontLeft, centerAngle + windowPairAngle / 2f);
    }

    private void PositionVideoScreen(VideoWindow window, VideoScreen screenSetting, float angle)
    {
        if (screenSetting?.screen == null) return;

        Transform screen = screenSetting.screen;
        float headYaw = centerEyeAnchor.eulerAngles.y;
        Vector3 pos = centerEyeAnchor.position
            + Quaternion.Euler(0, headYaw + angle, 0) * Vector3.forward * windowDistance
            + Quaternion.Euler(0, headYaw, 0) * Vector3.right * panelLateralOffset;
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

    // Renderers under each robot root, cached with their original enabled state so hiding/showing is reversible.
    private Renderer[] robotRenderers;
    private bool[] robotRenderersWereEnabled;

    private void CacheRobotRenderers()
    {
        if (robotRenderers != null) return;
        List<Renderer> found = new();
        if (robotObjects != null)
            foreach (GameObject go in robotObjects)
                if (go != null)
                    found.AddRange(go.GetComponentsInChildren<Renderer>(true));
        robotRenderers = found.ToArray();
        robotRenderersWereEnabled = new bool[robotRenderers.Length];
        for (int i = 0; i < robotRenderers.Length; i++)
            robotRenderersWereEnabled[i] = robotRenderers[i].enabled;
    }

    // Show/hide the robots by toggling renderers (GameObjects stay active, so drive/odometry keep running).
    // When hiding with showGhostArmInNoSpotView on, the ghost-arm renderers are kept visible.
    private void SetRobotVisible(bool visible)
    {
        CacheRobotRenderers();
        for (int i = 0; i < robotRenderers.Length; i++)
        {
            Renderer r = robotRenderers[i];
            if (r == null) continue;
            if (visible)
                r.enabled = robotRenderersWereEnabled[i];
            else
                r.enabled = robotRenderersWereEnabled[i] && showGhostArmInNoSpotView && IsUnderGhostArm(r.transform);
        }
    }

    private bool IsUnderGhostArm(Transform t)
    {
        if (ghostArmObjects == null) return false;
        foreach (GameObject g in ghostArmObjects)
            if (g != null && t.IsChildOf(g.transform))
                return true;
        return false;
    }

    private void SetVideoWindowsActive(bool isActive)
    {
        SetVideoWindowActive(spotOneWindow, isActive);
        SetVideoWindowActive(spotTwoWindow, isActive);
    }

    private void SetVideoWindowActive(VideoWindow window, bool isActive)
    {
        if (window == null) return;

        bool useStitched = isActive && ShouldUseStitchedFront(window);
        SetVideoScreenActive(window.stitchedFront, useStitched);
        SetVideoScreenActive(window.frontRight, isActive && !useStitched);
        SetVideoScreenActive(window.frontLeft, isActive && !useStitched);
    }

    private void SetVideoScreenActive(VideoScreen s, bool isActive)
    {
        if (s?.screen != null && s.screen.gameObject.activeSelf != isActive)
            s.screen.gameObject.SetActive(isActive);
    }

    private bool ShouldUseStitchedFront(VideoWindow window)
    {
        return useStitchedFrontWindows && window?.stitchedFront?.screen != null;
    }
}
