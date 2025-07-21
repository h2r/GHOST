using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonList : MonoBehaviour
{
    public string title;
    public NamedMode[] options;
    public GameObject titlePrefab, buttonPrefab;

    private GameObject[] buttons;

    public void Start()
    {
        var titleObj = Instantiate(titlePrefab, transform);
        titleObj.GetComponent<RectTransform>().localPosition = new(0, 0, 0);
        titleObj.GetComponent<TMP_Text>().text = title;

        buttons = new GameObject[options.Length];
        float y = -55;
        for (int i = 0; i < options.Length; i++)
        {
            buttons[i] = Instantiate(buttonPrefab, transform);
            buttons[i].GetComponent<RectTransform>().localPosition = new(0, y, 0);
            buttons[i].transform.Find("Text").GetComponent<TMP_Text>().text = options[i].GetName();

            y -= 55;
        }
    }

    public bool TryHoverButton(GameObject hitObj)
    {
        return buttons.Contains(hitObj);
    }

    public bool PressButton(GameObject hitObj, Action<NamedMode> action)
    {
        if (!buttons.Contains(hitObj))
            return false;

        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            Color color;
            if (button == hitObj)
            {
                if (options[i].GetType() == typeof(SpotMode))
                {
                    var spotColor = ((SpotMode)options[i]).color;
                    color = new(spotColor.r, spotColor.g, spotColor.b, 0.5f);
                }
                else
                {
                    color = new(0, 1, 0, 0.5f);
                }
                action(options[i]);
            }
            else
            {
                color = new(1, 1, 1, 0.5f);
            }
            button.GetComponent<Image>().color = color;
        }

        return true;
    }

    public void HighlightButton(GameObject hovered)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            Color color;
            if (button == hovered)
            {
                // Subtle blue highlight for hover
                color = new Color(0.5f, 0.7f, 1f, 0.5f);
            }
            else
            {
                color = new(1, 1, 1, 0.5f);
            }
            button.GetComponent<Image>().color = color;
        }
    }
}
