using System;
using UnityEngine;

// Two controller mode
public class DriveAndRotateMode : TwoControllerMode
{
    public GameObject pivotVisualizer;
    private bool isMultiSpot = false;

    [Header("Locomotion")]
    public GameObject cameraRig;
    public RigPositioner rigPositioner;
    public Transform headTransform;
    public float moveSpeed = 2.0f;
    public bool useHeadRelativeMovement = true;

    [Header("Rotation Settings")]
    public float rotationSpeed = 120f;
    public bool useSnapTurn = false;
    public float snapAngle = 25f;
    public float snapTurnDeadzone = 0.5f;
    public float snapTurnCooldown = 0.4f;

    private float prevJoyX = 0f;
    private float lastSnapTime = 0f;

    public override void ControlUpdate(SpotController spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        // if in multi-spot mode, show the pivot visualizer to help with move orientation for the whole robot group
        if (spot is MultiSpotController)
            isMultiSpot = true;
        else
            isMultiSpot = false;
        if (pivotVisualizer != null)
            pivotVisualizer.SetActive(isMultiSpot);

        bool isLocomotion = OVRInput.Get(leftModel.gripButton);

        if (isLocomotion)
        {
            // === VR Locomotion Mode ===
            leftModel.joystickLabel = "Rotate View";
            rightModel.joystickLabel = "Fly";
            leftModel.gripLabel = "Controlling View";
            rightModel.gripLabel = "";
            leftModel.axLabel = "";
            leftModel.byLabel = "";
            rightModel.axLabel = "";
            rightModel.byLabel = "";

            if (cameraRig == null || rigPositioner == null || headTransform == null)
            {
                leftModel.joystickLabel = "Loco Not Setup";
                rightModel.joystickLabel = "Loco Not Setup";
                return;
            }

            // --- Right Stick: Fly ---
            var flyJoystick = OVRInput.Get(rightModel.joystick);
            if (flyJoystick.magnitude > 0.1f)
            {
                Vector3 move;
                if (useHeadRelativeMovement)
                {
                    Quaternion headYaw = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);
                    Vector3 forward = headYaw * Vector3.forward;
                    Vector3 right = headYaw * Vector3.right;
                    move = (forward * flyJoystick.y + right * flyJoystick.x);
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
                    move = (forward * flyJoystick.y + right * flyJoystick.x);
                }
                rigPositioner.pos += move * moveSpeed * Time.deltaTime;
            }

            // --- Left Stick: Rotate ---
            var rotateJoystick = OVRInput.Get(leftModel.joystick);
            if (Mathf.Abs(rotateJoystick.x) > 0.1f)
            {
                Vector3 rotationAxis = Vector3.up;
                Vector3 rotationCenter = headTransform.position;

                if (useSnapTurn)
                {
                    if (rotateJoystick.x < -snapTurnDeadzone && prevJoyX >= -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                    {
                        cameraRig.transform.RotateAround(rotationCenter, rotationAxis, -snapAngle);
                        lastSnapTime = Time.time;
                    }
                    else if (rotateJoystick.x > snapTurnDeadzone && prevJoyX <= snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                    {
                        cameraRig.transform.RotateAround(rotationCenter, rotationAxis, snapAngle);
                        lastSnapTime = Time.time;
                    }
                }
                else
                {
                    float angle = rotationSpeed * rotateJoystick.x * Time.deltaTime;
                    cameraRig.transform.RotateAround(rotationCenter, rotationAxis, angle);
                }
                rigPositioner.pos = cameraRig.transform.position;
                rigPositioner.rotation = cameraRig.transform.rotation;
            }
            prevJoyX = rotateJoystick.x;
        }
        else
        {
            // === Robot Control Mode ===
            leftModel.joystickLabel = "Drive";
            rightModel.joystickLabel = "Rotate";
            leftModel.gripLabel = "Hold: Control View";
            rightModel.gripLabel = "";
            rightModel.axLabel = "Lower Body";
            rightModel.byLabel = "Raise Body";
            leftModel.axLabel = "";
            leftModel.byLabel = "";

            var leftJoystick = OVRInput.Get(leftModel.joystick);
            var rightJoystick = OVRInput.Get(rightModel.joystick);

            // Left joystick drives robot
            if (leftJoystick.magnitude > 0.1)
                spot.Drive(leftJoystick * 0.5f);

            // Right joystick rotates robot
            if (Mathf.Abs(rightJoystick.x) > 0.1)
                spot.Rotate(rightJoystick.x * 0.5f);

            // Right buttons adjust height
            if (OVRInput.Get(rightModel.byButton))
                spot.AdjustHeight(0.02f);

            if (OVRInput.Get(rightModel.axButton))
                spot.AdjustHeight(-0.02f);
        }
    }

    public override string GetName()
    {
        return "Drive & Rotate";
    }

    public override void OnModeExit()
    {
        if (pivotVisualizer != null)
            pivotVisualizer.SetActive(false);
    }

    public override int ModeIndex => 4;
    public override bool ControlsSpot => true;

    public override void AssignDefaultLabels(ControllerModel leftExampleModel, ControllerModel rightExampleModel)
    {
        leftExampleModel.joystickLabel = "Drive";
        rightExampleModel.joystickLabel = "Rotate";
        leftExampleModel.gripLabel = "Hold: Control View";
        rightExampleModel.axLabel = "Lower Body";
        rightExampleModel.byLabel = "Raise Body";
    }
}
