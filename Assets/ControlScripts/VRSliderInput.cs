using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRSliderInput : MonoBehaviour
{

    //input all 6 slider values
    public ManualRotationSlider sh0Slider;
    public ManualRotationSlider sh1Slider;
    public ManualRotationSlider el0Slider;
    public ManualRotationSlider el1Slider;
    public ManualRotationSlider wr0Slider;
    public ManualRotationSlider wr1Slider;

    public GhostArmPublisher GArmPublisher;
    public bool fromInspector; 
    public double[] inspectorVals = new double[6];
    public ManualRotationSlider[] sliders = new ManualRotationSlider[6];
    
    double[] prevVals;

    void Start()
    {
        sliders = new ManualRotationSlider[6];

        sliders[0] = sh0Slider;
        sliders[1] = sh1Slider;
        sliders[2] = el0Slider;
        sliders[3] = el1Slider;
        sliders[4] = wr0Slider;
        sliders[5] = wr1Slider;
    }


    void Update()
    {
        if (fromInspector)
        {
            SetSliderVals(inspectorVals);
            GArmPublisher.RotateGhostArm(GetSliderVals());
        }
        else
            GArmPublisher.RotateGhostArm(GetSliderVals());

        GArmPublisher.PublishGhostArm();
    }

    private void OnEnable()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].getCanvas().enabled = true;
        }


        SetSliderVals(prevVals);

    }
    private void OnDisable()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].getCanvas().enabled = false;
        }
        
    }



    public void SetPrevAngles(double[] angles)
    {
            prevVals = angles;
    }

    //getter
    public double[] GetSliderVals()
    {
        double[] sliderVals = new double[6];

        sliderVals[0] = sh0Slider.GetSliderVal();
        sliderVals[1] = sh1Slider.GetSliderVal();
        sliderVals[2] = el0Slider.GetSliderVal();
        sliderVals[3] = el1Slider.GetSliderVal();
        sliderVals[4] = wr0Slider.GetSliderVal();
        sliderVals[5] = wr1Slider.GetSliderVal();

        return sliderVals;
    }

    //setter
    public void SetSliderVals(double[] angles)
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].SetSliderVal(angles[i]);
        }
    }
}
