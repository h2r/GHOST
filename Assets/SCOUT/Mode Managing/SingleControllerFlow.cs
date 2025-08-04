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
        if (spot != null && control != null)
        {
            if (this.control.ControlsSpot)
            {
                model.SetColor(spot.color);
            }
            else
            {
                model.SetColor(Color.white);
            }
            if (!isPaused)
            {
                control.ControlUpdate(spot, model, null);
            }
        }
    }

    public void SetSpot(SpotMode spot)
    {
        this.spot = spot;
    }

    public SpotMode GetSpot()
    {
        return this.spot;
    }

    public void SetControl(NewControlMode control)
    {
        this.control = control;
    }

    public NewControlMode GetControl()
    {
        return this.control;
    }

    public void SetPaused(bool isPaused)
    {
        this.isPaused = isPaused;
        model.SetLabels(new[] { "", "", "", "", "", "" });
    }
}
