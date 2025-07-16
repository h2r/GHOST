using TMPro;
using UnityEngine;

public class SingleControllerFlow : MonoBehaviour
{
    public ControllerModel model;

    private SpotMode spot;
    private NewControlMode control;
    private bool isPaused = true;

    public void Start()
    {
        model.SetLabels(new[] { "", "", "", "", "", "" });
    }

    public void Update()
    {
        if (!isPaused && spot != null && control != null)
        {
            control.ControlUpdate(spot, model, null);
            model.SetColor(spot.color);
        }
        else if (isPaused)
        {
            model.SetColor(Color.white);
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
        model.SetLabels(new[] { "", "", "", "", "", "" });
    }
}