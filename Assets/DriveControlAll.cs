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
        if (isSpot2)
        {
            if (state == 0)
            {
                state = 1;
            }

            if (state == 1)
            {
                state = 2;
            }

            if (state == 2)
            {
                state = 0;
            }
        }
    }

    public bool getStates(bool isSpot2)
    {
        if (state == 0) 
        {
            if(!isSpot2) 
            {
                return true;
            }
            return false;
        }

        if (state == 1)
        {
            if (!isSpot2)
            {
                return false;
            }
            return true;
        }

        if (state == 2)
        {
            if (!isSpot2)
            {
                return true;
            }
            return true;
        }

        return true;
    }
}
