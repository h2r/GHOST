using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using static Unity.Sentis.Model;
using System;
using UnityEngine.VFX;
using System.Runtime.InteropServices;
using Meta.WitAi;

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

    // Imports from UnityBYOM.dll (Make sure it's copied to Assets/Plugins)
    [DllImport("UnityBYOM", CharSet = CharSet.Ansi)]
    private static extern IntPtr UB_LoadModel(string modelPath, string backend);
    [DllImport("UnityBYOM")]
    private static extern bool UB_RunInference(
        IntPtr texResource,      // ID3D12Resource* of the Unity texture
        IntPtr outputBuffer,     // ComputeBuffer.GetNativeBufferPtr()
        int inputCount,          // Number of input elements
        int outputCount          // Number of output elements
    );

    [DllImport("UnityBYOM")]
    private static extern bool UB_RunInferenceBatched(
        IntPtr[] inputResources,    // Array of resource pointers
        IntPtr[] outputResources,   // Array of output resource pointers
        int batchSize,
        int inputSizePerItem,
        int outputSizePerItem
    );

    [DllImport("UnityBYOM")]
    private static extern void UB_SetUnityLogCallback(LogCallback callback);

    private delegate void LogCallback(string message);

    private static void PluginLogCallback(string message)
    {
        Debug.Log("[Plugin Callback] " + message);
    }


    // =============================================================================== //
    //                               Init & OnRelease                                  //
    // =============================================================================== //
    void Start()
    {
        runtimeModelRGB = ModelLoader.Load(rgbModel);
        workerRGB = new Worker(runtimeModelRGB, BackendType.GPUCompute);

        runtimeModelRGBD = ModelLoader.Load(rgbdModel);
        workerRGBD = new Worker(runtimeModelRGBD, BackendType.GPUCompute);

        UB_SetUnityLogCallback(PluginLogCallback);
        IntPtr result = UB_LoadModel("C:/Users/faisa/workspace/Unity/tests/danything_mono_metric_finetune_epoch5_valloss0.0798-scripted-batch2.pt", "cuda");
        Debug.LogWarning("Model Load result = " + result);

        TensorShape shape = new TensorShape(1, 1, 480, 640); // Depth shape
        depth_outputTensor_0 = new Tensor<float>(shape);
        computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

        depth_outputTensor_1 = new Tensor<float>(shape);
        computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;
    }

    void OnDestroy()
    {
        // TODO: Destroy C++ model
        

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
            //ComputeTensorData gpuTensorColor0 = ComputeTensorData.Pin(color_tensor_0);
            //IntPtr colorBuffer0 = gpuTensorColor0.buffer.GetNativeBufferPtr();
            //ComputeTensorData detphTensorColor0 = ComputeTensorData.Pin(depth_tensor_0);

            //// IntPtr rawBuf = computeTensorDataTest.GetNativeBufferPtr();

            //Debug.Log("before getnativebufferptr: depth_outputTensor_0.dataOnBackend = " + depth_outputTensor_0.dataOnBackend);
            //Debug.Log("gpuTensorColor0.buffer.GetType() = " + gpuTensorColor0.buffer.GetType());
            //Debug.Log("color_tensor_0.count = " + color_tensor_0.count);
            //Debug.Log("color_tensor_0.shape = " + color_tensor_0.shape);

            //IntPtr outputBuffer = computeTensorData0.GetNativeBufferPtr();

            //TensorShape shape = color_tensor_0.shape;
            //int out_size = depth_outputTensor_0.count;

            //Debug.Log("depth_outputTensor_0.count = " + depth_outputTensor_0.count);
            //Debug.Log("depth_outputTensor_0.shape = " + depth_outputTensor_0.shape);
            //Debug.Log("computeTensorData0.count = " + out_size);
            //Debug.Log("after getnativebufferptr: depth_outputTensor_0.dataOnBackend = " + depth_outputTensor_0.dataOnBackend);

            //bool ret = UB_RunInference(colorBuffer0, outputBuffer, 640 * 480 * 3, 640 * 480);
            //if (!ret) {
            //    Debug.LogError("Failed to run inference via UnityBrains");
            //}

            //// TODO: batch the inference calls

            //ComputeTensorData gpuTensorColor1 = ComputeTensorData.Pin(color_tensor_1);
            //IntPtr colorBuffer1 = gpuTensorColor1.buffer.GetNativeBufferPtr();
            //Debug.Log("before pin: depth_tensor_1.dataOnBackend = " + depth_tensor_1.dataOnBackend);
            //ComputeTensorData detphTensorColor1 = ComputeTensorData.Pin(depth_tensor_1);
            //Debug.Log("after pin: depth_tensor_1.dataOnBackend = " + depth_tensor_1.dataOnBackend);


            //Debug.Log("before getnativebufferptr: depth_outputTensor_1.dataOnBackend = " + depth_outputTensor_1.dataOnBackend);
            //Debug.Log("color_tensor_1.count = " + color_tensor_1.count);
            //Debug.Log("color_tensor_1.shape = " + color_tensor_1.shape);

            //outputBuffer = computeTensorData1.GetNativeBufferPtr();

            //Debug.Log("depth_outputTensor_1.count = " + depth_outputTensor_1.count);
            //Debug.Log("depth_outputTensor_1.shape = " + depth_outputTensor_1.shape);

            //ret = UB_RunInference(colorBuffer1, outputBuffer, 640 * 480 * 3, 640 * 480);
            //if (!ret)
            //{
            //    Debug.LogError("Failed to run second inference via UnityBrains");
            //}


            ComputeTensorData gpuTensorColor0 = ComputeTensorData.Pin(color_tensor_0);
            ComputeTensorData gpuTensorColor1 = ComputeTensorData.Pin(color_tensor_1);

            // Create arrays for batched call
            IntPtr[] inputResources = new IntPtr[2];
            IntPtr[] outputResources = new IntPtr[2];

            inputResources[0] = gpuTensorColor0.buffer.GetNativeBufferPtr();
            inputResources[1] = gpuTensorColor1.buffer.GetNativeBufferPtr();
            outputResources[0] = computeTensorData0.GetNativeBufferPtr();
            outputResources[1] = computeTensorData1.GetNativeBufferPtr();

            // Single batched inference call
            bool ret = UB_RunInferenceBatched(
                inputResources,
                outputResources,
                2,              // batch size
                640 * 480 * 3,  // input size per item
                640 * 480       // output size per item
            );

            if (!ret)
            {
                Debug.LogError("Failed to run batched inference via UnityBrains");
            }


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
        }
        

        return (computeTensorData0, computeTensorData1);

    }
}