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
                cameraPosition = (spotOne.transform.position + spotTwo.transform.position) / 2 - new Vector3(0, 0, 5);
                break;

            case Preset.BehindSpotOne:
                cameraPosition = spotOne.transform.position - new Vector3(0, 0, 6.5f);
                break;

            case Preset.BehindSpotTwo:
                cameraPosition = spotTwo.transform.position - new Vector3(0, 0, 6.5f);
                break;

            case Preset.ArmSpotOne:
                cameraPosition = spotOneArm.transform.position - new Vector3(0, 0, 4.5f);
                break;

            case Preset.ArmSpotTwo:
                cameraPosition = spotTwoArm.transform.position - new Vector3(0, 0, 4.5f);
                break;
        }

        cameraRig.transform.position = cameraPosition;
    }
}