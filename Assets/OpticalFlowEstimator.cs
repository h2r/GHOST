using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class OpticalFlowEstimator : MonoBehaviour
{
    public Unity.InferenceEngine.ModelAsset NeuV2;
    Unity.InferenceEngine.Model runtimeModelNeuV2;
    Unity.InferenceEngine.Worker workerNeuV2R;
    Unity.InferenceEngine.Worker workerNeuV2L;

    //ComputeBuffer computeTensorData0;
    //ComputeBuffer computeTensorData1;

    // Start is called before the first frame update
    void Start()
    {
        runtimeModelNeuV2 = Unity.InferenceEngine.ModelLoader.Load(NeuV2);
        workerNeuV2R = new Unity.InferenceEngine.Worker(runtimeModelNeuV2, Unity.InferenceEngine.BackendType.GPUCompute);

        workerNeuV2L = new Unity.InferenceEngine.Worker(runtimeModelNeuV2, Unity.InferenceEngine.BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        workerNeuV2R.Dispose();
        workerNeuV2L.Dispose();
        if (runtimeModelNeuV2 != null)
        {
            runtimeModelNeuV2 = null;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public (ComputeBuffer, ComputeBuffer) estimate_all(Unity.InferenceEngine.Tensor<float> rgbL0, Unity.InferenceEngine.Tensor<float> rgbL1, Unity.InferenceEngine.Tensor<float> rgbR0, Unity.InferenceEngine.Tensor<float> rgbR1)
    {
        //Debug.Log("estimate Flow");
        // left
        workerNeuV2L.SetInput("input1", rgbL0);
        workerNeuV2L.SetInput("input2", rgbL1);
        workerNeuV2L.Schedule();

        Unity.InferenceEngine.Tensor<float> depth_outputTensor_0 = workerNeuV2L.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
        //depth_outputTensor_0.Reshape(new TensorShape(2, 480, 640));
        ComputeBuffer computeData0 = Unity.InferenceEngine.ComputeTensorData.Pin(depth_outputTensor_0).buffer;
        //Debug.Log(depth_outputTensor_0);

        // right
        workerNeuV2R.SetInput("input1", rgbR0);
        workerNeuV2R.SetInput("input2", rgbR1);
        workerNeuV2R.Schedule();

        Unity.InferenceEngine.Tensor<float> depth_outputTensor_1 = workerNeuV2R.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
        //depth_outputTensor_1.Reshape(new TensorShape(2, 480, 640));
        ComputeBuffer computeData1 = Unity.InferenceEngine.ComputeTensorData.Pin(depth_outputTensor_1).buffer;
        //Debug.Log(depth_outputTensor_1);

        //depth_outputTensor_0.Dispose();
        //depth_outputTensor_1.Dispose();

        //Debug.Log("estimate Flow Return");
        return (computeData0, computeData1);
    }

}