using UnityEngine;
using RosSharp.RosBridgeClient;

public class ArmCameraUIManager : MonoBehaviour
{
    [Header("Tracking")]
    public Transform centerEyeAnchor;

    [Header("UI Elements")]
    public GameObject leftUI;
    public GameObject rightUI;

    [Header("Offsets")]
    public Vector3 leftOffset = new Vector3(-120f, 65f, 225f);
    public Vector3 rightOffset = new Vector3(120f, 65f, 225f);

    [Header("ROS Subscribers")]
    public JPEGImageSubscriber leftSubscriber;
    public JPEGImageSubscriber rightSubscriber;

    public string spot1Topic = "/spot/stream_image/hand_color_image/image";
    public string spot2Topic = "/spot/stream_image2/hand_color_image/image";

    private UIManager uiManager;

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();

        if (leftSubscriber != null)
            leftSubscriber.Topic = spot2Topic;
        if (rightSubscriber != null)
            rightSubscriber.Topic = spot1Topic;
    }
    private void Update()
    {
        if (uiManager == null || centerEyeAnchor == null) return;

        bool shouldShow = false;//!uiManager.GetOpen() && uiManager.IsInArmPerspective();

        leftUI.SetActive(shouldShow);
        rightUI.SetActive(shouldShow);

        if (!shouldShow) return;

        // Calculate the flat yaw rotation to position the panels relative to the headset
        Quaternion flatRotation = Quaternion.Euler(0f, centerEyeAnchor.eulerAngles.y, 0f);

        // Position panels relative to the headset using yaw-only rotation
        Vector3 leftPos = centerEyeAnchor.position + flatRotation * (leftOffset);
        Vector3 rightPos = centerEyeAnchor.position + flatRotation * (rightOffset);

        leftUI.transform.position = leftPos;
        rightUI.transform.position = rightPos;

        // Make panels look at the headset, but lock their 'up' vector to world up (no roll)
        leftUI.transform.rotation = Quaternion.LookRotation(centerEyeAnchor.position - leftUI.transform.position, Vector3.up);
        rightUI.transform.rotation = Quaternion.LookRotation(centerEyeAnchor.position - rightUI.transform.position, Vector3.up);
    }

}
