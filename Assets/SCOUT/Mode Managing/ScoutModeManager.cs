using System;
using UnityEngine;

public enum SuperMode
{
    SingleDrive,
    DualDrive
}

public class ScoutModeManager : MonoBehaviour
{
    public ControllerModel leftModel, rightModel, leftExampleModel, rightExampleModel;

    [NonSerialized]
    public SuperMode activeSuperMode = SuperMode.SingleDrive;

    public SingleDriveSuperMode singleDrive = new();
    public DualDriveSuperMode dualDrive = new();

    [NonSerialized]
    public bool isMenuOpen = true;

    void Update()
    {
        if (isMenuOpen)
        {
            leftModel.ClearLabels();
            rightModel.ClearLabels();
            leftModel.color = Color.white;
            rightModel.color = Color.white;

            // leftExampleModel.ClearLabels();
            // rightExampleModel.ClearLabels();
            // switch (activeSuperMode)
            // {
            //     case SuperMode.SingleDrive:
            //         singleDrive.AssignExampleModels(leftExampleModel, rightExampleModel);
            //         break;

            //     case SuperMode.DualDrive:
            //         dualDrive.AssignExampleModels(leftExampleModel, rightExampleModel);
            //         break;
            // }

            return;
        }

        switch (activeSuperMode)
        {
            case SuperMode.SingleDrive:
                singleDrive.Update(leftModel, rightModel);
                break;

            case SuperMode.DualDrive:
                dualDrive.Update(leftModel, rightModel);
                break;
        }
    }
}

public class SingleDriveSuperMode
{
    public SpotMode leftSpot, rightSpot;
    public OneControllerMode leftControl, rightControl;

    public void Update(ControllerModel leftModel, ControllerModel rightModel)
    {
        if (leftSpot != null && leftControl != null)
        {
            leftControl.ControlUpdate(leftSpot, leftModel);
            leftModel.color = leftSpot.color;
        }
        if (rightSpot != null && rightControl != null)
        {
            rightControl.ControlUpdate(rightSpot, rightModel);
            rightModel.color = rightSpot.color;
        }
    }

    public void AssignExampleModels(ControllerModel leftExampleModel, ControllerModel rightExampleModel)
    {
        if (leftSpot != null)
            leftExampleModel.color = leftSpot.color;
        if (leftControl != null)
            leftControl.AssignDefaultLabels(leftExampleModel);

        if (rightSpot != null)
            rightExampleModel.color = rightSpot.color;
        if (rightControl != null)
            rightControl.AssignDefaultLabels(rightExampleModel);
    }
}

public class DualDriveSuperMode
{
    public SpotMode spot;
    public TwoControllerMode control;

    public void Update(ControllerModel leftModel, ControllerModel rightModel)
    {
        if (spot != null && control != null)
        {
            control.ControlUpdate(spot, leftModel, rightModel);
            leftModel.color = spot.color;
            rightModel.color = spot.color;
        }
    }

    public void AssignExampleModels(ControllerModel leftExampleModel, ControllerModel rightExampleModel)
    {
        if (spot != null)
        {
            leftExampleModel.color = spot.color;
            rightExampleModel.color = spot.color;
        }
        if (control != null)
        {
            control.AssignDefaultLabels(leftExampleModel, rightExampleModel);
        }
    }
}