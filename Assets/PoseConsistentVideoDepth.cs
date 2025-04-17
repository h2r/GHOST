using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseConsistentVideoDepth : MonoBehaviour
{
    public ComputeShader pose_consistent_depth_shader;

    int transformation_kernel;
    int edge_kernel;
    int cvd_kernel;

    int num_frames = 30;

    private ComputeBuffer depthBufferCompute;
    private ComputeBuffer poseBufferCompute;
    private ComputeBuffer opticalBufferCompute;

    private ComputeBuffer depthReturnCompute;

    int buffer_pos = 0;

    int groupsX = (640 + 16 - 1) / 16;
    int groupsY = (480 + 8 - 1) / 8;

    // intrinsics
    public float CX, CY, FX, FY;

    void Start()
    {
        // kernel
        transformation_kernel = pose_consistent_depth_shader.FindKernel("Transformation");
        edge_kernel = pose_consistent_depth_shader.FindKernel("EdgeDetection");
        cvd_kernel = pose_consistent_depth_shader.FindKernel("CVD");

        // Depth Buffer
        depthBufferCompute = new ComputeBuffer(480 * 640 * num_frames, sizeof(float) * 3);

        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "depth_buffer", depthBufferCompute);
        //pose_consistent_depth_shader.SetBuffer(edge_kernel, "depth_buffer", depthBufferCompute);
        pose_consistent_depth_shader.SetBuffer(cvd_kernel, "depth_buffer", depthBufferCompute);

        // Pose Buffer
        poseBufferCompute = new ComputeBuffer(num_frames, sizeof(float) * 16);
        //pose_consistent_depth_shader.SetBuffer(transformation_kernel, "pose_buffer", poseBufferCompute);
        pose_consistent_depth_shader.SetBuffer(cvd_kernel, "pose_buffer", poseBufferCompute);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "pose_buffer", poseBufferCompute);

        // Optical Buffer
        opticalBufferCompute = new ComputeBuffer(480 * 640 * num_frames * 2, sizeof(float));
        pose_consistent_depth_shader.SetBuffer(cvd_kernel, "optical_buffer", opticalBufferCompute);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "optical_buffer", opticalBufferCompute);

        // Return Ar Buffer
        depthReturnCompute = new ComputeBuffer(480 * 640, sizeof(float) * 3);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "output_ar", depthReturnCompute);
        pose_consistent_depth_shader.SetBuffer(edge_kernel, "output_ar", depthReturnCompute);
        pose_consistent_depth_shader.SetBuffer(cvd_kernel, "output_ar", depthReturnCompute);

        // others
        pose_consistent_depth_shader.SetInt("num_frames", num_frames);

        Vector4 intr = new Vector4(CX, CY, FX, FY);
        pose_consistent_depth_shader.SetVector("intrinsics", intr);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        depthBufferCompute.Release();
        poseBufferCompute.Release();
    }

    public ComputeBuffer consistent_depth(ComputeBuffer depth_buffer, Matrix4x4 pose_mat, ComputeBuffer optical_buffer, bool activate_CVD, float edgethreshold, bool activate_edge_detection, bool activate_depth_completion, float cvd_weight)
    {
        pose_consistent_depth_shader.SetInt("buffer_pos", buffer_pos);
        //if (activate_CVD || activate_edge_detection || activate_depth_completion)
        if (true)
        {
            pose_consistent_depth_shader.SetBuffer(transformation_kernel, "optical_ar", optical_buffer);
            pose_consistent_depth_shader.SetMatrix("pose", pose_mat);
            pose_consistent_depth_shader.SetBuffer(transformation_kernel, "depth_ar", depth_buffer);
            pose_consistent_depth_shader.Dispatch(transformation_kernel, groupsX, groupsY, 1);
        }

        if (activate_edge_detection)
        {
            //pose_consistent_depth_shader.SetBuffer(edge_kernel, "depth_ar", depth_buffer);
            pose_consistent_depth_shader.SetFloat("edgethreshold", edgethreshold);
            pose_consistent_depth_shader.Dispatch(edge_kernel, groupsX, groupsY, 1);
        }

        pose_consistent_depth_shader.SetFloat("cvd_weight", cvd_weight);

        if (activate_CVD && cvd_weight < 99)
        {
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "depth_ar", depth_buffer);
            Matrix4x4 inverse_pose_mat = Matrix4x4.Inverse(pose_mat);
            pose_consistent_depth_shader.SetMatrix("inverse_pose", inverse_pose_mat);
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "optical_ar", optical_buffer);
            pose_consistent_depth_shader.Dispatch(cvd_kernel, groupsX, groupsY, 1);
        }



        buffer_pos = (buffer_pos + 1) % num_frames;

        return depthReturnCompute;
    }

}