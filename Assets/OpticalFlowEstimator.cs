using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class OpticalFlowEstimator : MonoBehaviour
{
    public ModelAsset NeuV2;
    Model runtimeModelNeuV2;
    Worker workerNeuV2R;
    Worker workerNeuV2L;

    //ComputeBuffer computeTensorData0;
    //ComputeBuffer computeTensorData1;

    // Start is called before the first frame update
    void Start()
    {
        runtimeModelNeuV2 = ModelLoader.Load(NeuV2);
        workerNeuV2R = new Worker(runtimeModelNeuV2, BackendType.GPUCompute);

        workerNeuV2L = new Worker(runtimeModelNeuV2, BackendType.GPUCompute);
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

    public (ComputeBuffer, ComputeBuffer) estimate_all(Tensor<float> rgbL0, Tensor<float> rgbL1, Tensor<float> rgbR0, Tensor<float> rgbR1)
    {
        //Debug.Log("estimate Flow");
        // left
        workerNeuV2L.SetInput("input1", rgbL0);
        workerNeuV2L.SetInput("input2", rgbL1);
        workerNeuV2L.Schedule();

        Tensor<float> depth_outputTensor_0 = workerNeuV2L.PeekOutput() as Tensor<float>;
        //depth_outputTensor_0.Reshape(new TensorShape(2, 480, 640));
        ComputeBuffer computeData0 = ComputeTensorData.Pin(depth_outputTensor_0).buffer;
        //Debug.Log(depth_outputTensor_0);

        // right
        workerNeuV2R.SetInput("input1", rgbR0);
        workerNeuV2R.SetInput("input2", rgbR1);
        workerNeuV2R.Schedule();

        Tensor<float> depth_outputTensor_1 = workerNeuV2R.PeekOutput() as Tensor<float>;
        //depth_outputTensor_1.Reshape(new TensorShape(2, 480, 640));
        ComputeBuffer computeData1 = ComputeTensorData.Pin(depth_outputTensor_1).buffer;
        //Debug.Log(depth_outputTensor_1);

        //depth_outputTensor_0.Dispose();
        //depth_outputTensor_1.Dispose();

        //Debug.Log("estimate Flow Return");
        return (computeData0, computeData1);
    }

}