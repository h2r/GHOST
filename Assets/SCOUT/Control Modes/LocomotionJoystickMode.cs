using System;
using UnityEngine;

public class LocomotionJoystickMode : OneControllerMode
{
    [Header("References")]
    public GameObject cameraRig;
    public GameObject vignette;
    public Transform headTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Locomotion Settings")]
    public float moveSpeed = 2.0f;

    [Header("Rotation Settings")]
    [Tooltip("Degrees per second for smooth turn. Adjustable in inspector.")]
    public float rotationSpeed = 120f;  // default same as moveSpeed, but editable separately

    [Tooltip("Enable snap turn rotation instead of smooth rotation.")]
    public bool useSnapTurn = false;    // Disabled by default

    [Tooltip("Degrees per snap turn. 25 is comfortable for most users.")]
    public float snapAngle = 25f;

    [Tooltip("Minimum stick deflection to trigger snap turn.")]
    public float snapTurnDeadzone = 0.5f;

    [Tooltip("Minimum seconds between snap turns.")]
    public float snapTurnCooldown = 0.4f;

    [Tooltip("Enable rotational inertia smoothing on smooth turning.")]
    public bool useRotationalInertia = false;

    [Tooltip("Acceleration rate for rotational inertia (degrees/sec²).")]
    public float turnAcceleration = 120f;

    [Tooltip("Damping rate for rotational inertia.")]
    public float turnDamping = 5f;

    public bool vignetteEnabled = false;

    // Public properties to expose state
    public Vector3 LastMoveDelta { get; private set; }
    public bool IsJoystickInUse { get; private set; }

    // Internal state
    private float initialY;
    private bool hasInitialY = false;
    private bool isTriggerHeld = false;
    private float prevJoyX = 0f;
    private float lastSnapTime = 0f;
    private float currentTurnVelocity = 0f;

    private void Awake()
    {
        LastMoveDelta = Vector3.zero;
        IsJoystickInUse = false;
    }

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        if (cameraRig == null) return;

        Vector3 positionBeforeUpdate = cameraRig.transform.position;

        if (vignette != null)
            vignette.SetActive(vignetteEnabled);

        Vector2 joystick = OVRInput.Get(model.joystick);
        IsJoystickInUse = joystick.magnitude > 0.1f; // Set the public property

        bool trigger = OVRInput.Get(model.indexButton);
        bool grip = OVRInput.Get(model.gripButton);

        if (trigger && !isTriggerHeld)
        {
            initialY = cameraRig.transform.position.y;
            hasInitialY = true;
        }
        isTriggerHeld = trigger;

        Vector3 rotationAxis = Vector3.up;
        Vector3 rotationCenter = headTransform != null ? headTransform.position : cameraRig.transform.position;

        if (trigger)
        {
            if (useSnapTurn)
            {
                // Snap turn logic
                if (joystick.x < -snapTurnDeadzone && prevJoyX >= -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(rotationCenter, rotationAxis, -snapAngle);
                    lastSnapTime = Time.time;
                    currentTurnVelocity = 0f;
                }
                else if (joystick.x > snapTurnDeadzone && prevJoyX <= snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(rotationCenter, rotationAxis, snapAngle);
                    lastSnapTime = Time.time;
                    currentTurnVelocity = 0f;
                }
            }
            else
            {
                // Smooth turning with or without inertia
                if (Mathf.Abs(joystick.x) > 0.1f)
                {
                    if (useRotationalInertia)
                    {
                        float targetTurnSpeed = rotationSpeed * joystick.x;

                        currentTurnVelocity = Mathf.MoveTowards(currentTurnVelocity, targetTurnSpeed, turnAcceleration * Time.deltaTime);
                        currentTurnVelocity = Mathf.Lerp(currentTurnVelocity, 0f, turnDamping * Time.deltaTime);

                        cameraRig.transform.RotateAround(rotationCenter, rotationAxis, currentTurnVelocity * Time.deltaTime);
                    }
                    else
                    {
                        float angle = rotationSpeed * joystick.x * Time.deltaTime;
                        cameraRig.transform.RotateAround(rotationCenter, rotationAxis, angle);
                        currentTurnVelocity = 0f;
                    }
                }

                else
                {
                    if (useRotationalInertia && Mathf.Abs(currentTurnVelocity) > 0.01f)
                    {
                        currentTurnVelocity = Mathf.Lerp(currentTurnVelocity, 0f, turnDamping * Time.deltaTime);
                        cameraRig.transform.RotateAround(rotationCenter, rotationAxis, currentTurnVelocity * Time.deltaTime);
                    }
                    else
                    {
                        currentTurnVelocity = 0f;
                    }
                }
            }
        }
        else if (grip)
        {
            if (Mathf.Abs(joystick.y) > 0.1)
            {
                cameraRig.transform.position += joystick.y * moveSpeed * Time.deltaTime * Vector3.up;
            }
        }
        else
        {
            Vector3 move = Vector3.zero;

            //horizontal movement/strafe 
            if (IsJoystickInUse)
            {
                Quaternion rigRotation = cameraRig.transform.rotation;

                Vector3 forward = rigRotation * Vector3.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = rigRotation * Vector3.right;
                right.y = 0;
                right.Normalize();

                Vector3 horizontalMove = forward * joystick.y + right * joystick.x;
                move += horizontalMove;
            }

            cameraRig.transform.position += move * moveSpeed * Time.deltaTime;
        }

        prevJoyX = joystick.x;

        bool resetY = OVRInput.GetDown(model.axButton);

        if (resetY && hasInitialY)
        {
            Vector3 pos = cameraRig.transform.position;
            pos.y = initialY;
            cameraRig.transform.position = pos;
        }

        // Reset Y label
        model.axLabel = model.isLeft ? (hasInitialY ? "Reset Y (X)" : "") : (hasInitialY ? "Reset Y (A)" : "");

        if (grip)
            model.joystickLabel = "Up/Down";
        else if (trigger)
            model.joystickLabel = "Rotate";
        else
            model.joystickLabel = "Fly";

        model.indexLabel = trigger ? "" : "Hold: Rotate";
        model.gripLabel = grip ? "" : "Hold: Up/Down";

        // Calculate the final delta
        LastMoveDelta = cameraRig.transform.position - positionBeforeUpdate;
    }

    public override string GetName()
    {
        return "Fly";
    }

    public override int ModeIndex => 3;
    public override bool ControlsSpot => false;

    public override void AssignDefaultLabels(ControllerModel exampleModel)
    {
        exampleModel.axLabel = "Reset Y";
        exampleModel.joystickLabel = "Fly";
        exampleModel.indexLabel = "Hold: Rotate";
        exampleModel.gripLabel = "Hold: Up/Down";
    }
}
