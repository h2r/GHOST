using UnityEngine;

public class PanoCamPositioner : MonoBehaviour
{
    public enum RotationOption
    {
        None,
        Left90,
        Right90,
        UpsideDown180
    }

    [System.Serializable]
    public class CameraSettings
    {
        public Transform camera;
        public RotationOption rotation;
        public float width = 1.0f;
        public float height = 0.5f;
    }

    [Header("References")]
    public Transform centerEyeAnchor;
    public CameraSettings[] cameraSettings;

    [Header("Panorama Settings")]
    public float radius = 2.0f; // Distance of cameras from the center
    public float cameraHeightOffset = 0.0f; // Vertical offset from headset Y
    public float zScale = 0.001f; // For flatness

    private Vector3 panoramaCenter;
    private Quaternion initialHeadsetYawRotation; // Only yaw, to fix the panorama orientation

    void OnEnable()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogWarning("CenterEyeAnchor not assigned in PanoCamPositioner.");
            return;
        }

        panoramaCenter = centerEyeAnchor.position;
        initialHeadsetYawRotation = Quaternion.Euler(0, centerEyeAnchor.rotation.eulerAngles.y, 0);

        PositionCameras();
    }

    void Update()
    {
        if (ScoutModeManager.Instance != null && ScoutModeManager.Instance.isMenuOpen)
        {
            SetCamerasActive(false);
            return;
        }
        SetCamerasActive(true);

        PositionCameras();
    }

    void SetCamerasActive(bool isActive)
    {
        foreach (var settings in cameraSettings)
        {
            if (settings.camera != null && settings.camera.gameObject.activeSelf != isActive)
            {
                settings.camera.gameObject.SetActive(isActive);
            }
        }
    }

    void PositionCameras()
    {
        if (centerEyeAnchor == null) return;

        panoramaCenter.y = centerEyeAnchor.position.y;
        

        float baseCameraY = centerEyeAnchor.position.y + cameraHeightOffset;

        for (int i = 0; i < cameraSettings.Length; i++)
        {
            var settings = cameraSettings[i];
            if (settings.camera == null) continue;

            float angle = 0;
            if (i == 0) angle = -30;  // Front Left
            if (i == 1) angle = 30;   // Front Right
            if (i == 2) angle = 180;  // Back
            if (i == 3) angle = -90;  // Left
            if (i == 4) angle = 90;   // Right

            Vector3 pos = initialHeadsetYawRotation * Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            settings.camera.position = new Vector3(panoramaCenter.x + pos.x, baseCameraY, panoramaCenter.z + pos.z);

            Quaternion lookRotation = Quaternion.LookRotation(settings.camera.position - panoramaCenter, Vector3.up);
            settings.camera.rotation = lookRotation * Quaternion.Euler(0, 180, 0); // Base rotation to face outwards

            // Apply custom rotation
            switch (settings.rotation)
            {
                case RotationOption.Left90:
                    settings.camera.Rotate(0, 0, 90);
                    break;
                case RotationOption.Right90:
                    settings.camera.Rotate(0, 0, -90);
                    break;
                case RotationOption.UpsideDown180:
                    settings.camera.Rotate(0, 0, 180);
                    break;
            }

            // Apply scale
            settings.camera.localScale = new Vector3(settings.width, settings.height, zScale);
        }
    }
}