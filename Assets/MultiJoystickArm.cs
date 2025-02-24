using System;  // for Math.Tan, Math.Sign, etc.
using UnityEngine;
using RosSharp.RosBridgeClient; // if needed for ROS functionality
using TMPro;

public class MultiJoystickArm : MonoBehaviour
{
    public OVRInput.RawButton T1;               // index trigger (button, not analog)
    public OVRInput.RawAxis2D Ax;               // thumbstick
    public OVRInput.RawButton ThumbstickPress;  // thumbstick press

    public JoyArmPublisher joyArmPublisher;
    public TextMeshProUGUI curModeTextMesh;

    // Operation modes: 0 = Gripper Translation, 1 = Gripper rotation, 2 = Gripper nod/swing
    private int currentMode = 0;
    private readonly string[] modeTexts = { "Gripper Translation", "Gripper Nod", "Gripper Rotate" };

    // Speed/angle constants
    private const float ARM_SPEED = 0.01f;
    private const float ARM_UP_SPEED = 0.1f;
    private const float GRIPPER_ROTATE_ANGLE = Mathf.PI / 8f;   // ~22.5 degrees
    private const float GRIPPER_SWING_ANGLE = Mathf.PI / 12f;   // ~15 degrees

    // Double-click detection for LT1
    private float lastLT1PressTime = 0f;
    private const float doubleClickInterval = 0.5f; // Adjust as needed

    void OnEnable()
    {
        if (curModeTextMesh != null)
        {
            curModeTextMesh.text = modeTexts[currentMode];
        }
    }

    void Update()
    {
        Quaternion rotationChange = Quaternion.identity;

        // Get thumbstick and button inputs
        Vector2 laxMove = OVRInput.Get(Ax);


        // This returns true while LT1 is held down (every frame until released)
        bool isLT1Pressed = OVRInput.Get(T1);

        // ---------------------------------------------------
        // Double-Click Detection on LT1 to switch modes
        // ---------------------------------------------------
        if (OVRInput.GetDown(T1))
        {
            float timeSinceLastPress = Time.time - lastLT1PressTime;
            if (timeSinceLastPress <= doubleClickInterval)
            {
                // Double-click detected => switch mode
                currentMode = (currentMode + 1) % 3;
                curModeTextMesh.text = modeTexts[currentMode];
                Debug.Log("Mode switched (LT1 double-click) -> " + currentMode);
            }
            // Update the timestamp
            lastLT1PressTime = Time.time;
        }

        // Initialize movement/rotation variables
        float armUpDown = 0f;
        float armFrontBack = 0f;
        float armLeftRight = 0f;
        float gripperRotate = 0f;
        float gripperSwing = 0f;
        float gripperNod = 0f;

        // ---------------------------------------------------
        // Example: Press LT1 + hold thumbstick press => move arm up
        // Here we use Get(...) rather than GetDown(...) so it's true while held
        // ---------------------------------------------------
        if (isLT1Pressed && OVRInput.Get(ThumbstickPress))
        {
            armUpDown = ARM_UP_SPEED;  // Move arm up
        }
        // If the thumbstick is pressed without LT1, move arm down
        else if (!isLT1Pressed && OVRInput.Get(ThumbstickPress))
        {
            armUpDown = -ARM_UP_SPEED; // Move arm down
        }
        else
        {
            // Otherwise, handle arm/gripper movement based on the current mode
            switch (currentMode)
            {
                case 0:
                    // Mode 0: Arm movement front-back / left-right
                    armFrontBack = ARM_SPEED * (float)Math.Tan(1.55 * laxMove.y);
                    armLeftRight = -ARM_SPEED * (float)Math.Tan(1.55 * laxMove.x);
                    break;

                case 1:
                    // Mode 1: Gripper nod (up/down) and swing (left/right)
                    // Not using swing for now, found it useless

                    gripperNod = System.MathF.Sign(laxMove.y) * GRIPPER_SWING_ANGLE;
                    rotationChange.y = -gripperNod;

                    //gripperSwing = System.MathF.Sign(laxMove.x) * GRIPPER_SWING_ANGLE;
                    //rotationChange.z = -gripperSwing;

                    break;


                case 2:
                    // Mode 2: Gripper rotation
                    // Reason for using System.MathF here is because the Unity's Mathf.sign(0) will return 1
                    gripperRotate = System.MathF.Sign(laxMove.y) * GRIPPER_ROTATE_ANGLE;
                    rotationChange.x = gripperRotate;

                    break;
            }
        }

        // Publish if there is any non-zero movement/rotation
        if (Mathf.Abs(armFrontBack) > 0.0001f ||
            Mathf.Abs(armLeftRight) > 0.0001f ||
            Mathf.Abs(armUpDown) > 0.0001f ||
            Mathf.Abs(gripperRotate) > 0.0001f ||
            Mathf.Abs(gripperSwing) > 0.0001f ||
            Mathf.Abs(gripperNod) > 0.0001f)
        {
            Debug.Log($"Publish: Mode={currentMode}, armFB={armFrontBack}, " +
                      $"armLR={armLeftRight}, armUD={armUpDown}");
            joyArmPublisher.setCoordinate(
                armFrontBack,
                armLeftRight,
                armUpDown,
                rotationChange
            );
        }
    }
}
