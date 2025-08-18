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

    [Header("Layout Settings")]
    public bool swapLayouts = false;
    public float horizontalSpacing = 2.5f;
    public float verticalPadding = 0.1f;
    public float cameraWidth = 1.0f;
    public float cameraHeight = 0.75f;
    public float zScale = 0.001f;
    public Vector3 layoutOffset = new Vector3(0, 0, 2);

    void Update()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogError("CenterEyeAnchor not assigned in AllCamsPositioner.");
            return;
        }

        if (ScoutModeManager.Instance != null && ScoutModeManager.Instance.isMenuOpen)
        {
            SetAllCamerasActive(false);
            return;
        }
        SetAllCamerasActive(true);

        // Calculate the base positions for the left and right layouts
        Vector3 leftLayoutBase = centerEyeAnchor.position + centerEyeAnchor.forward * layoutOffset.z - centerEyeAnchor.right * (horizontalSpacing / 2);
        Vector3 rightLayoutBase = centerEyeAnchor.position + centerEyeAnchor.forward * layoutOffset.z + centerEyeAnchor.right * (horizontalSpacing / 2);

        if (swapLayouts)
        {
            PositionSingleColumnLayout(spot2Cams, leftLayoutBase);
            PositionSingleColumnLayout(spot1Cams, rightLayoutBase);
        }
        else
        {
            PositionSingleColumnLayout(spot1Cams, leftLayoutBase);
            PositionSingleColumnLayout(spot2Cams, rightLayoutBase);
        }
    }

    void PositionSingleColumnLayout(SpotCams cams, Vector3 basePosition)
    {
        // Row 1: Front-left and Front-right
        PositionCamera(cams.frontLeftCam, basePosition + centerEyeAnchor.up * (cameraHeight + verticalPadding) - centerEyeAnchor.right * (cameraWidth / 2));
        PositionCamera(cams.frontRightCam, basePosition + centerEyeAnchor.up * (cameraHeight + verticalPadding) + centerEyeAnchor.right * (cameraWidth / 2));

        // Row 2: Left and Right
        PositionCamera(cams.leftCam, basePosition - centerEyeAnchor.right * (cameraWidth / 2));
        PositionCamera(cams.rightCam, basePosition + centerEyeAnchor.right * (cameraWidth / 2));

        // Row 3: Back
        PositionCamera(cams.backCam, basePosition - centerEyeAnchor.up * (cameraHeight + verticalPadding));
    }

    void PositionCamera(CameraSetting setting, Vector3 position)
    {
        if (setting == null || setting.camera == null) return;

        setting.camera.position = position;
        setting.camera.rotation = centerEyeAnchor.rotation;

        // Apply custom rotation
        switch (setting.rotation)
        {
            case RotationOption.Left90:
                setting.camera.Rotate(0, 0, 90, Space.Self);
                break;
            case RotationOption.Right90:
                setting.camera.Rotate(0, 0, -90, Space.Self);
                break;
            case RotationOption.UpsideDown180:
                setting.camera.Rotate(0, 0, 180, Space.Self);
                break;
        }

        setting.camera.localScale = new Vector3(cameraWidth, cameraHeight, zScale);
    }

    void SetAllCamerasActive(bool isActive)
    {
        SetSpotCamerasActive(spot1Cams, isActive);
        SetSpotCamerasActive(spot2Cams, isActive);
    }

    void SetSpotCamerasActive(SpotCams cams, bool isActive)
    {
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
