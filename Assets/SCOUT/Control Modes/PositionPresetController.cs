using UnityEngine;

public class PositionPresetController : MonoBehaviour
{
    public GameObject cameraRig, spotOne, spotTwo, spotOneArm, spotTwoArm;

    enum Preset
    {
        BetweenSpots,
        BehindSpotOne,
        BehindSpotTwo,
        ArmSpotOne,
        ArmSpotTwo
    }

    private readonly Preset[] presetOrder = {
        Preset.BetweenSpots,
        Preset.BehindSpotOne,
        Preset.BehindSpotTwo,
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
            case Preset.BetweenSpots:
                cameraPosition = (spotOne.transform.position + spotTwo.transform.position) / 2;
                break;

            case Preset.BehindSpotOne:
                cameraPosition = spotOne.transform.position + new Vector3(0, 0, 1);
                break;

            case Preset.BehindSpotTwo:
                cameraPosition = spotTwo.transform.position + new Vector3(0, 0, 1);
                break;

            case Preset.ArmSpotOne:
                cameraPosition = spotOneArm.transform.position;
                break;

            case Preset.ArmSpotTwo:
                cameraPosition = spotTwoArm.transform.position;
                break;
        }

        cameraRig.transform.position = cameraPosition;
    }
}