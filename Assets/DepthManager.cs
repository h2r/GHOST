using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;
using Unity.VisualScripting;

public class DepthManager : MonoBehaviour
{
    public bool avg_before_completion;
    public float varianceThreshold;
    public float meanThreshold;

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

    public DepthAveraging AveragerLeft, AveragerRight;

    private Tensor<float> depth_left_t_1;
    private Tensor<float> rgb_left_t_1;

    private Tensor<float> depth_right_t_1;
    private Tensor<float> rgb_right_t_1;

    private bool received_left_1 = false;
    private bool received_right_1 = false;

    float[] left_depth_avg = new float[480 * 640];
    float[] right_depth_avg = new float[480 * 640];

    private bool depth_process_lock = false;

    public DrawMeshInstanced Left_Depth_Renderer_1;
    public DrawMeshInstanced Right_Depth_Renderer_1;

    private ComputeBuffer temp_depth_left;
    private ComputeBuffer temp_depth_right;
    private ComputeBuffer temp_optical_left, temp_depth_left_return;
    private ComputeBuffer temp_optical_right, temp_depth_right_return;

    Matrix4x4 mat_l, mat_r;

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

    float[] old_depth1 = new float[480 * 640];
    float[] old_depth2 = new float[480 * 640];

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

        depth_left_t_1 = new Tensor<float>(depth_shape);
        depth_right_t_1 = new Tensor<float>(depth_shape);

        rgb_left_t_1 = new Tensor<float>(color_shape, data: null);
        rgb_right_t_1 = new Tensor<float>(color_shape, data: null);

        if (activate_depth_estimation)
        {
            StartCoroutine(ResetActivateDepthEstimation());
        }

    }

    void OnDestroy() {
        rgb_left_t_1?.Dispose();
        depth_left_t_1?.Dispose();
        rgb_right_t_1?.Dispose();
        depth_right_t_1?.Dispose();
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.G))
        {
            activate_depth_estimation = !activate_depth_estimation;
            activate_edge_detection = !activate_edge_detection;
            avg_before_completion = !avg_before_completion;
        }

        if (received_left_1 && received_right_1 && !depth_process_lock)
        {
            depth_process_lock = true;

            bool not_moving = Left_Depth_Renderer_1.get_ready_to_freeze();
            (temp_output_left_1, temp_output_right_1, icp_trans_temp) = process_depth(depth_left_t_1, rgb_left_t_1, depth_right_t_1, rgb_right_t_1, not_moving, false);

            received_left_1 = false;
            received_right_1 = false;

            depth_process_lock = false;
        }
    }

    public static bool AreFloatArraysEqual(float[] arr1, float[] arr2, float epsilon = 1e-6f)
    {
        // Check for null
        if (arr1 == null || arr2 == null)
        {
            return arr1 == arr2; // both null is considered "equal", otherwise false
        }

        // Check lengths
        if (arr1.Length != arr2.Length)
        {
            return false;
        }

        // Compare elements with tolerance
        for (int i = 0; i < arr1.Length; i++)
        {
            if (Mathf.Abs(arr1[i] - arr2[i]) > epsilon)
            {
                return false;
            }
        }

        return true;
    }


    private IEnumerator ResetActivateDepthEstimation()
    {
        activate_depth_estimation = false;
        yield return new WaitForSeconds(0.1f);
        activate_depth_estimation = true;
    }


    public (ComputeBuffer, Matrix4x4, float[]) update_depth_from_renderer(Texture2D rgb, float[] depth, int camera_index, bool calculate_icp, bool new_depth, bool avg_before_complete)
    {
        TextureTransform tform = new();
        tform.SetDimensions(rgb.width, rgb.height, 3);


        if (depth.Length != 480 * 640)
        {
            if (camera_index == 0)
            {
                return (temp_output_left_1, icp_trans_temp, left_depth_avg);
            }
            else if (camera_index == 1)
            {
                return (temp_output_right_1, icp_trans_temp, right_depth_avg);
            }
        }
        
        if (camera_index == 0 && !received_left_1 && new_depth)
        {
            //fps_timer.start(left_eye_data_timer_id);

            left_depth_avg = AveragerLeft.averaging(depth, Left_Depth_Renderer_1.get_ready_to_freeze(), avg_before_completion);
            if (depth_left_t_1 != null)
            {
                depth_left_t_1.Upload(left_depth_avg);
            }
            else
            {
                depth_left_t_1 = new Tensor<float>(depth_shape, left_depth_avg);
            }
            TextureConverter.ToTensor(rgb, rgb_left_t_1, tform);
            rgb_left_t_1.Reshape(color_shape);

            received_left_1 = true;

            //fps_timer.end(left_eye_data_timer_id);
        }
        else if (camera_index == 1 && !received_right_1 && new_depth)
        {
            //fps_timer.start(right_eye_data_timer_id);

            right_depth_avg = AveragerRight.averaging(depth, Left_Depth_Renderer_1.get_ready_to_freeze(), avg_before_completion);
            if (depth_right_t_1 != null)
            {
                depth_right_t_1.Upload(right_depth_avg);
            }
            else
            {
                depth_right_t_1 = new Tensor<float>(depth_shape, right_depth_avg);
            }
            TextureConverter.ToTensor(rgb, rgb_right_t_1, tform);
            rgb_right_t_1.Reshape(color_shape);

            received_right_1 = true;

            //fps_timer.end(right_eye_data_timer_id);
        }

        if (camera_index == 0)
        {
            return (temp_output_left_1, icp_trans_temp, left_depth_avg);
        }
        else if (camera_index == 1)
        {
            return (temp_output_right_1, icp_trans_temp, right_depth_avg);
        }

        return (temp_output_left_1, icp_trans_temp, right_depth_avg);
    }

    private (ComputeBuffer, ComputeBuffer, Matrix4x4) process_depth(Tensor<float> depthL_1, Tensor<float> rgbL_1, Tensor<float> depthR_1, Tensor<float> rgbR_1, bool is_not_moving, bool calculate_icp)
    {
        ICP_trans = Matrix4x4.identity;
        if (calculate_icp && activate_ICP) 
        {
            ICP_trans = ICP_launcher.run_ICP();
        }

        float edgethreshold = 0.0f;

        (temp_depth_left, temp_depth_right, mat_l, mat_r, temp_optical_left, temp_optical_right) = CVD_generator.generatePoseData(depthL_1, rgbL_1, depthR_1, rgbR_1, activate_depth_estimation, activate_CVD && is_not_moving);

        temp_depth_left_return = CVDLeft.consistent_depth(temp_depth_left, mat_l, temp_optical_left, activate_CVD && is_not_moving, edgethreshold, activate_edge_detection, activate_depth_estimation, cvd_weight);
        temp_depth_right_return = CVDRight.consistent_depth(temp_depth_right, mat_r, temp_optical_right, activate_CVD && is_not_moving, edgethreshold, activate_edge_detection, activate_depth_estimation, cvd_weight);

        return (temp_depth_left_return, temp_depth_right_return, ICP_trans);
    }
}
