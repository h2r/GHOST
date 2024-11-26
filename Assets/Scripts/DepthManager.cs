using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;
using static RosSharp.Urdf.Link.Visual.Material;

public class DepthManager : MonoBehaviour
{
    public bool activate_depth_estimation;
    public bool activate_CVD;
    //public bool activate_mean_averaging;
    //public bool mean_averaging;
    //public bool median_averaging;
    //public bool edge_detection;

    //public float edge_threshold;

    private Tensor<float> depth_left_t;
    private Tensor<float> rgb_left_t;

    private Tensor<float> depth_right_t;
    private Tensor<float> rgb_right_t;

    //private float[] output_left = new float[480 * 640];
    //private float[] output_right = new float[480 * 640];

    private bool received_left = false;
    private bool received_right = false;

    private bool depth_process_lock = false;

    //public DepthAveraging AveragerLeft;
    //public DepthAveraging AveragerRight;

    public DrawMeshInstanced Left_Depth_Renderer;
    public DrawMeshInstanced Right_Depth_Renderer;

    //bool first_run = false;

    //private float deltaTime = 0.0f;

    //public GameObject FPSDisplayObject;

    //private FPSCounter fps_timer;
    private int depth_completion_timer_id;
    private int averaging_timer_id;
    private int left_eye_data_timer_id;
    private int right_eye_data_timer_id;

    private DepthCompletion depth_completion;
    //private ComputeBuffer temp_output_left;
    //private ComputeBuffer temp_output_right;
    private ComputeBuffer temp_depth_left;
    private ComputeBuffer temp_depth_right;
    private ComputeBuffer temp_optical_left, temp_depth_left_return;
    private ComputeBuffer temp_optical_right, temp_depth_right_return;

    TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
    TensorShape color_shape = new TensorShape(1, 3, 480, 640);

    private CVDDataGenerator CVD_generator;
    public PoseConsistentVideoDepth CVDLeft;
    public PoseConsistentVideoDepth CVDRight;

    Matrix4x4 mat_l, mat_r;

    public float edgethreshold;
    public bool activate_edge_detection;

    // Start is called before the first frame update
    void Start()
    {
        CVD_generator = GetComponent<CVDDataGenerator>();
        depth_completion = GetComponent<DepthCompletion>();

        //fps_timer = FPSDisplayObject.GetComponent<FPSCounter>();

        //depth_completion_timer_id = fps_timer.registerTimer("Depth completion");
        //averaging_timer_id = fps_timer.registerTimer("Mean Averaging");
        //left_eye_data_timer_id = fps_timer.registerTimer("Left eye data");
        //right_eye_data_timer_id = fps_timer.registerTimer("Right eye data");

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

        // Setup depth-completion input tensors
        depth_left_t = new Tensor<float>(depth_shape, data: null);
        depth_right_t = new Tensor<float>(depth_shape, data: null);

        rgb_left_t = new Tensor<float>(color_shape, data: null);
        rgb_right_t = new Tensor<float>(color_shape, data: null);

    }

    // Update is called once per frame
    void Update()
    {
    }

    public ComputeBuffer update_depth_from_renderer(Texture2D rgb, float[] depth, int camera_index)
    {
        Debug.LogWarning("depth.Length = " + depth.Length);
        Debug.LogWarning("depth_shape.length = " + depth_shape.length);

        TextureTransform tform = new();
        tform.SetDimensions(rgb.width, rgb.height, 3);

        if (camera_index == 0 && !received_left)
        {
            //fps_timer.start(left_eye_data_timer_id);

            //depth_left = (float[])depth.Clone();

            //if (rgb_left != null)
            //{
            //    Destroy(rgb_left);
            //}
            //rgb_left = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            //Graphics.CopyTexture(rgb, rgb_left);

            //if (depth_left_t != null)
            //{
            //    depth_left_t.Dispose();
            //}
            //if (rgb_left_t != null)
            //{
            //    rgb_left_t.Dispose();
            //}

            depth_left_t = new Tensor<float>(depth_shape, depth);
            // depth_right_t.Upload(depth);
            TextureConverter.ToTensor(rgb, rgb_left_t, tform);
            rgb_left_t.Reshape(color_shape);

            received_left = true;

            //fps_timer.end(left_eye_data_timer_id);
        }
        else if (camera_index == 1 && !received_right)
        {
            //fps_timer.start(right_eye_data_timer_id);

            //if (depth_right_t != null)
            //{
            //    depth_right_t.Dispose();
            //}
            //if (rgb_right_t != null)
            //{
            //    rgb_right_t.Dispose();
            //}
            
            depth_right_t = new Tensor<float>(depth_shape, depth);
            //depth_right_t.Upload(depth);
            TextureConverter.ToTensor(rgb, rgb_right_t, tform);
            rgb_right_t.Reshape(color_shape);

            received_right = true;

            //fps_timer.end(right_eye_data_timer_id);
        }

        if (received_left && received_right && !depth_process_lock)
        {
            depth_process_lock = true;

            //bool not_moving = Left_Depth_Renderer.get_ready_to_freeze() && Right_Depth_Renderer.get_ready_to_freeze();
            bool not_moving = Left_Depth_Renderer.get_ready_to_freeze();
            //not_moving = true;

            Debug.LogWarning("Processing depth");
            (temp_depth_left_return, temp_depth_right_return) = process_depth(depth_left_t, rgb_left_t, depth_right_t, rgb_right_t, not_moving);

            if (depth_left_t != null)
            {
                depth_left_t.Dispose();
            }
            if (rgb_left_t != null)
            {
                rgb_left_t.Dispose();
            }
            if (depth_right_t != null)
            {
                depth_right_t.Dispose();
            }
            if (rgb_right_t != null)
            {
                rgb_right_t.Dispose();
            }

            received_left = false;
            received_right = false;

            depth_process_lock = false;
            //first_run = true;
        }

        if (temp_depth_left_return == null)
        {
            Debug.LogWarning("temp_depth_left_return is null");
        }

        if (temp_depth_right_return == null)
        {
            Debug.LogWarning("temp_depth_right_return is null");
        }

        if (camera_index == 0)
        {
            return temp_depth_left_return;
        }
        else if (camera_index == 1)
        {
            return temp_depth_right_return;
        }

        return temp_depth_right_return;
    }

    private (ComputeBuffer, ComputeBuffer) process_depth(Tensor<float> depthL, Tensor<float> rgbL, Tensor<float> depthR, Tensor<float> rgbR, bool is_not_moving)
    {
        //if (median_averaging && mean_averaging)
        //{
        //    mean_averaging = false;
        //}

        ////float[] temp_output_left = depthL, temp_output_right = depthR;

        //// depth completion
        ////Debug.Log("depth completion");
        //if (activate_depth_estimation && is_not_moving)
        //{
        //    //fps_timer.start(depth_completion_timer_id);
        //    (temp_output_left, temp_output_right) = depth_completion.complete(depthL, rgbL, depthR, rgbR);
        //    //fps_timer.end(depth_completion_timer_id);
        //}
        //else
        //{
        //    temp_output_left = ComputeTensorData.Pin(depthL).buffer;
        //    temp_output_right = ComputeTensorData.Pin(depthR).buffer;
        //}

        ////fps_timer.start(averaging_timer_id);
        //temp_output_left = AveragerLeft.averaging(temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        //temp_output_right = AveragerRight.averaging(temp_output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        ////fps_timer.end(averaging_timer_id);

        ////float[] ol = new float[480 * 640], or = new float[480 * 640];

        ////temp_output_left.GetData(ol);
        ////temp_output_right.GetData(or);

        //return (temp_output_left, temp_output_right);

        //Debug.Log("1 start manager");
        //(temp_depth_left, temp_depth_right, temp_optical_left, temp_optical_right) = CVD_generator.generateData(depthL, rgbL, depthR, rgbR, activate_depth_estimation, activate_CVD);
        Debug.LogWarning("calling CVD generator with: ");
        Debug.LogWarning("depthL = " + depthL +
                         " rgbL = " + rgbL +
                         " depthR = " + depthR +
                         " rgbR = " + rgbR +
                         " activate_depth_estimation = " + activate_depth_estimation +
                         " actvate_CVD = " + activate_CVD);

        (temp_depth_left, temp_depth_right, mat_l, mat_r, temp_optical_left, temp_optical_right) = CVD_generator.generatePoseData(depthL, rgbL, depthR, rgbR, activate_depth_estimation, activate_CVD);


        //Debug.Log("2 generate data");
        temp_depth_left_return = CVDLeft.consistent_depth(temp_depth_left, mat_l, temp_optical_left, activate_CVD, edgethreshold, activate_edge_detection);
        //Debug.Log("2 kernel 1");
        temp_depth_right_return = CVDRight.consistent_depth(temp_depth_right, mat_r, temp_optical_right, activate_CVD, edgethreshold, activate_edge_detection);
        //Debug.Log("4 kernel 2");


        return (temp_depth_left_return, temp_depth_right_return);
    }
}


