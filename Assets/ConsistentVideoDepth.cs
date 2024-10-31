using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsistentVideoDepth : MonoBehaviour
{
    public ComputeShader consistent_depth_shader;

    int kernel;

    int num_frames = 5;

    float[,] depth_buffer = new float[5, 480 * 640];
    private ComputeBuffer depthBufferCompute;

    float[,] optical_buffer = new float[10, 480 * 640];
    private ComputeBuffer opticalBufferCompute;

    int buffer_pos = 0;

    int groupsX = (640 + 16 - 1) / 16;
    int groupsY = (480 + 16 - 1) / 16;

    // Start is called before the first frame update
    void Start()
    {
        // kernel
        kernel = consistent_depth_shader.FindKernel("CSMain");

        // Depth Buffer
        depthBufferCompute = new ComputeBuffer(480 * 640 * num_frames, sizeof(float));
        consistent_depth_shader.SetInt("num_frames", num_frames);
        depthBufferCompute.SetData(depth_buffer);

        consistent_depth_shader.SetBuffer(kernel, "depth_buffer", depthBufferCompute);

        // Optical Buffer
        opticalBufferCompute = new ComputeBuffer(480 * 640 * 10, sizeof(float));
        opticalBufferCompute.SetData(optical_buffer);

        consistent_depth_shader.SetBuffer(kernel, "optical_buffer", opticalBufferCompute);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        depthBufferCompute.Release();
    }

    public ComputeBuffer consistent_depth(ComputeBuffer depth_buffer, ComputeBuffer optical_buffer, bool activate_optical_flow)
    {
        if (activate_optical_flow)
        {
            consistent_depth_shader.SetInt("buffer_pos", buffer_pos);
            consistent_depth_shader.SetBuffer(kernel, "depth_ar", depth_buffer);
            consistent_depth_shader.SetBuffer(kernel, "optical_ar", optical_buffer);

            consistent_depth_shader.Dispatch(kernel, groupsX, groupsY, 1);

            buffer_pos = (buffer_pos + 1) % (num_frames - 1);

            Debug.Log("kernel optical flow");
        }
        Debug.Log("kernel optical flow return");
        return depth_buffer;
    }

}
