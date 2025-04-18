using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthAveraging : MonoBehaviour
{
    public ComputeShader average_shader;

    int edge_kernel;
    int mean_kernel;
    int clear_kernel;
    int median_kernel;
    int fast_median_kernel;

    int num_frames = 30;

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


        // Data & Buffer
        //depthArCompute = new ComputeBuffer(480 * 640, sizeof(float));
        depthBufferCompute = new ComputeBuffer(480 * 640 * num_frames, sizeof(float));
        average_shader.SetInt("num_frames", num_frames);
        depthBufferCompute.SetData(depth_buffer);

        average_shader.SetBuffer(mean_kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(clear_kernel, "depth_buffer", depthBufferCompute);
    }

    public void ClearBuffer()
    {
        average_shader.Dispatch(clear_kernel, groupsX, groupsY, 1);
        buffer_pos = 0;
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
            average_shader.SetBool("activate", is_not_moving);   // activate = activate_averaging
            average_shader.SetInt("buffer_pos", buffer_pos);
            average_shader.SetFloat("varianceThreshold", varianceThreshold);
            depth_ar_buffer.SetData(depth_ar);

            average_shader.SetBuffer(mean_kernel, "depth_ar", depth_ar_buffer);
            average_shader.Dispatch(mean_kernel, groupsX, groupsY, 1);

            buffer_pos = (buffer_pos + 1) % (num_frames - 1);

            depth_ar_buffer.GetData(res_ar);
            return res_ar;
        }

        return depth_ar;
    }
}
