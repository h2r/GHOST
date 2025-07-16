# GHOST VR ControlScripts - File Overview

**ArmInputPublisher.cs**  
Publishes robot arm joint angles to ROS, converting Unity angles to the robot's format.

**ModeManager.cs**  
Manages switching between different control modes and updates the UI.

**VRGeneralControls.cs**  
Handles global VR actions like stowing the arm, switching modes, and toggling the gripper.

**VRDriveSpot.cs**  
Handles robot driving in VR, including joystick input and UI feedback.

**VRDynamicArmMode.cs**  
Handles real-time (dynamic) arm control based on hand/controller position.

**VRStaticArmMode.cs**  
Handles static (discrete) arm control, publishing arm pose when triggered.

**VRSliderInput.cs**  
Manages manual arm joint control via UI sliders.

# Files outside 
**DriveControlAll.cs**
State machine that allows you to switch between controlling spot 1, spot 2, and both. 

**HeighAdjuster.cs**
Handles height of the VR camera, allowing the user to move the camera rig up 
and down when in arm mode.

**JoyArmPublisher.cs**
Handles changing the position of the arm by communicating with ROS 

**MultiDriveSpot.cs**
Handles recieving joystick input to translate, rotate, and adjust the height of the Spot 

**SpotSwitcher.cs**
Handles switching between spots, listens for the 'Q' keyboard press

**MultiJoystickArm.cs**
Handles recieving joystick input to move and robot the arm/gripper using the JoyArmPublisher
Supports three different modes of control: arm movement, gripper nod and swing, and gripper
rotation