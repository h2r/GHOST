using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class controller : MonoBehaviour
{
    //public DepthManager depthManager;
    //public DrawMeshInstanced left_renderer;
    //public DrawMeshInstanced right_renderer;
    //public DepthAveraging left_averager;
    //public DepthAveraging right_averager;

    //public bool overall_control;
    //public bool depth_completion;
    //public bool averaging;
    //public bool edge_detection;
    //public bool inpainting;

    //private bool previous_overall_control;
    //private bool previous_depth_completion;
    //private bool previous_averaging;
    //private bool previous_edge_detection;
    //private bool previous_inpainting;

    //// Start is called before the first frame update
    //void Start()
    //{
    //    depthManager.activate_depth_estimation = depth_completion;
    //    depthManager.median_averaging = averaging;
    //    depthManager.edge_detection = edge_detection;

    //    left_renderer.activate_inpainting = inpainting;
    //    right_renderer.activate_inpainting = inpainting;

    //    previous_overall_control = overall_control;
    //    previous_depth_completion = depth_completion; 
    //    previous_averaging = averaging; 
    //    previous_edge_detection = edge_detection; 
    //    previous_inpainting = inpainting;
    //}

    //// Update is called once per frame
    //void Update()
    //{
    //    if (overall_control != previous_overall_control)
    //    {
    //        depth_completion = overall_control;
    //        averaging = overall_control;
    //        edge_detection = overall_control;
    //        inpainting = overall_control;

    //        previous_overall_control = overall_control;

    //        left_averager.clear_buffer();
    //        right_averager.clear_buffer();
    //    }

    //    if (depth_completion != previous_depth_completion)
    //    {
    //        depthManager.activate_depth_estimation = depth_completion;
    //        previous_depth_completion= depth_completion;
    //    }

    //    if (averaging != previous_averaging)
    //    {
    //        depthManager.median_averaging = averaging;
    //        previous_averaging = averaging;
    //    }

    //    if (edge_detection != previous_edge_detection)
    //    {
    //        depthManager.edge_detection = edge_detection;
    //        previous_edge_detection = edge_detection;
    //    }

    //    if (inpainting != previous_inpainting)
    //    {
    //        left_renderer.activate_inpainting = inpainting;
    //        right_renderer.activate_inpainting = inpainting;
    //        previous_inpainting = inpainting;
    //    }
    //}
}
