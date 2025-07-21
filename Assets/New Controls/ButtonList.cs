using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonList : MonoBehaviour
{
    public string title;
    public MenuDefinition menuDefinition;
    public GameObject titlePrefab, buttonPrefab;

    [HideInInspector] public GameObject[] buttons;

    public void Start()
    {
        if (menuDefinition == null || menuDefinition.options == null)
        {
            Debug.LogWarning($"{name}: MenuDefinition or options not set!");
            return;
        }

        var titleObj = Instantiate(titlePrefab, transform);
        titleObj.GetComponent<RectTransform>().localPosition = new(0, 0, 0);
        titleObj.GetComponent<TMP_Text>().text = title;

        buttons = new GameObject[menuDefinition.options.Length];
        float y = -55;
        for (int i = 0; i < menuDefinition.options.Length; i++)
        {
            buttons[i] = Instantiate(buttonPrefab, transform);
            buttons[i].GetComponent<RectTransform>().localPosition = new(0, y, 0);
            buttons[i].transform.Find("Text").GetComponent<TMP_Text>().text = menuDefinition.options[i].GetName();

            y -= 55;
        }
    }

    public bool TryHoverButton(GameObject hitObj)
    {
        return buttons != null && buttons.Contains(hitObj);
    }

    public bool ContainsButton(GameObject obj)
    {
        return buttons != null && buttons.Contains(obj);
    }

    public void HighlightButton(GameObject hovered)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            Color color;
            if (button == hovered)
            {
                color = new Color(0.5f, 0.7f, 1f, 0.5f); // subtle blue
            }
            else
            {
                color = new(1, 1, 1, 0.5f);
            }
            button.GetComponent<Image>().color = color;
        }
    }

    public bool PressButton(GameObject hitObj, Action<NamedMode> action)
    {
        if (buttons == null || !buttons.Contains(hitObj))
            return false;

        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            Color color;
            if (button == hitObj)
            {
                var mode = menuDefinition.options[i];
                if (mode.GetType() == typeof(SpotMode))
                {
                    var spotColor = ((SpotMode)mode).color;
                    color = new(spotColor.r, spotColor.g, spotColor.b, 0.5f);
                }
                else
                {
                    color = new(0, 1, 0, 0.5f);
                }
                action(mode);
            }
            else
            {
                color = new(1, 1, 1, 0.5f);
            }
            button.GetComponent<Image>().color = color;
        }

        return true;
    }
}
