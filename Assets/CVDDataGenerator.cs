using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class CVDDataGenerator : MonoBehaviour
{
    private bool first_run = true;

    Tensor<float> previous_rgbL;
    Tensor<float> previous_rgbR;

    //ComputeBuffer buffer_depthL;
    //ComputeBuffer buffer_depthR;
    //ComputeBuffer buffer_opticalL;
    //ComputeBuffer buffer_opticalR;

    private DepthCompletion depth_completion;
    private OpticalFlowEstimator optical_flow_estimation;

    ComputeBuffer buffer_depthL, buffer_depthR, buffer_opticalL, buffer_opticalR;

    void Start()
    {
        depth_completion = GetComponent<DepthCompletion>();
        optical_flow_estimation = GetComponent<OpticalFlowEstimator>();
        StartCoroutine(SetFirstRunFalseAfterDelay(5f));
    }

    IEnumerator SetFirstRunFalseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        first_run = false;
        Debug.Log("first_run set to false");
    }

    public (ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer) generateData(Tensor<float> depthL, Tensor<float> rgbL, Tensor<float> depthR, Tensor<float> rgbR, bool activate_depth_completion, bool activate_optical_flow)
    {
        if (first_run) 
        {
            previous_rgbL = rgbL;
            previous_rgbR = rgbR;

            //buffer_depthL?.Release();
            //buffer_depthR?.Release();
            //buffer_opticalL?.Release();
            //buffer_opticalR?.Release();

            buffer_depthL = ComputeTensorData.Pin(depthL).buffer;
            buffer_depthR = ComputeTensorData.Pin(depthL).buffer;
            buffer_opticalL = new ComputeBuffer(480 * 640 * 2, sizeof(float));
            buffer_opticalR = new ComputeBuffer(480 * 640 * 2, sizeof(float));

            //Debug.Log("first run return");

            return (buffer_depthL, buffer_depthR, buffer_opticalL, buffer_opticalR);
        }
        else
        {
            if (activate_depth_completion)
            {
                //buffer_opticalL?.Release();
                //buffer_opticalR?.Release();
                (buffer_depthL, buffer_depthR) = depth_completion.complete(depthL, rgbL, depthR, rgbR);
            }
            else
            {
                //buffer_depthL?.Release();
                //buffer_depthR?.Release();
                buffer_depthL = ComputeTensorData.Pin(depthL).buffer;
                buffer_depthR = ComputeTensorData.Pin(depthR).buffer;
            }


            if (activate_optical_flow)
            {
                //Debug.Log("optical flow?");
                //buffer_opticalL?.Release();
                //buffer_opticalR?.Release();
                (buffer_opticalL, buffer_opticalR) = optical_flow_estimation.estimate_all(previous_rgbL, rgbL, previous_rgbR, rgbR);
            }
            else
            {
                //buffer_opticalL?.Release();
                //buffer_opticalR?.Release();
                buffer_opticalL = new ComputeBuffer(480 * 640 * 2, sizeof(float));
                buffer_opticalR = new ComputeBuffer(480 * 640 * 2, sizeof(float));
            }


            previous_rgbL = rgbL;
            previous_rgbR = rgbR;

            //Debug.Log("not firstrun return");

            return (buffer_depthL, buffer_depthR, buffer_opticalL, buffer_opticalR);
        }

        
    }

}