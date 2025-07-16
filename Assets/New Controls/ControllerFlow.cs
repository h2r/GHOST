using UnityEngine;

public class ControllerFlow : MonoBehaviour
{
    public GameObject anchor;
    public bool isLeft;

    private SpotMode spot;
    private NewControlMode control;
    private bool isPaused = true;

    public void Update()
    {
        if (!isPaused && spot != null && control != null)
            control.ControlUpdate(spot, anchor, isLeft);
    }

    public void SetSpot(SpotMode spot)
    {
        this.spot = spot;
    }

    public void SetControl(NewControlMode control)
    {
        this.control = control;
    }

    public void SetPaused(bool isPaused)
    {
        this.isPaused = isPaused;
    }
}