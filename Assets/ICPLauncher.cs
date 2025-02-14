using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ICPLauncher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Matrix4x4 run_ICP(ComputeBuffer depth0, ComputeBuffer depth1, ComputeBuffer depth2, ComputeBuffer depth3)
    {
        return Matrix4x4.identity;
    }
}
