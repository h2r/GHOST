using UnityEngine;
using Unity.Netcode;
using System.Diagnostics;
public class TextFaceCamera : MonoBehaviour
{
    private GameObject mainCamera; // Assign in Inspector or use Camera.main
    void Start()
    {
        mainCamera=NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.transform.Find("TrackingSpace/CenterEyeAnchor").gameObject;
        UnityEngine.Debug.Log("Main Camera:"+mainCamera.name);
    }
    void Update()
    {
        Quaternion lookRotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);
        // unset everything but the y rotation
        transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
    }
}
