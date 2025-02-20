using System;  // for Math.Tan, Math.Sign, etc.
using UnityEngine;
using RosSharp.RosBridgeClient; // if needed for ROS functionality

public class JoyStickArm : MonoBehaviour
{

    public OVRInput.RawButton RT1; // right trigger button
    public OVRInput.RawButton LT1; // left trigger button
    public OVRInput.RawAxis2D LAx; // left joystick
    public OVRInput.RawAxis2D RAx; // right joystick

    public Transform spotBody;
    public SetGripper gripper;                // Your script controlling the gripper
    public VRGeneralControls generalControls; // Your script for general VR controls

    public JoyArmPublisher joyArmPublisher;

    // Materials for switching the robot from translucent to opaque
    public Material translucentSpotMaterial;
    public Material opaqueSpotMaterial;
    public Material opaqueSpotArmMaterial;
    public Material translucentSpotArmMaterial;

    // Variables to track arm and gripper motion
    private double armFrontBack;
    private double armLeftRight;
    private double armUpDown;
    private float gripperRotate;
    private float gripperSwing;
    private float gripperNod;

    private void OnEnable()
    {
        // Make the robot translucent when this script is enabled
        setSpotVisible(spotBody, false);

        armFrontBack = 0.0;
        armLeftRight = 0.0;
        armUpDown = 0.0;
        gripperRotate = 0.0f;
        gripperSwing = 0.0f;
        gripperNod = 0.0f;
    }

    void Update()
    {
        Vector3 locationChange;
        Quaternion rotationChange = new Quaternion(0f, 0f, 0f, 0f);

        // Get joystick input from OVRInput
        Vector2 laxMove = OVRInput.Get(LAx); // left joystick
        Vector2 raxMove = OVRInput.Get(RAx); // right joystick

        // Check if there is any significant movement on either joystick
        if (Math.Abs(laxMove.x) > 0.001f || Math.Abs(laxMove.y) > 0.001f ||
            Math.Abs(raxMove.x) > 0.001f || Math.Abs(raxMove.y) > 0.001f)
        {
            // If the LT1 (left trigger) is pressed
            if (OVRInput.Get(LT1))
            {
                // Control the "gripper rotate" using the sign of laxMove.y
                gripperRotate = Math.Sign(laxMove.y) * (float)Math.PI / 8f;


                rotationChange.x = gripperRotate;
                joyArmPublisher.setCoordinate(0f, 0f, 0f, rotationChange);
            }
            // If the RT1 (right trigger) is pressed
            else if (OVRInput.Get(RT1))
            {
                // Control the "gripper nod" using the sign of raxMove.y
                // and the "gripper swing" using the sign of laxMove.x
                gripperNod = Math.Sign(raxMove.y) * (float)Math.PI / 12f;
                gripperSwing = Math.Sign(laxMove.x) * (float)Math.PI / 12f;

                rotationChange.y = -gripperNod;
                rotationChange.z = -gripperSwing;
                joyArmPublisher.setCoordinate(0f, 0f, 0f, rotationChange);
            }
            else
            {
                // Use the joysticks for arm movement if neither LT1 nor RT1 is pressed
                armFrontBack = laxMove.y;
                armLeftRight = laxMove.x;

                // Non-linear mapping with a tan function (example only)
                armFrontBack = 0.01 * Math.Tan(1.55 * armFrontBack);
                armLeftRight = -0.01 * Math.Tan(1.55 * armLeftRight);

                // Right joystick moves arm up/down
                armUpDown = raxMove.y;
                armUpDown = 0.01 * Math.Tan(1.55 * armUpDown);

                joyArmPublisher.setCoordinate(
                    (float)armFrontBack,
                    (float)armLeftRight,
                    (float)armUpDown,
                    rotationChange
                );
            }
        }

        // If you want to control the gripper open/close using LT1 + left joystick Y:
        /*
        if (OVRInput.Get(LT1))
        {
            Vector2 leftMove = OVRInput.Get(LAx);
            if (leftMove.y < 0)
            {
                if (generalControls.gripperPercentage > 0)
                {
                    generalControls.gripperPercentage -= 0.25f;
                    gripper.setGripperPercentage(generalControls.gripperPercentage);
                    generalControls.gripperOpen = false;
                }
            }
            if (leftMove.y > 0)
            {
                if (generalControls.gripperPercentage < 100.0f)
                {
                    generalControls.gripperPercentage += 0.25f;
                    gripper.setGripperPercentage(generalControls.gripperPercentage);
                    generalControls.gripperOpen = true;
                }
            }
        }
        */
    }

    /// <summary>
    /// Recursive function that sets the visuals of "unnamed" objects under 'parent'
    /// to either translucent or opaque. It will skip children under "arm0.link_wr1" 
    /// and "dummy_arm0.link_wr1".
    /// </summary>
    private void setSpotVisible(Transform parent, bool visible)
    {
        foreach (Transform child in parent)
        {
            // Check if parent is named "unnamed"
            if (parent.gameObject.name == "unnamed")
            {
                MeshRenderer rend = child.gameObject.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    if (!visible)
                    {
                        if (child.gameObject.name.Contains("arm"))
                            rend.material = translucentSpotArmMaterial;
                        else
                            rend.material = translucentSpotMaterial;
                    }
                    else
                    {
                        if (child.gameObject.name.Contains("arm"))
                            rend.material = opaqueSpotArmMaterial;
                        else
                            rend.material = opaqueSpotMaterial;
                    }
                }
            }
            // Skip children of these specific names
            else if (child.gameObject.name == "arm0.link_wr1" ||
                     child.gameObject.name == "dummy_arm0.link_wr1")
            {
                return;
            }
            else
            {
                // Recursively continue for other children
                setSpotVisible(child, visible);
            }
        }
    }

    private void OnDisable()
    {
        // When the script is disabled, make the robot opaque again
        setSpotVisible(spotBody, true);
    }
}
