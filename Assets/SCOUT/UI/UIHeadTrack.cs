using UnityEngine;
using Unity.Netcode;
public class UIHeadTrack : MonoBehaviour
{
    private enum PanelState
    {
        DEFAULT,        // Panel is at its default position
        MOVING_TO_NEW,  // Moving to a new yaw-based position
        STUCK_AT_NEW,   // Holding position at a yaw-determined location
        RETURNING       // Returning to default position
    }

    [Header("Panel Settings")]
    public float yawThresholdDegrees = 45f;
    public float returnThresholdDegrees = 15f;
    public float moveSpeed = 5.0f;
    public Vector3 panelOffset = new Vector3(0f, -0.2f, 0f);

    [Header("Nod Gesture Settings")]
    public int nodCountThreshold = 2;             // Number of nods needed
    public float nodDetectionWindow = 1.2f;       // Time (seconds) to complete nods
    public float pitchThresholdDegrees = 20f;     // Minimum pitch delta for a nod

    [Header("References")]
    public Transform centerEyeAnchor;             // Should be the head/camera anchor (e.g., main camera or XR rig center)

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private float defaultYaw;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetYaw;

    private PanelState state = PanelState.DEFAULT;

    // Nod detection state
    private float lastPitch;
    private int lastPitchSign = 0;
    private float lastNodTime;
    private int nodCount = 0;

    void Start()
    {
        centerEyeAnchor=NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.transform.Find("TrackingSpace/CenterEyeAnchor");
        if (centerEyeAnchor == null)
        {
            Debug.LogError("Center Eye Anchor not assigned.");
            enabled = false;
            return;
        }

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
        defaultYaw = GetHeadYaw();
        lastPitch = GetHeadPitch();
    }

    void Update()
    {
        float currentYaw = GetHeadYaw();
        float currentPitch = GetHeadPitch();

        DetectNods(currentPitch); // Check for nod gesture to recenter

        switch (state)
        {
            case PanelState.DEFAULT:
                HandleYawMovement(currentYaw);
                break;

            case PanelState.MOVING_TO_NEW:
                MoveTowardsTarget(targetPosition, targetRotation);
                if (IsAtTarget(targetPosition, targetRotation))
                    state = PanelState.STUCK_AT_NEW;
                break;

            case PanelState.STUCK_AT_NEW:
                HandleYawAdjustOrReturn(currentYaw);
                break;

            case PanelState.RETURNING:
                MoveTowardsTarget(defaultPosition, defaultRotation);
                if (IsAtTarget(defaultPosition, defaultRotation))
                    state = PanelState.DEFAULT;
                break;
        }
    }

    void HandleYawMovement(float currentYaw)
    {
        float yawDelta = Mathf.DeltaAngle(defaultYaw, currentYaw);
        if (Mathf.Abs(yawDelta) >= yawThresholdDegrees)
        {
            targetYaw = currentYaw;
            ComputeTargetTransform(targetYaw);
            state = PanelState.MOVING_TO_NEW;
        }
    }

    void HandleYawAdjustOrReturn(float currentYaw)
    {
        float yawFromTarget = Mathf.DeltaAngle(targetYaw, currentYaw);
        float yawToDefault = Mathf.DeltaAngle(currentYaw, defaultYaw);

        if (Mathf.Abs(yawToDefault) < returnThresholdDegrees)
        {
            state = PanelState.RETURNING;
        }
        else if (Mathf.Abs(yawFromTarget) >= yawThresholdDegrees)
        {
            targetYaw = currentYaw;
            ComputeTargetTransform(targetYaw);
            state = PanelState.MOVING_TO_NEW;
        }
    }

    void DetectNods(float currentPitch)
    {
        float pitchDelta = currentPitch - lastPitch;
        int currentSign = (int)Mathf.Sign(pitchDelta);

        if (Mathf.Abs(pitchDelta) > pitchThresholdDegrees)
        {
            if (currentSign != 0 && currentSign != lastPitchSign)
            {
                nodCount++;
                lastPitchSign = currentSign;
            }

            if (Time.time - lastNodTime > nodDetectionWindow)
            {
                nodCount = 0;
            }

            lastNodTime = Time.time;
        }

        lastPitch = currentPitch;

        if (nodCount >= nodCountThreshold)
        {
            nodCount = 0;
            lastPitchSign = 0;

            // Recenter to default
            targetYaw = defaultYaw;
            ComputeTargetTransform(targetYaw);
            state = PanelState.MOVING_TO_NEW;
        }
    }

    void ComputeTargetTransform(float yaw)
    {
        Vector3 flatForward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        float distance = Vector3.Distance(centerEyeAnchor.position, defaultPosition);

        targetPosition = centerEyeAnchor.position + flatForward * distance + panelOffset;
        targetRotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    void MoveTowardsTarget(Vector3 position, Quaternion rotation)
    {
        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * moveSpeed);
    }

    bool IsAtTarget(Vector3 position, Quaternion rotation)
    {
        return Vector3.Distance(transform.position, position) < 0.01f &&
               Quaternion.Angle(transform.rotation, rotation) < 0.5f;
    }

    float GetHeadYaw()
    {
        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0f;
        forward.Normalize();

        return Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
    }

    float GetHeadPitch()
    {
        Vector3 forward = centerEyeAnchor.forward;
        return Mathf.Asin(forward.y) * Mathf.Rad2Deg;
    }
}
