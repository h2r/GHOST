using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using static Unity.Sentis.Model;
using System;
using UnityEngine.VFX;

public class DepthCompletion : MonoBehaviour
{
    public ModelAsset Baseline;
    public ModelAsset MaskBaseline;
    Model runtimeModelBaseline;
    Model runtimeModelMaskBaseline;
    Worker workerBaseline;
    Worker workerMaskBaseline;

    public bool use_baseline;
    //TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
    //TensorShape color_shape = new TensorShape(1, 3, 480, 640);


    //// =============================================================================== //
    ////                               Init & OnRelease                                  //
    //// =============================================================================== //
    void Start()
    {
        runtimeModelBaseline = ModelLoader.Load(Baseline);
        workerBaseline = new Worker(runtimeModelBaseline, BackendType.GPUCompute);

        runtimeModelMaskBaseline = ModelLoader.Load(MaskBaseline);
        workerMaskBaseline = new Worker(runtimeModelMaskBaseline, BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        workerBaseline.Dispose();
        workerMaskBaseline.Dispose();
        if (runtimeModelBaseline != null)
        {
            runtimeModelBaseline = null;
        }
        if (runtimeModelMaskBaseline != null)
        {
            runtimeModelMaskBaseline = null;
        }
    }

    // =============================================================================== //
    //                               Depth Completion                                  //
    // =============================================================================== //
    //public (ComputeBuffer, ComputeBuffer) complete(float[] depth_data_0, Texture2D color_data_0, float[] depth_data_1, Texture2D color_data_1)
    public (ComputeBuffer, ComputeBuffer) complete(Tensor<float> depth_tensor_0, Tensor<float> color_tensor_0, Tensor<float> depth_tensor_1, Tensor<float> color_tensor_1)
    {
        if (use_baseline)
        {
            workerBaseline.SetInput("rgb_0", color_tensor_0);
            workerBaseline.SetInput("rgb_1", color_tensor_1);
            workerBaseline.SetInput("depth_0", depth_tensor_0);
            workerBaseline.SetInput("depth_1", depth_tensor_1);
            workerBaseline.Schedule();

            Tensor<float> depth_outputTensor_0 = workerBaseline.PeekOutput("output_depth_0") as Tensor<float>;
            //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
            ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

            Tensor<float> depth_outputTensor_1 = workerBaseline.PeekOutput("output_depth_1") as Tensor<float>;
            //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
            ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

            //color_tensor_0.Dispose();
            //color_tensor_1.Dispose();
            //depth_tensor_0.Dispose();
            //depth_tensor_1.Dispose();

            //depth_outputTensor_0.Dispose();
            //depth_outputTensor_1.Dispose();

            Debug.Log("Complete Depth return");

            return (computeTensorData0, computeTensorData1);
        }
        else
        {
            workerMaskBaseline.SetInput("rgb_0", color_tensor_0);
            workerMaskBaseline.SetInput("rgb_1", color_tensor_1);
            workerMaskBaseline.SetInput("depth_0", depth_tensor_0);
            workerMaskBaseline.SetInput("depth_1", depth_tensor_1);
            workerMaskBaseline.Schedule();

            Tensor<float> depth_outputTensor_0 = workerMaskBaseline.PeekOutput("output_depth_0") as Tensor<float>;
            //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
            ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

            Tensor<float> depth_outputTensor_1 = workerMaskBaseline.PeekOutput("output_depth_1") as Tensor<float>;
            //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
            ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

            //depth_outputTensor_0.Dispose();
            //depth_outputTensor_1.Dispose();

            //color_tensor_0.Dispose();
            //color_tensor_1.Dispose();
            //depth_tensor_0.Dispose();
            //depth_tensor_1.Dispose();

            return (computeTensorData0, computeTensorData1);
        }


    }
}