using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;


public class UBModel : MonoBehaviour
{
    // Imports from UnityBYOM.dll (Make sure all .dlls are copied to Assets/Plugins)
    [DllImport("UnityBYOM", CharSet = CharSet.Ansi)]
    private static extern bool UB_LoadModel(string modelPath, string backend);
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
        Debug.Log("[UnityBYOM] " + message);
    }

    public string UB_Model_file;
    
    
    private bool modelLoaded = false;

    Tensor<float> outputTensor0, outputTensor1;
    ComputeBuffer computeTensorData0, computeTensorData1;
    IntPtr outputBuffer0, outputBuffer1;

    void Start()
    {
        UB_SetUnityLogCallback(PluginLogCallback);
        bool result = UB_LoadModel(UB_Model_file, "cuda");
        if (!result)
        {
            Debug.LogError("Failed to load model from: " + UB_Model_file);
            modelLoaded = false;
        } else {
            Debug.Log("Model loaded successfully from: " + UB_Model_file);
            modelLoaded = true;
        }

            TensorShape shape = new TensorShape(1, 1, 480, 640); // Depth shape
        outputTensor0 = new Tensor<float>(shape);
        computeTensorData0 = ComputeTensorData.Pin(outputTensor0).buffer;
        outputBuffer0 = computeTensorData0.GetNativeBufferPtr();
        outputTensor1 = new Tensor<float>(shape);
        computeTensorData1 = ComputeTensorData.Pin(outputTensor1).buffer;
        outputBuffer1 = computeTensorData1.GetNativeBufferPtr();
    }

    private void OnDestroy()
    {
        outputTensor0?.Dispose();
        outputTensor1?.Dispose();
        // TODO: Release c++ model if using UnityBYOM
    }

    public (ComputeBuffer, ComputeBuffer) Complete(Tensor<float> depth_tensor_0, Tensor<float> color_tensor_0, Tensor<float> depth_tensor_1, Tensor<float> color_tensor_1)
    {
        if (!modelLoaded)
        {
            Debug.LogError("Model not loaded. Please ensure the model is loaded before calling Complete.");
            return (computeTensorData0, computeTensorData1);
        }

        // Use UnityBYOM for inference (FMZFIXME: reenable )
        //IntPtr colorBuffer0 = ComputeTensorData.Pin(color_tensor_0).buffer.GetNativeBufferPtr();
        //bool ret = UB_RunInference(colorBuffer0, computeTensorData0.GetNativeBufferPtr(), 640 * 480 * 3, 640 * 480);
        //if (!ret)
        //{
        //    Debug.LogError("Failed to run inference via UnityBrains");
        //}
        //IntPtr colorBuffer1 = ComputeTensorData.Pin(color_tensor_1).buffer.GetNativeBufferPtr();
        //ret = UB_RunInference(colorBuffer1, computeTensorData1.GetNativeBufferPtr(), 640 * 480 * 3, 640 * 480);
        //if (!ret)
        //{
        //    Debug.LogError("Failed to run second inference via UnityBrains");
        //}

        ComputeTensorData gpuTensorColor0 = ComputeTensorData.Pin(color_tensor_0);
        IntPtr colorBuffer0 = gpuTensorColor0.buffer.GetNativeBufferPtr();
        ComputeTensorData detphTensorColor0 = ComputeTensorData.Pin(depth_tensor_0);

        Debug.Log("before getnativebufferptr: outputTensor0.dataOnBackend = " + outputTensor0.dataOnBackend);
        Debug.Log("color_tensor_0.count = " + color_tensor_0.count);
        Debug.Log("color_tensor_0.shape = " + color_tensor_0.shape);

        TensorShape shape = color_tensor_0.shape;
        int out_size = outputTensor0.count;

        Debug.Log("outputTensor0.count = " + outputTensor0.count);
        Debug.Log("outputTensor0.shape = " + outputTensor0.shape);
        Debug.Log("computeTensorData0.count = " + out_size);
        Debug.Log("after getnativebufferptr: outputTensor0.dataOnBackend = " + outputTensor0.dataOnBackend);

        Debug.Log("0: input buffer = " + colorBuffer0);
        Debug.Log("0: output buffer = " + outputBuffer0);
        bool ret = UB_RunInference(colorBuffer0, outputBuffer0, 640 * 480 * 3, 640 * 480);
        if (!ret)
        {
            Debug.LogError("Failed to run inference via UnityBrains");
        }

        // TODO: batch the inference calls

        ComputeTensorData gpuTensorColor1 = ComputeTensorData.Pin(color_tensor_1);
        IntPtr colorBuffer1 = gpuTensorColor1.buffer.GetNativeBufferPtr();
        Debug.Log("before pin: depth_tensor_1.dataOnBackend = " + depth_tensor_1.dataOnBackend);
        ComputeTensorData detphTensorColor1 = ComputeTensorData.Pin(depth_tensor_1);
        Debug.Log("after pin: depth_tensor_1.dataOnBackend = " + depth_tensor_1.dataOnBackend);


        Debug.Log("before getnativebufferptr: outputTensor1.dataOnBackend = " + outputTensor1.dataOnBackend);
        Debug.Log("color_tensor_1.count = " + color_tensor_1.count);
        Debug.Log("color_tensor_1.shape = " + color_tensor_1.shape);

        Debug.Log("outputTensor1.count = " + outputTensor1.count);
        Debug.Log("outputTensor1.shape = " + outputTensor1.shape);

        Debug.Log("1: input buffer = " + colorBuffer1);
        Debug.Log("1: output buffer = " + outputBuffer1);
        ret = UB_RunInference(colorBuffer1, outputBuffer1, 640 * 480 * 3, 640 * 480);
        if (!ret)
        {
            Debug.LogError("Failed to run second inference via UnityBrains");
        }

        return (computeTensorData0, computeTensorData1);

    }
}