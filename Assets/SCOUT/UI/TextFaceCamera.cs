using UnityEngine;
using Unity.Netcode;
using System.Diagnostics;
using System.Data.SqlTypes;
public class TextFaceCamera : MonoBehaviour
{
    private GameObject mainCamera;
    public PlayerObjectsBridge bridge; // Assign in Inspector or use Camera.main
    void Start()
    {
        if (bridge.Storage != null)
        {
            mainCamera=bridge.Storage.CenterEyeAnchor;
        }
        
    }
    void Update()
    {
        if (mainCamera==null)
        {
            if (bridge.Storage != null)
            {
                mainCamera=bridge.Storage.CenterEyeAnchor;
            }
            else
            {
                return;
            }
        }
        
        Quaternion lookRotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);
        // unset everything but the y rotation
        transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
    }
}
