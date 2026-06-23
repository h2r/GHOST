using System;
using UnityEngine;

public class DriveAndArm : OneControllerMode
{
    [Header("View Settings")]
    public GameObject viewOptionsConfigurer;
    private PointCloudCycler pointCloudCycler;
    private PositionPresetCycler positionPresetCycler;
    private float indexDownTimeSeconds;

    public enum ArmControlMode
    {
        AbsolutePos,
        AbsoluteController,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.AbsolutePos;

    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    private void Awake()
    {
        pointCloudCycler = viewOptionsConfigurer.GetComponent<PointCloudCycler>();
        positionPresetCycler = viewOptionsConfigurer.GetComponent<PositionPresetCycler>();
    }

    private void ToggleArmCamera()
    {
        var uiManager = GameObject.FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            var armCameraView = uiManager.FindCameraMode<ArmCameraView>();
            var allOffView = uiManager.FindCameraMode<AllOffView>();
            if (ScoutModeManager.Instance.cameraView.activeCameraMode == armCameraView)
            {
                ScoutModeManager.Instance.cameraView.SetActiveCameraMode(allOffView);
            }
            else
            {
                ScoutModeManager.Instance.cameraView.SetActiveCameraMode(armCameraView);
            }
        }
    }

    public override void ControlUpdate(SpotMode spot, ControllerModel model)
    {
        bool isIndexHeld = OVRInput.Get(model.indexButton);
        bool isArmMode;
        bool indexPressed = OVRInput.GetDown(model.indexButton);
        bool isGripHeld = OVRInput.Get(model.gripButton);
        bool isJoystickPressed = OVRInput.GetDown(model.joystickButton);
        bool gripPressed = OVRInput.GetDown(model.gripButton);
        Vector2 joystick = OVRInput.Get(model.joystick);


        // UI Labels
        string thumbstickLabel = "";
        string triggerLabel = "";
        string gripLabel = "";

        if(indexPressed)
        {
            float deltaTime=Time.time-indexDownTimeSeconds;
            if(deltaTime<.3f)
            {
                spot.StowArm();
            }
            indexDownTimeSeconds = Time.time;
            isArmMode=false; // default to drive mode on index press, if they want arm mode they have to hold for >0.3 seconds
        }
        else if (isIndexHeld&&Time.time-indexDownTimeSeconds>0.3f)
        {
            isArmMode = true;
        }
        else
        {
            isArmMode = false;
        }
        if (isArmMode) // behave as Arm Mode
        {
            // === Arm Control Mode ===
            if (OVRInput.GetDown(model.byButton))
                positionPresetCycler.CyclePresets();
            if (OVRInput.GetDown(model.axButton))
                pointCloudCycler.CyclePointClouds();

            switch (armControlMode)
            {
                case ArmControlMode.AbsolutePos:
                    spot.SetGripperTf(model.anchor.transform);
                    isRelativeModeActive = false;
                    break;

                case ArmControlMode.AbsoluteController:
                    model.anchor.transform.SetPositionAndRotation(
                        spot.GetGripperPos().position,
                        spot.GetGripperPos().rotation);
                    isRelativeModeActive = false;
                    break;

                case ArmControlMode.Relative:
                    if (!isRelativeModeActive)
                    {
                        initialControllerPosition = model.anchor.transform.position;
                        initialControllerRotation = model.anchor.transform.rotation;
                        initialGripperPosition = spot.GetGripperPos().position;
                        initialGripperRotation = spot.GetGripperPos().rotation;
                        isRelativeModeActive = true;
                    }

                    Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
                    Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

                    Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
                    Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

                    spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
                    break;
            }

            // Grip toggles gripper open/closed
            if (gripPressed)
                spot.SetGripperOpen(!spot.GetGripperOpen());

            bool isGripperOpen = spot.GetGripperOpen();

            thumbstickLabel = "Arm Mode";
            triggerLabel = "";
            gripLabel = isGripperOpen ? "Close Gripper" : " Open Gripper";
            model.axLabel = "Cycle PointClouds";
            model.byLabel = "Cycle Views";
        }
        else // behave as Drive Mode
        {
            // === Drive Mode ===
            isRelativeModeActive = false;

            if (isJoystickPressed && !isGripHeld)
            {
                // === Body Up/Down Mode ===
                isRelativeModeActive = false;

                if (Mathf.Abs(joystick.y) > 0.1)
                    spot.AdjustHeight(joystick.y * 0.02f);

                thumbstickLabel = "Body Up/Down";
                gripLabel = "";
                triggerLabel = ""; // Trigger disabled while body up/down
            }
            else if (isGripHeld)
            {
                // === Rotate Mode ===
                isRelativeModeActive = false;

                if (Mathf.Abs(joystick.x) > 0.1)
                    spot.Rotate(joystick.x * 0.5f);

                thumbstickLabel = "Rotate Spot | Press to Stow Arm";
                gripLabel = "";
                triggerLabel = "";

                if (isJoystickPressed)
                {
                    spot.StowArm();
                }
            }
            else
            {
                // === Normal Drive Mode ===
                if (joystick.magnitude > 0.1f)
                    spot.Drive(joystick * 0.5f);

                if (OVRInput.Get(model.byButton))
                    spot.AdjustHeight(0.03f);

                if (OVRInput.Get(model.axButton))
                    spot.AdjustHeight(-0.03f);
                triggerLabel = "Double Press to Stow Arm";
                thumbstickLabel = "Drive Spot";
                triggerLabel = "Hold: Control Arm";
                gripLabel = "Hold: Rotate";
                model.axLabel = "Lower Body";
                model.byLabel = "Raise Body";
            }
        }

        model.joystickLabel = thumbstickLabel;
        model.indexLabel = triggerLabel;
        model.gripLabel = gripLabel;


        spot.ChangeGripperColorBasedOnDistance();
    }

    public override string GetName() => "Dynamic Control";

    public override int ModeIndex => 0;

    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;

    public override void AssignDefaultLabels(ControllerModel exampleModel)
    {
        exampleModel.joystickLabel = "Drive";
        exampleModel.indexLabel = "Hold to Rotate";
        exampleModel.gripLabel = "Hold to Control Arm";
    }
}