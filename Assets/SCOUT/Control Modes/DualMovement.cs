using UnityEngine;
using UnityEngine.UI;

public class DualMovement : MonoBehaviour
{
    [Header("References")]
    public GameObject cameraRig;
    public Transform headTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Movement Settings")]
    [Tooltip("Movement speed in meters per second.")]
    public float moveSpeed = 1.8f;

    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 90f;

    [Tooltip("Enable snap turn.")]
    public bool useSnapTurn = false;

    [Tooltip("Degrees per snap turn.")]
    public float snapAngle = 30f;

    [Tooltip("Joystick deadzone for snap turn.")]
    public float snapTurnDeadzone = 0.5f;

    [Tooltip("Cooldown time between snap turns.")]
    public float snapTurnCooldown = 0.4f;

    [Header("Orbit Settings")]
    [Tooltip("Toggle orbit mode on/off.")]
    public bool useOrbitMode = false;

    [Tooltip("Pivot point to orbit around.")]
    public Transform orbitPivot;

    [Header("UI Settings")]
    public Text leftJoystickLabel;
    public Text rightJoystickLabel;
    public Text modeLabel;

    // Internal state
    private float lastSnapTime = 0f;
    private float currentTurnVelocity = 0f;
    private float turnDamping = 5f;
    private float turnAcceleration = 120f;

    void Update()
    {
        // // Get left joystick input (movement)
        // Vector2 leftJoy = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // // Get right joystick input (rotation)
        // Vector2 rightJoy = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // // Movement - left joystick controls horizontal locomotion
        // if (leftJoy.magnitude > 0.1f)
        // {
        //     Vector3 forward = cameraRig.transform.forward;
        //     Vector3 right = cameraRig.transform.right;

        //     forward.y = 0;
        //     right.y = 0;

        //     Vector3 move = forward.normalized * leftJoy.y + right.normalized * leftJoy.x;
        //     cameraRig.transform.position += move * moveSpeed * Time.deltaTime;
        // }

        // // Rotation - right joystick controls rotation around orbitPivot or self
        // Vector3 rotationAxis = Vector3.up;
        // Vector3 rotationCenter = useOrbitMode && orbitPivot != null ? orbitPivot.position : cameraRig.transform.position;

        // // Snap turn logic
        // if (useSnapTurn)
        // {
        //     if (rightJoy.x < -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
        //     {
        //         cameraRig.transform.RotateAround(rotationCenter, rotationAxis, -snapAngle);
        //         lastSnapTime = Time.time;
        //         currentTurnVelocity = 0f;
        //     }
        //     else if (rightJoy.x > snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
        //     {
        //         cameraRig.transform.RotateAround(rotationCenter, rotationAxis, snapAngle);
        //         lastSnapTime = Time.time;
        //         currentTurnVelocity = 0f;
        //     }
        // }
        // else
        // {
        //     // Smooth rotation with inertia smoothing
        //     if (Mathf.Abs(rightJoy.x) > 0.1f)
        //     {
        //         float targetTurnSpeed = rotationSpeed * rightJoy.x;

        //         currentTurnVelocity = Mathf.MoveTowards(currentTurnVelocity, targetTurnSpeed, turnAcceleration * Time.deltaTime);
        //         currentTurnVelocity = Mathf.Lerp(currentTurnVelocity, 0f, turnDamping * Time.deltaTime);

        //         cameraRig.transform.RotateAround(rotationCenter, rotationAxis, currentTurnVelocity * Time.deltaTime);
        //     }
        //     else
        //     {
        //         // Decay rotation velocity when no input
        //         currentTurnVelocity = Mathf.Lerp(currentTurnVelocity, 0f, turnDamping * Time.deltaTime);
        //         if (Mathf.Abs(currentTurnVelocity) > 0.01f)
        //         {
        //             cameraRig.transform.RotateAround(rotationCenter, rotationAxis, currentTurnVelocity * Time.deltaTime);
        //         }
        //         else
        //         {
        //             currentTurnVelocity = 0f;
        //         }
        //     }
        // }

        // // Update UI labels if assigned
        // if (leftJoystickLabel != null)
        //     leftJoystickLabel.text = $"Left Joystick:\nMove ({leftJoy.x:F2}, {leftJoy.y:F2})";

        // if (rightJoystickLabel != null)
        //     rightJoystickLabel.text = $"Right Joystick:\nRotate ({rightJoy.x:F2}, {rightJoy.y:F2})";

        // if (modeLabel != null)
        // {
        //     string modeText = useOrbitMode ? "Orbit Mode: ON" : "Orbit Mode: OFF";
        //     modeText += $"\nSnap Turn: {(useSnapTurn ? "ON" : "OFF")}";
        //     modeText += $"\nRotation Speed: {rotationSpeed}°/sec";
        //     modeLabel.text = modeText;
        // }
    }
}
