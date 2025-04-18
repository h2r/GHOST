using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveControlAll : MonoBehaviour
{
    public int state; // 0: spot1, 1: spot2, 2: both
    // Start is called before the first frame update
    void Start()
    {
        state = 0; 
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateState(bool isSpot2)
    {
        if (!isSpot2)
        {
            if (state == 0)
            {
                state = 1;
            }
            else if (state == 1)
            {
                state = 2;
            }
            else if (state == 2)
            {
                state = 0;
            }
        }
        //Debug.LogWarning("State: " + state.ToString());
    }

    public bool getState_Bool(bool isSpot2)
    {
        if (state == 0) 
        {
            if(!isSpot2) 
            {
                return true;
            }
            return false;
        }
        else if (state == 1)
        {
            if (!isSpot2)
            {
                return false;
            }
            return true;
        }
        else if (state == 2)
        {
            if (!isSpot2)
            {
                return true;
            }
            return true;
        }

        return true;
    }

    public int getState_Int()
    {
        return state;
    }
}
