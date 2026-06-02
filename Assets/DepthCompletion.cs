using Unity.InferenceEngine;
using UnityEngine;

public class DepthCompletion : MonoBehaviour
{
    public ModelAsset rgbdModelAsset;
    private Model runtimeModelRGBD;
    private Worker workerRGBD;

    public bool Use_UB; // Use UnityBYOM for inference
    public UBModel UB_Model; // Reference to UBModel script

    void Start()
    {
        if (Use_UB)
            return;

        runtimeModelRGBD = ModelLoader.Load(rgbdModelAsset);
        workerRGBD = new Worker(runtimeModelRGBD, BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        workerRGBD?.Dispose();
        runtimeModelRGBD = null;
    }

    public (ComputeBuffer, ComputeBuffer) complete(Tensor<float> depth_tensor_0, Tensor<float> color_tensor_0, Tensor<float> depth_tensor_1, Tensor<float> color_tensor_1)
    {
        if (Use_UB)
        {
            // UBModel returns GPU buffers backed by its own tensors; ownership stays with UBModel.
            return UB_Model.Complete(color_tensor_0, depth_tensor_0, color_tensor_1, depth_tensor_1);
        }

        workerRGBD.SetInput("rgb_0", color_tensor_0);
        workerRGBD.SetInput("rgb_1", color_tensor_1);

        workerRGBD.SetInput("depth_0", depth_tensor_0);
        workerRGBD.SetInput("depth_1", depth_tensor_1);

        workerRGBD.Schedule();

        Tensor<float> depthOutputTensor0 = workerRGBD.PeekOutput("output_depth_0") as Tensor<float>;
        ComputeBuffer computeTensorData0 = ComputeTensorData.Pin(depthOutputTensor0).buffer;

        Tensor<float> depthOutputTensor1 = workerRGBD.PeekOutput("output_depth_1") as Tensor<float>;
        ComputeBuffer computeTensorData1 = ComputeTensorData.Pin(depthOutputTensor1).buffer;

        return (computeTensorData0, computeTensorData1);
    }
}
