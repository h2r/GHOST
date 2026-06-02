using UnityEngine;

public class FoDemoControl : MonoBehaviour
{
    public DepthManager depthManager1, depthManager2;

    public bool vision_control = true;
    bool pre_vision_control = true;

    void Update()
    {
        if (vision_control != pre_vision_control)
        {
            depthManager1.activate_depth_estimation = vision_control;
            depthManager1.activate_edge_detection = vision_control;

            depthManager2.activate_depth_estimation = vision_control;
            depthManager2.activate_edge_detection = vision_control;

            pre_vision_control = vision_control;
        }
    }
}
