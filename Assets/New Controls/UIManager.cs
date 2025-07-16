using UnityEngine;

public class UIManager : MonoBehaviour
{
    public ControllerFlow leftFlow;
    public ButtonList leftSpotList, leftControlList;

    private bool isOpen = true;

    public void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            isOpen = !isOpen;
            print("menu open: " + isOpen);
            transform.parent.gameObject.GetComponent<Canvas>().enabled = isOpen;
            leftFlow.SetPaused(isOpen);
        }
    }

    public bool TryRaycastHover(GameObject hit)
    {
        return leftSpotList.TryHoverButton(hit) ||
        leftControlList.TryHoverButton(hit);
    }

    public void RaycastPress(GameObject hit)
    {
        _ = leftSpotList.PressButton(hit, m => leftFlow.SetSpot((SpotMode)m)) ||
        leftControlList.PressButton(hit, m => leftFlow.SetControl((NewControlMode)m));
    }

    public bool GetOpen()
    {
        return isOpen;
    }
}