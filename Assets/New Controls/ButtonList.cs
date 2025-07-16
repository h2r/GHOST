using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonList : MonoBehaviour
{
    public string title;
    public NamedMode[] options = {
        new DriveJoystickMode(),
        new Arm6AxisMode()
    };
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
            if (button == hitObj)
            {
                button.GetComponent<Image>().color = new(1, 0, 0, 0.5f);
                action(options[i]);
            }
            else
            {
                button.GetComponent<Image>().color = new(1, 1, 1, 0.5f);
            }
        }

        return true;
    }
}