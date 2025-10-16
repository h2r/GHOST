using UnityEngine;
using SCOUT;

public class PositionPresetController : MonoBehaviour
{
    public RigPositioner rigPositioner;
    public GameObject spotOne, spotTwo, spotOneArm, spotTwoArm;
    public MessageBadge messageManager;


    public enum RigAnchorPoints
    {
        World,
        SpotOne,
        SpotTwo
    }
    public RigAnchorPoints currentAnchorPoint = RigAnchorPoints.World;

    enum Preset
    {
        BehindSpotOne,
        BehindSpotTwo,
        BetweenSpots,
        ArmSpotOne,
        ArmSpotTwo
    }

    private readonly Preset[] presetOrder = {
        Preset.BehindSpotOne,
        Preset.BehindSpotTwo,
        Preset.BetweenSpots,
        // Preset.ArmSpotOne,
        // Preset.ArmSpotTwo
    };
    private int curPresetIndex = -1;

    // Track relative offset and rotation from anchor point
    private Vector3 relativeOffset = Vector3.zero;
    private Quaternion relativeRotation = Quaternion.identity;
    private Vector3 lastAnchorPosition = Vector3.zero;
    private Quaternion lastAnchorRotation = Quaternion.identity;

    private void Update()
    {
        if (currentAnchorPoint == RigAnchorPoints.World)
            return;

        Transform anchorTransform = GetCurrentAnchorTransform();
        if (anchorTransform == null)
            return;

        // Check if anchor has moved or rotated
        if (anchorTransform.position != lastAnchorPosition || anchorTransform.rotation != lastAnchorRotation)
        {
            // Calculate world position by applying relative offset to anchor
            Vector3 worldOffset = anchorTransform.TransformDirection(relativeOffset);
            Vector3 newCameraPosition = anchorTransform.position + worldOffset;

            // Calculate world rotation by combining anchor rotation with relative rotation
            Quaternion newCameraRotation = anchorTransform.rotation * relativeRotation;

            rigPositioner.x = newCameraPosition.x;
            rigPositioner.z = newCameraPosition.z;
            rigPositioner.rotation = newCameraRotation;

            lastAnchorPosition = anchorTransform.position;
            lastAnchorRotation = anchorTransform.rotation;
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
        curPresetIndex = (curPresetIndex + 1) % presetOrder.Length;

        Vector3 cameraPosition = Vector3.zero;
        Quaternion cameraRotation = Quaternion.identity;
        Transform anchorTransform = null;

        switch (presetOrder[curPresetIndex])
        {
            case Preset.BehindSpotOne:
                anchorTransform = spotOne.transform;
                cameraPosition = anchorTransform.position + anchorTransform.TransformDirection(new Vector3(0, 0, -2.65f));
                currentAnchorPoint = RigAnchorPoints.SpotOne;
                if (messageManager) messageManager.Show("Camera Anchor: Lock To Spot One");
                break;

            case Preset.BehindSpotTwo:
                anchorTransform = spotTwo.transform;
                cameraPosition = anchorTransform.position + anchorTransform.TransformDirection(new Vector3(0, 0, -2.65f));
                currentAnchorPoint = RigAnchorPoints.SpotTwo;
                if (messageManager) messageManager.Show("Camera Anchor: Lock To Spot Two");
                break;

            case Preset.BetweenSpots:
                cameraPosition = (spotOne.transform.position + spotTwo.transform.position) / 2 - new Vector3(0, 0, .5f);
                currentAnchorPoint = RigAnchorPoints.World;
                if (messageManager) messageManager.Show("Camera Anchor: World");
                break;

            case Preset.ArmSpotOne:
                anchorTransform = spotOne.transform;
                cameraPosition = spotOneArm.transform.position + anchorTransform.TransformDirection(new Vector3(0, 0, -1.45f));
                currentAnchorPoint = RigAnchorPoints.SpotOne;
                if (messageManager) messageManager.Show("Camera Anchor: Lock To Spot One");
                break;

            case Preset.ArmSpotTwo:
                anchorTransform = spotTwo.transform;
                cameraPosition = spotTwoArm.transform.position + anchorTransform.TransformDirection(new Vector3(0, 0, -1.45f));
                currentAnchorPoint = RigAnchorPoints.SpotTwo;
                if (messageManager) messageManager.Show("Camera Anchor: Lock To Spot Two");
                break;
        }

        // update the rotation of the camera, only keeping yaw rotation.
        // unity uses body ZXY order for euler angles, so to extract yaw only, we only need to zero out X and Z
        // if in the future we want to keep pitch and roll, we will need to do a more complex calculation.
        cameraRotation = Quaternion.Euler(0, anchorTransform.eulerAngles.y, 0);

        rigPositioner.x = cameraPosition.x;
        rigPositioner.z = cameraPosition.z;
        if (currentAnchorPoint != RigAnchorPoints.World) 
            rigPositioner.rotation = cameraRotation;

        // Update relative offset, rotation, and last anchor state
        if (anchorTransform != null)
        {
            relativeOffset = anchorTransform.InverseTransformDirection(cameraPosition - anchorTransform.position);
            relativeRotation = Quaternion.Inverse(anchorTransform.rotation) * rigPositioner.rotation;
            lastAnchorPosition = anchorTransform.position;
            lastAnchorRotation = anchorTransform.rotation;
        }
    }

    public void SetInitialPreset()
    {
        curPresetIndex = -1;
        CyclePresets();
    }
}