using MathNet.Numerics.Statistics;
using UnityEngine;

public class UIHeadTrack : MonoBehaviour
{
    private enum PanelState {
        DEFAULT, 
        MOVING, 
        MOVED
    }

    public float yawThresholdDegrees = 45f;
    public float moveSpeed = 3.5f;
    public Vector3 panelOffset = new Vector3(0f, -0.2f, 0f);

    private Transform cameraTransform;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    private Vector3 targetPosition;
    private Quaternion targetRotation; 

    private float panelDistance;
    private PanelState state = PanelState.DEFAULT;
    private float baseYaw; 

    void Start()
    {
        cameraTransform = Camera.main.transform;

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;

        // Calculate initial distance from camera to panel
        panelDistance = Vector3.Distance(cameraTransform.position, transform.position);
        baseYaw = 0; 
    }

    void Update()
    {
        float headYaw = GetHeadYaw();
        if (state != PanelState.MOVING  && Mathf.Abs(baseYaw - headYaw) >= yawThresholdDegrees)
        {
            ComputeNewPanelTarget();
            state = PanelState.MOVING;


        }
        else if (state == PanelState.MOVING)
        {
            MovePanelInFront();
        }
        else if (state == PanelState.MOVED)
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
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f &&
            Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
        {
            state = PanelState.MOVED;
            baseYaw = GetHeadYaw(); 
        }
    }

    void ComputeNewPanelTarget()
    {
        Vector3 FlatForward = cameraTransform.forward;
        FlatForward.y = 0;
        FlatForward.Normalize();

        targetPosition = cameraTransform.position + FlatForward * panelDistance + panelOffset;
        targetPosition.y = defaultPosition.y;

    }

    void ReturnPanelToDefault()
    {
        transform.position = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, defaultRotation, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, defaultPosition) < 0.01f &&
            Quaternion.Angle(transform.rotation, defaultRotation) < 0.5f)
        {
            state = PanelState.DEFAULT;
            baseYaw = GetHeadYaw();
        }
    }
}