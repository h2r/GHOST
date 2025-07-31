using UnityEngine;

public class UIHeadTrack : MonoBehaviour
{
    private enum PanelState
    {
        DEFAULT,       // Stuck at default position
        MOVING_TO_NEW, // Smoothly moving to new front-facing position (yaw-based)
        STUCK_AT_NEW,  // Stuck at last moved position
        RETURNING      // Smoothly returning to default
    }

    [Header("Settings")]
    public float yawThresholdDegrees = 45f;      // Angle to trigger move to new front position
    public float returnThresholdDegrees = 15f;   // Angle to trigger return to default
    public float moveSpeed = 5.0f;                // Speed of smooth movement/rotation
    public Vector3 panelOffset = new Vector3(0f, -0.2f, 0f);

    [Header("References")]
    public Transform centerEyeAnchor;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private float defaultYaw;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetYaw;

    private PanelState state = PanelState.DEFAULT;

    void Start()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogError("Assign centerEyeAnchor in inspector.");
            enabled = false;
            return;
        }

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
        defaultYaw = GetHeadYaw();
    }

    void Update()
    {
        float currentYaw = GetHeadYaw();

        switch (state)
        {
            case PanelState.DEFAULT:
                // Panel stuck at default position
                float yawFromDefault = Mathf.DeltaAngle(defaultYaw, currentYaw);
                if (Mathf.Abs(yawFromDefault) >= yawThresholdDegrees)
                {
                    // Move to new yaw position, based on current head yaw
                    targetYaw = currentYaw;
                    ComputeTargetPosition(targetYaw);
                    state = PanelState.MOVING_TO_NEW;
                }
                break;

            case PanelState.MOVING_TO_NEW:
                MoveTowardsTarget(targetPosition, targetRotation);

                if (IsAtTarget(targetPosition, targetRotation))
                {
                    state = PanelState.STUCK_AT_NEW;
                }
                break;

            case PanelState.STUCK_AT_NEW:
                // Panel stuck at last moved position (targetYaw)
                float yawFromTarget = Mathf.DeltaAngle(targetYaw, currentYaw);

                // Return if looking back near default yaw
                float yawToDefault = Mathf.DeltaAngle(currentYaw, defaultYaw);
                if (Mathf.Abs(yawToDefault) < returnThresholdDegrees)
                {
                    state = PanelState.RETURNING;
                }
                // Optional: update panel target if user keeps looking far away in a different direction?
                else if (Mathf.Abs(yawFromTarget) >= yawThresholdDegrees)
                {
                    // User looks further left/right — update target smoothly
                    targetYaw = currentYaw;
                    ComputeTargetPosition(targetYaw);
                    state = PanelState.MOVING_TO_NEW;
                }
                break;

            case PanelState.RETURNING:
                MoveTowardsTarget(defaultPosition, defaultRotation);

                if (IsAtTarget(defaultPosition, defaultRotation))
                {
                    state = PanelState.DEFAULT;
                }
                break;
        }
    }

    void ComputeTargetPosition(float yaw)
    {
        // Compute a forward vector from yaw angle relative to world forward
        Vector3 flatForward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;

        float distance = Vector3.Distance(centerEyeAnchor.position, defaultPosition);

        targetPosition = centerEyeAnchor.position + flatForward * distance + panelOffset;
        targetRotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    void MoveTowardsTarget(Vector3 pos, Quaternion rot)
    {
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * moveSpeed);
    }

    bool IsAtTarget(Vector3 pos, Quaternion rot)
    {
        return Vector3.Distance(transform.position, pos) < 0.01f &&
               Quaternion.Angle(transform.rotation, rot) < 0.5f;
    }

    float GetHeadYaw()
    {
        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0;
        forward.Normalize();

        return Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
    }
}
