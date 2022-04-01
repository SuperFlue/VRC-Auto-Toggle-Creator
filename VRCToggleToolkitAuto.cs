using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
using VRCToggleToolkit.Util;

namespace VRCToggleToolkit.Main
{
    public static class ReferenceObjects
    {
        public static GameObject refGameObject;
        public static AnimatorController refAnimController;
        public static VRCExpressionParameters vrcParam;
        public static VRCExpressionsMenu vrcMenu;
        public static string saveDir;
        public static string assetContainerPath;

        public static void generateSavePath()
        {
            string controllerpath;
            if (refAnimController != null)
            {
                assetContainerPath = AssetDatabase.GetAssetPath(refAnimController);
                controllerpath = assetContainerPath.Substring(0, assetContainerPath.Length - refAnimController.name.Length - 11);
                saveDir = controllerpath + Settings.saveSubfolder + "/";
            }
        }
    }
    public static class Settings
    {
        public static bool showAdvanced = false;
        public static bool showAdvancedDangerous = false;
        public static bool parameterSave = false;
        public static bool writeDefaults = false;
        public static bool createExclusiveToggle = false;
        public static bool createFallbackState = false;
        public static bool recreateLayers = false;
        public static string saveSubfolder = "ToggleAnimations";
        public static string paramprefix = "";
        public static string layerprefix = "";
        public static string menuprefix = "";
    }

    public static class ATCCustomStyle
    {
        public static GUIStyle redLabel = new GUIStyle(EditorStyles.label);

        static ATCCustomStyle()
        {
            redLabel.normal.textColor = Color.red;

        }
    }

    public class VRCToggleToolkitAuto : EditorWindow
    {
        public List<GameObject> toggleObjects = new List<GameObject>();
        private List<ObjectListConfig> objectList = new List<ObjectListConfig>();
        private bool disableAutoFill;

        [MenuItem("Tools/VRCToogleToolkit/AutoToogleCreator")]

        static void Init()
        {
            // Get existing open window or if none, make a new one:
            VRCToggleToolkitAuto window = (VRCToggleToolkitAuto)GetWindow(typeof(VRCToggleToolkitAuto));
            window.titleContent = new GUIContent("AutoToggleCreator");
            window.Show();

        }

        void OnSelectionChange()
        {
            Repaint();
        }

        public void OnGUI()
        {
            EditorGUILayout.Space(10);

            if (Selection.activeTransform != null)
            {
                disableAutoFill = Selection.activeTransform.GetComponent<VRCAvatarDescriptor>() == null;
            }
            else
            {
                disableAutoFill = true;
            }
            using (new EditorGUI.DisabledScope(disableAutoFill))
            {
                if (GUILayout.Button("Auto-Fill with Selected Avatar", GUILayout.Height(30f)))
                {
                    
                    Transform SelectedObj = Selection.activeTransform;
                    ReferenceObjects.refGameObject = SelectedObj.gameObject;
                    ReferenceObjects.refAnimController = (AnimatorController)SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController;
                    ReferenceObjects.vrcParam = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionParameters;
                    ReferenceObjects.vrcMenu = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionsMenu;

                }
            }
            if (disableAutoFill)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Selected object requires a VRCAvatarDescriptor for auto-fill.", EditorStyles.label);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            //Avatar Animator
            GUILayout.Label("Animation base reference GameObject", EditorStyles.boldLabel);
            ReferenceObjects.refGameObject = (GameObject)EditorGUILayout.ObjectField(ReferenceObjects.refGameObject, typeof(GameObject), true, GUILayout.Height(20f));

            //FX Animator Controller
            GUILayout.Label("FX Animation Controller", EditorStyles.boldLabel);
            ReferenceObjects.refAnimController = (AnimatorController)EditorGUILayout.ObjectField(ReferenceObjects.refAnimController, typeof(AnimatorController), true, GUILayout.Height(20f));

            //VRCExpressionParameters
            GUILayout.Label("VRC Expression Parameters", EditorStyles.boldLabel);
            ReferenceObjects.vrcParam = (VRCExpressionParameters)EditorGUILayout.ObjectField(ReferenceObjects.vrcParam, typeof(VRCExpressionParameters), true, GUILayout.Height(20f));

            //VRCExpressionMenu
            GUILayout.Label("VRC Expressions Menu", EditorStyles.boldLabel);
            ReferenceObjects.vrcMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(ReferenceObjects.vrcMenu, typeof(VRCExpressionsMenu), true, GUILayout.Height(20f));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            //Toggle Object List
            GUILayout.Label("Objects create toggles for:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add slot"))
            {
                toggleObjects.Add(null);
            }
            if (GUILayout.Button("Remove slot"))
            {
                toggleObjects.RemoveAt(toggleObjects.Count - 1);
            }
            EditorGUILayout.EndHorizontal();

            ScriptableObject target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty toggleObjectsProperty = so.FindProperty("toggleObjects");
            EditorGUILayout.PropertyField(toggleObjectsProperty, true);


            GUILayout.Space(15f);
            GUILayout.Label($"Toggles will be written to:\n{ReferenceObjects.saveDir}\nThis can be changed under Advanced settings.", EditorStyles.helpBox);

            GUILayout.Space(10f);
            if (ReferenceObjects.refAnimController != true)
            {
                GUILayout.Label("Minimum reqires a animation controller to proceed", ATCCustomStyle.redLabel);
            }

            using (new EditorGUI.DisabledScope(ReferenceObjects.refAnimController != true))
            {
                //pressCreate = GUILayout.Button("Create Toggles!", GUILayout.Height(40f));
                if (GUILayout.Button("Create Toggles!", GUILayout.Height(40f)))
                {
                    VRCToggleToolkitUtils.checkSaveDir(ReferenceObjects.saveDir); //Checks that the save directory exists or creates it if not
                    VRCToggleToolkitUtils.validateGameObjectList(ref toggleObjects); //Ruimentary way to remove empty gameobject from the toggles list to prevent issues.
                    objectList = VRCToggleToolkitUtils.MakeStateConfigList(toggleObjects);
                    if (ReferenceObjects.refGameObject != null)
                    {
                        CreateClips(); //Creates the Animation Clips needed for toggles.
                        CreateNullClip(); // Creates an Animation Clip with an invalid path needed for stability. 
                    }
                    if (ReferenceObjects.refAnimController != null)
                    {
                        ApplyToAnimator(); //Handles making toggle bool property, layer setup, states and transitions.
                        if (Settings.createExclusiveToggle)
                        {
                            ExclusiveStateConfig();
                        }
                        if (Settings.createFallbackState)
                        {
                            FallBackCreator();
                        }
                    }
                    if (ReferenceObjects.vrcParam != null)
                    {
                        MakeVRCParameter(); //Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
                    }
                    if (ReferenceObjects.vrcMenu != null)
                    {
                        MakeVRCMenu(); //Adds toggles to menu
                    }
                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.Space(10f);
            GUILayout.BeginVertical();

            Settings.showAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(Settings.showAdvanced, "Advanced settings");
            if (Settings.showAdvanced)
            {
                GUILayout.Label("Output subdirectory (relative to controller):", EditorStyles.boldLabel);
                Settings.saveSubfolder = GUILayout.TextField(Settings.saveSubfolder, GUILayout.Height(20f));
                //Toggle to save VRCParameter values
                Settings.parameterSave = EditorGUILayout.ToggleLeft("Set VRC Parameters to save state?", Settings.parameterSave, EditorStyles.boldLabel);
                Settings.createExclusiveToggle = EditorGUILayout.ToggleLeft("Make the object toggles exclusive (only one active at the time)?", Settings.createExclusiveToggle, EditorStyles.boldLabel);
                Settings.createFallbackState = EditorGUILayout.ToggleLeft("Create fallback (if all toggles are off set first object active)?", Settings.createFallbackState, EditorStyles.boldLabel);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(10f);
            Settings.showAdvancedDangerous = EditorGUILayout.BeginFoldoutHeaderGroup(Settings.showAdvancedDangerous, "Dangerous settings");
            if (Settings.showAdvancedDangerous)
            {
                Settings.recreateLayers = EditorGUILayout.ToggleLeft("Recreate layers?", Settings.recreateLayers, EditorStyles.boldLabel);
                GUILayout.Label("WARNING: This will delete layers with the same name as the objects!", ATCCustomStyle.redLabel);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.EndVertical();

            ReferenceObjects.generateSavePath();
            so.ApplyModifiedProperties();

            GUILayout.TextArea("TIP!\nFor instructions, updates or to report issues, check out the GitHub!\nhttps://github.com/SuperFlue/VRC-Auto-Toggle-Creator", EditorStyles.helpBox);

        }

        private void CreateClips()
        {
            foreach (ObjectListConfig objectListItem in objectList)
            {
                //Make animation clips for on and off state and set curves for game objects on and off
                //Clip for ON
                AnimationClip toggleClipOn = new AnimationClip();
                VRCToggleToolkitAnimUtils.makeGameObjectToggleCurve(ref toggleClipOn, objectListItem.gameobjectpath, true);

                //Clip for OFF
                AnimationClip toggleClipOff = new AnimationClip();
                VRCToggleToolkitAnimUtils.makeGameObjectToggleCurve(ref toggleClipOff, objectListItem.gameobjectpath, false);

                //Save on animation clips
                AssetDatabase.CreateAsset(toggleClipOn, $"{ReferenceObjects.saveDir}/{objectListItem.objname}-On.anim");
                AssetDatabase.CreateAsset(toggleClipOff, $"{ReferenceObjects.saveDir}/{objectListItem.objname}-Off.anim");
                AssetDatabase.SaveAssets();
            }
        }

        private void CreateNullClip()
        {
            // Animation for the empty states. This is needed because a WD off state without an animation will behave like it is set with Write Defaults on.
            AnimationClip nullAnimation = new AnimationClip();
            nullAnimation.SetCurve("null_path/null", typeof(GameObject), "m_IsActive", AnimationCurve.Linear(0,0,0,1));
            AssetDatabase.CreateAsset(nullAnimation, $"{ReferenceObjects.saveDir}/nullAnimation.anim");
            AssetDatabase.SaveAssets();

        }



        private void ApplyToAnimator()
        {
            //Check if a parameter already exists with that name. If so, Ignore adding parameter.
            if (!VRCToggleToolkitUtils.doesNameExistParam("TrackingType", ReferenceObjects.refAnimController.parameters))
            {
                ReferenceObjects.refAnimController.AddParameter("TrackingType", AnimatorControllerParameterType.Int);
            }

            foreach (ObjectListConfig objectListItem in objectList)
            {
                if (!VRCToggleToolkitUtils.doesNameExistParam(objectListItem.paramname, ReferenceObjects.refAnimController.parameters))
                {
                    ReferenceObjects.refAnimController.AddParameter(objectListItem.paramname, AnimatorControllerParameterType.Bool);
                }

                //Check if a layer already exists with that name. If so, Ignore adding layer.
                if (Settings.recreateLayers == true)
                {
                    int layerIndex = VRCToggleToolkitUtils.FindAnimationLayerIndex(objectListItem.layername, ReferenceObjects.refAnimController);
                    if (layerIndex != 0)
                    {
                        ReferenceObjects.refAnimController.RemoveLayer(layerIndex);
                    }
                }
                AnimatorControllerLayer currentLayer = VRCToggleToolkitUtils.FindAnimationLayer(objectListItem.layername, ref ReferenceObjects.refAnimController);
                if (currentLayer == null)
                {
                    VRCToggleToolkitAnimUtils.AddLayerWithWeight(objectListItem.layername, 1f, ref ReferenceObjects.refAnimController, ReferenceObjects.assetContainerPath);
                    //currentLayer = FindAnimationLayer(objectListItem.layername);
                    currentLayer = VRCToggleToolkitUtils.FindAnimationLayer(objectListItem.layername, ref ReferenceObjects.refAnimController);

                    //Create our states first
                    //Create a wait for Init state (prevents toggles being on/off when someone loads their avatar)
                    AnimatorState Idle = new AnimatorState
                    {
                        name = currentLayer.stateMachine.MakeUniqueStateName("Idle-WaitForInit"),
                        motion = (Motion)AssetDatabase.LoadAssetAtPath($"{ReferenceObjects.saveDir}/nullAnimation.anim", typeof(Motion)),
                        writeDefaultValues = Settings.writeDefaults,
                        hideFlags = HideFlags.HideInHierarchy
                    };
                    currentLayer.stateMachine.AddState(Idle, new Vector3(260, 240, 0));
                    AssetDatabase.AddObjectToAsset(Idle, ReferenceObjects.assetContainerPath);


                    //Creating On state
                    AnimatorState stateOn = new AnimatorState
                    {
                        name = currentLayer.stateMachine.MakeUniqueStateName($"{objectListItem.objname} On"),
                        motion = (Motion)AssetDatabase.LoadAssetAtPath($"{ReferenceObjects.saveDir}/{objectListItem.objname}-On.anim", typeof(Motion)),
                        writeDefaultValues = Settings.writeDefaults,
                        hideFlags = HideFlags.HideInHierarchy
                    };
                    currentLayer.stateMachine.AddState(stateOn, new Vector3(260, 120, 0));
                    AssetDatabase.AddObjectToAsset(stateOn, ReferenceObjects.assetContainerPath);

                    //Create Off state
                    AnimatorState stateOff = new AnimatorState
                    {
                        name = currentLayer.stateMachine.MakeUniqueStateName($"{objectListItem.objname} Off"),
                        motion = (Motion)AssetDatabase.LoadAssetAtPath($"{ReferenceObjects.saveDir}/{objectListItem.objname}-Off.anim", typeof(Motion)),
                        writeDefaultValues = Settings.writeDefaults,
                        hideFlags = HideFlags.HideInHierarchy
                    };
                    currentLayer.stateMachine.AddState(stateOff, new Vector3(520, 120, 0));
                    AssetDatabase.AddObjectToAsset(stateOff, ReferenceObjects.assetContainerPath);


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
                    InitWaitOn[0] = VRCToggleToolkitAnimUtils.MakeIfTrueCondition(objectListItem.paramname);
                    InitWaitOn[1] = TrackingNot0;
                    VRCToggleToolkitAnimUtils.MakeTransition(currentLayer.stateMachine.states[0].state, currentLayer.stateMachine.states[1].state, "InitWait-OnState", InitWaitOn, ReferenceObjects.assetContainerPath);

                    // Add transition Init -> Off
                    AnimatorCondition[] InitWaitOff = new AnimatorCondition[2];
                    InitWaitOff[0] = VRCToggleToolkitAnimUtils.MakeIfFalseCondition(objectListItem.paramname);
                    InitWaitOff[1] = TrackingNot0;
                    VRCToggleToolkitAnimUtils.MakeTransition(currentLayer.stateMachine.states[0].state, currentLayer.stateMachine.states[2].state, "InitWait-OffState", InitWaitOff, ReferenceObjects.assetContainerPath);

                    //On <-> Off transitions
                    //Off -> On Transition
                    AnimatorCondition[] OffOnCondition = new AnimatorCondition[1];
                    OffOnCondition[0] = VRCToggleToolkitAnimUtils.MakeIfTrueCondition(objectListItem.paramname);
                    VRCToggleToolkitAnimUtils.MakeTransition(currentLayer.stateMachine.states[2].state, currentLayer.stateMachine.states[1].state, "Off->On", OffOnCondition, ReferenceObjects.assetContainerPath);

                    //Off -> On Transition
                    AnimatorCondition[] OnOffCondition = new AnimatorCondition[1];
                    OnOffCondition[0] = VRCToggleToolkitAnimUtils.MakeIfFalseCondition(objectListItem.paramname);
                    VRCToggleToolkitAnimUtils.MakeTransition(currentLayer.stateMachine.states[1].state, currentLayer.stateMachine.states[2].state, "On->Off", OnOffCondition, ReferenceObjects.assetContainerPath);

                }
            }

            EditorUtility.SetDirty(ReferenceObjects.refAnimController);
            AssetDatabase.SaveAssets();

        }
        private void ExclusiveStateConfig()
        {
            foreach (ObjectListConfig objectListItem in objectList)
            {
                AnimatorControllerLayer currentLayer = VRCToggleToolkitUtils.FindAnimationLayer(objectListItem.layername, ref ReferenceObjects.refAnimController);
                (string toggleName, float toggleValue)[] toggleList = new (string, float)[objectList.Count - 1];
                int i = 0;
                foreach (ObjectListConfig toggle in objectList)
                {
                    if (toggle.objname != objectListItem.objname)
                    {
                        toggleList[i].toggleName = toggle.paramname;
                        toggleList[i].toggleValue = 0;
                        i++;
                    }
                }
                StateMachineBehaviour[] machineBehaviours = new StateMachineBehaviour[1];
                machineBehaviours[0] = VRCToggleToolkitAnimUtils.MakeVRCParameterSetDriver(toggleList, ReferenceObjects.assetContainerPath);
                currentLayer.stateMachine.states[1].state.behaviours = machineBehaviours;
            }

            EditorUtility.SetDirty(ReferenceObjects.refAnimController);
            AssetDatabase.SaveAssets();
        }

        private void FallBackCreator()
        {

            AnimatorControllerLayer layerWithFallback = VRCToggleToolkitUtils.FindAnimationLayer(objectList[0].layername, ref ReferenceObjects.refAnimController);

            AnimatorState Fallback = new AnimatorState
            {
                name = layerWithFallback.stateMachine.MakeUniqueStateName("Fallback"),
                motion = (Motion)AssetDatabase.LoadAssetAtPath($"{ReferenceObjects.saveDir}/nullAnimation.anim", typeof(Motion)),
                writeDefaultValues = Settings.writeDefaults,
                hideFlags = HideFlags.HideInHierarchy
            };
            layerWithFallback.stateMachine.AddState(Fallback, new Vector3(260, 0, 0));
            AssetDatabase.AddObjectToAsset(Fallback, ReferenceObjects.assetContainerPath);

            AnimatorCondition[] offToFallbackState = new AnimatorCondition[objectList.Count];
            for (int i = 0; i < objectList.Count; i++)
            {
                offToFallbackState[i] = VRCToggleToolkitAnimUtils.MakeIfFalseCondition(objectList[i].paramname);
            }
            VRCToggleToolkitAnimUtils.MakeTransition(layerWithFallback.stateMachine.states[2].state, layerWithFallback.stateMachine.states[3].state, "Off -> Fallback", offToFallbackState, ReferenceObjects.assetContainerPath);

            // Fallback to On
            AnimatorCondition[] OnOffCondition = new AnimatorCondition[1];
            OnOffCondition[0] = VRCToggleToolkitAnimUtils.MakeIfTrueCondition(objectList[0].paramname);
            VRCToggleToolkitAnimUtils.MakeTransition(layerWithFallback.stateMachine.states[3].state, layerWithFallback.stateMachine.states[1].state, "Fallback -> On", OnOffCondition, ReferenceObjects.assetContainerPath);

            (string toggleName, float toggleValue)[] toggleList = new (string, float)[1];
            toggleList[0].toggleName = objectList[0].paramname;
            toggleList[0].toggleValue = 1;
            StateMachineBehaviour[] machineBehaviours = new StateMachineBehaviour[1];
            machineBehaviours[0] = VRCToggleToolkitAnimUtils.MakeVRCParameterSetDriver(toggleList, ReferenceObjects.assetContainerPath);
            layerWithFallback.stateMachine.states[3].state.behaviours = machineBehaviours;
            EditorUtility.SetDirty(ReferenceObjects.refAnimController);
            AssetDatabase.SaveAssets();
        }

        private void MakeVRCParameter()
        {
            int ogparamlength = ReferenceObjects.vrcParam.parameters.Length;
            int newitemlength = toggleObjects.Count;
            int counter = ogparamlength;
            int nullcounter = 0;

            VRCExpressionParameters.Parameter[] newListFull = new VRCExpressionParameters.Parameter[ogparamlength + newitemlength];

            //Add parameters that were already on the SO
            for (int i = 0; i < ReferenceObjects.vrcParam.parameters.Length; i++)
            {
                newListFull[i] = ReferenceObjects.vrcParam.parameters[i];
            }


            foreach (ObjectListConfig objectListItem in objectList)
            {
                //Make new parameter to add to list
                VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter
                {
                    //Modify parameter according to user settings and object name
                    name = objectListItem.paramname,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = 0
                };

                //Check to see if parameter is saved
                if (Settings.parameterSave == true) { newParam.saved = true; } else { newParam.saved = false; }
                if (!VRCToggleToolkitUtils.doesNameExistVRCParam(newParam.name, newListFull))
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

            if (VRCToggleToolkitUtils.vrcParamSanityCheck(finalNewList))
            {
                ReferenceObjects.vrcParam.parameters = finalNewList;
                EditorUtility.SetDirty(ReferenceObjects.vrcParam);
            }
            else
            {
                Debug.LogError("Adding parameters would go past the limit of " + VRCExpressionParameters.MAX_PARAMETER_COST + ". No parameters added.");
            }
        }

        private void MakeVRCMenu()
        {
            int maxVRCMenuItems = 8;
            foreach (ObjectListConfig objectListItem in objectList)
            {
                VRCExpressionsMenu.Control controlItem = new VRCExpressionsMenu.Control
                {
                    name = objectListItem.menuname,
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter()
                };
                controlItem.parameter.name = objectListItem.paramname;

                if (!VRCToggleToolkitUtils.doesNameExistVRCMenu(controlItem.name, ReferenceObjects.vrcMenu.controls))
                {
                    if (ReferenceObjects.vrcMenu.controls.Count < maxVRCMenuItems)
                    {
                        ReferenceObjects.vrcMenu.controls.Add(controlItem);
                    }
                    else
                    {
                        Debug.LogWarning($"Unable to add: {controlItem.name}. To menu: {ReferenceObjects.vrcMenu.name}. Too many entires! Max {maxVRCMenuItems} allowed!");
                    }
                }
            }
            EditorUtility.SetDirty(ReferenceObjects.vrcMenu);
        }
    }
}

#endif
