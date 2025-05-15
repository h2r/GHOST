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

    Tensor<float> depth_outputTensor_0, depth_outputTensor_1, testinputtensor, testinputtensor2;
    ComputeBuffer computeTensorData0, computeTensorData1, computeTensorDataTest, computeTensorDataTest2;

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
        IntPtr result = UB_LoadModel(rgbdModel.ToString(), "TorchScript");
        Debug.LogWarning("Model Load result = " + result);

        TensorShape shape = new TensorShape(640, 480, 1, 1); // RGB input shape
        depth_outputTensor_0 = new Tensor<float>(shape);
        computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

        testinputtensor = new Tensor<float>(shape);
        testinputtensor2 = new Tensor<float>(shape);


        // Fill tensor with dummy data
        for (int i = 0; i < testinputtensor.count; i++)
        {
            if (i % 10 == 0)
            {
                testinputtensor[i] = 15.0f;
                testinputtensor2[i] = 7.0f;
            }
            else
            {
                testinputtensor[i] = 0.0f;
                testinputtensor2[i] = 0.0f;
            }
            //Debug.LogError("testinputtensor[" + i + "] = " + testinputtensor[i]);
        }

        computeTensorDataTest = ComputeTensorData.Pin(testinputtensor).buffer;
        computeTensorDataTest2 = ComputeTensorData.Pin(testinputtensor2).buffer;



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
            ComputeTensorData gpuTensorColor0 = ComputeTensorData.Pin(color_tensor_0);
            //IntPtr colorBuffer0 = gpuTensorColor0.buffer.GetNativeBufferPtr();
            ComputeTensorData detphTensorColor0 = ComputeTensorData.Pin(depth_tensor_0);
            IntPtr rawBuf = detphTensorColor0.buffer.GetNativeBufferPtr();

            // IntPtr rawBuf = computeTensorDataTest.GetNativeBufferPtr();

            Debug.Log("before getnativebufferptr: depth_outputTensor_0.dataOnBackend = " + depth_outputTensor_0.dataOnBackend);

            IntPtr outputBuffer = computeTensorData0.GetNativeBufferPtr();

            TensorShape shape = color_tensor_0.shape;
            int out_size = depth_outputTensor_0.count;

            Debug.Log("depth_outputTensor_0.count = " + depth_outputTensor_0.count);
            Debug.Log("depth_outputTensor_0.shape = " + depth_outputTensor_0.shape);
            Debug.Log("computeTensorData0.count = " + out_size);
            Debug.Log("after getnativebufferptr: depth_outputTensor_0.dataOnBackend = " + depth_outputTensor_0.dataOnBackend);

            bool ret = UB_RunInference(rawBuf, outputBuffer, 640 * 480, 640 * 480);
            if (!ret) {
                Debug.LogError("Failed to run inference via UnityBrains");
            }

            // TODO: batch the inference calls

            ComputeTensorData gpuTensorColor1 = ComputeTensorData.Pin(color_tensor_1);
            IntPtr colorBuffer1 = gpuTensorColor1.buffer.GetNativeBufferPtr();
            Debug.Log("before pin: depth_tensor_1.dataOnBackend = " + depth_tensor_1.dataOnBackend);
            ComputeTensorData detphTensorColor1 = ComputeTensorData.Pin(depth_tensor_1);
            Debug.Log("after pin: depth_tensor_1.dataOnBackend = " + depth_tensor_1.dataOnBackend);
            rawBuf = detphTensorColor1.buffer.GetNativeBufferPtr();

            //rawBuf = computeTensorDataTest2.GetNativeBufferPtr();


            Debug.Log("before getnativebufferptr: depth_outputTensor_1.dataOnBackend = " + depth_outputTensor_1.dataOnBackend);

            outputBuffer = computeTensorData1.GetNativeBufferPtr();

            Debug.Log("depth_outputTensor_1.count = " + depth_outputTensor_1.count);
            Debug.Log("depth_outputTensor_1.shape = " + depth_outputTensor_1.shape);

            ret = UB_RunInference(rawBuf, outputBuffer, 640 * 480, 640 * 480);
            if (!ret)
            {
                Debug.LogError("Failed to run second inference via UnityBrains");
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