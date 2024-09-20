using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro; 

public class Clock : MonoBehaviour
{
    public TextMeshProUGUI clockText;

    void Start()
    {
        clockText = GetComponent<TextMeshProUGUI>(); 
    }

    void Update()
    {
        DateTime time = DateTime.Now;
        string hour = LeadingZero(time.Hour);
        string minute = LeadingZero(time.Minute);
        string second = LeadingZero(time.Second); 

        clockText.text = hour + ":" + minute + ":" + second; //10:20:30   09:08:06
    }

    string LeadingZero (int n)
    {
        return n.ToString().PadLeft(2, '0');
    }
}
