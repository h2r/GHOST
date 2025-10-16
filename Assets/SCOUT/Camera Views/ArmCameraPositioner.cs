  using UnityEngine;

public class ArmCameraPositioner : MonoBehaviour
{
    public enum LayoutMode
    {
        Front,
        Wrist
    }

    public enum WristLayoutMode
    {
        Respective,
        BothOnRight,
        BothOnLeft
    }

    public enum RotationMode
    {
        Default, // LookAtHeadset for Front, AlignWithController for Wrist
        LookAtHeadset, // Always look at the headset
        Manual
    }

    public enum WristRotation
    {
        None,
        FlipVertical,
        TurnLeft,
        TurnRight
    }

    [Header("Layout Settings")]
    public LayoutMode layoutMode = LayoutMode.Front;
    [Tooltip("Only used when Layout Mode is set to Wrist")]
    public WristLayoutMode wristLayoutMode = WristLayoutMode.Respective;

    [Header("Rotation Settings")]
    public RotationMode rotationMode = RotationMode.Default;
    [Tooltip("Manual rotation for both cameras")]
    public Vector3 commonRotation = Vector3.zero;
    [Tooltip("Manual rotation offset for camera 1")]
    public Vector3 cam1RotationOffset = Vector3.zero;
    [Tooltip("Manual rotation offset for camera 2")]
    public Vector3 cam2RotationOffset = Vector3.zero;

    [Header("Wrist Rotation Toggles")]
    [Tooltip("Additional rotation for the first camera in Wrist mode")]
    public WristRotation wristCam1Rotation = WristRotation.None;
    [Tooltip("Additional rotation for the second camera in Wrist mode")]
    public WristRotation wristCam2Rotation = WristRotation.None;


    [Header("References")]
    public Transform centerEyeAnchor;
    public Transform armCam1; // Top Right in Front Layout, or first camera in Wrist Layout
    public Transform armCam2; // Top Left in Front Layout, or second camera in Wrist Layout
    public Transform leftController;
    public Transform rightController;


    [Header("Front Layout Settings")]
    public float frontFeedWidth = 0.5f;
    public float frontFeedHeight = 0.3f;
    public Vector3 frontCam1Offset = new Vector3(0.25f, 0.5f, 1.0f);
    public Vector3 frontCam2Offset = new Vector3(-0.25f, 0.5f, 1.0f);


    [Header("Wrist Layout Settings")]
    public float wristFeedWidth = 0.3f;
    public float wristFeedHeight = 0.2f;
    [Tooltip("A common offset for both wrist cameras, should be 'above' the controller.")]
    public Vector3 commonWristOffset = new Vector3(0, 0.1f, 0);
    [Tooltip("A specific offset for the first wrist camera, added to the common offset")]
    public Vector3 wristCam1Offset = Vector3.zero;
    [Tooltip("A specific offset for the second wrist camera, added to the common offset")]
    public Vector3 wristCam2Offset = Vector3.zero;


    void Update()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogWarning("CenterEyeAnchor not assigned in ArmCameraPositioner.");
            return;
        }

        switch (layoutMode)
        {
            case LayoutMode.Front:
                PositionFrontLayout();
                break;
            case LayoutMode.Wrist:
                PositionWristLayout();
                break;
        }
    }

    void PositionFrontLayout()
    {
        Vector3 basePosition = centerEyeAnchor.position;
        Quaternion headsetRotation = centerEyeAnchor.rotation;

        if (armCam1 != null)
        {
            Vector3 targetPos1 = basePosition + headsetRotation * frontCam1Offset;
            armCam1.position = targetPos1;
            armCam1.localScale = new Vector3(frontFeedWidth, frontFeedHeight, 0.001f);

            switch (rotationMode)
            {
                case RotationMode.Default:
                case RotationMode.LookAtHeadset:
                    armCam1.LookAt(centerEyeAnchor);
                    break;
                case RotationMode.Manual:
                    armCam1.rotation = Quaternion.Euler(commonRotation + cam1RotationOffset);
                    break;
            }
        }

        if (armCam2 != null)
        {
            Vector3 targetPos2 = basePosition + headsetRotation * frontCam2Offset;
            armCam2.position = targetPos2;
            armCam2.localScale = new Vector3(frontFeedWidth, frontFeedHeight, 0.001f);

            switch (rotationMode)
            {
                case RotationMode.Default:
                case RotationMode.LookAtHeadset:
                    armCam2.LookAt(centerEyeAnchor);
                    break;
                case RotationMode.Manual:
                    armCam2.rotation = Quaternion.Euler(commonRotation + cam2RotationOffset);
                    break;
            }
        }
    }

    void PositionWristLayout()
    {
        if (leftController == null || rightController == null)
        {
            Debug.LogWarning("Controller references not set for Wrist Layout.");
            return;
        }

        switch (wristLayoutMode)
        {
            case WristLayoutMode.Respective:
                PositionCameraOnWrist(armCam1, rightController, commonWristOffset + wristCam1Offset, wristCam1Rotation);
                PositionCameraOnWrist(armCam2, leftController, commonWristOffset + wristCam2Offset, wristCam2Rotation);
                break;
            case WristLayoutMode.BothOnRight:
                PositionCameraOnWrist(armCam1, rightController, commonWristOffset + wristCam1Offset, wristCam1Rotation);
                PositionCameraOnWrist(armCam2, rightController, commonWristOffset + wristCam2Offset, wristCam2Rotation);
                break;
            case WristLayoutMode.BothOnLeft:
                PositionCameraOnWrist(armCam1, leftController, commonWristOffset + wristCam1Offset, wristCam1Rotation);
                PositionCameraOnWrist(armCam2, leftController, commonWristOffset + wristCam2Offset, wristCam2Rotation);
                break;
        }
    }

    void PositionCameraOnWrist(Transform camera, Transform controller, Vector3 offset, WristRotation rotationToggle)
    {
        if (camera == null || controller == null) return;

        // Position the camera above the controller, regardless of controller rotation
        camera.position = controller.position + (controller.rotation * commonWristOffset) + offset;
        camera.localScale = new Vector3(wristFeedWidth, wristFeedHeight, 0.001f);

        // Base rotation
        switch (rotationMode)
        {
            case RotationMode.Default:
                camera.rotation = controller.rotation;
                break;
            case RotationMode.LookAtHeadset:
                camera.LookAt(centerEyeAnchor);
                break;
            case RotationMode.Manual:
                Vector3 rotationOffset = (camera == armCam1) ? cam1RotationOffset : cam2RotationOffset;
                camera.rotation = Quaternion.Euler(commonRotation + rotationOffset);
                break;
        }

        // Apply additional toggle-based rotation
        Quaternion additionalRotation = Quaternion.identity;
        switch (rotationToggle)
        {
            case WristRotation.FlipVertical:
                additionalRotation = Quaternion.Euler(0, 0, 180);
                break;
            case WristRotation.TurnLeft:
                additionalRotation = Quaternion.Euler(0, 90, 0);
                break;
            case WristRotation.TurnRight:
                additionalRotation = Quaternion.Euler(0, -90, 0);
                break;
        }
        camera.rotation *= additionalRotation;
    }
}