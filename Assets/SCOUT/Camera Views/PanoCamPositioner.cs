using UnityEngine;

public class PanoCamPositioner : MonoBehaviour
{
    [Header("References")]
    public Transform centerEyeAnchor;
    public Transform frontLeftCam;
    public Transform frontRightCam;
    public Transform backCam;
    public Transform leftCam;
    public Transform rightCam;

    [Header("Panorama Settings")]
    public float radius = 2.0f; // Distance of cameras from the center
    public float cameraHeightOffset = 0.0f; // Vertical offset from headset Y
    public float cameraWidth = 1.0f; // Width of each camera feed
    public float cameraHeight = 0.5f; // Height of each camera feed
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

        // Capture the initial position and yaw rotation of the headset
        panoramaCenter = centerEyeAnchor.position;
        initialHeadsetYawRotation = Quaternion.Euler(0, centerEyeAnchor.rotation.eulerAngles.y, 0);

        PositionCameras();
    }

    void Update()
    {
        // In case the centerEyeAnchor moves significantly after OnEnable, re-position
        // Or if parameters are changed in editor during runtime
        PositionCameras();
    }

    void PositionCameras()
    {
        if (centerEyeAnchor == null) return;

        // Calculate the base Y position for all cameras
        float baseCameraY = centerEyeAnchor.position.y + cameraHeightOffset;

        // Apply scale to all cameras
        Vector3 camScale = new Vector3(cameraWidth, cameraHeight, zScale);

        // Front Left Camera
        if (frontLeftCam != null)
        {
            Vector3 pos = initialHeadsetYawRotation * Quaternion.Euler(0, -30, 0) * Vector3.forward * radius; // -30 degrees for left
            frontLeftCam.position = new Vector3(panoramaCenter.x + pos.x, baseCameraY, panoramaCenter.z + pos.z);
            frontLeftCam.LookAt(panoramaCenter); // Look towards the center
            frontLeftCam.Rotate(0, 180, 0); // Flip to face outwards
            frontLeftCam.localScale = camScale;
        }

        // Front Right Camera
        if (frontRightCam != null)
        {
            Vector3 pos = initialHeadsetYawRotation * Quaternion.Euler(0, 30, 0) * Vector3.forward * radius; // +30 degrees for right
            frontRightCam.position = new Vector3(panoramaCenter.x + pos.x, baseCameraY, panoramaCenter.z + pos.z);
            frontRightCam.LookAt(panoramaCenter);
            frontRightCam.Rotate(0, 180, 0);
            frontRightCam.localScale = camScale;
        }

        // Back Camera
        if (backCam != null)
        {
            Vector3 pos = initialHeadsetYawRotation * Quaternion.Euler(0, 180, 0) * Vector3.forward * radius;
            backCam.position = new Vector3(panoramaCenter.x + pos.x, baseCameraY, panoramaCenter.z + pos.z);
            backCam.LookAt(panoramaCenter);
            backCam.Rotate(0, 180, 0);
            backCam.localScale = camScale;
        }

        // Left Camera
        if (leftCam != null)
        {
            Vector3 pos = initialHeadsetYawRotation * Quaternion.Euler(0, -90, 0) * Vector3.forward * radius;
            leftCam.position = new Vector3(panoramaCenter.x + pos.x, baseCameraY, panoramaCenter.z + pos.z);
            leftCam.LookAt(panoramaCenter);
            leftCam.Rotate(0, 180, 0);
            leftCam.localScale = camScale;
        }

        // Right Camera
        if (rightCam != null)
        {
            Vector3 pos = initialHeadsetYawRotation * Quaternion.Euler(0, 90, 0) * Vector3.forward * radius;
            rightCam.position = new Vector3(panoramaCenter.x + pos.x, baseCameraY, panoramaCenter.z + pos.z);
            rightCam.LookAt(panoramaCenter);
            rightCam.Rotate(0, 180, 0);
            rightCam.localScale = camScale;
        }
    }
}
