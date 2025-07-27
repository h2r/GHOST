using UnityEngine;

public class StayAtY : MonoBehaviour
{
    public GameObject mainCamera; // Assign in Inspector or use Camera.main

    void Update()
    {
        // Get a position in front of the camera
        Vector3 forwardPosition = mainCamera.transform.position + mainCamera.transform.forward * 3f;

        // Keep Y = 0 in world space
        transform.position = new Vector3(forwardPosition.x, 0.5f, forwardPosition.z);

        transform.LookAt(mainCamera.transform.position);

    }
}