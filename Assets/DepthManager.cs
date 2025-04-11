using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;

public class DepthManager : MonoBehaviour
{
    public bool activate_CVD;
    public float cvd_weight;
    public bool show_spot;
    public DepthManager another_manager;

    private CVDDataGenerator CVD_generator;
    public PoseConsistentVideoDepth CVDLeft;
    public PoseConsistentVideoDepth CVDRight;

    public bool activate_depth_estimation;
    public bool activate_ICP;
    public bool activate_edge_detection;

    public float edgeThreshold;
    public int maxNeighbourNum;
    //public bool mean_averaging;
    //public bool median_averaging;
    //public bool edge_detection;

    //public float edge_threshold;

    private Tensor<float> depth_left_t_1;
    private Tensor<float> rgb_left_t_1;

    private Tensor<float> depth_right_t_1;
    private Tensor<float> rgb_right_t_1;

    //private float[] output_left = new float[480 * 640];
    //private float[] output_right = new float[480 * 640];

    private bool received_left_1 = false;
    private bool received_right_1 = false;

    private bool depth_process_lock = false;

    //public DepthAveraging AveragerLeft;
    //public DepthAveraging AveragerRight;

    public DrawMeshInstanced Left_Depth_Renderer_1;
    public DrawMeshInstanced Right_Depth_Renderer_1;

    private ComputeBuffer temp_depth_left;
    private ComputeBuffer temp_depth_right;
    private ComputeBuffer temp_optical_left, temp_depth_left_return;
    private ComputeBuffer temp_optical_right, temp_depth_right_return;

    Matrix4x4 mat_l, mat_r;

    //bool first_run = false;

    //private float deltaTime = 0.0f;

    //public GameObject FPSDisplayObject;

    //private FPSCounter fps_timer;
    //private int depth_completion_timer_id;
    //private int averaging_timer_id;
    //private int left_eye_data_timer_id;
    //private int right_eye_data_timer_id;

    private DepthCompletion depth_completion;
    private ICPLauncher ICP_launcher;
    private ComputeBuffer temp_output_left_1;
    private ComputeBuffer temp_output_right_1;

    private ComputeBuffer normal_temp_output_left_1;
    private ComputeBuffer normal_temp_output_right_1;

    private ComputeBuffer complete_output_left_1;
    private ComputeBuffer complete_output_right_1;

    private ComputeBuffer final_out;

    TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
    TensorShape color_shape = new TensorShape(1, 3, 480, 640);

    Matrix4x4 ICP_trans = Matrix4x4.identity;
    Matrix4x4 icp_trans_temp = Matrix4x4.identity;

    public float y_max;
    public float z_max;

    public bool show_sampling_res;

    // Start is called before the first frame update
    void Start()
    {
        CVD_generator = GetComponent<CVDDataGenerator>();

        mat_l = new Matrix4x4(
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0)
        );

        mat_r = new Matrix4x4(
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0)
        );

        depth_completion = GetComponent<DepthCompletion>();
        ICP_launcher = GetComponent<ICPLauncher>();

        temp_output_left_1 = new ComputeBuffer(480 * 640, sizeof(float));
        temp_output_right_1 = new ComputeBuffer(480 * 640, sizeof(float));

        normal_temp_output_left_1 = new ComputeBuffer(480 * 640, sizeof(float));
        normal_temp_output_right_1 = new ComputeBuffer(480 * 640, sizeof(float));

        final_out = new ComputeBuffer(480 * 640, sizeof(float));

        //fps_timer = FPSDisplayObject.GetComponent<FPSCounter>();

        //depth_completion_timer_id = fps_timer.registerTimer("Depth completion");
        //averaging_timer_id = fps_timer.registerTimer("Mean Averaging");
        //left_eye_data_timer_id = fps_timer.registerTimer("Left eye data");
        //right_eye_data_timer_id = fps_timer.registerTimer("Right eye data");

        depth_left_t_1 = new Tensor<float>(depth_shape, data: null);
        depth_right_t_1 = new Tensor<float>(depth_shape, data: null);

        rgb_left_t_1 = new Tensor<float>(color_shape, data: null);
        rgb_right_t_1 = new Tensor<float>(color_shape, data: null);

        if (activate_depth_estimation)
        {
            StartCoroutine(ResetActivateDepthEstimation());
        }

    }


    private IEnumerator ResetActivateDepthEstimation()
    {
        activate_depth_estimation = false;
        yield return new WaitForSeconds(0.1f);
        activate_depth_estimation = true;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public (ComputeBuffer, Matrix4x4) update_depth_from_renderer(Texture2D rgb, float[] depth, int camera_index, bool calculate_icp)
    {
        TextureTransform tform = new();
        tform.SetDimensions(rgb.width, rgb.height, 3);


        if (depth.Length != 480 * 640)
        {
            if (camera_index == 0)
            {
                return (temp_output_left_1, icp_trans_temp);
            }
            else if (camera_index == 1)
            {
                return (temp_output_right_1, icp_trans_temp);
            }
        }
        //Debug.Log("GO");

        if (camera_index == 0 && !received_left_1)
        {
            //fps_timer.start(left_eye_data_timer_id);

            //depth_left = (float[])depth.Clone();

            //if (rgb_left != null)
            //{
            //    Destroy(rgb_left);
            //}
            //rgb_left = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            //Graphics.CopyTexture(rgb, rgb_left);

            depth_left_t_1 = new Tensor<float>(depth_shape, depth);
            TextureConverter.ToTensor(rgb, rgb_left_t_1, tform);
            rgb_left_t_1.Reshape(color_shape);

            normal_temp_output_left_1.SetData(depth);

            received_left_1 = true;

            //fps_timer.end(left_eye_data_timer_id);
        }
        else if (camera_index == 1 && !received_right_1)
        {
            //fps_timer.start(right_eye_data_timer_id);

            depth_right_t_1 = new Tensor<float>(depth_shape, depth);
            TextureConverter.ToTensor(rgb, rgb_right_t_1, tform);
            rgb_right_t_1.Reshape(color_shape);

            normal_temp_output_right_1.SetData(depth);

            received_right_1 = true;

            //fps_timer.end(right_eye_data_timer_id);
        }

        //Debug.Log(received_left_1); Debug.Log(received_right_1);
        //Debug.Log(received_left_2); Debug.Log(received_right_2);

        if (received_left_1 && received_right_1 && !depth_process_lock)
        {
            depth_process_lock = true;

            //bool not_moving = Left_Depth_Renderer.get_ready_to_freeze() && Right_Depth_Renderer.get_ready_to_freeze();
            //bool not_moving = Left_Depth_Renderer_1.get_ready_to_freeze();
            bool not_moving = true;
            //not_moving = true;
            (temp_output_left_1, temp_output_right_1, icp_trans_temp) = process_depth(depth_left_t_1, rgb_left_t_1, depth_right_t_1, rgb_right_t_1, not_moving, calculate_icp);
            //Debug.Log("hihi");

            if (depth_left_t_1 != null) { depth_left_t_1.Dispose(); }
            if (rgb_left_t_1 != null) { rgb_left_t_1.Dispose(); }
            if (depth_right_t_1 != null) { depth_right_t_1.Dispose(); }
            if (rgb_right_t_1 != null) { rgb_right_t_1.Dispose(); }

            received_left_1 = false;
            received_right_1 = false;

            depth_process_lock = false;
            //first_run = true;
        }

        if (activate_depth_estimation)
        {
            if (camera_index == 0 && temp_output_left_1 != null)
            {
                return (temp_output_left_1, icp_trans_temp);
            }
            else if (camera_index == 1 && temp_output_right_1 != null)
            {
                return (temp_output_right_1, icp_trans_temp);
            }
            return (final_out, icp_trans_temp);
        }
        else
        {
            if (camera_index == 0 && normal_temp_output_left_1 != null)
            {
                return (normal_temp_output_left_1, icp_trans_temp);
            }
            else if (camera_index == 1 && normal_temp_output_right_1 != null)
            {
                return (normal_temp_output_right_1, icp_trans_temp);
            }
            return (final_out, icp_trans_temp);
        }
        
    }

    private (ComputeBuffer, ComputeBuffer, Matrix4x4) process_depth(Tensor<float> depthL_1, Tensor<float> rgbL_1, Tensor<float> depthR_1, Tensor<float> rgbR_1, bool is_not_moving, bool calculate_icp)
    {
        //if (median_averaging && mean_averaging)
        //{
        //    mean_averaging = false;
        //}

        //float[] temp_output_left = depthL, temp_output_right = depthR;

        //complete_output_left_1 = ComputeTensorData.Pin(depthL_1).buffer;
        //complete_output_right_1 = ComputeTensorData.Pin(depthR_1).buffer;
        //complete_output_left_2 = ComputeTensorData.Pin(depthL_2).buffer;
        //complete_output_right_2 = ComputeTensorData.Pin(depthR_2).buffer;

        ICP_trans = Matrix4x4.identity;
        if (calculate_icp && activate_ICP) 
        {
            ICP_trans = ICP_launcher.run_ICP();
        }

        //// depth completion
        //Debug.Log("depth completion");
        //is_not_moving = true;
        //if (activate_depth_estimation && is_not_moving)
        //{
        //    //fps_timer.start(depth_completion_timer_id);
        //    (complete_output_left_1, complete_output_right_1) = depth_completion.complete(depthL_1, rgbL_1, depthR_1, rgbR_1);
        //    //fps_timer.end(depth_completion_timer_id);
        //}
        //else
        //{
        //    //complete_output_left_1 = ComputeTensorData.Pin(depthL_1).buffer;
        //    //complete_output_right_1 = ComputeTensorData.Pin(depthR_1).buffer;
        //    //complete_output_left_2 = ComputeTensorData.Pin(depthL_2).buffer;
        //    //complete_output_right_2 = ComputeTensorData.Pin(depthR_2).buffer;
        //}

        float edgethreshold = 0.0f;

        (temp_depth_left, temp_depth_right, mat_l, mat_r, temp_optical_left, temp_optical_right) = CVD_generator.generatePoseData(depthL_1, rgbL_1, depthR_1, rgbR_1, activate_depth_estimation, activate_CVD);


        temp_depth_left_return = CVDLeft.consistent_depth(temp_depth_left, mat_l, temp_optical_left, activate_CVD, edgethreshold, activate_edge_detection, activate_depth_estimation, cvd_weight);
        //Debug.Log("2 kernel 1");
        temp_depth_right_return = CVDRight.consistent_depth(temp_depth_right, mat_r, temp_optical_right, activate_CVD, edgethreshold, activate_edge_detection, activate_depth_estimation, cvd_weight);

        //fps_timer.start(averaging_timer_id);
        //temp_output_left = AveragerLeft.averaging(temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        //temp_output_right = AveragerRight.averaging(temp_output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        //fps_timer.end(averaging_timer_id);

        //float[] ol = new float[480 * 640], or = new float[480 * 640];

        //temp_output_left.GetData(ol);
        //temp_output_right.GetData(or);


        // merge and downsample
        // find correspondence
        // calculate center of mass
        // accumulate
        // return transformation


        //return (complete_output_left_1, complete_output_right_1, ICP_trans);

        return (temp_depth_left_return, temp_depth_right_return, ICP_trans);
    }
}
