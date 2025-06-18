using RosSharp.RosBridgeClient;
using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class CVDDataGenerator : MonoBehaviour
{
    private bool first_run = true;

    Tensor<float> previous_rgbL;
    Tensor<float> previous_rgbR;

    //public BodyPoseSubscriber pose_subscriber;

    public DrawMeshInstanced render_left;
    public DrawMeshInstanced render_right;

    //ComputeBuffer buffer_depthL;
    //ComputeBuffer buffer_depthR;
    //ComputeBuffer buffer_opticalL;
    //ComputeBuffer buffer_opticalR;

    private DepthCompletion depth_completion;
    private OpticalFlowEstimator optical_flow_estimation;

    ComputeBuffer buffer_depthL, buffer_depthR, buffer_opticalL, buffer_opticalR;

    Matrix4x4 mat_l, mat_r;

    void Start()
    {
        depth_completion = GetComponent<DepthCompletion>();
        optical_flow_estimation = GetComponent<OpticalFlowEstimator>();
        StartCoroutine(SetFirstRunFalseAfterDelay(1f));

        mat_l = new Matrix4x4(
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0)
        );

        mat_r = new Matrix4x4(
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0)
        );
    }

    void OnDestroy()
    {
        previous_rgbL?.Dispose();
        previous_rgbR?.Dispose();
        
        buffer_depthL?.Dispose();
        buffer_depthR?.Dispose();
        buffer_opticalL?.Dispose();
        buffer_opticalR?.Dispose();
        Debug.Log("CVDDataGenerator destroyed and buffers released.");
    }

    IEnumerator SetFirstRunFalseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        //first_run = false;
        Debug.Log("first_run set to false");
    }

    //public (ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer) generateData(Tensor<float> depthL, Tensor<float> rgbL, Tensor<float> depthR, Tensor<float> rgbR, bool activate_depth_completion, bool activate_optical_flow)
    //{
    //    if (first_run) 
    //    {
    //        previous_rgbL = rgbL;
    //        previous_rgbR = rgbR;

    //        //buffer_depthL?.Release();
    //        //buffer_depthR?.Release();
    //        //buffer_opticalL?.Release();
    //        //buffer_opticalR?.Release();

    //        buffer_depthL = ComputeTensorData.Pin(depthL).buffer;
    //        buffer_depthR = ComputeTensorData.Pin(depthR).buffer;
    //        buffer_opticalL = new ComputeBuffer(480 * 640 * 2, sizeof(float));
    //        buffer_opticalR = new ComputeBuffer(480 * 640 * 2, sizeof(float));

    //        //Debug.Log("first run return");

    //        return (buffer_depthL, buffer_depthR, buffer_opticalL, buffer_opticalR);
    //    }
    //    else
    //    {
    //        if (activate_depth_completion)
    //        {
    //            //buffer_opticalL?.Release();
    //            //buffer_opticalR?.Release();
    //            (buffer_depthL, buffer_depthR) = depth_completion.complete(depthL, rgbL, depthR, rgbR);
    //        }
    //        else
    //        {
    //            //buffer_depthL?.Release();
    //            //buffer_depthR?.Release();
    //            buffer_depthL = ComputeTensorData.Pin(depthL).buffer;
    //            buffer_depthR = ComputeTensorData.Pin(depthR).buffer;
    //        }


    //        if (activate_optical_flow)
    //        {
    //            //Debug.Log("optical flow?");
    //            //buffer_opticalL?.Release();
    //            //buffer_opticalR?.Release();
    //            (buffer_opticalL, buffer_opticalR) = optical_flow_estimation.estimate_all(previous_rgbL, rgbL, previous_rgbR, rgbR);
    //        }
    //        else
    //        {
    //            //buffer_opticalL?.Release();
    //            //buffer_opticalR?.Release();
    //            buffer_opticalL = new ComputeBuffer(480 * 640 * 2, sizeof(float));
    //            buffer_opticalR = new ComputeBuffer(480 * 640 * 2, sizeof(float));
    //        }


    //        previous_rgbL = rgbL;
    //        previous_rgbR = rgbR;

    //        //Debug.Log("not firstrun return");

    //        return (buffer_depthL, buffer_depthR, buffer_opticalL, buffer_opticalR);
    //    }


    //}

    public (ComputeBuffer, ComputeBuffer, Matrix4x4, Matrix4x4, ComputeBuffer, ComputeBuffer) generatePoseData(Tensor<float> depthL, Tensor<float> rgbL, Tensor<float> depthR, Tensor<float> rgbR, bool activate_depth_completion, bool activate_CVD)
    {
        if (first_run)
        {
            previous_rgbL = new Tensor<float>(rgbL.shape);
            previous_rgbR = new Tensor<float>(rgbR.shape);

            //buffer_depthL?.Release();
            //buffer_depthR?.Release();
            //buffer_opticalL?.Release();
            //buffer_opticalR?.Release();

            buffer_depthL = ComputeTensorData.Pin(depthL).buffer;
            buffer_depthR = ComputeTensorData.Pin(depthR).buffer;
            buffer_opticalL = new ComputeBuffer(480 * 640 * 2, sizeof(float));
            buffer_opticalR = new ComputeBuffer(480 * 640 * 2, sizeof(float));

            first_run = false;
            //Debug.Log("first run return");

            return (buffer_depthL, buffer_depthR, mat_l, mat_r, buffer_opticalL, buffer_opticalR);
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


            if (activate_CVD)
            {

                mat_l = render_left.get_current_pose();
                mat_r = render_right.get_current_pose();
                //Debug.Log("optical flow?");
                //buffer_opticalL?.Release();
                //buffer_opticalR?.Release();
                (buffer_opticalL, buffer_opticalR) = optical_flow_estimation.estimate_all(previous_rgbL, rgbL, previous_rgbR, rgbR);

                //previous_rgbL = ops.copy(rgbL);
                // TODO: Come up with a better way to handle this copy.
                for (int i = 0; i < previous_rgbL.count; i++)
                {
                    previous_rgbL[i] = rgbL[i];
                    previous_rgbR[i] = rgbR[i];

                }
            }

            //Debug.Log("not firstrun return");

            return (buffer_depthL, buffer_depthR, mat_l, mat_r, buffer_opticalL, buffer_opticalR);
        }
    }
}