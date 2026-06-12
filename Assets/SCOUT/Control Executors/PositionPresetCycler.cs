using UnityEngine;
using SCOUT;

public class PositionPresetCycler : MonoBehaviour
{
    public RigPositioner rigPositioner;
    public GameObject spotOne, spotTwo, spotOneArm, spotTwoArm;
    public MessageBadge messageManager;

    // Current control flow leaves preset cycling disabled, but keeping it reachable avoids dead code.
    public bool enablePresetCycling;

    [Header("Spot Front-Camera Lock")]
    [Tooltip("Match rig yaw to the spot heading")]
    public bool matchSpotHeading = true;

    // offset from the spot anchor to the front-camera viewpoint, in the spot's local frame
    [Tooltip("Forward offset to the front camera (+Z is forward)")]
    [Range(-3f, 3f)] public float cameraForwardOffset = 0.45f;
    [Tooltip("Height offset to the front camera")]
    [Range(-2f, 2f)] public float cameraHeightOffset = 0.0f;
    [Tooltip("Sideways offset to the front camera (+X is right)")]
    [Range(-2f, 2f)] public float cameraLateralOffset = 0.0f;

    public enum RigAnchorPoints
    {
        World,
        SpotOne,
        SpotTwo
    }
    public RigAnchorPoints currentAnchorPoint = RigAnchorPoints.World;

    // true while the rig is locked to a spot
    public bool IsLockedToSpot => currentAnchorPoint != RigAnchorPoints.World;

    enum Preset
    {
        BehindSpotOne,
        BehindSpotTwo,
        BetweenSpots,
        ArmSpotOne,
        ArmSpotTwo
    }

    private readonly Preset[] presetOrder = {
        Preset.BetweenSpots,
        Preset.BehindSpotOne,
        Preset.BehindSpotTwo,
        // Preset.ArmSpotOne,
        // Preset.ArmSpotTwo
    };
    private int curPresetIndex = -1;

    private Vector3 CameraOffset => new(cameraLateralOffset, cameraHeightOffset, cameraForwardOffset);

    // re-pin the rig to the spot after the control modes run, so the joystick can't move it
    private void LateUpdate()
    {
        if (!IsLockedToSpot)
            return;

        Transform anchorTransform = GetCurrentAnchorTransform();
        if (anchorTransform == null)
            return;

        ApplyLockedPose(anchorTransform);
    }

    private void ApplyLockedPose(Transform anchorTransform)
    {
        Vector3 cameraPosition = anchorTransform.position + anchorTransform.TransformDirection(CameraOffset);

        rigPositioner.x = cameraPosition.x;
        rigPositioner.y = cameraPosition.y;
        rigPositioner.z = cameraPosition.z;

        if (matchSpotHeading)
        {
            // face where the robot faces; head tracking still lets you look around freely
            rigPositioner.rotation = Quaternion.Euler(0, anchorTransform.eulerAngles.y, 0);
        }
    }

    private Transform GetCurrentAnchorTransform()
    {
        switch (currentAnchorPoint)
        {
            case RigAnchorPoints.SpotOne:
                return spotOne?.transform;
            case RigAnchorPoints.SpotTwo:
                return spotTwo?.transform;
            default:
                return null;
        }
    }

    public void CyclePresets()
    {
        if (!enablePresetCycling)
            return;

        curPresetIndex = (curPresetIndex + 1) % presetOrder.Length;

        switch (presetOrder[curPresetIndex])
        {
            case Preset.BehindSpotOne:
                currentAnchorPoint = RigAnchorPoints.SpotOne;
                ApplyLockedPose(spotOne.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot One");
                break;

            case Preset.BehindSpotTwo:
                currentAnchorPoint = RigAnchorPoints.SpotTwo;
                ApplyLockedPose(spotTwo.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot Two");
                break;

            case Preset.BetweenSpots:
                currentAnchorPoint = RigAnchorPoints.World;
                Vector3 cameraPosition = (spotOne.transform.position + spotTwo.transform.position) / 2 - new Vector3(0, 0, .5f);
                rigPositioner.x = cameraPosition.x;
                rigPositioner.z = cameraPosition.z;
                if (messageManager) messageManager.ShowMessage("Camera Anchor: World");
                break;

            case Preset.ArmSpotOne:
                currentAnchorPoint = RigAnchorPoints.SpotOne;
                ApplyLockedPose(spotOneArm.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot One");
                break;

            case Preset.ArmSpotTwo:
                currentAnchorPoint = RigAnchorPoints.SpotTwo;
                ApplyLockedPose(spotTwoArm.transform);
                if (messageManager) messageManager.ShowMessage("Camera Anchor: Lock To Spot Two");
                break;
        }
    }

    public void SetInitialPreset()
    {
        curPresetIndex = -1;
        CyclePresets();
    }
}
