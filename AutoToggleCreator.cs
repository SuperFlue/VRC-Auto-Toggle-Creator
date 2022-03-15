using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
public class AutoToggleCreator : EditorWindow
{
    public List<GameObject> toggleObjects;
    static bool parameterSave;
    static string saveSubfolder = "ToggleAnimations";
    public static ReferenceObjects refObjects = new ReferenceObjects();
    bool showPosition = false;
    bool missingAvatarDesc = false;
    static bool writeDefaults = false;

    public class ReferenceObjects
    {
        public GameObject refGameObject;
        public AnimatorController refAnimController;
        public VRCExpressionParameters vrcParam;
        public VRCExpressionsMenu vrcMenu;
        public string saveDir;
        public string assetContainerPath;

        public ReferenceObjects()
        {
            refGameObject = null;
            refAnimController = null;
            vrcParam = null;
            vrcMenu = null;
            saveDir = null;
        }
        public void generateSavePath()
        {
            string controllerpath;
            if (refAnimController != null)
            {
                assetContainerPath = AssetDatabase.GetAssetPath(refAnimController);
                controllerpath = assetContainerPath.Substring(0, assetContainerPath.Length - refAnimController.name.Length - 11);
                saveDir = controllerpath + saveSubfolder + "/";
            }
        }
    }

    [MenuItem("Tools/Cascadian/AutoToggleCreator")]

    static void Init()
    {
        // Get existing open window or if none, make a new one:
        AutoToggleCreator window = (AutoToggleCreator)GetWindow(typeof(AutoToggleCreator));
        window.Show();

    }

    public void OnGUI()
    {
        GUIStyle redLabel = new GUIStyle(EditorStyles.label);
        redLabel.normal.textColor = Color.red;
        EditorGUILayout.Space(15);
        if (GUILayout.Button("Auto-Fill with Selected Avatar", GUILayout.Height(30f)))
        {
            if (Selection.activeTransform.GetComponent<VRCAvatarDescriptor>() == null)
            {
                missingAvatarDesc = true;
                return;
            }
            missingAvatarDesc = false;
            GameObject SelectedObj = Selection.activeGameObject;
            refObjects.refGameObject = SelectedObj;
            refObjects.refAnimController = (AnimatorController)SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController;
            refObjects.vrcParam = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionParameters;
            refObjects.vrcMenu = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionsMenu;

        }
        if (missingAvatarDesc)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("GameObject requires a VRCAvatarDescriptor for auto-fill.", redLabel);
            EditorGUILayout.EndVertical();
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
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add"))
        {
            toggleObjects.Add(null);
        }
        if (GUILayout.Button("Remove"))
        {
            toggleObjects.RemoveAt(toggleObjects.Count - 1);
        }
        EditorGUILayout.EndHorizontal();
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty toggleObjectsProperty = so.FindProperty("toggleObjects");
        EditorGUILayout.PropertyField(toggleObjectsProperty, true);
        GUILayout.Space(10f);
        GUILayout.Label("Toggles will be written to:\n" + refObjects.saveDir + "\nThis can be changed under Advanced settings.", EditorStyles.helpBox);

        if (refObjects.refAnimController != true)
        {
            GUILayout.Label("Minimum reqires a animation controller to proceed", redLabel);
        }

        //using (new EditorGUI.DisabledScope((refObjects.refGameObject && refObjects.refAnimController && refObjects.vrcParam && refObjects.vrcMenu) != true))
        using (new EditorGUI.DisabledScope(refObjects.refAnimController != true))

        {
            //pressCreate = GUILayout.Button("Create Toggles!", GUILayout.Height(40f));
            if (GUILayout.Button("Create Toggles!", GUILayout.Height(40f)))
            {
                checkSaveDir(); //Sets the save directory
                validateGameObjectList(); //Ruimentary way to remove empty gameobject from the toggles list to prevent issues.
                if (refObjects.refGameObject != null)
                {
                    CreateClips(); //Creates the Animation Clips needed for toggles.
                }
                if (refObjects.refAnimController != null)
                {
                    ApplyToAnimator(); //Handles making toggle bool property, layer setup, states and transitions.
                }
                if (refObjects.vrcParam != null)
                {
                    MakeVRCParameter(); //Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
                }
                if (refObjects.vrcMenu != null)
                {
                    MakeVRCMenu(); //Adds toggles to menu
                }
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.Space(10f);
        GUILayout.BeginVertical();

        showPosition = EditorGUILayout.BeginFoldoutHeaderGroup(showPosition, "Advanced settings");
        if (showPosition)
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
            //Clip for ON
            AnimationClip toggleClipOn = new AnimationClip();
            toggleClipOn.SetCurve
                (GetGameObjectPath(gameObject.transform).Substring(refObjects.refGameObject.gameObject.name.Length + 1),
                typeof(GameObject),
                "m_IsActive",
                new AnimationCurve(new Keyframe(0, 1, 0, 0),
                new Keyframe(0.016666668f, 1, 0, 0))
                );

            //Clip for OFF
            AnimationClip toggleClipOff = new AnimationClip();
            toggleClipOff.SetCurve
                (GetGameObjectPath(gameObject.transform).Substring(refObjects.refGameObject.gameObject.name.Length + 1),
                typeof(GameObject),
                "m_IsActive",
                new AnimationCurve(new Keyframe(0, 0, 0, 0),
                new Keyframe(0.016666668f, 0, 0, 0))
                );

            //Save on animation clips
            AssetDatabase.CreateAsset(toggleClipOn, $"{refObjects.saveDir}/{gameObject.name}-On.anim");
            AssetDatabase.CreateAsset(toggleClipOff, $"{refObjects.saveDir}/{gameObject.name}-Off.anim");
            AssetDatabase.SaveAssets();
        }
    }

    private void ApplyToAnimator()
    {
        //Check if a parameter already exists with that name. If so, Ignore adding parameter.
        if (!doesNameExistParam("TrackingType", refObjects.refAnimController.parameters))
        {
            refObjects.refAnimController.AddParameter("TrackingType", AnimatorControllerParameterType.Int);
        }
        foreach (GameObject gameObject in toggleObjects)
        {
            string gameObjectName = gameObject.name;
            string currentParamName = gameObjectName + "Toggle";

            //Check if a parameter already exists with that name. If so, Ignore adding parameter.
            if (!doesNameExistParam(currentParamName, refObjects.refAnimController.parameters))
            {
                refObjects.refAnimController.AddParameter(currentParamName, AnimatorControllerParameterType.Bool);
            }

            string currentlayername = gameObjectName.Replace(".", "_");
            //Check if a layer already exists with that name. If so, Ignore adding layer.
            AnimatorControllerLayer currentLayer = FindAnimationLayer(currentlayername);
            if (currentLayer == null)
            {
                AddLayerWithWeight(currentlayername, 1f);
                currentLayer = FindAnimationLayer(currentlayername);

                //Create our states first
                //Create a wait for Init state (prevents toggles being on/off when someone loads their avatar)
                AnimatorState Idle = new AnimatorState
                {
                    name = currentLayer.stateMachine.MakeUniqueStateName("Idle-WaitForInit"),
                    writeDefaultValues = writeDefaults,
                    hideFlags = HideFlags.HideInHierarchy
                };
                currentLayer.stateMachine.AddState(Idle, new Vector3(260, 240, 0));
                AssetDatabase.AddObjectToAsset(Idle, refObjects.assetContainerPath);

                //Creating On state
                AnimatorState stateOn = new AnimatorState
                {
                    name = currentLayer.stateMachine.MakeUniqueStateName($"{gameObjectName} On"),
                    motion = (Motion)AssetDatabase.LoadAssetAtPath($"{refObjects.saveDir}/{gameObjectName}-On.anim", typeof(Motion)),
                    writeDefaultValues = writeDefaults,
                    hideFlags = HideFlags.HideInHierarchy
                };
                currentLayer.stateMachine.AddState(stateOn, new Vector3(260, 120, 0));
                AssetDatabase.AddObjectToAsset(stateOn, refObjects.assetContainerPath);

                //Create Off state
                AnimatorState stateOff = new AnimatorState
                {
                    name = currentLayer.stateMachine.MakeUniqueStateName($"{gameObjectName} Off"),
                    motion = (Motion)AssetDatabase.LoadAssetAtPath($"{refObjects.saveDir}/{gameObjectName}-Off.anim", typeof(Motion)),
                    writeDefaultValues = writeDefaults,
                    hideFlags = HideFlags.HideInHierarchy
                };
                currentLayer.stateMachine.AddState(stateOff, new Vector3(520, 120, 0));
                AssetDatabase.AddObjectToAsset(stateOff, refObjects.assetContainerPath);


                //Transition states
                //Init state transitions, this prevents avatar from showing in a unwanted state while it's still loading for the wearer
                //We want to wait till tracking type is not 0
                AnimatorCondition TrackingNot0 = new AnimatorCondition
                {
                    parameter = "TrackingType",
                    mode = AnimatorConditionMode.NotEqual,
                    threshold = 0
                };
                // Add transition Init -> On
                AnimatorCondition[] InitWaitOn = new AnimatorCondition[2];
                InitWaitOn[0] = MakeIfTrueCondition(currentParamName);
                InitWaitOn[1] = TrackingNot0;
                MakeTransition(currentLayer.stateMachine.states[0].state, currentLayer.stateMachine.states[1].state, "InitWait-OnState", InitWaitOn, refObjects.assetContainerPath);

                // Add transition Init -> Off
                AnimatorCondition[] InitWaitOff = new AnimatorCondition[2];
                InitWaitOff[0] = MakeIfFalseCondition(currentParamName);
                InitWaitOff[1] = TrackingNot0;
                MakeTransition(currentLayer.stateMachine.states[0].state, currentLayer.stateMachine.states[2].state, "InitWait-OffState", InitWaitOff, refObjects.assetContainerPath);

                //On <-> Off transitions
                //Off -> On Transition
                AnimatorCondition[] OffOnCondition = new AnimatorCondition[1];
                OffOnCondition[0] = MakeIfTrueCondition(currentParamName);
                MakeTransition(currentLayer.stateMachine.states[2].state, currentLayer.stateMachine.states[1].state, "Off->On", OffOnCondition, refObjects.assetContainerPath);
                
                //Off -> On Transition
                AnimatorCondition[] OnOffCondition = new AnimatorCondition[1];
                OnOffCondition[0] = MakeIfFalseCondition(currentParamName);
                MakeTransition(currentLayer.stateMachine.states[1].state, currentLayer.stateMachine.states[2].state, "On->Off", OnOffCondition, refObjects.assetContainerPath);

            }
        }

        EditorUtility.SetDirty(refObjects.refAnimController);
        AssetDatabase.SaveAssets();

    }
    private static AnimatorCondition MakeIfTrueCondition(string boolParameterName)
    {
        AnimatorCondition animatorCondition = new AnimatorCondition
        {
            parameter = boolParameterName,
            mode = AnimatorConditionMode.If,
            threshold = 0
        };
        return animatorCondition;
    }
    private static AnimatorCondition MakeIfFalseCondition(string boolParameterName)
    {
        AnimatorCondition animatorCondition = new AnimatorCondition
        {
            parameter = boolParameterName,
            mode = AnimatorConditionMode.IfNot,
            threshold = 0
        };
        return animatorCondition;
    }

    private static void MakeTransition(AnimatorState sourceState, AnimatorState destinationState, string transitionName, AnimatorCondition[] conditions, string assetContainerPath)
    {
        AnimatorStateTransition newTransition = new AnimatorStateTransition
        {
            name = transitionName,
            hasExitTime = false,
            destinationState = destinationState,
            hideFlags = HideFlags.HideInHierarchy,
            conditions = conditions
        };
        sourceState.AddTransition(newTransition);
        AssetDatabase.AddObjectToAsset(newTransition, assetContainerPath);
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

    private void AddLayerWithWeight(string layerName, float weightWhenCreating)
    {
        var layerUniqueName = refObjects.refAnimController.MakeUniqueLayerName(layerName);
        var newLayer = new AnimatorControllerLayer
        {
            name = layerUniqueName,
            defaultWeight = weightWhenCreating
        };
        newLayer.stateMachine = new AnimatorStateMachine
        {
            name = layerUniqueName,
            hideFlags = HideFlags.HideInHierarchy
        };
        AssetDatabase.AddObjectToAsset(newLayer.stateMachine, AssetDatabase.GetAssetPath(refObjects.refAnimController));
        refObjects.refAnimController.AddLayer(newLayer);
    }

    private void MakeVRCParameter()
    {
        int ogparamlength = refObjects.vrcParam.parameters.Length;
        int newitemlength = toggleObjects.Count;
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
        }
        else
        {
            Debug.LogError("Adding parameters would go past the limit of " + VRCExpressionParameters.MAX_PARAMETER_COST + ". No parameters added.");
        }
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
                if (refObjects.vrcMenu.controls.Count < maxmenuitems)
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
    }


    private void checkSaveDir()
    {
        if (!Directory.Exists(refObjects.saveDir))
        {
            Directory.CreateDirectory(refObjects.saveDir);
        }
    }
    private void validateGameObjectList()
    {
        for (var i = toggleObjects.Count - 1; i >= 0; i--)
        {
            if (toggleObjects[i] == null)
                toggleObjects.RemoveAt(i);
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

}

#endif
