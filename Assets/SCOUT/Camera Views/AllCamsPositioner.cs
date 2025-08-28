using UnityEngine;

public class AllCamsPositioner : MonoBehaviour
{
    public enum RotationOption
    {
        None,
        Left90,
        Right90,
        UpsideDown180
    }

    [System.Serializable]
    public class CameraSetting
    {
        public Transform camera;
        public RotationOption rotation = RotationOption.None;
    }

    [System.Serializable]
    public class SpotCams
    {
        public CameraSetting frontLeftCam;
        public CameraSetting frontRightCam;
        public CameraSetting backCam;
        public CameraSetting leftCam;
        public CameraSetting rightCam;
    }

    [Header("References")]
    public Transform centerEyeAnchor;

    [Header("Spot Camera References")]
    public SpotCams spot1Cams;
    public SpotCams spot2Cams;

    [Header("Curved Grid Layout Settings")]
    [Tooltip("The distance of the camera grid from the player.")]
    public float radius = 3f;
    [Tooltip("The vertical distance between rows of cameras.")]
    public float verticalPadding = 0.05f;
    [Tooltip("The angle in degrees separating the center of the two columns.")]
    public float columnSeparationAngle = 45f;
    [Tooltip("The angle in degrees separating cameras within a single column.")]
    public float columnWidthAngle = 16.6f;
    [Tooltip("A global position offset for the entire camera rig.")]
    public Vector3 globalOffset = new Vector3(0, 0, 0);

    [Header("Camera View Settings")]
    public float cameraWidth = 0.75f;
    public float cameraHeight = 0.75f;
    public float zScale = 0.001f;
    public bool swapLayouts = false;

    void Update()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogError("CenterEyeAnchor not assigned in AllCamsPositioner.");
            return;
        }

        bool shouldBeActive = ScoutModeManager.Instance == null || !ScoutModeManager.Instance.isMenuOpen;
        SetAllCamerasActive(shouldBeActive);

        if (shouldBeActive)
        {
            PositionCameraRig();
        }
    }

    void PositionCameraRig()
    {
        float leftColumnBaseAngle = -columnSeparationAngle / 2f;
        float rightColumnBaseAngle = columnSeparationAngle / 2f;

        if (swapLayouts)
        {
            PositionSingleColumnLayout(spot2Cams, leftColumnBaseAngle);
            PositionSingleColumnLayout(spot1Cams, rightColumnBaseAngle);
        }
        else
        {
            PositionSingleColumnLayout(spot1Cams, leftColumnBaseAngle);
            PositionSingleColumnLayout(spot2Cams, rightColumnBaseAngle);
        }
    }

    void PositionSingleColumnLayout(SpotCams cams, float baseAngle)
    {
        float halfWidthAngle = columnWidthAngle / 2f;
        float row1Y = centerEyeAnchor.position.y + globalOffset.y + cameraHeight + verticalPadding;
        float row2Y = centerEyeAnchor.position.y + globalOffset.y;
        float row3Y = centerEyeAnchor.position.y + globalOffset.y - cameraHeight - verticalPadding;

        // Row 1: Front-left and Front-right
        PositionCamera(cams.frontLeftCam, baseAngle - halfWidthAngle, row1Y);
        PositionCamera(cams.frontRightCam, baseAngle + halfWidthAngle, row1Y);

        // Row 2: Left and Right
        PositionCamera(cams.leftCam, baseAngle - halfWidthAngle, row2Y);
        PositionCamera(cams.rightCam, baseAngle + halfWidthAngle, row2Y);

        // Row 3: Back (centered)
        PositionCamera(cams.backCam, baseAngle, row3Y);
    }

    void PositionCamera(CameraSetting setting, float angle, float yPos)
    {
        if (setting == null || setting.camera == null) return;

        // Calculate position on a cylinder around the player
        Vector3 centerPoint = centerEyeAnchor.position + new Vector3(globalOffset.x, 0, globalOffset.z);
        Vector3 position = centerPoint + (Quaternion.Euler(0, centerEyeAnchor.eulerAngles.y + angle, 0) * Vector3.forward * radius);
        position.y = yPos;

        setting.camera.position = position;
        setting.camera.LookAt(centerEyeAnchor.position);

        // Apply custom Z-axis rotation
        setting.camera.Rotate(0, 0, GetZRotationFromOption(setting.rotation), Space.Self);

        setting.camera.localScale = new Vector3(cameraWidth, cameraHeight, zScale);
    }

    private float GetZRotationFromOption(RotationOption option)
    {
        switch (option)
        {
            case RotationOption.Left90: return 90f;
            case RotationOption.Right90: return -90f;
            case RotationOption.UpsideDown180: return 180f;
            default: return 0f;
        }
    }

    void SetAllCamerasActive(bool isActive)
    {
        SetSpotCamerasActive(spot1Cams, isActive);
        SetSpotCamerasActive(spot2Cams, isActive);
    }

    void SetSpotCamerasActive(SpotCams cams, bool isActive)
    {
        if (cams == null) return;
        SetCameraActive(cams.frontLeftCam, isActive);
        SetCameraActive(cams.frontRightCam, isActive);
        SetCameraActive(cams.backCam, isActive);
        SetCameraActive(cams.leftCam, isActive);
        SetCameraActive(cams.rightCam, isActive);
    }

    void SetCameraActive(CameraSetting setting, bool isActive)
    {
        if (setting != null && setting.camera != null && setting.camera.gameObject.activeSelf != isActive)
        {
            setting.camera.gameObject.SetActive(isActive);
        }
    }
}
