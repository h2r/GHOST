using TMPro;
using UnityEngine;

public class ControllerModel : MonoBehaviour
{
    public GameObject anchor;
    public SkinnedMeshRenderer skinRenderer;
    public TMP_Text[] labels;
    public bool isLeft;

    public Color color
    {
        get => skinRenderer.material.color;
        set => skinRenderer.material.color = value;
    }

    public OVRInput.Button axButton
    {
        get => isLeft ? OVRInput.Button.One : OVRInput.Button.Three;
    }

    public OVRInput.Button byButton
    {
        get => isLeft ? OVRInput.Button.Two : OVRInput.Button.Four;
    }

    public OVRInput.Button joystickButton
    {
        get => isLeft ? OVRInput.Button.PrimaryThumbstick : OVRInput.Button.SecondaryThumbstick;
    }

    public OVRInput.Button menuButton
    {
        get => isLeft ? OVRInput.Button.Start : OVRInput.Button.Back;
    }

    public OVRInput.Button indexButton
    {
        get => isLeft ? OVRInput.Button.PrimaryIndexTrigger : OVRInput.Button.SecondaryIndexTrigger;
    }

    public OVRInput.Button gripButton
    {
        get => isLeft ? OVRInput.Button.PrimaryHandTrigger : OVRInput.Button.SecondaryHandTrigger;
    }

    public OVRInput.Axis2D joystick
    {
        get => isLeft ? OVRInput.Axis2D.PrimaryThumbstick : OVRInput.Axis2D.SecondaryThumbstick;
    }

    public string axLabel
    {
        get => labels[0].text;
        set => labels[0].text = value;
    }

    public string byLabel
    {
        get => labels[1].text;
        set => labels[1].text = value;
    }

    public string joystickLabel
    {
        get => labels[2].text;
        set => labels[2].text = value;
    }

    public string menuLabel
    {
        get => labels[3].text;
        set => labels[3].text = value;
    }

    public string indexLabel
    {
        get => labels[4].text;
        set => labels[4].text = value;
    }

    public string gripLabel
    {
        get => labels[5].text;
        set => labels[5].text = value;
    }

    public void ClearLabels()
    {
        foreach (var label in labels)
            label.text = "";
    }
}