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
    Model modifiedModel;
    Model runtimeModelMaskBaseline;
    Worker workerBaseline;
    Worker workerMaskBaseline;

    public bool use_baseline;
    //TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
    //TensorShape color_shape = new TensorShape(1, 3, 480, 640);

    Tensor<float> depth_outputTensor_0, depth_outputTensor_1;
    ComputeBuffer computeTensorData0, computeTensorData1;


    // =============================================================================== //
    //                               Init & OnRelease                                  //
    // =============================================================================== //
    void Start()
    {
        runtimeModelBaseline = ModelLoader.Load(Baseline);
        workerBaseline = new Worker(runtimeModelBaseline, BackendType.GPUCompute);

        /*        runtimeModelBaseline = ModelLoader.Load(Baseline);

                // Prepend and append transpose ops (as a hack)
                FunctionalGraph graph = new FunctionalGraph();

                // Add the model's input to the graph. This gives you a FunctionalTensor.
                FunctionalTensor input = graph.AddInput<float>(new TensorShape( 1, 3, 480, 640 ));
                FunctionalTensor fixedInput = input.Reshape(new int[] { 1, 3, 480, 640 });

                // Prepend a transpose on the input.
                // Adjust the axes indices to your desired order.
                // For example, if you want to swap axis 0 and axis 3:
                FunctionalTensor transposedInput = fixedInput.Transpose(2, 3);

        *//*        FunctionalTensor[] inputs_to_model = { transposedInput };
        *//*
                // Pass the transposed input through the baseline model.
                FunctionalTensor[] modelOutputs = Functional.Forward(runtimeModelBaseline, transposedInput);

                // Assuming your model has a single output, take the first one.
                FunctionalTensor output = modelOutputs[0];
                FunctionalTensor staticOutput = output.Reshape(new int[] { 1, 3, 640, 480 });

                // Append a transpose to the output.
                // Again, adjust the axes indices as needed.
                FunctionalTensor finalOutput = staticOutput.Transpose(2, 3);

                // Compile the new graph into a modified model.
                modifiedModel = graph.Compile(finalOutput);

                // Create a new worker using the updated model.
                workerBaseline = new Worker(modifiedModel, BackendType.GPUCompute);*/



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
            // Crop the rgb tensor to match the expected input dims
            Debug.Log("Tensor dims: " + color_tensor_0.shape);
            workerBaseline.SetInput("rgb", color_tensor_0);
            workerBaseline.Schedule();

            depth_outputTensor_0 = workerBaseline.PeekOutput("depth") as Tensor<float>;
            computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

            workerBaseline.SetInput("rgb", color_tensor_1);
            workerBaseline.Schedule();

            depth_outputTensor_1 = workerBaseline.PeekOutput("depth") as Tensor<float>;
            computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

            //color_tensor_0.Dispose();
            //color_tensor_1.Dispose();
            //depth_tensor_0.Dispose();
            //depth_tensor_1.Dispose();

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

            depth_outputTensor_0 = workerMaskBaseline.PeekOutput("output_depth_0") as Tensor<float>;
            //float[] output_depth_0 = depth_outputTensor_0.DownloadToArray();
            computeTensorData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;

            depth_outputTensor_1 = workerMaskBaseline.PeekOutput("output_depth_1") as Tensor<float>;
            //float[] output_depth_1 = depth_outputTensor_1.DownloadToArray();
            computeTensorData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;

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