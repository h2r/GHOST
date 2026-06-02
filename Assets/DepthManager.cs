using UnityEngine;

public class DepthManager : MonoBehaviour
{
    // Shared inspector state consumed by DrawMeshInstanced and PoseConsistentVideoDepth.
    public float meanThreshold;

    public bool activate_CVD;
    public float cvd_weight;
    public bool show_spot;

    public bool activate_depth_estimation;
    public bool activate_edge_detection;

    public float edgeThreshold;

    public float y_max;
    public float z_max;
}
