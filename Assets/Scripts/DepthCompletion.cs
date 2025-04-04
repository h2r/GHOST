using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using static Unity.Sentis.Model;
using System;
using UnityEngine.VFX;

public class DepthCompletion : MonoBehaviour
{
    public ModelAsset rgbModel;
    public ModelAsset rgbdModel;
    Model runtimeModelRGB;
    Model runtimeModelRGBD;
    Worker workerRGB;
    Worker workerRGBD;

    public bool useRawDepth;

    Tensor<float> depth_outputTensor_0, depth_outputTensor_1;
    ComputeBuffer computeTensorData0, computeTensorData1;


    // =============================================================================== //
    //                               Init & OnRelease                                  //
    // =============================================================================== //
    void Start()
    {
        runtimeModelRGB = ModelLoader.Load(rgbModel);
        workerRGB = new Worker(runtimeModelRGB, BackendType.GPUCompute);

        runtimeModelRGBD = ModelLoader.Load(rgbdModel);
        workerRGBD = new Worker(runtimeModelRGBD, BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        workerRGB.Dispose();
        workerRGBD.Dispose();
        if (runtimeModelRGB != null)
        {
            runtimeModelRGB = null;
        }
        if (runtimeModelRGBD != null)
        {
            runtimeModelRGBD = null;
        }

        depth_outputTensor_0?.Dispose();
        depth_outputTensor_1?.Dispose();
    }

    // =============================================================================== //
    //                               Depth Completion                                  //
    // =============================================================================== //
    //public (ComputeBuffer, ComputeBuffer) complete(float[] depth_data_0, Texture2D color_data_0, float[] depth_data_1, Texture2D color_data_1)
    public (ComputeBuffer, ComputeBuffer) complete(Tensor<float> depth_tensor_0, Tensor<float> color_tensor_0, Tensor<float> depth_tensor_1, Tensor<float> color_tensor_1)
    {
        if (!useRawDepth) {
            // Crop the rgb tensor to match the expected input dims
            workerRGB.SetInput("rgb", color_tensor_0);
            workerRGB.Schedule();

            depth_outputTensor_0 = workerRGB.PeekOutput("depth") as Tensor<float>;
            computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

            workerRGB.SetInput("rgb", color_tensor_1);
            workerRGB.Schedule();

            depth_outputTensor_1 = workerRGB.PeekOutput("depth") as Tensor<float>;
            computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

            return (computeTensorData0, computeTensorData1);
        } else {
            workerRGBD.SetInput("rgb_0", color_tensor_0);
            workerRGBD.SetInput("rgb_1", color_tensor_1);
            workerRGBD.SetInput("depth_0", depth_tensor_0);
            workerRGBD.SetInput("depth_1", depth_tensor_1);
            workerRGBD.Schedule();

            depth_outputTensor_0 = workerRGBD.PeekOutput("output_depth_0") as Tensor<float>;
            computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

            depth_outputTensor_1 = workerRGBD.PeekOutput("output_depth_1") as Tensor<float>;
            computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

            return (computeTensorData0, computeTensorData1);
        }
    }
}