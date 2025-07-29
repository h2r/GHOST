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

    void Start()
    {
        cameraTransform = Camera.main.transform;

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;

        // Calculate initial distance from camera to panel
        panelDistance = Vector3.Distance(cameraTransform.position, transform.position);
    }

    void Update()
    {
        float headYaw = GetHeadYaw();

        if (Mathf.Abs(headYaw) >= yawThresholdDegrees)
        {
            MovePanelInFront();
            panelMoved = true;
        }
        else if (panelMoved)
        {
            ReturnPanelToDefault();
        }
    }

    float GetHeadYaw()
    {
        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        float yawAngle = Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
        return yawAngle;
    }

    void MovePanelInFront()
    {
        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * panelDistance + panelOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
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