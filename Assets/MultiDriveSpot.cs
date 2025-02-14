using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;

public class MultiDriveSpot : MonoBehaviour
{
    //public OVRInput.RawAxis2D MoveTranslation;
    //public OVRInput.RawAxis2D MoveRotation;
    //public OVRInput.RawButton rightPress;
    //public OVRInput.RawButton leftPress;

    //public RosSharp.RosBridgeClient.MoveSpot drive;

    //public bool defaultLow;

    //private float height;
    //private const float HEIGHT_INC = 0.005f;
    //private const float HEIGHT_MIN = -0.1f;
    //private const float HEIGHT_MAX = 0.3f;

    //void Start()
    //{
    //    if (defaultLow)
    //    {
    //        height = HEIGHT_MIN;
    //    }
    //    else
    //    {
    //        height = 0f;
    //    }
    //}

    //void Update()
    //{

    //    Vector2 leftMove;
    //    Vector2 rightMove;

    //    bool heightChanged = false;


    //    // Detect joystick press values
    //    if (OVRInput.Get(leftPress) && (height - HEIGHT_INC) > HEIGHT_MIN)
    //    {
    //        height -= HEIGHT_INC;
    //        heightChanged = true;
    //    }
    //    // No else so that if both are pressed, nothing happens
    //    if (OVRInput.Get(rightPress) && (height + HEIGHT_INC) < HEIGHT_MAX)
    //    {
    //        height += HEIGHT_INC;
    //        heightChanged = true;
    //    }


    //    // Read base movement values, adjust speeds
    //    rightMove = OVRInput.Get(MoveRotation) * 0.75f;
    //    leftMove = OVRInput.Get(MoveTranslation) * 0.5f;
    //    leftMove.x *= 0.5f;

    //    // Move the robot if any adjustments have been made
    //    if (rightMove.x != 0f || leftMove.magnitude != 0f || heightChanged)
    //    {
    //        // Set movement so only one direction is moved with the left stick at a time
    //        if (Mathf.Abs(leftMove.x) > Mathf.Abs(leftMove.y)) { leftMove.y = 0; }
    //        else if (Mathf.Abs(leftMove.y) > Mathf.Abs(leftMove.x)) { leftMove.x = 0; }

    //        drive.drive(leftMove, rightMove.x, height);

    //    }
    //}

    public OVRInput.RawAxis2D MoveJoystick;   // Left joystick for movement/rotation
    public OVRInput.RawButton T1;             // Left controller trigger button
    public OVRInput.RawButton JoystickPress;  // Clicking the joystick

    public RosSharp.RosBridgeClient.MoveSpot drive;

    public bool defaultLow;

    private float height;
    private const float HEIGHT_INC = 0.003f;
    private const float HEIGHT_MIN = -0.2f;
    private const float HEIGHT_MAX = 0.3f;

    void Start()
    {
        height = defaultLow ? HEIGHT_MIN : 0f;
    }

    void Update()
    {
        Vector2 moveInput = OVRInput.Get(MoveJoystick);
        bool isT1Held = OVRInput.Get(T1);
        bool isJoystickPressed = OVRInput.Get(JoystickPress);

        Vector2 translationMove = Vector2.zero;
        float rotationMove = 0f;
        bool heightChanged = false;

        // Move normally using joystick
        if (!isT1Held)
        {
            translationMove = moveInput * 0.8f; // Adjust speed
            translationMove.x *= 0.5f;  // Reduce sideways movement speed
        }
        // Rotate when T1 is held
        else
        {
            rotationMove = moveInput.x * 0.75f;  // Use joystick X-axis for rotation
        }

        // Joystick Press Actions (Adjust Height)
        if (isJoystickPressed)
        {
            if (!isT1Held && (height - HEIGHT_INC) > HEIGHT_MIN)
            {
                height -= HEIGHT_INC; // Lower Spot
                heightChanged = true;
            }
            else if (isT1Held && (height + HEIGHT_INC) < HEIGHT_MAX)
            {
                height += HEIGHT_INC; // Raise Spot
                heightChanged = true;
            }
        }

        // Ensure movement is only in one primary direction
        if (Mathf.Abs(translationMove.x) > Mathf.Abs(translationMove.y))
        {
            translationMove.y = 0;
        }
        else if (Mathf.Abs(translationMove.y) > Mathf.Abs(translationMove.x))
        {
            translationMove.x = 0;
        }

        // Send command to the robot if any movement or height change occurs
        if (translationMove.magnitude > 0f || rotationMove != 0f || heightChanged)
        {
            drive.drive(translationMove, rotationMove, height);
        }
    }

}
