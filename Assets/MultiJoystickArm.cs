using System;  // for Math.Tan, Math.Sign, etc.
using UnityEngine;
using RosSharp.RosBridgeClient; // if needed for ROS functionality

public class MultiJoystickArm : MonoBehaviour
{
    public OVRInput.RawButton LT1; // left index trigger button
    public OVRInput.RawButton LHandTrigger; // left hand trigger button
    public OVRInput.RawAxis2D LAx; // left joystick
    public OVRInput.RawButton LThumbstickPress; // left joystick press

    public JoyArmPublisher joyArmPublisher;

    // Variables to track arm and gripper motion
    private double armFrontBack;
    private double armLeftRight;
    private double armUpDown;
    private float gripperRotate;
    private float gripperSwing;
    private float gripperNod;

    private void OnEnable()
    {
        // Initialize movement values
        armFrontBack = 0.0;
        armLeftRight = 0.0;
        armUpDown = 0.0;
        gripperRotate = 0.0f;
        gripperSwing = 0.0f;
        gripperNod = 0.0f;
    }

    void Update()
    {
        Quaternion rotationChange = Quaternion.identity;

        // Get joystick input from OVRInput
        Vector2 laxMove = OVRInput.Get(LAx); // left joystick movement
        bool isJoystickPressed = OVRInput.Get(LThumbstickPress); // check if joystick is pressed

        // Track button states separately for robustness
        bool isLT1Pressed = OVRInput.Get(LT1);
        bool isLHandPressed = OVRInput.Get(LHandTrigger);

        // Reset movement variables
        armUpDown = 0.0;
        armFrontBack = 0.0;
        armLeftRight = 0.0;
        gripperRotate = 0.0f;
        gripperSwing = 0.0f;
        gripperNod = 0.0f;

        // PRIORITY ORDER: (1) Move Up, (2) Gripper Rotation, (3) Gripper Nod/Swing, (4) Arm Movement

        // 1. Move Up when LT1 + LHandTrigger + Joystick Press are all held
        if (isLT1Pressed && isLHandPressed && isJoystickPressed)
        {
            armUpDown = 0.1;  // Move up
        }
        // 2. Gripper Rotation (LT1 Held)
        else if (isLT1Pressed)
        {
            gripperRotate = Math.Sign(laxMove.y) * (float)Math.PI / 8f;
            rotationChange.x = gripperRotate;
        }
        // 3. Gripper Nod/Swing (LHandTrigger Held)
        else if (isLHandPressed)
        {
            gripperNod = Math.Sign(laxMove.y) * (float)Math.PI / 12f;
            gripperSwing = Math.Sign(laxMove.x) * (float)Math.PI / 12f;

            rotationChange.y = -gripperNod;
            rotationChange.z = -gripperSwing;
        }
        // 4. Arm Movement (Only Joystick Movement)
        else
        {
            armFrontBack = 0.01 * Math.Tan(1.55 * laxMove.y);
            armLeftRight = -0.01 * Math.Tan(1.55 * laxMove.x);

            // Move Down when Joystick Press is detected (but without LT1 + LHandTrigger)
            if (isJoystickPressed)
            {
                armUpDown = -0.1;
            }
        }

        //  Publish Movement & Rotation Only When There's a Change
        if (armFrontBack != 0 || armLeftRight != 0 || armUpDown != 0 ||
            gripperRotate != 0 || gripperSwing != 0 || gripperNod != 0)
        {
            Debug.Log("pushing joystick mode arm move");
            joyArmPublisher.setCoordinate(
                (float)armFrontBack,
                (float)armLeftRight,
                (float)armUpDown,
                rotationChange
            );
        }
    }
}
