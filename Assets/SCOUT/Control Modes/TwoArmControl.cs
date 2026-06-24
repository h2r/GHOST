using UnityEngine;
using System.Collections;

public class TwoArmControl : TwoControllerMode
{
    [Header("Locomotion & Rotation")]
    public GameObject cameraRig;
    public RigPositioner rigPositioner;
    public Transform headTransform;
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 90f;
    public bool useHeadRelativeMovement = true;
    public bool useSnapTurn = false;
    public float snapAngle = 25f;
    public float snapTurnDeadzone = 0.5f;
    public float snapTurnCooldown = 0.4f;

    // --- Private State ---
    private ScoutModeManager modeManager;
    private SpotMode redSpot, blueSpot;
    private bool isSwapped = false;

    // Relative mode state variables for both arms
    private bool isRelativeModeActiveL = false;
    private bool isRelativeModeActiveR = false;
    private Vector3 initialControllerPositionL, initialControllerPositionR;
    private Quaternion initialControllerRotationL, initialControllerRotationR;
    private Vector3 initialGripperPositionL, initialGripperPositionR;
    private Quaternion initialGripperRotationL, initialGripperRotationR;
    
    private float lastSnapTime = 0f;

    private void Awake()
    {
        modeManager = ScoutModeManager.Instance;
    }

    public override void ControlUpdate(SpotMode spot, ControllerModel leftModel, ControllerModel rightModel)
    {
        // This mode requires direct access to both spots, so we get them from the manager
        if (redSpot == null || blueSpot == null)
        {
            if (modeManager.spots[0].color == Color.red)
            {
                redSpot = modeManager.spots[0];
                blueSpot = modeManager.spots[1];
            }
            else
            {
                redSpot = modeManager.spots[1];
                blueSpot = modeManager.spots[0];
            }
        }

        // --- Get All Inputs ---
        Vector2 leftJoy = OVRInput.Get(leftModel.joystick);
        Vector2 rightJoy = OVRInput.Get(rightModel.joystick);
        bool leftTrigger = OVRInput.Get(leftModel.indexButton);
        bool leftGripDown = OVRInput.GetDown(leftModel.gripButton);
        bool rightTrigger = OVRInput.Get(rightModel.indexButton);
        bool rightGripDown = OVRInput.GetDown(rightModel.gripButton);

        // --- Swap Logic ---
        if (OVRInput.GetDown(leftModel.axButton)) // 'X' button
        {
            isSwapped = !isSwapped;
        }

        SpotMode leftTargetSpot = isSwapped ? redSpot : blueSpot;
        SpotMode rightTargetSpot = isSwapped ? blueSpot : redSpot;

        // --- Locomotion (Left Joystick) ---
        if (leftJoy.magnitude > 0.1f)
        {
            Vector3 move;
            if (useHeadRelativeMovement)
            {
                Quaternion headYaw = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);
                move = (headYaw * Vector3.forward * leftJoy.y) + (headYaw * Vector3.right * leftJoy.x);
            }
            else
            {
                Vector3 forward = cameraRig.transform.forward; forward.y = 0; forward.Normalize();
                Vector3 right = cameraRig.transform.right; right.y = 0; right.Normalize();
                move = (forward * leftJoy.y) + (right * leftJoy.x);
            }
            rigPositioner.pos += move * moveSpeed * Time.deltaTime;
        }

        // --- Rotation (Right Joystick) ---
        if (Mathf.Abs(rightJoy.x) > 0.1f)
        {
            if (useSnapTurn)
            {
                if (rightJoy.x < -snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(headTransform.position, Vector3.up, -snapAngle);
                    lastSnapTime = Time.time;
                }
                else if (rightJoy.x > snapTurnDeadzone && Time.time - lastSnapTime > snapTurnCooldown)
                {
                    cameraRig.transform.RotateAround(headTransform.position, Vector3.up, snapAngle);
                    lastSnapTime = Time.time;
                }
            }
            else
            {
                float angle = rotationSpeed * rightJoy.x * Time.deltaTime;
                cameraRig.transform.RotateAround(headTransform.position, Vector3.up, angle);
            }
        }

        // --- Left Arm Control (Trigger) ---
        if (leftTrigger)
        {
            if (!isRelativeModeActiveL)
            {
                initialControllerPositionL = leftModel.anchor.transform.position;
                initialControllerRotationL = leftModel.anchor.transform.rotation;
                initialGripperPositionL = leftTargetSpot.GetGripperPos().position;
                initialGripperRotationL = leftTargetSpot.GetGripperPos().rotation;
                isRelativeModeActiveL = true;
            }
            Vector3 deltaPos = leftModel.anchor.transform.position - initialControllerPositionL;
            Quaternion deltaRot = leftModel.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotationL);
            leftTargetSpot.SetGripperWorldPoseRPC(initialGripperPositionL + deltaPos, deltaRot * initialGripperRotationL);
            leftTargetSpot.ChangeGripperColorBasedOnDistance();
        }
        else { isRelativeModeActiveL = false; }

        // --- Right Arm Control (Trigger) ---
        if (rightTrigger)
        {
            if (!isRelativeModeActiveR)
            {
                initialControllerPositionR = rightModel.anchor.transform.position;
                initialControllerRotationR = rightModel.anchor.transform.rotation;
                initialGripperPositionR = rightTargetSpot.GetGripperPos().position;
                initialGripperRotationR = rightTargetSpot.GetGripperPos().rotation;
                isRelativeModeActiveR = true;
            }
            Vector3 deltaPos = rightModel.anchor.transform.position - initialControllerPositionR;
            Quaternion deltaRot = rightModel.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotationR);
            rightTargetSpot.SetGripperWorldPoseRPC(initialGripperPositionR + deltaPos, deltaRot * initialGripperRotationR);
            rightTargetSpot.ChangeGripperColorBasedOnDistance();
        }
        else { isRelativeModeActiveR = false; }

        // --- Gripper Control (Grip buttons) ---
        if (leftGripDown) { leftTargetSpot.SetGripperOpen(!leftTargetSpot.GetGripperOpen()); }
        if (rightGripDown) { rightTargetSpot.SetGripperOpen(!rightTargetSpot.GetGripperOpen()); }

        // --- Update Labels ---
        UpdateLabels(leftModel, rightModel, leftTargetSpot, rightTargetSpot, leftTrigger, rightTrigger);
    }

    private void UpdateLabels(ControllerModel left, ControllerModel right, SpotMode leftTarget, SpotMode rightTarget, bool leftTrigger, bool rightTrigger)
    {
        left.joystickLabel = "Fly";
        right.joystickLabel = "Rotate";
        left.axLabel = "Swap Arms";
        right.axLabel = "";

        left.indexLabel = leftTrigger ? $"Controlling {leftTarget.GetName()} Arm" : $"Hold: Control {leftTarget.GetName()} Arm";
        right.indexLabel = rightTrigger ? $"Controlling {rightTarget.GetName()} Arm" : $"Hold: Control {rightTarget.GetName()} Arm";

        left.gripLabel = $"Toggle {leftTarget.GetName()} Gripper";
        right.gripLabel = $"Toggle {rightTarget.GetName()} Gripper";
    }

    public override void AssignDefaultLabels(ControllerModel left, ControllerModel right)
    {
        left.joystickLabel = "Fly";
        right.joystickLabel = "Rotate";
        left.axLabel = "Swap Arms";
        right.axLabel = "";
        left.indexLabel = "Hold: Control Blue Arm";
        right.indexLabel = "Hold: Control Red Arm";
        left.gripLabel = "Toggle Blue Gripper";
        right.gripLabel = "Toggle Red Gripper";
    }

    public override string GetName() => "Two Arm Control";
    public override int ModeIndex => 8;
    public override bool ControlsSpot => true;
    public override bool RequiresArmCamera => true;
}
