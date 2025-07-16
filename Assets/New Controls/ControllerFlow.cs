using TMPro;
using UnityEngine;

public class ControllerFlow : MonoBehaviour
{
    public GameObject anchor;
    public SkinnedMeshRenderer skinRenderer;
    public TMP_Text[] labels;
    public bool isLeft;

    private SpotMode spot;
    private NewControlMode control;
    private bool isPaused = true;

    public void Start()
    {
        SetLabels(new[] { "", "", "", "", "", "" });
    }

    public void Update()
    {
        if (!isPaused && spot != null && control != null)
        {
            control.ControlUpdate(spot, anchor, isLeft, SetLabels);
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
        SetLabels(new[] { "", "", "", "", "", "" });
    }

    public void SetLabels(string[] texts)
    {
        for (int i = 0; i < labels.Length; i++)
            labels[i].text = texts[i];
    }
}