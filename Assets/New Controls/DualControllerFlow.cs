using UnityEngine;

public class DualControllerFlow : MonoBehaviour
{
    public ControllerModel leftModel, rightModel;

    private SpotMode spot;
    private NewControlMode control;
    private bool isPaused = true;

    public void Start()
    {
        leftModel.SetLabels(new[] { "", "", "", "", "", "" });
        rightModel.SetLabels(new[] { "", "", "", "", "", "" });
    }

    public void Update()
    {
        if (!isPaused && spot != null && control != null)
        {
            control.ControlUpdate(spot, leftModel, rightModel);
            leftModel.SetColor(spot.color);
            rightModel.SetColor(spot.color);
        }
        else
        {
            leftModel.SetColor(Color.white);
            rightModel.SetColor(Color.white);
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
        leftModel.SetLabels(new[] { "", "", "", "", "", "" });
        rightModel.SetLabels(new[] { "", "", "", "", "", "" });
    }
}