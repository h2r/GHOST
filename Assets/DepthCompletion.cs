using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using static Unity.Sentis.Model;

public class DepthCompletion : MonoBehaviour
{
    public ModelAsset rgbdModelAsset;
    Model runtimeModelRGBD;
    Worker workerRGBD;

    public bool Use_UB; // Use UnityBYOM for inference
    public UBModel UB_Model; // Reference to UBModel script

    Tensor<float> depth_outputTensor_0, depth_outputTensor_1;
    ComputeBuffer computeTensorData0, computeTensorData1;

    //// =============================================================================== //
    ////                               Init & OnRelease                                  //
    //// =============================================================================== //
    void Start()
    {
        // Setup sentis model
        runtimeModelRGBD = ModelLoader.Load(rgbdModelAsset);
        workerRGBD = new Worker(runtimeModelRGBD, BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        workerRGBD?.Dispose();
        runtimeModelRGBD = null;

        depth_outputTensor_0?.Dispose();
        depth_outputTensor_1?.Dispose();
    }

    // =============================================================================== //
    //                               Depth Completion                                  //
    // =============================================================================== //
    public (ComputeBuffer, ComputeBuffer) complete(Tensor<float> depth_tensor_0, Tensor<float> color_tensor_0, Tensor<float> depth_tensor_1, Tensor<float> color_tensor_1)
    {
        if (Use_UB)
        {  
            return UB_Model.Complete(depth_tensor_0, color_tensor_0, depth_tensor_1, color_tensor_1);
        } else { 
            workerRGBD.SetInput("rgb_0", color_tensor_0);
            workerRGBD.SetInput("rgb_1", color_tensor_1);

            workerRGBD.SetInput("depth_0", depth_tensor_0);
            workerRGBD.SetInput("depth_1", depth_tensor_1);


            workerRGBD.Schedule();

            Tensor<float> depth_outputTensor_0 = workerRGBD.PeekOutput("output_depth_0") as Tensor<float>;
            ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

            Tensor<float> depth_outputTensor_1 = workerRGBD.PeekOutput("output_depth_1") as Tensor<float>;
            ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

            return (computeTensorData0, computeTensorData1);
        }
    }
}