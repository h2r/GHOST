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
