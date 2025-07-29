using UnityEngine;

public class UIHeadTrack : MonoBehaviour
{
    public float yawThresholdDegrees = 30f;
    public float moveSpeed = 5f;
    public Vector3 panelOffset = new Vector3(0f, -0.2f, 0f);

    private Transform cameraTransform;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private float panelDistance;

    private Vector3 yawReferenceForward;
    private bool tracking = true;
    private bool isMoving = false;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
        panelDistance = Vector3.Distance(cameraTransform.position, transform.position);

        yawReferenceForward = GetFlatForward(cameraTransform.forward);
    }

    void Update()
    {
        if (isMoving)
        {
            // Animate position and rotation toward target
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);

            // Check if close enough to stop
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f &&
                Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;

                yawReferenceForward = GetFlatForward(cameraTransform.forward);
                isMoving = false;
            }

            return;
        }

        float headYaw = GetHeadYaw();

        if (Mathf.Abs(headYaw) >= yawThresholdDegrees && tracking)
        {
            BeginPanelMove();
        }
        else if (Mathf.Abs(headYaw) < yawThresholdDegrees)
        {
            tracking = true;
        }
    }

    float GetHeadYaw()
    {
        Vector3 currentFlatForward = GetFlatForward(cameraTransform.forward);
        return Vector3.SignedAngle(yawReferenceForward, currentFlatForward, Vector3.up);
    }

    Vector3 GetFlatForward(Vector3 vec)
    {
        vec.y = 0;
        return vec.normalized;
    }

    void BeginPanelMove()
    {
        tracking = false;
        isMoving = true;

        targetPosition = cameraTransform.position + cameraTransform.forward * panelDistance + panelOffset;
        targetRotation = Quaternion.LookRotation(targetPosition - cameraTransform.position);
    }
}
