using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoDemoControl : MonoBehaviour
{
    public DepthManager depthManager1, depthManager2;

    public bool vision_control = true;
    bool pre_vision_control = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (vision_control != pre_vision_control)
        {
            depthManager1.activate_depth_estimation = vision_control;
            depthManager1.activate_edge_detection = vision_control;
            depthManager1.avg_before_completion = vision_control;

            depthManager2.activate_depth_estimation = vision_control;
            depthManager2.activate_edge_detection = vision_control;
            depthManager2.avg_before_completion = vision_control;

            pre_vision_control = vision_control;
        }
    }
}
