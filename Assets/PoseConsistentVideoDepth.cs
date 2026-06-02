using UnityEngine;

public class PoseConsistentVideoDepth : MonoBehaviour
{
    public ComputeShader pose_consistent_depth_shader;

    int transformation_kernel;
    int cvd_kernel;

    // Default live camera size. DrawMeshInstanced updates these before initialization.
    public int Width = 640;
    public int Height = 480;

    int num_frames = 30;

    // Ring buffers owned by this component for temporal CVD history.
    private ComputeBuffer depthBufferCompute;
    private ComputeBuffer poseBufferCompute;
    private ComputeBuffer opticalBufferCompute;

    private Vector4 intrinsics;

    int buffer_pos = 0;
    private int allocatedWidth;
    private int allocatedHeight;

    int groupsX;
    int groupsY;

    // intrinsics
    public float CX, CY, FX, FY;

    private int FrameSize => Width * Height;

    void Start()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (pose_consistent_depth_shader == null)
            return;

        Width = Mathf.Max(1, Width);
        Height = Mathf.Max(1, Height);
        if (depthBufferCompute != null && allocatedWidth == Width && allocatedHeight == Height)
            return;

        ReleaseBuffers();

        groupsX = Mathf.CeilToInt(Width / 16f);
        groupsY = Mathf.CeilToInt(Height / 8f);
        allocatedWidth = Width;
        allocatedHeight = Height;

        // kernel
        transformation_kernel = pose_consistent_depth_shader.FindKernel("Transformation");
        cvd_kernel = pose_consistent_depth_shader.FindKernel("CVD");

        // Depth Buffer
        depthBufferCompute = new ComputeBuffer(FrameSize * num_frames, sizeof(float) * 3);

        // Pose Buffer
        poseBufferCompute = new ComputeBuffer(num_frames, sizeof(float) * 16);

        // Optical Buffer
        opticalBufferCompute = new ComputeBuffer(FrameSize * num_frames, sizeof(float) * 2);

        // Intrinsics
        intrinsics = new Vector4(CX, CY, FX, FY);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void ReleaseBuffers()
    {
        depthBufferCompute?.Release();
        depthBufferCompute = null;

        poseBufferCompute?.Release();
        poseBufferCompute = null;

        opticalBufferCompute?.Release();
        opticalBufferCompute = null;
    }

    // Writes converted/CVD-filtered float3 depth into the caller-owned output buffer.
    // The input depth and optical buffers are borrowed and are never released here.
    public bool WriteConsistentDepth(ComputeBuffer depth_buffer, ComputeBuffer output_buffer, Matrix4x4 pose_mat, ComputeBuffer optical_buffer, bool activate_CVD, float edgethreshold, bool activate_depth_completion, float cvd_weight)
    {
        if (depth_buffer == null || output_buffer == null || optical_buffer == null || pose_consistent_depth_shader == null)
            return false;

        EnsureInitialized();
        if (depthBufferCompute == null || poseBufferCompute == null || opticalBufferCompute == null)
            return false;

        intrinsics = new Vector4(CX, CY, FX, FY);
        pose_consistent_depth_shader.SetInt("frameWidth", Width);
        pose_consistent_depth_shader.SetInt("frameHeight", Height);
        pose_consistent_depth_shader.SetInt("buffer_pos", buffer_pos);
        pose_consistent_depth_shader.SetInt("num_frames", num_frames);
        pose_consistent_depth_shader.SetVector("intrinsics", intrinsics);
        pose_consistent_depth_shader.SetFloat("edgethreshold", edgethreshold);

        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "depth_ar", depth_buffer);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "optical_ar", optical_buffer);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "output_ar", output_buffer);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "depth_buffer", depthBufferCompute);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "optical_buffer", opticalBufferCompute);
        pose_consistent_depth_shader.SetBuffer(transformation_kernel, "pose_buffer", poseBufferCompute);

        pose_consistent_depth_shader.SetMatrix("pose", pose_mat);
        pose_consistent_depth_shader.Dispatch(transformation_kernel, groupsX, groupsY, 1);

        pose_consistent_depth_shader.SetFloat("cvd_weight", cvd_weight);

        if (activate_CVD && cvd_weight < 99)
        {
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "depth_ar", depth_buffer);
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "optical_ar", optical_buffer);
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "output_ar", output_buffer);
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "depth_buffer", depthBufferCompute);
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "optical_buffer", opticalBufferCompute);
            pose_consistent_depth_shader.SetBuffer(cvd_kernel, "pose_buffer", poseBufferCompute);


            Matrix4x4 inverse_pose_mat = Matrix4x4.Inverse(pose_mat);
            pose_consistent_depth_shader.SetMatrix("inverse_pose", inverse_pose_mat);
            pose_consistent_depth_shader.Dispatch(cvd_kernel, groupsX, groupsY, 1);

        }



        buffer_pos = (buffer_pos + 1) % num_frames;
        return true;
    }

}
