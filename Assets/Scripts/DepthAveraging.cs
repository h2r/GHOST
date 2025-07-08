using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DepthAveraging : MonoBehaviour
{
    public ComputeShader average_shader;

    int edge_kernel;
    int mean_kernel;
    int clear_kernel;
    int median_kernel;
    int fast_median_kernel;

    int depth_prefill_kernel;
    int depth_buffer_update_kernel;

    int num_frames = 2;

    float[,] depth_buffer;
    //private ComputeBuffer depthArCompute;
    private ComputeBuffer depthBufferCompute;

    int buffer_pos = 0;
    private bool activate_fast_median_calculation = false;

    int groupsX = (640 + 16 - 1) / 16;
    int groupsY = (480 + 16 - 1) / 16;

    float[] output = new float[480 * 640];

    float[] res_ar = new float[480 * 640];

    ComputeBuffer depth_ar_buffer;
    bool buffer_empty = true;

    public DepthManager depthManager;

    float varianceThreshold;

    // Update is called once per frame
    void Update()
    {
        varianceThreshold = depthManager.varianceThreshold;
    }

    void Start()
    {

        depth_buffer = new float[num_frames, 480 * 640];
        // kernel
        depth_ar_buffer = new ComputeBuffer(480 * 640, sizeof(float));
        mean_kernel = average_shader.FindKernel("MeanAveraging");
        clear_kernel = average_shader.FindKernel("ClearBuffer");

        depth_prefill_kernel = average_shader.FindKernel("fill_depth_from_prev");
        depth_buffer_update_kernel = average_shader.FindKernel("update_depth_buffer");

        // Data & Buffer
        //depthArCompute = new ComputeBuffer(480 * 640, sizeof(float));
        depthBufferCompute = new ComputeBuffer(480 * 640 * num_frames, sizeof(float));
        average_shader.SetInt("num_frames", num_frames);
        depthBufferCompute.SetData(depth_buffer);

        buffer_empty = true;
    }

    public void ClearBuffer()
    {
        average_shader.SetBuffer(clear_kernel, "depth_buffer", depthBufferCompute);
        average_shader.Dispatch(clear_kernel, groupsX, groupsY, 1);
        buffer_pos = 0;
        buffer_empty = true;
    }

    void OnDestroy()
    {
        depthBufferCompute.Release();
        //depthArCompute.Release();
    }

    // TODO: return ComputBuffer directly
    public float[] averaging(float[] depth_ar, bool is_not_moving, bool mean_averaging)
    {
        //float[] temp = new float[480 * 640];

        bool is_moving = !is_not_moving;


        // aceraging && edge detection
        if (mean_averaging && is_not_moving)
        {
            throw new System.Exception("Mean averaging is not supported in this version. Use fast median or edge detection instead.");
            average_shader.SetBool("activate", is_not_moving);   // activate = activate_averaging
            average_shader.SetInt("buffer_pos", buffer_pos);
            average_shader.SetFloat("varianceThreshold", varianceThreshold);
            depth_ar_buffer.SetData(depth_ar);

            average_shader.SetBuffer(mean_kernel, "depth_ar", depth_ar_buffer);
            average_shader.SetBuffer(mean_kernel, "depth_buffer", depthBufferCompute);

            average_shader.Dispatch(mean_kernel, groupsX, groupsY, 1);

            //buffer_pos = (buffer_pos + 1) % (num_frames - 1);

            depth_ar_buffer.GetData(res_ar);
            return res_ar;
        }

        return depth_ar;
    }

    public void prev_filling(ComputeBuffer depth_ar)
    {
        if (buffer_empty)
        {
            return;
        }
        average_shader.SetInt("prev_buffer_pos", buffer_pos);

        average_shader.SetBuffer(depth_prefill_kernel, "depth_ar", depth_ar);
        average_shader.SetBuffer(depth_prefill_kernel, "depth_buffer", depthBufferCompute);

        Debug.Log("Filling depth buffer with previous frame data: " + buffer_pos);
        average_shader.Dispatch(depth_prefill_kernel, groupsX, groupsY, 1);

        Debug.Log("Depth buffer prefilled with previous frame data.");
    }

    public void update_depth_buffer(ComputeBuffer depth_ar)
    {
        buffer_pos = (buffer_pos + 1) % num_frames;

        Debug.Log("Committing current frame to past depth buffer: " + buffer_pos + " depth array: " + depthBufferCompute.GetNativeBufferPtr());

        average_shader.SetInt("buffer_pos", buffer_pos);
        average_shader.SetBuffer(depth_buffer_update_kernel, "depth_ar", depth_ar);
        average_shader.SetBuffer(depth_buffer_update_kernel, "depth_buffer", depthBufferCompute);
        average_shader.Dispatch(depth_buffer_update_kernel, groupsX, groupsY, 1);

        buffer_empty = false;

        Debug.Log("Updated depth buffer.");
    }
}
