using System;
using UnityEngine;

public class LocomotionJoystickMode : OneControllerMode
{
    [Header("View Settings")]
    public GameObject viewOptionsConfigurer;
    private PointCloudCycler pointCloudCycler;
    private PositionPresetCycler positionPresetCycler;

    [Header("References")]
    public GameObject cameraRig;
    public RigPositioner rigPositioner;
    public GameObject vignette;
    public Transform headTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Locomotion Settings")]
    public float moveSpeed = 2.0f;

    [Tooltip("Use head-relative direction for joystick locomotion.")]
    public bool useHeadRelativeMovement = true;

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
    private bool isAdjustingHeight = false;
    private float prevJoyX = 0f;
    private float lastSnapTime = 0f;
    private float currentTurnVelocity = 0f;

    private void Awake()
    {
        LastMoveDelta = Vector3.zero;
        IsJoystickInUse = false;

        pointCloudCycler = viewOptionsConfigurer.GetComponent<PointCloudCycler>();
        positionPresetCycler = viewOptionsConfigurer.GetComponent<PositionPresetCycler>();
    }

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        if (cameraRig == null) return;

        Vector3 positionBeforeUpdate = cameraRig.transform.position;

        if (vignette != null)
            vignette.SetActive(vignetteEnabled);

        Vector2 joystick = OVRInput.Get(model.joystick);
        IsJoystickInUse = joystick.magnitude > 0.1f; // Set the public property

        bool doRotate = OVRInput.Get(model.gripButton);
        bool doHeightAdjust = OVRInput.Get(model.indexButton);

        if (doHeightAdjust && !isAdjustingHeight)
        {
            initialY = rigPositioner.y;
            hasInitialY = true;
        }
        isAdjustingHeight = doHeightAdjust;

        Vector3 rotationAxis = transform.up;
        Vector3 rotationCenter = headTransform != null ? headTransform.position : rigPositioner.pos;

        // cycle point cloud / views
        if (OVRInput.GetDown(model.axButton))
            pointCloudCycler.CyclePointClouds();
        if (OVRInput.GetDown(model.byButton))
            positionPresetCycler.CyclePresets();

        if (doRotate)
        {
            if (Mathf.Abs(joystick.x) > 0.1)
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

            // if (Mathf.Abs(joystick.y) > 0.1)
            // {
            //     cameraRig.transform.RotateAround(rotationCenter, cameraRig.transform.right, -joystick.y * rotationSpeed * 0.5f * Time.deltaTime);
            // }

            rigPositioner.pos = cameraRig.transform.position;
            rigPositioner.rotation = cameraRig.transform.rotation;
        }
        else if (doHeightAdjust)
        {
            if (Mathf.Abs(joystick.y) > 0.1)
            {
                rigPositioner.pos += joystick.y * moveSpeed * Time.deltaTime * Vector3.up;
            }
        }
        else
        {
            Vector3 move = Vector3.zero;

            //horizontal movement/strafe 
            if (IsJoystickInUse)
            {
                if (useHeadRelativeMovement)
                {
                    Quaternion headYaw = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);

                    Vector3 forward = headYaw * Vector3.forward;
                    Vector3 right = headYaw * Vector3.right;

                    Vector3 horizontalMove = forward * joystick.y + right * joystick.x;
                    move += horizontalMove;
                }
                else
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
            }

            rigPositioner.pos += move * moveSpeed * Time.deltaTime;
        }

        prevJoyX = joystick.x;

        bool resetY = OVRInput.GetDown(model.axButton);

        if (resetY && hasInitialY)
        {
            Vector3 pos = rigPositioner.pos;
            pos.y = initialY;
            rigPositioner.pos = pos;
        }

        // Reset Y label
        model.axLabel = model.isLeft ? (hasInitialY ? "Reset Y (X)" : "") : (hasInitialY ? "Reset Y (A)" : "");

        if (doRotate)
            model.joystickLabel = "Rotate";
        else if (doHeightAdjust)
            model.joystickLabel = "Up/Down";
        else
            model.joystickLabel = "Fly";

        model.gripLabel = doRotate ? "" : "Hold: Rotate";
        model.indexLabel = doHeightAdjust ? "" : "Hold: Up/Down";

        // Calculate the final delta
        LastMoveDelta = cameraRig.transform.position - positionBeforeUpdate;
    }

    public override string GetName()
    {
        return "Fly";
    }

    public override int ModeIndex => 3;
    public override bool ControlsSpot => false;

    public override void AssignDefaultLabels(ControllerModel controller_model)
    {
        controller_model.axLabel = "Cycle PointClouds";
        controller_model.byLabel = "Cycle ViewPoints";
        controller_model.joystickLabel = "Fly";
        controller_model.indexLabel = "Hold: Rotate";
        controller_model.gripLabel = "Hold: Up/Down";
    }
}
