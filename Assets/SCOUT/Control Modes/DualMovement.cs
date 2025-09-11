using UnityEngine;

public class DualMovement : TwoControllerMode
{
    [Header("References")]
    public GameObject cameraRig;
    public Transform headTransform;
    public RigPositioner rigPositioner;

    [Header("Movement Settings")]
    [Tooltip("Movement speed in meters per second.")]
    public float moveSpeed = 1.8f;

    [Tooltip("Use head-relative direction for joystick locomotion.")]
    public bool useHeadRelativeMovement = true;

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

    // Internal state
    private float lastSnapTime = 0f;
    private float currentTurnVelocity = 0f;
    private float turnDamping = 5f;
    private float turnAcceleration = 120f;

    public override void ControlUpdate(SpotMode spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        leftModel.color = Color.white;
        rightModel.color = Color.white;

        if (cameraRig == null || rigPositioner == null) return;

        // Get inputs
        Vector2 leftJoy = OVRInput.Get(leftModel.joystick);
        Vector2 rightJoy = OVRInput.Get(rightModel.joystick);
        bool rightGrip = OVRInput.Get(rightModel.gripButton);

        // --- Left Joystick: Fly Movement ---
        if (leftJoy.magnitude > 0.1f)
        {
            Vector3 move = Vector3.zero;
            if (useHeadRelativeMovement)
            {
                Quaternion headYaw = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);
                Vector3 forward = headYaw * Vector3.forward;
                Vector3 right = headYaw * Vector3.right;
                move = (forward * leftJoy.y + right * leftJoy.x);
            }
            else
            {
                Vector3 forward = cameraRig.transform.forward;
                Vector3 right = cameraRig.transform.right;
                forward.y = 0;
                right.y = 0;
                move = (forward.normalized * leftJoy.y + right.normalized * leftJoy.x);
            }
            rigPositioner.pos += move * moveSpeed * Time.deltaTime;
        }

        // --- Right Joystick: Rotation or Up/Down ---
        if (rightGrip)
        {
            // --- Vertical Movement ---
            if (Mathf.Abs(rightJoy.y) > 0.1f)
            {
                rigPositioner.pos += rightJoy.y * moveSpeed * Time.deltaTime * Vector3.up;
            }
            rightModel.joystickLabel = "Up/Down";
        }
        else
        {
            // --- Rotation ---
            Vector3 rotationAxis = Vector3.up;
            Vector3 rotationCenter = useOrbitMode && orbitPivot != null ? orbitPivot.position : rigPositioner.pos;

            if (useSnapTurn)
            {
                if (rightJoy.x < -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(rotationCenter, rotationAxis, -snapAngle);
                    lastSnapTime = Time.time;
                }
                else if (rightJoy.x > snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(rotationCenter, rotationAxis, snapAngle);
                    lastSnapTime = Time.time;
                }
            }
            else
            {
                if (Mathf.Abs(rightJoy.x) > 0.1f)
                {
                    float targetTurnSpeed = rotationSpeed * rightJoy.x;
                    currentTurnVelocity = Mathf.MoveTowards(currentTurnVelocity, targetTurnSpeed, turnAcceleration * Time.deltaTime);
                    cameraRig.transform.RotateAround(rotationCenter, rotationAxis, currentTurnVelocity * Time.deltaTime);
                }
                else
                {
                    currentTurnVelocity = Mathf.Lerp(currentTurnVelocity, 0f, turnDamping * Time.deltaTime);
                    if (Mathf.Abs(currentTurnVelocity) > 0.01f)
                    {
                        cameraRig.transform.RotateAround(rotationCenter, rotationAxis, currentTurnVelocity * Time.deltaTime);
                    }
                    else
                    {
                        currentTurnVelocity = 0f;
                    }
                }
            }
            rightModel.joystickLabel = "Rotate";
        }

        // Set labels
        leftModel.joystickLabel = "Fly";
        rightModel.gripLabel = rightGrip ? "" : "Hold: Up/Down";
    }

    public override void AssignDefaultLabels(ControllerModel leftExampleModel, ControllerModel rightExampleModel)
    {
        leftExampleModel.joystickLabel = "Fly";
        rightExampleModel.joystickLabel = "Rotate";
        rightExampleModel.gripLabel = "Hold: Up/Down";
    }

    public override string GetName()
    {
        return "Fly";
    }

    public override int ModeIndex => 6;
    public override bool ControlsSpot => false;
}