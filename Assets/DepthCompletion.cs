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
    //public ModelAsset MaskBaseline;
    Model runtimeModelBaseline;
    //Model runtimeModelMaskBaseline;
    Worker workerBaseline;
    //Worker workerMaskBaseline;

    //public bool use_baseline;
    //TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
    //TensorShape color_shape = new TensorShape(1, 3, 480, 640);


    //// =============================================================================== //
    ////                               Init & OnRelease                                  //
    //// =============================================================================== //
    void Start()
    {
        runtimeModelBaseline = ModelLoader.Load(Baseline);
        workerBaseline = new Worker(runtimeModelBaseline, BackendType.GPUCompute);

        //runtimeModelMaskBaseline = ModelLoader.Load(MaskBaseline);
        //workerMaskBaseline = new Worker(runtimeModelMaskBaseline, BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        workerBaseline.Dispose();
        //workerMaskBaseline.Dispose();
        if (runtimeModelBaseline != null)
        {
            runtimeModelBaseline = null;
        }
        //if (runtimeModelMaskBaseline != null)
        //{
        //    runtimeModelMaskBaseline = null;
        //}
    }

    // =============================================================================== //
    //                               Depth Completion                                  //
    // =============================================================================== //
    //public (ComputeBuffer, ComputeBuffer) complete(float[] depth_data_0, Texture2D color_data_0, float[] depth_data_1, Texture2D color_data_1)
    public (ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer) complete(Tensor<float> depth_tensor_0, Tensor<float> color_tensor_0, Tensor<float> depth_tensor_1, Tensor<float> color_tensor_1, Tensor<float> depth_tensor_2, Tensor<float> color_tensor_2, Tensor<float> depth_tensor_3, Tensor<float> color_tensor_3)
    {
        //Tensor<float> depth_tensor_0 = new Tensor<float>(depth_shape, depth_data_0);
        //Tensor<float> color_tensor_0 = TextureConverter.ToTensor(color_data_0, channels: 3);
        //color_tensor_0.Reshape(color_shape);

        //Tensor<float> depth_tensor_1 = new Tensor<float>(depth_shape, depth_data_1);
        //Tensor<float> color_tensor_1 = TextureConverter.ToTensor(color_data_1, channels: 3);
        //color_tensor_1.Reshape(color_shape);

        //if (use_baseline)
        //{
//<<<<<<< HEAD
//            workerBaseline.SetInput("rgb_0", color_tensor_0);
//            workerBaseline.SetInput("rgb_1", color_tensor_1);
//            workerBaseline.SetInput("rgb_2", color_tensor_2);
//            workerBaseline.SetInput("rgb_3", color_tensor_3);

//            workerBaseline.SetInput("depth_0", depth_tensor_0);
//            workerBaseline.SetInput("depth_1", depth_tensor_1);
//            workerBaseline.SetInput("depth_2", depth_tensor_2);
//            workerBaseline.SetInput("depth_3", depth_tensor_3);

//            workerBaseline.Schedule();

//            Tensor<float> depth_outputTensor_0 = workerBaseline.PeekOutput("output_depth_0") as Tensor<float>;
//            //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
//            ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

//            Tensor<float> depth_outputTensor_1 = workerBaseline.PeekOutput("output_depth_1") as Tensor<float>;
//            //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
//            ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

//            Tensor<float> depth_outputTensor_2 = workerBaseline.PeekOutput("output_depth_2") as Tensor<float>;
//            //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
//            ComputeBuffer computeTensorData2 = ComputeTensorData.Pin(depth_outputTensor_2).buffer;

//            Tensor<float> depth_outputTensor_3 = workerBaseline.PeekOutput("output_depth_3") as Tensor<float>;
//            //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
//            ComputeBuffer computeTensorData3 = ComputeTensorData.Pin(depth_outputTensor_3).buffer;

//            //color_tensor_0.Dispose();
//            //color_tensor_1.Dispose();
//            //color_tensor_2.Dispose();
//            //color_tensor_3.Dispose();

//            //depth_tensor_0.Dispose();
//            //depth_tensor_1.Dispose();
//            //depth_tensor_2.Dispose();
//            //depth_tensor_3.Dispose();

//            Debug.Log("Run Model");

//            return (computeTensorData0, computeTensorData1, computeTensorData2, computeTensorData3);
//=======
        workerBaseline.SetInput("rgb_0", color_tensor_0);
        workerBaseline.SetInput("rgb_1", color_tensor_1);
        //workerBaseline.SetInput("rgb_2", color_tensor_2);
        //workerBaseline.SetInput("rgb_3", color_tensor_3);

        workerBaseline.SetInput("depth_0", depth_tensor_0);
        workerBaseline.SetInput("depth_1", depth_tensor_1);
        //workerBaseline.SetInput("depth_2", depth_tensor_2);
        //workerBaseline.SetInput("depth_3", depth_tensor_3);

        workerBaseline.Schedule();

        Tensor<float> depth_outputTensor_0 = workerBaseline.PeekOutput("output_depth_0") as Tensor<float>;
        //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
        ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

        Tensor<float> depth_outputTensor_1 = workerBaseline.PeekOutput("output_depth_1") as Tensor<float>;
        //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
        ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

        //Tensor<float> depth_outputTensor_2 = workerBaseline.PeekOutput("output_depth_2") as Tensor<float>;
        ////float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
        //ComputeBuffer computeTensorData2 = ComputeTensorData.Pin(depth_outputTensor_2).buffer;

        //Tensor<float> depth_outputTensor_3 = workerBaseline.PeekOutput("output_depth_3") as Tensor<float>;
        ////float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
        //ComputeBuffer computeTensorData3 = ComputeTensorData.Pin(depth_outputTensor_3).buffer;

        //color_tensor_0.Dispose();
        //color_tensor_1.Dispose();
        //color_tensor_2.Dispose();
        //color_tensor_3.Dispose();

        //depth_tensor_0.Dispose();
        //depth_tensor_1.Dispose();
        //depth_tensor_2.Dispose();
        //depth_tensor_3.Dispose();



        workerBaseline.SetInput("rgb_0", color_tensor_2);
        workerBaseline.SetInput("rgb_1", color_tensor_3);
        //workerBaseline.SetInput("rgb_2", color_tensor_2);
        //workerBaseline.SetInput("rgb_3", color_tensor_3);

        workerBaseline.SetInput("depth_0", depth_tensor_2);
        workerBaseline.SetInput("depth_1", depth_tensor_3);
        //workerBaseline.SetInput("depth_2", depth_tensor_2);
        //workerBaseline.SetInput("depth_3", depth_tensor_3);

        workerBaseline.Schedule();

        Tensor<float> depth_outputTensor_2 = workerBaseline.PeekOutput("output_depth_0") as Tensor<float>;
        //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
        ComputeBuffer computeTensorData2 = ComputeTensorData.Pin(depth_outputTensor_2).buffer;

        Tensor<float> depth_outputTensor_3 = workerBaseline.PeekOutput("output_depth_1") as Tensor<float>;
        //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
        ComputeBuffer computeTensorData3 = ComputeTensorData.Pin(depth_outputTensor_3).buffer;

        Debug.Log("Run Model");

        return (computeTensorData0, computeTensorData1, computeTensorData2, computeTensorData3);
>>>>>>> multiDev-debug
        //}
        //else
        //{
        //    workerMaskBaseline.SetInput("rgb_0", color_tensor_0);
        //    workerMaskBaseline.SetInput("rgb_1", color_tensor_1);
        //    workerMaskBaseline.SetInput("depth_0", depth_tensor_0);
        //    workerMaskBaseline.SetInput("depth_1", depth_tensor_1);
        //    workerMaskBaseline.Schedule();

        //    Tensor<float> depth_outputTensor_0 = workerMaskBaseline.PeekOutput("output_depth_0") as Tensor<float>;
        //    //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
        //    ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

        //    Tensor<float> depth_outputTensor_1 = workerMaskBaseline.PeekOutput("output_depth_1") as Tensor<float>;
        //    //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
        //    ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

        //    color_tensor_0.Dispose();
        //    color_tensor_1.Dispose();
        //    depth_tensor_0.Dispose();
        //    depth_tensor_1.Dispose();

        //    return (computeTensorData0, computeTensorData1);
        //}


    }
}