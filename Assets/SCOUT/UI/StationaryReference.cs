using UnityEngine;

public class StationaryReference : MonoBehaviour
{
    public Transform centerEyeAnchor;
    private GameObject stationaryReference;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stationaryReference = new GameObject("StationaryReference");
        
        if (centerEyeAnchor != null) {
            stationaryReference.transform.SetParent(centerEyeAnchor.parent);
        } else {
            Debug.LogError("CenterEyeAnchor not assigned in StationaryReference.");
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (centerEyeAnchor != null) {
            stationaryReference.transform.position = centerEyeAnchor.position;
            stationaryReference.transform.rotation = centerEyeAnchor.rotation;
        }
    }

    public Transform GetReference()
    {
        return stationaryReference.transform;
    }
}
