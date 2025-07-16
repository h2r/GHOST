using UnityEngine;

public class ControllerFlow : MonoBehaviour
{
    public GameObject anchor;
    public SkinnedMeshRenderer skinRenderer;
    public bool isLeft;

    private SpotMode spot;
    private NewControlMode control;
    private bool isPaused = true;

    public void Update()
    {
        if (!isPaused && spot != null && control != null)
        {
            control.ControlUpdate(spot, anchor, isLeft);
            skinRenderer.material.color = spot.color;
        }
        else
        {
            skinRenderer.material.color = Color.white;
        }
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