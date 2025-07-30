using System;
using UnityEngine;

public class LocomotionJoystickMode : NewControlMode
{
    [Header("References")]
    public GameObject cameraRig;
    public GameObject vignette;
    public Transform headTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Locomotion Settings")]
    public float moveSpeed = 2.0f;

    [Header("Flying Settings")]
    public float flySpeed = 1.4f;

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

    // Internal state
    private float initialY;
    private bool hasInitialY = false;
    private bool isTriggerHeld = false;
    private float prevJoyX = 0f;
    private float lastSnapTime = 0f;
    private float currentTurnVelocity = 0f;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        if (vignette != null)
            vignette.SetActive(vignetteEnabled);

        Vector2 joystick = model.isLeft
            ? OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)
            : OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        bool trigger = model.isLeft
            ? OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.5f
            : OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.5f;

        bool grip = model.isLeft
            ? OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > 0.5f
            : OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0.5f;

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
                    // Decay inertia velocity if no input
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
            if (Mathf.Abs(joystick.y) > 0.1f)
            {
                Vector3 pos = cameraRig.transform.position;
                pos.y += joystick.y * flySpeed * Time.deltaTime;
                cameraRig.transform.position = pos;
            }
        }
        else
        {
            if (joystick.magnitude > 0.1f)
            {
                Vector3 move = cameraRig.transform.forward * joystick.y + cameraRig.transform.right * joystick.x;
                move.y = 0;
                cameraRig.transform.position += move * moveSpeed * Time.deltaTime;
            }
        }

        prevJoyX = joystick.x;

        bool resetY = model.isLeft
            ? OVRInput.GetDown(OVRInput.Button.Three)
            : OVRInput.GetDown(OVRInput.Button.One);

        if (resetY && hasInitialY)
        {
            Vector3 pos = cameraRig.transform.position;
            pos.y = initialY;
            cameraRig.transform.position = pos;
        }

        string[] labels = new string[6];
        labels[0] = model.isLeft ? (hasInitialY ? "Reset Y (X)" : "") : (hasInitialY ? "Reset Y (A)" : "");
        labels[1] = "";
        labels[2] = "";
        labels[3] = grip ? "Fly" : (trigger ? "Rotate" : "Locomote");
        labels[4] = trigger ? "" : "Hold: Rotate";
        labels[5] = grip ? "" : "Hold: Fly";

        model.SetLabels(labels);
    }

    public override string GetName()
    {
        return "Locomotion (Joystick)";
    }
}
