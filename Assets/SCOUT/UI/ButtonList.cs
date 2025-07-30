using System;
using System.Linq;
using Meta.WitAi.Composer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonList : MonoBehaviour
{
    public string title;
    public NamedMode[] options;
    public GameObject titlePrefab, buttonPrefab;
    public Sprite redBlueSprite, blueRedSprite; 
    private bool swap; 

    private GameObject[] buttons;

    public void Start()
    {
        float y = 0;
        float x = 0; 
        if (options.Length > 1) {
            var titleObj = Instantiate(titlePrefab, transform);
            titleObj.GetComponent<RectTransform>().localPosition = new(0, 0, 0);
            titleObj.GetComponent<TMP_Text>().text = title;
            y = -55;
        }
        else
        {
            x = -325;
        }

            buttons = new GameObject[options.Length];

        for (int i = 0; i < options.Length; i++)
        {
            buttons[i] = Instantiate(buttonPrefab, transform);
            buttons[i].GetComponent<RectTransform>().localPosition = new(x, y, 0);
            buttons[i].transform.Find("Text").GetComponent<TMP_Text>().text = options[i].GetName();

            y -= 55;
        }
        if(options.Length == 1)
        {
            buttons[0].GetComponent<Image>().sprite = blueRedSprite;
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
            if (options[i].GetType() == typeof(SwapSpot))
            {

                action(options[i]);
                swap = !swap;
                var img = button.GetComponent<Image>();
                img.sprite = swap ? redBlueSprite : blueRedSprite;
                return true; 
            }
            else if (button == hitObj)
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

    public bool PressButtonIndex(int index, Action<NamedMode> action)
    {
        return PressButton(buttons[index], action); 
    }

    public GameObject GetButton(int index)
    {
        return buttons[index];
    }
}