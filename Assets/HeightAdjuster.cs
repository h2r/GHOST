using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class HeightAdjuster : MonoBehaviour
{
    public OVRInput.RawButton goHigher;
    public OVRInput.RawButton goLower;
    public OVRInput.RawAxis2D LAx;
    public OVRInput.RawAxis2D RAx;
    public OVRInput.RawButton LT1;
    public Transform cameraOffset;
    public Transform mainCamera;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 leftMove;
        Vector2 rightMove;
        Quaternion relativeRot;
        bool low;
        bool high;


        /* Set the camera higher or lower */
        low = OVRInput.Get(goLower);
        high = OVRInput.Get(goHigher);

        /* Go lower */
        if (low && !high)
        {
            cameraOffset.position = new Vector3(cameraOffset.position.x, cameraOffset.position.y - speed, cameraOffset.position.z);
        }
        /* Go higher */
        else if(!low && high)
        {
            cameraOffset.position = new Vector3(cameraOffset.position.x, cameraOffset.position.y + speed, cameraOffset.position.z);
        }

        if (!OVRInput.Get(LT1))
        {
            /* Move camera position around according to left stick */
            leftMove = OVRInput.Get(LAx) / 50f;
            relativeRot = Quaternion.Euler(0f, mainCamera.rotation.eulerAngles.y, 0f);// cameraTransform.rotation;
            cameraOffset.position += relativeRot * new Vector3(leftMove.x, 0f, leftMove.y);

            /* Adjust camera rotation according to right stick*/
            rightMove = OVRInput.Get(RAx);
            if (rightMove.magnitude > 0f)
            {
                /* Only change one axis at a time */
                if (Math.Abs(rightMove.x) > Math.Abs(rightMove.y))
                {
                    /* Rotate left/right relative to world space */
                    cameraOffset.Rotate(new Vector3(0f, rightMove.x, 0f), Space.World);
                }
                else
                {
                    /* Rotate up/down relative to world space */
                    /* Disabled for now */
                    // cameraTransform.Rotate(new Vector3(rightMove.y * 0.5f, 0f, 0f), Space.World);
                }
                /* Don't allow z rotation to change */
                cameraOffset.rotation = Quaternion.Euler(new Vector3(cameraOffset.rotation.eulerAngles.x, cameraOffset.rotation.eulerAngles.y, 0f));
            }
        }
    }
}
