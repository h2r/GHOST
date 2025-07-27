using System;
using UnityEngine;

public enum RotationReference
{
    World,      // Rotates around the global Y axis (tank-style)
    Head,       // Rotates around the up direction of the head/camera
    Controller  // Rotates around the up direction of the active controller
}

// One controller mode
public class LocomotionJoystickMode : NewControlMode
{
    [Header("References")]
    public GameObject cameraRig;
    public GameObject vignette;
    public Transform headTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Locomotion Settings")]
    [Tooltip("Meters per second. 1.8 is a comfortable walk speed.")]
    public float moveSpeed = 1.8f;
    public bool useSnapTurn = true;

    [Header("Flying Settings")]
    [Tooltip("Meters per second for vertical flying.")]
    public float flySpeed = 1.4f;

    [Header("Rotation Settings")]
    [Tooltip("Degrees per snap turn. 25 is comfortable for most users.")]
    public float snapAngle = 25f;
    [Tooltip("Degrees per second for smooth turn. 30 is comfortable.")]
    public float smoothTurnSpeed = 30f;
    [Tooltip("Minimum stick deflection to trigger snap turn.")]
    public float snapTurnDeadzone = 0.5f;
    [Tooltip("Minimum seconds between snap turns.")]
    public float snapTurnCooldown = 0.4f;
    public bool vignetteEnabled = false;

    [Header("Rotation Reference")]
    public RotationReference rotationReference = RotationReference.World;

    // [[ Internal state ]]
    private float initialY;
    private bool hasInitialY = false;
    private bool isTriggerHeld = false;
    private float prevJoyX = 0f;
    private float lastSnapTime = 0f;

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

        // [[ Hand grip for flying ]]
        bool grip = model.isLeft
            ? OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > 0.5f
            : OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0.5f;

        if (trigger && !isTriggerHeld)
        {
            initialY = cameraRig.transform.position.y;
            hasInitialY = true;
        }
        isTriggerHeld = trigger;

        // [[ Rotation ]]
        Vector3 up;
        switch (rotationReference)
        {
            case RotationReference.Head:
                up = headTransform != null ? headTransform.up : Vector3.up;
                break;
            case RotationReference.Controller:
                Transform controllerTransform = model.isLeft ? leftControllerTransform : rightControllerTransform;
                up = controllerTransform != null ? controllerTransform.up : Vector3.up;
                break;
            default:
                up = Vector3.up;
                break;
        }

        // --- Movement/Rotation/Fly Logic ---
        if (grip)
        {
            // [[ Fly ]]
            if (Mathf.Abs(joystick.y) > 0.1f)
            {
                Vector3 pos = cameraRig.transform.position;
                pos.y += joystick.y * flySpeed * Time.deltaTime;
                cameraRig.transform.position = pos;
            }
        }
        else if (trigger)
        {
            // [[ Rotation ]]
            if (useSnapTurn)
            {
                if (joystick.x < -snapTurnDeadzone && prevJoyX >= -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(cameraRig.transform.position, up, -snapAngle);
                    lastSnapTime = Time.time;
                }
                else if (joystick.x > snapTurnDeadzone && prevJoyX <= snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(cameraRig.transform.position, up, snapAngle);
                    lastSnapTime = Time.time;
                }
            }
            else
            {
                if (Mathf.Abs(joystick.x) > 0.1f)
                {
                    float angle = smoothTurnSpeed * joystick.x * Time.deltaTime;
                    cameraRig.transform.RotateAround(cameraRig.transform.position, up, angle);
                }
            }
        }
        else
        {
            // [[ Locomotion ]]
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

        // [[ Labels ]]
        // A/X, B/Y, start/menu, thumbstick, trigger, gripper
        string[] labels = new string[6];

        // Reset Y label
        labels[0] = model.isLeft ? (hasInitialY ? "Reset Y (X)" : "") : (hasInitialY ? "Reset Y (A)" : "");
        labels[1] = "";
        labels[2] = "";

        // Thumbstick label
        if (grip)
            labels[3] = "Fly";
        else if (trigger)
            labels[3] = "Rotate";
        else
            labels[3] = "Locomote";

        // Trigger label
        labels[4] = trigger ? "" : "Hold: Rotate";

        // Gripper label
        labels[5] = grip ? "" : "Hold: Fly";

        model.SetLabels(labels);
    }

    public override string GetName()
    {
        return "Locomotion (Joystick)";
    }
}