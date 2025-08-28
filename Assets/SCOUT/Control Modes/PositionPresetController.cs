using UnityEngine;

public class PositionPresetController : MonoBehaviour
{
    public RigPositioner rigPositioner;
    public GameObject spotOne, spotTwo, spotOneArm, spotTwoArm;

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
        Preset.ArmSpotOne,
        Preset.ArmSpotTwo
    };
    private int curPresetIndex = -1;

    public void CyclePresets()
    {
        curPresetIndex = (curPresetIndex + 1) % presetOrder.Length;

        var cameraPosition = Vector3.zero;
        switch (presetOrder[curPresetIndex])
        {
            case Preset.BehindSpotOne:
                cameraPosition = spotOne.transform.position - new Vector3(0, 0, 6.5f);
                break;

            case Preset.BehindSpotTwo:
                cameraPosition = spotTwo.transform.position - new Vector3(0, 0, 6.5f);
                break;

            case Preset.BetweenSpots:
                cameraPosition = (spotOne.transform.position + spotTwo.transform.position) / 2 - new Vector3(0, 0, 5);
                break;

            case Preset.ArmSpotOne:
                cameraPosition = spotOneArm.transform.position - new Vector3(0, 0, 4.5f);
                break;

            case Preset.ArmSpotTwo:
                cameraPosition = spotTwoArm.transform.position - new Vector3(0, 0, 4.5f);
                break;
        }

        rigPositioner.x = cameraPosition.x;
        rigPositioner.z = cameraPosition.z;
    }

    public void SetInitialPreset()
    {
        curPresetIndex = -1;
        CyclePresets();
    }
}