using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//AUTHOR: Ezra Odole/Summer 2023 Undergraduates
public class ControlMode : MonoBehaviour
{
    //this script simply contains information about what to enable/disable for the mode

    public string modeName;
    [TextArea(2,2)]
    public string description;
    [TextArea(3,5)]
    public string listOfControlMappings;

    public List<GameObject> objectsOfModeToEnable;
    public List<Behaviour> scriptsOfModeToEnable;

    //when entering this mode, these are the things we want enabled/disabled
    public void enableMode()
    {
        foreach (GameObject g in objectsOfModeToEnable)
            g.SetActive(true);

        foreach (Behaviour s in scriptsOfModeToEnable)
            s.enabled = true;
 
    }

    //when exiting this mode, we return these things to their previous state?
    public void disableMode()
    {
        foreach (GameObject g in objectsOfModeToEnable)
            g.SetActive(false);

        foreach (Behaviour s in scriptsOfModeToEnable)
            s.enabled = false;
    }


    override
    public string ToString() { return modeName + "\n\nDescription: " + description + "\n\nControls:\n" + listOfControlMappings; }

}
