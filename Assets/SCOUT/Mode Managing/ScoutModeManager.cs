using System;
using UnityEngine;

public enum SuperMode
{
    SingleDrive,
    DualDrive,
    Camera,
    TabSelection
}

public class ScoutModeManager : MonoBehaviour
{
    public static ScoutModeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public ControllerModel leftModel, rightModel, leftExampleModel, rightExampleModel;
    public SpotMode spotOne, spotTwo;
    public PositionPresetController positionPresetController;

    [NonSerialized]
    public SuperMode uiSuperMode = SuperMode.Camera;
    [NonSerialized]
    public SuperMode activeSuperMode = SuperMode.SingleDrive;

    public SingleDriveSuperMode singleDrive = new();
    public DualDriveSuperMode dualDrive = new();
    public CameraSuperMode cameraView = new();

    [NonSerialized]
    public bool isMenuOpen = false;
    private bool _previousIsMenuOpen; // To track changes in isMenuOpen
    private bool hasMenuClosed = false;

    void Update()
    {
        if (isMenuOpen)
        {
            spotOne.SetArmPoseEnabled(false);
            spotTwo.SetArmPoseEnabled(false);
        }

        // Check if isMenuOpen has changed
        if (isMenuOpen != _previousIsMenuOpen)
        {
            // If the menu is now closed and a camera mode is active, activate its controlledGameObject
            if (!isMenuOpen && cameraView.activeCameraMode != null && cameraView.activeCameraMode.controlledGameObject != null)
            {
                cameraView.activeCameraMode.controlledGameObject.SetActive(true);
            }
            // If the menu is now open and a camera mode is active, deactivate its controlledGameObject
            else if (isMenuOpen && cameraView.activeCameraMode != null && cameraView.activeCameraMode.controlledGameObject != null)
            {
                cameraView.activeCameraMode.controlledGameObject.SetActive(false);
            }
            _previousIsMenuOpen = isMenuOpen; // Update the previous state
        }

        if (!isMenuOpen && !hasMenuClosed)
        {
            positionPresetController.SetInitialPreset();
            hasMenuClosed = true;
        }

        // Update colors while menu is open
        if (isMenuOpen)
        {
            leftModel.ClearLabels();
            rightModel.ClearLabels();
            leftModel.menuLabel = "Menu";
            // leftExampleModel.ClearLabels();
            // rightExampleModel.ClearLabels();
            switch (activeSuperMode)
            {
                case SuperMode.SingleDrive:
                    singleDrive.AssignColors(leftModel, rightModel);
                    // singleDrive.AssignExampleModels(leftExampleModel, rightExampleModel);
                    break;

                case SuperMode.DualDrive:
                    dualDrive.AssignColors(leftModel, rightModel);
                    // dualDrive.AssignExampleModels(leftExampleModel, rightExampleModel);
                    break;
            }

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

        if (!isMenuOpen)
        {
            bool spotOneArm = (activeSuperMode == SuperMode.SingleDrive && singleDrive.leftSpot == spotOne && singleDrive.leftControl != null && singleDrive.leftControl.RequiresArmCamera) ||
                              (activeSuperMode == SuperMode.SingleDrive && singleDrive.rightSpot == spotOne && singleDrive.rightControl != null && singleDrive.rightControl.RequiresArmCamera) ||
                              (activeSuperMode == SuperMode.DualDrive && dualDrive.spot == spotOne && dualDrive.control != null && dualDrive.control.RequiresArmCamera);

            bool spotTwoArm = (activeSuperMode == SuperMode.SingleDrive && singleDrive.leftSpot == spotTwo && singleDrive.leftControl != null && singleDrive.leftControl.RequiresArmCamera) ||
                              (activeSuperMode == SuperMode.SingleDrive && singleDrive.rightSpot == spotTwo && singleDrive.rightControl != null && singleDrive.rightControl.RequiresArmCamera) ||
                              (activeSuperMode == SuperMode.DualDrive && dualDrive.spot == spotTwo && dualDrive.control != null && dualDrive.control.RequiresArmCamera);

            if (spotOne != null) spotOne.SetArmPoseEnabled(spotOneArm);
            if (spotTwo != null) spotTwo.SetArmPoseEnabled(spotTwoArm);
        }
    }

    public void SetUISuperMode(SuperMode mode)
    {
        uiSuperMode = mode;
        if (mode == SuperMode.SingleDrive || mode == SuperMode.DualDrive)
        {
            activeSuperMode = mode; 
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
        }
        if (rightSpot != null && rightControl != null)
        {
            rightControl.ControlUpdate(rightSpot, rightModel);
        }
        AssignColors(leftModel, rightModel);
    }

    public void AssignColors(ControllerModel leftModel, ControllerModel rightModel)
    {
        if (leftSpot != null && leftControl != null)
        {
            if (leftControl.ControlsSpot)
                leftModel.color = leftSpot.color;
            else
            {
                leftModel.color = Color.white; // Reset color if not controlling spot
            }
        }
        if (rightSpot != null && rightControl != null)
        {
            if (rightControl.ControlsSpot)
                rightModel.color = rightSpot.color;
            else
            {
                rightModel.color = Color.white; // Reset color if not controlling spot
            }
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
        }
    }

    public void AssignColors(ControllerModel leftModel, ControllerModel rightModel)
    {
        if (spot != null && control != null && control.ControlsSpot)
        {
            if (control.ControlsSpot)
            {
                leftModel.color = spot.color;
                rightModel.color = spot.color;
            }
            else
            {
                leftModel.color = Color.white; // Reset color if not controlling spot
                rightModel.color = Color.white; // Reset color if not controlling spot
            }
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

public class CameraSuperMode
{
    public CameraMode cameraMode;
    public CameraMode activeCameraMode; // ADDED: To store the currently selected camera mode

    public void SetActiveCameraMode(CameraMode mode)
    {
        if (activeCameraMode != null && activeCameraMode.controlledGameObject != null)
        {
            activeCameraMode.controlledGameObject.SetActive(false);
        }

        activeCameraMode = mode;

        if (activeCameraMode != null && activeCameraMode.controlledGameObject != null)
        {
            if (ScoutModeManager.Instance != null && !ScoutModeManager.Instance.isMenuOpen)
            {
                activeCameraMode.controlledGameObject.SetActive(true);
            }
            else
            {
                activeCameraMode.controlledGameObject.SetActive(false);
            }
        }
        Debug.Log("Active Camera Mode set to: " + mode.GetName()); // For debugging
    }
}
