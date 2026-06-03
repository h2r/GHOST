using Unity.InferenceEngine;
using UnityEngine;

public class CVDDataGenerator : MonoBehaviour
{
    public bool enableDebugLogging = false;
    private bool firstRun = true;

    private Tensor<float> previousRgbL;
    private Tensor<float> previousRgbR;

    public DrawMeshInstanced render_left;
    public DrawMeshInstanced render_right;

    private DepthCompletion depth_completion;
    private OpticalFlowEstimator optical_flow_estimation;

    private ComputeBuffer bufferDepthL, bufferDepthR, bufferOpticalL, bufferOpticalR;
    // Only fallback optical buffers are owned here. Depth tensors and estimator output are borrowed.
    private bool ownsOpticalBuffers;

    private Matrix4x4 leftPose, rightPose;

    private int GetOpticalBufferCount(Tensor<float> depthTensor)
    {
        return depthTensor != null ? Mathf.Max(1, depthTensor.count * 2) : 640 * 480 * 2;
    }

    void Start()
    {
        depth_completion = GetComponent<DepthCompletion>();
        optical_flow_estimation = GetComponent<OpticalFlowEstimator>();
    }

    void OnDestroy()
    {
        previousRgbL?.Dispose();
        previousRgbR?.Dispose();
        
        if (ownsOpticalBuffers)
        {
            bufferOpticalL?.Release();
            bufferOpticalR?.Release();
        }
        if (enableDebugLogging)
            Debug.Log("CVDDataGenerator destroyed and buffers released.");
    }

    private void CopyPreviousRgb(Tensor<float> rgbL, Tensor<float> rgbR)
    {
        // Optical flow needs history that survives SpotObserverClient reusing the live tensor buffers.
        for (int i = 0; i < previousRgbL.count; i++)
        {
            previousRgbL[i] = rgbL[i];
            previousRgbR[i] = rgbR[i];
        }
    }

    public (ComputeBuffer, ComputeBuffer, Matrix4x4, Matrix4x4, ComputeBuffer, ComputeBuffer) generatePoseData(Tensor<float> depthL, Tensor<float> rgbL, Tensor<float> depthR, Tensor<float> rgbR, bool activate_depth_completion, bool activate_CVD)
    {
        if (firstRun)
        {
            previousRgbL = new Tensor<float>(rgbL.shape);
            previousRgbR = new Tensor<float>(rgbR.shape);
            CopyPreviousRgb(rgbL, rgbR);

            bufferDepthL = ComputeTensorData.Pin(depthL).buffer;
            bufferDepthR = ComputeTensorData.Pin(depthR).buffer;
            bufferOpticalL = new ComputeBuffer(GetOpticalBufferCount(depthL), sizeof(float));
            bufferOpticalR = new ComputeBuffer(GetOpticalBufferCount(depthR), sizeof(float));
            ownsOpticalBuffers = true;

            firstRun = false;

            return (bufferDepthL, bufferDepthR, leftPose, rightPose, bufferOpticalL, bufferOpticalR);
        }
        else
        {
            if (activate_depth_completion)
            {
                (bufferDepthL, bufferDepthR) = depth_completion.complete(depthL, rgbL, depthR, rgbR);
            }
            else
            {
                bufferDepthL = ComputeTensorData.Pin(depthL).buffer;
                bufferDepthR = ComputeTensorData.Pin(depthR).buffer;
            }

            if (activate_CVD)
            {
                leftPose = render_left.GetCurrentPose();
                rightPose = render_right.GetCurrentPose();
                if (ownsOpticalBuffers)
                {
                    bufferOpticalL?.Release();
                    bufferOpticalR?.Release();
                    ownsOpticalBuffers = false;
                }
                (bufferOpticalL, bufferOpticalR) = optical_flow_estimation.estimate_all(previousRgbL, rgbL, previousRgbR, rgbR);

                CopyPreviousRgb(rgbL, rgbR);
            }

            return (bufferDepthL, bufferDepthR, leftPose, rightPose, bufferOpticalL, bufferOpticalR);
        }
    }
}
