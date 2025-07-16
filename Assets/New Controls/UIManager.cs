using UnityEngine;

public class UIManager : MonoBehaviour
{
    public ControllerFlow leftFlow, rightFlow;
    public ButtonList leftSpotList, leftControlList, perspectiveList, rightControlList, rightSpotList;
    public Transform cameraRig;

    private bool isOpen = true;

    public void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            isOpen = !isOpen;
            transform.parent.gameObject.GetComponent<Canvas>().enabled = isOpen;
            leftFlow.SetPaused(isOpen);
            rightFlow.SetPaused(isOpen);
            cameraRig.position = new(0, isOpen ? 100 : 0, 0);
        }
    }

    public bool TryRaycastHover(GameObject hit)
    {
        return leftSpotList.TryHoverButton(hit) ||
        leftControlList.TryHoverButton(hit) ||
        perspectiveList.TryHoverButton(hit) ||
        rightControlList.TryHoverButton(hit) ||
        rightSpotList.TryHoverButton(hit);
    }

    public void RaycastPress(GameObject hit)
    {
        _ = leftSpotList.PressButton(hit, m => leftFlow.SetSpot((SpotMode)m)) ||
        leftControlList.PressButton(hit, m => leftFlow.SetControl((NewControlMode)m)) ||
        perspectiveList.PressButton(hit, m => ((PerspectiveMode)m).PerspectiveStart()) ||
        rightControlList.PressButton(hit, m => rightFlow.SetControl((NewControlMode)m)) ||
        rightSpotList.PressButton(hit, m => rightFlow.SetSpot((SpotMode)m));
    }

    public bool GetOpen()
    {
        return isOpen;
    }
}