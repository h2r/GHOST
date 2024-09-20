using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualRotationSlider : MonoBehaviour
{
    private Slider slider;
    private TextMeshProUGUI sliderText;
    private Canvas thisCanvas;


    void Start()
    {
        slider = this.GetComponentInChildren<Slider>();
        sliderText = this.GetComponentInChildren<TextMeshProUGUI>();
        thisCanvas = this.GetComponentInParent<Canvas>();
    }

    //update slider's text
    void Update()
    {
        sliderText.text = slider.value.ToString();
    }

    public double GetSliderVal()
    {
        return (double)slider.value;
    }

    public void SetSliderVal(double input)
    {
        slider.value = (float)input;
    }

    public Canvas getCanvas()
    {
        return thisCanvas;
    }

}
