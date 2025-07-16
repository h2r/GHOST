using UnityEngine;

public class ControllerFlow : MonoBehaviour
{
    public GameObject anchor;

    private SpotMode spot;
    private NewControlMode control;

    public void Update()
    {
        if (spot != null && control != null)
            control.ControlUpdate(spot, anchor);
    }

    public void SetSpot(SpotMode spot)
    {
        this.spot = spot;
    }

    public void SetControl(NewControlMode control)
    {
        this.control = control;
    }
}