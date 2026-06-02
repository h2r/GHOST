using UnityEngine;
using RosSharp.RosBridgeClient;

public class ArmCameraUIController : MonoBehaviour
{
    [Header("Camera Tracking")]
    public Transform centerEyeAnchor;
    public bool isRightController = true;

    [Header("ROS Topics")]
    public string spot1Topic = "/spot/stream_image/hand_color_image/image";
    public string spot2Topic = "/spot/stream_image2/hand_color_image/image";

    [Header("UI Settings")]
    public Vector3 positionOffsetRight = new Vector3(10f, 8f, 435f); // works for your setup
    public Vector3 positionOffsetLeft = new Vector3(-10f, 8f, 435f); // mirrored left side



    private JPEGImageSubscriber jpegSubscriber;

    void Start()
    {
        jpegSubscriber = GetComponentInChildren<JPEGImageSubscriber>();

        if (jpegSubscriber == null)
            Debug.LogError("Missing JPEGImageSubscriber in ArmCameraUIController or children");

        if (centerEyeAnchor == null)
            Debug.LogError("CenterEyeAnchor not set on ArmCameraUIController");

        UpdateTopic();
        UpdatePosition();
    }

    void Update()
    {
        if (centerEyeAnchor == null || !gameObject.activeSelf)
            return;

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        Vector3 basePos = centerEyeAnchor.position;
        Vector3 offset = isRightController ? positionOffsetRight : positionOffsetLeft;

        // Offset from headset
        transform.position = basePos +
            centerEyeAnchor.right * offset.x +
            centerEyeAnchor.up * offset.y +
            centerEyeAnchor.forward * offset.z;

        // Make it look at you (headset)
        transform.LookAt(centerEyeAnchor);

        // Flip around Y so the front of the panel faces you (common for quads)
        transform.Rotate(0, 180f, 0); 
    }


    private void UpdateTopic()
    {
        if (jpegSubscriber == null)
            return;

        if (isRightController)
        {
            jpegSubscriber.Topic = spot1Topic;
        }
        else
        {
            jpegSubscriber.Topic = spot2Topic;
        }
    }
}
