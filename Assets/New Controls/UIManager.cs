using UnityEngine;

public class UIManager : MonoBehaviour
{
    public ControllerFlow leftFlow;

    public ButtonList leftSpotList, leftControlList;

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
}