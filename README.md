# GHOST
Brown University GHOST (Ghost Human Operator for Spot Teleoperation) Virtual Reality Project

This repo contains code for the Unity half of the GHOST system, which allows for robust VR robot teleoperation with immersive point-cloud visualization and intuitive gesture controls. 

Pre-requisites:
GHOST runs on windows and has been tested with Meta headsets, we recommend the Meta Quest Pro headset with touch controllers.


To set up this project on your computer:
1. Download Unity if not already installed (https://unity.com/download)
2. In Unity Hub, download the version indicated by this project (e.g. 6000.1.7f1). If you don't see this particular version as an option, you can download it from the Unity archive (https://unity.com/releases/editor/archive). Before you install in Unity Hub, select "Android Build Support" and its children from the "Platforms" menu.
3. With a git client, navigate to the desired directory and clone this repository ("git clone git@github.com:h2r/GHOST.git")
4. Check out the "main" branch in git.
5. In Unity Hub, select "Open", select the GHOST folder, and confirm by pressing "open".
6. Once Unity is open, open the "ghost" scene to see our Spot scene. To use GHOST with a different robot, you will need to upload the robot URDF through ROSBridge. 

This repo works with the ros\_reality repo, which can be found at github.com/h2r/ros\_reality

Git tracks all Unity assets according to unique IDs. All assets need to be created, moved, or renamed through Unity, so that Unity can manage the IDs and Git can keep track of everything.
