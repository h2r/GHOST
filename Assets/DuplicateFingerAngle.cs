using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicateFingerAngle : MonoBehaviour
{
    public Transform realFinger;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Set this object's rotation to the real finger's rotation
        transform.localRotation = realFinger.localRotation;
    }
}
