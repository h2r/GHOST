using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using static Unity.Sentis.Model;
using System;
using UnityEngine.VFX;

public class DepthCompletion : MonoBehaviour
{
    public ModelAsset modelAsset;
    Model runtimeModel;
    Worker worker;
    //TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
    //TensorShape color_shape = new TensorShape(1, 3, 480, 640);


    //// =============================================================================== //
    ////                               Init & OnRelease                                  //
    //// =============================================================================== //
    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        worker.Dispose();
        if (runtimeModel != null)
        {
            runtimeModel = null;
        }
    }

    // =============================================================================== //
    //                               Depth Completion                                  //
    // =============================================================================== //
    //public (ComputeBuffer, ComputeBuffer) complete(float[] depth_data_0, Texture2D color_data_0, float[] depth_data_1, Texture2D color_data_1)
    public (ComputeBuffer, ComputeBuffer) complete(Tensor<float> depth_tensor_0, Tensor<float> color_tensor_0, Tensor<float> depth_tensor_1, Tensor<float> color_tensor_1)
    {
        //Tensor<float> depth_tensor_0 = new Tensor<float>(depth_shape, depth_data_0);
        //Tensor<float> color_tensor_0 = TextureConverter.ToTensor(color_data_0, channels: 3);
        //color_tensor_0.Reshape(color_shape);

        //Tensor<float> depth_tensor_1 = new Tensor<float>(depth_shape, depth_data_1);
        //Tensor<float> color_tensor_1 = TextureConverter.ToTensor(color_data_1, channels: 3);
        //color_tensor_1.Reshape(color_shape);

        worker.SetInput("rgb_0", color_tensor_0);
        worker.SetInput("rgb_1", color_tensor_1);
        worker.SetInput("depth_0", depth_tensor_0);
        worker.SetInput("depth_1", depth_tensor_1);
        worker.Schedule();

        Tensor<float> depth_outputTensor_0 = worker.PeekOutput("output_depth_0") as Tensor<float>;
        //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
        ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

        Tensor<float> depth_outputTensor_1 = worker.PeekOutput("output_depth_1") as Tensor<float>;
        //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
        ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

        color_tensor_0.Dispose();
        color_tensor_1.Dispose();
        depth_tensor_0.Dispose();
        depth_tensor_1.Dispose();

        return (computeTensorData0, computeTensorData1);
    }
}