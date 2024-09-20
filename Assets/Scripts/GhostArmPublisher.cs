using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostArmPublisher : MonoBehaviour
{
    bool connectedToPublisher;
    public RosSharp.RosBridgeClient.ArmInputPublisher armPublisher;

    private GameObject armObjGhost;
    private GameObject shoulder1Ghost;
    private GameObject elbow0Ghost;
    private GameObject elbow1Ghost;
    private GameObject wrist0Ghost;
    private GameObject wrist1Ghost;
    private GameObject fingerGhost;

    public bool moveGArmWithInspector;
    public double[] ghostArmFromInspector = new double[6];
    //public double[] testingGhostArmAngleRetrieval = new double[6]; //testing to see why the retrieved angles are wrong (delete after testing)


    void Start()
    {
        connectedToPublisher = (armPublisher != null);

        armObjGhost = GameObject.Find("ghost arm");
        shoulder1Ghost = GameObject.Find("newarm0.link_sh1");
        elbow0Ghost = GameObject.Find("newarm0.link_el0");
        elbow1Ghost = GameObject.Find("newarm0.link_el1");
        wrist0Ghost = GameObject.Find("newarm0.link_wr0");
        wrist1Ghost = GameObject.Find("newarm0.link_wr1");
        fingerGhost = GameObject.Find("newarm0.link_fngr");
    }

    void Update()
    {
        //only used when manually pubvlishing ghost arm from inspector
        if (moveGArmWithInspector)
        {
            RotateGhostArm(ghostArmFromInspector);
            if (connectedToPublisher)
            {
                armPublisher.publishArmDegrees(GetGhostArmAngles());
            }
            else
            {
                Debug.Log("publisher object is null,, not sending any arm inputs from this script to ArmInputPublisher\nThis script: " + this.name.ToString());
            }
            connectedToPublisher = (armPublisher != null);
        }
        
    }

    public void PublishGhostArm()
    {
        if (connectedToPublisher)
        {
            armPublisher.publishArmDegrees(GetGhostArmAngles());
        }
        else
        {
            Debug.Log("publisher object is null,, not sending any arm inputs from this script to ArmInputPublisher\nThis script: " + this.name.ToString());
        }
        connectedToPublisher = (armPublisher != null);
    }


    public void RotateGhostArm(double[] inputDegrees)
    {
        armObjGhost.transform.localEulerAngles = new Vector3(0, (float)(inputDegrees[0]), 0);
        shoulder1Ghost.transform.localEulerAngles = new Vector3((float)(inputDegrees[1]), 0, 0);
        elbow0Ghost.transform.localEulerAngles = new Vector3((float)(inputDegrees[2]), 0, 0);
        elbow1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(inputDegrees[3]));
        wrist0Ghost.transform.localEulerAngles = new Vector3((float)(inputDegrees[4]), 0, 0);
        wrist1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(inputDegrees[5]));
    }

    public double[] GetGhostArmAngles()
    {
        double[] rawAngles = GetGhostArmAnglesRaw();
        double[] angles = ConformToSpotsAngleBounds(rawAngles);

        //testingGhostArmAngleRetrieval = angles; //for testing if angles aren't sending properly

        return angles;
    }


    public double[] GetGhostArmAnglesRaw()
    {
        double[] angles = new double[6];

        angles[0] = armObjGhost.transform.localEulerAngles.y;
        angles[1] = XAxisEulerCorrection(shoulder1Ghost.transform.localEulerAngles).x;
        //Debug.Log("sh1: " + shoulder1Ghost.transform.localEulerAngles); //for testing, can delete
        angles[2] = XAxisEulerCorrection(elbow0Ghost.transform.localEulerAngles).x;
        //Debug.Log("el0: " + elbow0Ghost.transform.localEulerAngles); //for testing, can delete
        angles[3] = elbow1Ghost.transform.localEulerAngles.z;
        angles[4] = XAxisEulerCorrection(wrist0Ghost.transform.localEulerAngles).x;
        //Debug.Log("wr0: " + wrist0Ghost.transform.localEulerAngles); //for testing, can delete
        angles[5] = wrist1Ghost.transform.localEulerAngles.z;

        return angles;
    }


    public double[] ConformToSpotsAngleBounds(double[] raw)
    {
        double[] conformed = new double[6];

        for (int i = 0; i < raw.Length; i++)
            conformed[i] = spotAngle(raw[i]);

        return conformed;
    }



    //helper function
    /*  when getting arm angles unity goes from quaternion to euler angles
     *  there are two euler angles that represent any given rotation
     *  the quaternion to euler function sometimes returns the other euler representation that we do not want
     *  this converts either to the correct one we want
     */
    public Vector3 XAxisEulerCorrection(Vector3 input)
    {
        Vector3 output = input;

        if (approximately(input.y, 180, 1) && approximately(input.z, 180, 1))
            output = new Vector3(180 - input.x, 0, 0);
        else if ((approximately(input.y, 0, 1) || approximately(input.y, 360, 1)) 
                && (approximately(input.z, 0, 1) || approximately(input.z, 360, 1)))
        { 
            //output = input
        }
        else
            Debug.Log("not converting Euler angle pair correctly when retreiving ghost arm angles");

        return output;
    }

    //helper function
    public double spotAngle(double rawAngle)
    {
        double conformed;

        if (rawAngle <= 180)
            conformed = rawAngle;
        else
            conformed = EquivalentNegativeAngle(rawAngle);

        return conformed;
    }

    //helper function
    public double EquivalentNegativeAngle(double positive)
    {
        return positive - 360.0;
    }

    //helper function
    private bool approximately(double a, double b, double threshold)
    {
        float diff = Mathf.Abs((float)(a-b));

        if (diff > threshold)
            return false;
        else
            return true;
    }

}
