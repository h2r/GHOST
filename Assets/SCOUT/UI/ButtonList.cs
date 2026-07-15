using System;
using System.Linq;
using TMPro;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;

public class ButtonList : MonoBehaviour
{
    public string title;
    public bool isHorizontal;
    public bool isToggle;
    private bool isActive = false;
    public NamedOption[] options;
    public GameObject titlePrefab, buttonPrefab;
    

    public Func<NamedOption> optionGetter;
    public Action<NamedOption> optionSetter;

    private GameObject[] buttons;

    public void Awake()
    {
        float x = 0, y = 0;
        
        if (title != ""  && title != "Unenabled Camera Mode" && !isHorizontal)
        {
            var titleObj = Instantiate(titlePrefab, transform);
            titleObj.GetComponent<RectTransform>().localPosition = new(0, 0, 0);
            titleObj.GetComponent<TMP_Text>().text = title;
            y = -55;
        }

        buttons = new GameObject[options.Length];

        if(title != "Unenabled Camera Mode")
        {
            for (int i = 0; i < options.Length; i++)
            {
                buttons[i] = Instantiate(buttonPrefab, transform);
                buttons[i].GetComponent<RectTransform>().localPosition = new(x, y, 0);
                buttons[i].transform.Find("Text").GetComponent<TMP_Text>().text = options[i].GetName();

                if (isHorizontal)
                    x += 150;
                else
                    y -= 55;
            }
        }
        
    }

    void Update() // Corrected: Removed extra '()'
    {
        if (optionGetter == null)
            return;

        var selectedOption = optionGetter();
        for (int i = 0; i < buttons.Length; i++)
        {
            bool isSelected = options[i] == selectedOption;

            Color color;
            if (isSelected)
            {
                var selectedColor = selectedOption.GetSelectedColor();
                color = new(selectedColor.r, selectedColor.g, selectedColor.b, 0.5f);
                isActive = !isActive;

            }
            else
            {
            
                color = new(1, 1, 1, 0.5f);
                
            }

            if(buttons[i].GetComponent<UIOption>().isToggle && isActive)
            {
                buttons[i].GetComponent<Image>().color = Color.red;
            }
            else
            {
                buttons[i].GetComponent<Image>().color = color;
            }
            

            
        }
    }

    public void UpdateTabSelections(ScoutModeManager modeManager)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Color color;
            if (((UITabOption)options[i]).superMode == modeManager.uiSuperMode)
                color = Color.magenta;
            else if (((UITabOption)options[i]).superMode == modeManager.activeSuperMode)
                color = Color.purple;
            else
                color = Color.gray;
            buttons[i].GetComponent<Image>().color = color;
        }
    }

    public void Reset()
    {
        // TEMP - until can distinguish UIOption superclass
        if (!options[0].GetName().StartsWith("Swap"))
            optionSetter(options[0]);
    }

    public bool TryHoverButton(GameObject hitObj)
    {
        return buttons.Contains(hitObj);
    }

    public bool PressButton(GameObject hitObj)
    {
        if (!buttons.Contains(hitObj))
            return false;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == hitObj)
            {
                optionSetter(options[i]);
                break;
            }
        }

        return true;
    }

    public bool PressButtonIndex(int index)
    {
        return PressButton(buttons[index]); 
    }
}