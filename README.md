# VRChat Toggle Toolkit
A Unity Editor tool to automatically setup the lengthy process of making animations, setting up a controller, and filling out the VRChat expression assets.
While this is made with VRChat in mind, that is only the tail end of the script and can be used for a variety of tasks related to generating animations and configuring animators.

# How to Use
1. Add the tool to your project using VRC Creator Companion from the repository listing [here](https://superflue.github.io/VRCToggleToolkit/).
2. Open the tool from **Tools -> VRCToggleToolkit -> AutoToggleCreator**
3. Make sure your avatar has the VRC_Descriptor and has the FXAnimationController, VRCExpressionsMenu and VRCExpressionParameters assets attached then click the auto fill button at the top. (Or drag in the four fields manually).
4. Next, drag in your game objects you would like to make a toggle for.
5. When that is done, you can click the "Create Toggles!" button and it will create the animation, layers, parameters and expression items needed.
6. Upload to VRChat and you should have a separate toggle for each game object you assigned!

***Tip:***  
If you want to have toggles over multiple menus (or just have way too many of them).  
Then run the tool against several different objects and menus.  
If you don't care much about the auto generated menus it will stop filling menus when they reach 8 items, but animations, parameters and layers will continue to be generated.
# Advanced Settings
- ***Output subdirectory:*** This is the subdirectory any animation clips will be placed in. You can change this if needed. This is always relative to the animation controller.
- ***Save VRC Parameters:*** If this button is checked, parameters will be set as "saved". This means the parameter state will persist even if you change world.
- ***Make object toggles exclusive:*** This will consider the list of objects as a "set" where only one of these can be active at a time. This is set up using VRCParameterDrivers
- ***Create fallback:*** This will consider the first object as the "fallback" state. This means if all of the toggles are off the first object gets turned on.  
This is useful for example if you have different clothes on your avatar, but no body mesh underneath (so toggling all off could leave only a floating head).
## "Dangerous settings"
These settings are considered dangerous because they will overwrite/delete stuff.
- ***Recreate Layers:*** This will delete any layer existing with the object name and recreate it with the scripted setup.  
This will delete any customization already done on the layer so be careful!
# Current known Issues

# How It Works
What this editor tool does is generate an animation clip and keyframes inside of it for the corresponding toggle object name (also checks to see if the default should be activating or deactivating the object).  
Once the clips are generated and places in the assets, the animator controller is accessed and a new layer and parameter is made for each toggle object. The transitions are setup in the configuration VRChat needs to behave with their expressions system.  
Once that is taken care of all the smaller settings/values set, the VRCExpressionsParameters and VRCExpressionsMenu assets are accessed and filled using the same naming conventions as with the animator controller. A control is made and assigned with the parameter and it's done!

Every animation toggle also gets created with a initialization state, to prevent toggles from going weird when you load in your avatar.

**Note!** By default this creates layers with Write Defaults off according VRC best practice.
Depending on you avatar you might have to tweak this.

# Credits
- [CascadianVR](https://github.com/CascadianVR) - Original code,
- [Hai](https://github.com/hai-vr) - Some methods borrowed from CGE.
