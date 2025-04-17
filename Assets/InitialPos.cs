using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitialPos : MonoBehaviour
{
    public Transform armBaseLocation;

    // Start is called before the first frame update
    void Start()
    {
        SetInitialPosition();
    }

    // Move the camera right above 
    // will try on 4/17 for fixing camera
    void SetInitialPosition()
    {
        transform.position = armBaseLocation.position + new Vector3(0f, -1f, 0.4f);
        transform.rotation = armBaseLocation.rotation;
    }

    // Update is called once per frame
    void Update()
    {        
    }
}
