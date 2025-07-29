using UnityEngine;

public class UIHeadTrack : MonoBehaviour
{
    public float yawThresholdDegrees = 45f;
    public float moveSpeed = 3.5f;

    public Vector3 panelOffset = new Vector3(0f, -0.2f, 0f);

    private Transform cameraTransform;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private float panelDistance;

    private bool panelMoved = false;
    private bool tracking = true;

    private Vector3 yawReferenceForward;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;

        // Use initial distance from camera to panel as panelDistance
        panelDistance = Vector3.Distance(cameraTransform.position, transform.position);

        // Set initial forward direction as yaw baseline
        yawReferenceForward = GetFlatForward(cameraTransform.forward);
    }

    void Update()
    {
        float headYaw = GetHeadYaw();

        if (Mathf.Abs(headYaw) >= yawThresholdDegrees && tracking)
        {
            MovePanelInFront();
        }
        else if (Mathf.Abs(headYaw) < yawThresholdDegrees)
        {
            // Once user looks back toward center, allow re-trigger
            tracking = true;
        }
    }

    float GetHeadYaw()
    {
        Vector3 currentFlatForward = GetFlatForward(cameraTransform.forward);
        float yawAngle = Vector3.SignedAngle(yawReferenceForward, currentFlatForward, Vector3.up);
        return yawAngle;
    }

    Vector3 GetFlatForward(Vector3 vec)
    {
        vec.y = 0;
        return vec.normalized;
    }

    void MovePanelInFront()
    {
        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * panelDistance + panelOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        Quaternion targetRotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);

        // Reset yaw center to the new direction
        yawReferenceForward = GetFlatForward(cameraTransform.forward);

        panelMoved = true;
        tracking = false; // Disable further triggering until reset
    }

    void ReturnPanelToDefault()
    {
        transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, defaultRotation, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, defaultPosition) < 0.01f &&
            Quaternion.Angle(transform.rotation, defaultRotation) < 0.5f)
        {
            panelMoved = false;
        }
    }
}
