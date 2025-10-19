using UnityEngine;

public class TextFaceCamera : MonoBehaviour
{
    public GameObject mainCamera; // Assign in Inspector or use Camera.main

    void Update()
    {
        Quaternion lookRotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);
        // unset everything but the y rotation
        transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
    }
}