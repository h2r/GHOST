using UnityEngine;

public class ArmCameraPositioner : MonoBehaviour
{
    [Header("References")]
    public Transform centerEyeAnchor;
    public Transform armCam1; // Top Right
    public Transform armCam2; // Top Left

    [Header("Adjustable Offsets")]
    public float armCam1YOffset = 0.5f; // Default Y offset for armCam1
    public float armCam2YOffset = 0.5f; // Default Y offset for armCam2
    public float cameraFeedWidth = 0.5f; // Desired width of the camera feed in Unity units
    public float cameraFeedHeight = 0.3f; // Desired height of the camera feed in Unity units
    public float horizontalSpacing = 0.05f; // Space between the two camera feeds
    public float forwardOffset = 1.0f; // How far in front of the headset

    void Update()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogWarning("CenterEyeAnchor not assigned in ArmCameraPositioner.");
            return;
        }

        // Calculate base position relative to centerEyeAnchor (only considering rotation)
        // This ensures the cameras move with the headset's rotation but stay at a fixed distance
        Vector3 basePosition = centerEyeAnchor.position;
        Quaternion headsetRotation = centerEyeAnchor.rotation;

        // Calculate the half width of a single camera feed, including half of the spacing
        float halfTotalWidth = (cameraFeedWidth + horizontalSpacing) / 2f;

        // Position armCam1 (Top Right)
        if (armCam1 != null)
        {
            Vector3 targetPos1 = basePosition + 
                                 headsetRotation * Vector3.forward * forwardOffset +
                                 headsetRotation * Vector3.right * halfTotalWidth +
                                 headsetRotation * Vector3.up * armCam1YOffset; // Incorporate Y offset relative to headset's up
            armCam1.position = targetPos1;
            armCam1.LookAt(centerEyeAnchor); // Make it look at the headset
            armCam1.localScale = new Vector3(cameraFeedWidth, cameraFeedHeight, 0.001f); // Apply explicit scale, keep Z very small for flatness
        }

        // Position armCam2 (Top Left)
        if (armCam2 != null)
        {
            Vector3 targetPos2 = basePosition + 
                                 headsetRotation * Vector3.forward * forwardOffset +
                                 headsetRotation * Vector3.left * halfTotalWidth +
                                 headsetRotation * Vector3.up * armCam2YOffset; // Incorporate Y offset relative to headset's up
            armCam2.position = targetPos2;
            armCam2.LookAt(centerEyeAnchor); // Make it look at the headset
            armCam2.localScale = new Vector3(cameraFeedWidth, cameraFeedHeight, 0.001f); // Apply explicit scale, keep Z very small for flatness
        }
    }
}
