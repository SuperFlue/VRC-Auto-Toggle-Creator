# VRChat Auto Toggle Creator
A Unity Editor tool to automatically setup the lengthy process of making animations, setting up a controller, and filling out the VRChat expression assets.
While this is made with VRChat in mind, that is only the tail end of the script and can be used for a variety of tasks related to generating animations and configuring animators.
# Download

https://github.com/SuperFlue/VRC-Auto-Toggle-Creator/releases

# How to Use
1. Download ***AutoToggleCreator.cs*** from the download page linked above.
2. Drag the script anywhere into your asset folder in unity and open the menu from the top Tools/Cascadian/AutoToggleCreator
4. Make sure your avatar has the VRC_Descriptor and has the FXAnimationController, VRCExpressionsMenu and VRCExpressionParameters assets attached then click the auto fill button at the top. (Or drag in the four fields manually).
5. Next, drag in your game objects you would like to make a toggle for.
6. When that is done, you can click the "Create Toggles!" button and it will create the animation, layers, parameters and expression items needed.
7. Upload to VRChat and you should have a separate toggle for each game object you assigned!

***Tip:***  
If you want to have toggles over multiple menus (or just have way too many of them).  
Then run the tool against several different objects and menus.  
If you don't care much about the auto generated menus it will stop filling menus when they reach 8 items, but animations, parameters and layers will continue to be generated.
# Advanced Settings
- ***Output subdirectory:*** This is the subdirectory any animation clips will be placed in. You can change this if needed. This is always relative to the animation controller.
- ***Save VRC Parameters:*** If this button is checked, parameters will be set as "saved". This means the parameter state will persist even if you change world.
# Current known Issues
- Random edge cases where error can occur due to names, order or invalid objects.
- Error sometimes when selecting multiple game objects in scene. Does not effect anything but NO CLUE why :/

# ~~Video Example of Use~~
Video is out of date with current state.

# How It Works
What this editor tool does is generate an animation clip and keyframes inside of it for the corresponding toggle object name (also checks to see if the default should be activating or deactivating the object). Once the clips are generated and places in the assets, the animator controller is accessed and a new layer and parameter is made for each toggle object. The transitions are setup in the configuration VRChat needs to behave with their expressions system. Once that is taken care of all the smaller settings/values set, the VRCExpressionsParameters and VRCExpressionsMenu assets are accessed and filled using the same naming conventions as with the animator controller. A control is made and assigned with the parameter and it's done!

Every animation toggle also gets created with a initialization state, to prevent toggles from going weird when you load in your avatar.

Note! By default this creates layers with Write Defaults off according VRC best practice.
Depending on you avatar you might have to tweak this.

# Credits
- [CascadianWorks](https://github.com/CascadianWorks) - Original code,
- [Hai](https://github.com/hai-vr) - Some methods borrowed from CGE.