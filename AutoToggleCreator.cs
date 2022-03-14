using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using static VRC.SDKBase.VRC_AvatarParameterDriver;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
public class AutoToggleCreator : EditorWindow
{
    public GameObject[] toggleObjects;
    static bool parameterSave;
    bool pressCreate = false;
    bool showPosition = false;
    static string saveSubfolder = "ToggleAnimations";
    public static ReferenceObjects refObjects = new ReferenceObjects();

    public class ReferenceObjects
    {
        public GameObject refGameObject;
        public AnimatorController refAnimController;
        public VRCExpressionParameters vrcParam;
        public VRCExpressionsMenu vrcMenu;
        public string saveDir;
        
        public ReferenceObjects()
        {
            this.refGameObject = null;
            this.refAnimController = null;
            this.vrcParam = null;
            this.vrcMenu = null;
            this.saveDir = null;
        }
        public void generateSavePath()
        {
            string controllerpath;
            if (this.refAnimController != null)
            {
                controllerpath = AssetDatabase.GetAssetPath(this.refAnimController);
                controllerpath = controllerpath.Substring(0, controllerpath.Length - this.refAnimController.name.Length - 11);
                this.saveDir = controllerpath + saveSubfolder + "/";
            }
        }
    }

    
    [MenuItem("Tools/Cascadian/AutoToggleCreator")]

    static void Init()
    {
        // Get existing open window or if none, make a new one:
        AutoToggleCreator window = (AutoToggleCreator)EditorWindow.GetWindow(typeof(AutoToggleCreator));
        window.Show();

    }

    public void OnGUI()
    {
        EditorGUILayout.Space(15);
        if (GUILayout.Button("Auto-Fill with Selected Avatar", GUILayout.Height(30f)))
        {
            //if (Selection.activeTransform.GetComponent<Animator>() == null) { return; }
            GameObject SelectedObj = Selection.activeGameObject;
            refObjects.refGameObject = SelectedObj;
            refObjects.refAnimController = (AnimatorController)SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController;
            refObjects.vrcParam = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionParameters;
            refObjects.vrcMenu = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionsMenu;
        }
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical();
        //Avatar Animator
        GUILayout.Label("ROOT GAMEOBJECT", EditorStyles.boldLabel);
        refObjects.refGameObject = (GameObject)EditorGUILayout.ObjectField(refObjects.refGameObject, typeof(GameObject), true, GUILayout.Height(20f));

        //FX Animator Controller
        GUILayout.Label("FX AVATAR CONTROLLER", EditorStyles.boldLabel);
        refObjects.refAnimController = (AnimatorController)EditorGUILayout.ObjectField(refObjects.refAnimController, typeof(AnimatorController), true, GUILayout.Height(20f));

        //VRCExpressionParameters
        GUILayout.Label("VRC EXPRESSION PARAMETERS", EditorStyles.boldLabel);
        refObjects.vrcParam = (VRCExpressionParameters)EditorGUILayout.ObjectField(refObjects.vrcParam, typeof(VRCExpressionParameters), true, GUILayout.Height(20f));

        //VRCExpressionMenu
        GUILayout.Label("VRC EXPRESISON MENU", EditorStyles.boldLabel);
        refObjects.vrcMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(refObjects.vrcMenu, typeof(VRCExpressionsMenu), true, GUILayout.Height(20f));
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        //Toggle Object List
        GUILayout.Label("Objects to Toggle On and Off:", EditorStyles.boldLabel);
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty toggleObjectsProperty = so.FindProperty("toggleObjects");
        EditorGUILayout.PropertyField(toggleObjectsProperty, true);
        GUILayout.Space(10f);
        GUILayout.Label("Toggles will be written to:\n" + refObjects.saveDir + "\nThis can be changed under Advanced settings.", EditorStyles.helpBox);
        GUILayout.Label("Needs all items filled to proceed.", EditorStyles.helpBox);

        using (new EditorGUI.DisabledScope((refObjects.refGameObject && refObjects.refAnimController && refObjects.vrcParam && refObjects.vrcMenu) != true))
        {
            pressCreate = GUILayout.Button("Create Toggles!", GUILayout.Height(40f));
        }

        if (pressCreate)
        {
            checkSaveDir(); //Sets the save directory
            CreateClips(); //Creates the Animation Clips needed for toggles.
            ApplyToAnimator(); //Handles making toggle bool property, layer setup, states and transitions.
            MakeVRCParameter(); //Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
            MakeVRCMenu(); //Adds toggles to menu
        }

        GUILayout.Space(10f);
        GUILayout.BeginVertical();
        
        showPosition = EditorGUILayout.BeginFoldoutHeaderGroup(showPosition, "Advanced settings");
        if(showPosition)
        {
            GUILayout.Label("Output subdirectory (relative to controller):", EditorStyles.boldLabel);
            saveSubfolder = GUILayout.TextField(saveSubfolder, GUILayout.Height(20f));
            //Toggle to save VRCParameter values
            parameterSave = (bool)EditorGUILayout.ToggleLeft("Set VRC Parameters to save state?", parameterSave, EditorStyles.boldLabel);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        GUILayout.EndVertical();

        refObjects.generateSavePath();
        so.ApplyModifiedProperties();


        /*// Debug button for easy testing
        bool debugbutton;
        debugbutton = GUILayout.Button("Debug", GUILayout.Height(40f));
        if(debugbutton)
        {
            
        }*/
    }

    private void CreateClips()
    {
        foreach (GameObject gameObject in toggleObjects)
        {
            //Make animation clips for on and off state and set curves for game objects on and off
            AnimationClip toggleClipOn = new AnimationClip(); //Clip for ON

            toggleClipOn.SetCurve
                (GetGameObjectPath(gameObject.transform).Substring(refObjects.refGameObject.gameObject.name.Length + 1),
                typeof(GameObject),
                "m_IsActive",
                new AnimationCurve(new Keyframe(0, 1, 0, 0),
                new Keyframe(0.016666668f, 1, 0, 0))
                );

            AnimationClip toggleClipOff = new AnimationClip(); //Clip for OFF

            toggleClipOff.SetCurve
                (GetGameObjectPath(gameObject.transform).Substring(refObjects.refGameObject.gameObject.name.Length + 1),
                typeof(GameObject),
                "m_IsActive",
                new AnimationCurve(new Keyframe(0, 0, 0, 0),
                new Keyframe(0.016666668f, 0, 0, 0))
                );

            //Save on animation clips (Off should not be needed?)
            AssetDatabase.CreateAsset(toggleClipOn, refObjects.saveDir + $"{gameObject.name}-On.anim");
            AssetDatabase.CreateAsset(toggleClipOff, refObjects.saveDir + $"{gameObject.name}-Off.anim");
            AssetDatabase.SaveAssets();
        }
    }

    private void ApplyToAnimator()
    {
        bool initParamExist = doesNameExistParam("TrackingType", refObjects.refAnimController.parameters);

        //Check if a parameter already exists with that name. If so, Ignore adding parameter.
        if (initParamExist == false)
        {
            refObjects.refAnimController.AddParameter("TrackingType", AnimatorControllerParameterType.Int);
        }
        foreach (GameObject gameObject in toggleObjects)
        {
            bool existParam = doesNameExistParam(gameObject.name + "Toggle", refObjects.refAnimController.parameters);
            //Check if a parameter already exists with that name. If so, Ignore adding parameter.
            if (existParam == false)
            {
                refObjects.refAnimController.AddParameter(gameObject.name + "Toggle", AnimatorControllerParameterType.Bool);
            }

            //Check if a layer already exists with that name. If so, Ignore adding layer.
            AnimatorControllerLayer currentLayer = FindAnimationLayer(gameObject.name);
            if (currentLayer == null)
            {
                AnimatorControllerLayer layer = new AnimatorControllerLayer
                {
                    name = gameObject.name.Replace(".", "_"),
                    defaultWeight = 1f,
                    stateMachine = new AnimatorStateMachine() // Make sure to create a StateMachine as well, as a default one is not created
                };
                refObjects.refAnimController.AddLayer(layer);
                currentLayer = FindAnimationLayer(gameObject.name);

                //Create a state that can wait for init (prevents toggles being on/off when someone loads their avatar)
                AnimatorState Idle = new AnimatorState();
                Idle.name = "Idle-WaitForInit";
                Idle.writeDefaultValues = false;

                //Creating On and Off(Empty) states
                AnimatorState stateOn = new AnimatorState();
                stateOn.name = $"{gameObject.name} On";
                stateOn.motion = (Motion)AssetDatabase.LoadAssetAtPath(refObjects.saveDir + $"/{gameObject.name}-On.anim", typeof(Motion));
                stateOn.writeDefaultValues = false;

                AnimatorState stateOff = new AnimatorState();
                stateOff.name = $"{gameObject.name} Off";
                stateOff.motion = (Motion)AssetDatabase.LoadAssetAtPath(refObjects.saveDir + $"/{gameObject.name}-Off.anim", typeof(Motion));
                stateOff.writeDefaultValues = false;

                //Adding created states to controller layer
                currentLayer.stateMachine.AddState(Idle, new Vector3(260, 240, 0));
                currentLayer.stateMachine.AddState(stateOn, new Vector3(260, 120, 0));
                currentLayer.stateMachine.AddState(stateOff, new Vector3(520, 120, 0));

                //Transition states
                // Add init wait transition
                AnimatorStateTransition InitWait = new AnimatorStateTransition();
                InitWait.name = "InitWait";
                InitWait.hasExitTime = false;
                InitWait.AddCondition(AnimatorConditionMode.NotEqual, 0, "TrackingType");
                InitWait.destinationState = currentLayer.stateMachine.states[1].state;
                currentLayer.stateMachine.states[0].state.AddTransition(InitWait);

                AnimatorStateTransition OnOff = new AnimatorStateTransition();
                OnOff.name = "OnOff";
                OnOff.hasExitTime = false;
                OnOff.AddCondition(AnimatorConditionMode.If, 0, gameObject.name + "Toggle");
                OnOff.destinationState = currentLayer.stateMachine.states[2].state;
                currentLayer.stateMachine.states[1].state.AddTransition(OnOff);

                AnimatorStateTransition OffOn = new AnimatorStateTransition();
                OffOn.name = "OffOn";
                OffOn.hasExitTime = false;
                OffOn.AddCondition(AnimatorConditionMode.IfNot, 0, gameObject.name + "Toggle");
                OffOn.destinationState = currentLayer.stateMachine.states[1].state;
                currentLayer.stateMachine.states[2].state.AddTransition(OffOn);

                AssetDatabase.SaveAssets();

            }
        }

        EditorUtility.SetDirty(refObjects.refAnimController);
        AssetDatabase.SaveAssets();
    }

    private void MakeVRCParameter()
    {
        int ogparamlength = refObjects.vrcParam.parameters.Length;
        int newitemlength = toggleObjects.Length;
        int counter = ogparamlength;
        int nullcounter = 0;

        VRCExpressionParameters.Parameter[] newListFull = new VRCExpressionParameters.Parameter[ogparamlength + newitemlength];

        //Add parameters that were already on the SO
        for (int i = 0; i < refObjects.vrcParam.parameters.Length; i++)
        {
            newListFull[i] = refObjects.vrcParam.parameters[i];
        }

        foreach (GameObject gameObject in toggleObjects)
        {
            //Make new parameter to add to list
            VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter
            {
                //Modify parameter according to user settings and object name
                name = gameObject.name + "Toggle",
                valueType = VRCExpressionParameters.ValueType.Bool,
                defaultValue = 0
            };

            //Check to see if parameter is saved
            if (parameterSave == true) { newParam.saved = true; } else { newParam.saved = false; }
            if (!doesNameExistVRCParam(newParam.name, newListFull))
            {
                newListFull[counter] = newParam;
                counter++;
            }
            else { nullcounter++; }
        }


        int finallenght = newListFull.Length - nullcounter;
        VRCExpressionParameters.Parameter[] finalNewList = new VRCExpressionParameters.Parameter[finallenght];
        for (int i = 0; i < finallenght; i++)
        {
            finalNewList[i] = newListFull[i];
        }
 
        if (vrcParamSanityCheck(finalNewList))
        {
            refObjects.vrcParam.parameters = finalNewList;
            EditorUtility.SetDirty(refObjects.vrcParam);
            AssetDatabase.SaveAssets();
        }
        else
        {
            Debug.LogError("Adding parameters would go past the limit of "+ VRCExpressionParameters.MAX_PARAMETER_COST + ". No parameters added.");
        }
    }

    private void MakeVRCMenu()
    {
        int maxmenuitems = 8;
        foreach (GameObject gameObject in toggleObjects)
        {
            VRCExpressionsMenu.Control controlItem = new VRCExpressionsMenu.Control
            {
                name = gameObject.name + "Toggle",
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter()
            };
            controlItem.parameter.name = gameObject.name + "Toggle";

            if (!doesNameExistVRCMenu(controlItem.name, refObjects.vrcMenu.controls))
            {
                if(refObjects.vrcMenu.controls.Count < maxmenuitems)
                {
                     refObjects.vrcMenu.controls.Add(controlItem);
                }
                else
                {
                    Debug.LogWarning("Unable to add: " + controlItem.name + ". To menu: " + refObjects.vrcMenu.name + ". Too many entires! Max " + maxmenuitems + " allowed!");
                }
            }
        }

        EditorUtility.SetDirty(refObjects.vrcMenu);
        AssetDatabase.SaveAssets();
    }

    private bool vrcParamSanityCheck(VRCExpressionParameters.Parameter[] parameters)
    {
        VRCExpressionParameters vrcParamSanitycheck = CreateInstance<VRCExpressionParameters>();
        vrcParamSanitycheck.parameters = parameters;
        int newcost = vrcParamSanitycheck.CalcTotalCost();
        if (newcost < VRCExpressionParameters.MAX_PARAMETER_COST)
        {
            return true;
        }
        return false;
    }
    private void checkSaveDir()
    {
        if (!Directory.Exists(refObjects.saveDir))
        {
            Directory.CreateDirectory(refObjects.saveDir);
        }
    }

    private bool doesNameExistParam(string name, AnimatorControllerParameter[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].name == name)
            {
                return true;
            }
        }
        return false;

    }
    private bool doesNameExistVRCParam(string name, VRCExpressionParameters.Parameter[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (!(array[i] == null))
            {
                if (array[i].name == name)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool doesNameExistVRCMenu(string name, List<VRCExpressionsMenu.Control> controls)
    {
        for (int i = 0; i < controls.Count; i++)
        {
            if (!(controls[i] == null))
            {
                if (controls[i].name == name)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
    private AnimatorControllerLayer FindAnimationLayer(string name)
    {
        foreach (AnimatorControllerLayer layer in refObjects.refAnimController.layers)
        {
            if (layer.name == name)
            {
                return layer;
            }
        }
        return null;
    }
}

#endif
