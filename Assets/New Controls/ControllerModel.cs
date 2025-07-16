using TMPro;
using UnityEngine;

public class ControllerModel : MonoBehaviour
{
    public GameObject anchor;
    public SkinnedMeshRenderer skinRenderer;
    public TMP_Text[] labels;
    public bool isLeft;

    public void SetColor(Color color)
    {
        skinRenderer.material.color = color;
    }

    public void SetLabels(string[] texts)
    {
        for (int i = 0; i < labels.Length; i++)
            labels[i].text = texts[i];
    }
}