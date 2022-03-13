using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using static VRC.SDKBase.VRC_AvatarParameterDriver;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
public class AutoToggleCreator : EditorWindow
{
    public GameObject[] toggleObjects;
    public Animator myAnimator;
    AnimatorController controller;
    VRCExpressionParameters vrcParam;
    VRCExpressionsMenu vrcMenu;
    static bool parameterSave;
    static bool defaultOn;
    public string saveDir;
    bool pressCreate = false;

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
            if (Selection.activeTransform.GetComponent<Animator>() == null) { return; }
            Transform SelectedObj = Selection.activeTransform;
            myAnimator = SelectedObj.GetComponent<Animator>();
            controller = (AnimatorController)SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController;
            vrcParam = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionParameters;
            vrcMenu = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionsMenu;
        }
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical();
        //Avatar Animator
        GUILayout.Label("AVATAR ANIMATOR", EditorStyles.boldLabel);
        myAnimator = (Animator)EditorGUILayout.ObjectField(myAnimator, typeof(Animator), true, GUILayout.Height(20f));
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        //FX Animator Controller
        GUILayout.Label("FX AVATAR CONTROLLER", EditorStyles.boldLabel);
        controller = (AnimatorController)EditorGUILayout.ObjectField(controller, typeof(AnimatorController), true, GUILayout.Height(20f));
        EditorGUILayout.EndVertical();
;
        EditorGUILayout.BeginVertical();
        //VRCExpressionParameters
        GUILayout.Label("VRC EXPRESSION PARAMETERS", EditorStyles.boldLabel);
        vrcParam = (VRCExpressionParameters)EditorGUILayout.ObjectField(vrcParam, typeof(VRCExpressionParameters), true, GUILayout.Height(20f));
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        //VRCExpressionMenu
        GUILayout.Label("VRC EXPRESISON MENU", EditorStyles.boldLabel);
        vrcMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(vrcMenu, typeof(VRCExpressionsMenu), true, GUILayout.Height(20f));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        EditorGUILayout.BeginHorizontal();
        //Toggle to save VRCParameter values
        parameterSave = (bool)EditorGUILayout.ToggleLeft("Set VRC Parameters to save state?", parameterSave, EditorStyles.boldLabel);

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10f);

        //Toggle Object List
        GUILayout.Label("Objects to Toggle On and Off:", EditorStyles.boldLabel);
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty toggleObjectsProperty = so.FindProperty("toggleObjects");
        EditorGUILayout.PropertyField(toggleObjectsProperty, true);
        GUILayout.Space(10f);

        GUILayout.Label("Needs all items filled to proceed.", EditorStyles.helpBox);

        using (new EditorGUI.DisabledScope((myAnimator && controller && vrcParam && vrcMenu) != true))
        {
            pressCreate = GUILayout.Button("Create Toggles!", GUILayout.Height(40f));
        }
        
        if (pressCreate)
        {
            setSaveDir(); //Sets the save directory
            CreateClips(); //Creates the Animation Clips needed for toggles.
            ApplyToAnimator(); //Handles making toggle bool property, layer setup, states and transitions.
            MakeVRCParameter(); //Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
            MakeVRCMenu(); //Adds toggles to menu
            Postprocessing();
        }
        so.ApplyModifiedProperties();
    }

    private void CreateClips()
    {
        foreach (GameObject gameObject in toggleObjects)
        {
            //Make animation clips for on and off state and set curves for game objects on and off
            AnimationClip toggleClipOn = new AnimationClip(); //Clip for ON

            toggleClipOn.SetCurve
                (GetGameObjectPath(gameObject.transform).Substring(myAnimator.gameObject.name.Length + 1),
                typeof(GameObject),
                "m_IsActive",
                new AnimationCurve(new Keyframe(0, 1, 0, 0),
                new Keyframe(0.016666668f, 1, 0, 0))
                );

            AnimationClip toggleClipOff = new AnimationClip(); //Clip for OFF

            toggleClipOff.SetCurve
                (GetGameObjectPath(gameObject.transform).Substring(myAnimator.gameObject.name.Length + 1),
                typeof(GameObject),
                "m_IsActive",
                new AnimationCurve(new Keyframe(0, 0, 0, 0),
                new Keyframe(0.016666668f, 0, 0, 0))
                );

            //Save on animation clips (Off should not be needed?)
            AssetDatabase.CreateAsset(toggleClipOn, saveDir + $"{gameObject.name}-On.anim");
            AssetDatabase.CreateAsset(toggleClipOff, saveDir + $"{gameObject.name}-Off.anim");
            AssetDatabase.SaveAssets();
        }
    }

    private void ApplyToAnimator()
    {
        bool initParamExist = doesNameExistParam("TrackingType", controller.parameters);
        //Check if a parameter already exists with that name. If so, Ignore adding parameter.
        if (initParamExist == false)
        {
            controller.AddParameter("TrackingType", AnimatorControllerParameterType.Int);
        }
        foreach (GameObject gameObject in toggleObjects)
        {
            bool existParam = doesNameExistParam(gameObject.name + "Toggle", controller.parameters);
            //Check if a parameter already exists with that name. If so, Ignore adding parameter.
            if (existParam == false)
            {
                controller.AddParameter(gameObject.name + "Toggle", AnimatorControllerParameterType.Bool);
            }

            //Check if a layer already exists with that name. If so, Ignore adding layer.
            AnimatorControllerLayer currentLayer = FindAnimationLayer(controller, gameObject.name);
            if (currentLayer == null)
            {
                AnimatorControllerLayer layer = new AnimatorControllerLayer
                {
                    name = gameObject.name.Replace(".", "_"),
                    defaultWeight = 1f,
                    stateMachine = new AnimatorStateMachine() // Make sure to create a StateMachine as well, as a default one is not created
                };
                controller.AddLayer(layer);
                currentLayer = FindAnimationLayer(controller, gameObject.name);

                //Create a state that can wait for init (prevents toggles being on/off when someone loads their avatar)
                AnimatorState Idle = new AnimatorState();
                Idle.name = "Idle-WaitForInit";
                Idle.writeDefaultValues = false;

                //Creating On and Off(Empty) states
                AnimatorState stateOn = new AnimatorState();
                stateOn.name = $"{gameObject.name} On";
                stateOn.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + $"/{gameObject.name}-On.anim", typeof(Motion));
                stateOn.writeDefaultValues = false;

                AnimatorState stateOff = new AnimatorState();
                stateOff.name = $"{gameObject.name} Off";
                stateOff.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + $"/{gameObject.name}-Off.anim", typeof(Motion));
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
    }

    private void MakeVRCParameter()
    {
        int ogparamlength;
        int newitemlength;
        ogparamlength = vrcParam.parameters.Length;
        newitemlength = toggleObjects.Length;

        VRCExpressionParameters.Parameter[] newListFull = new VRCExpressionParameters.Parameter[ogparamlength + newitemlength];
        

        //Add parameters that were already on the SO
        for (int i = 0; i < vrcParam.parameters.Length; i++)
        {
            newListFull[i] = vrcParam.parameters[i];
        }

        int counter = ogparamlength;
        int nullcounter = 0;
        bool same = false;
        foreach (GameObject gameObject in toggleObjects)
        {
            //Make new parameter to add to list
            VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter();

            //Modify parameter according to user settings and object name
            newParam.name = gameObject.name + "Toggle";
            newParam.valueType = VRCExpressionParameters.ValueType.Bool;
            newParam.defaultValue = 0;

            //Check to see if parameter is saved
            if (parameterSave == true) { newParam.saved = true; } else { newParam.saved = false; }
            same = doesNameExistVRCParam(newParam.name, newListFull);
            if (same == false)
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

        vrcParam.parameters = finalNewList;
    }

    private void MakeVRCMenu()
    {
        bool menutoggle = false;
        for (int i = 0; i < toggleObjects.Length; i++)
        {
            VRCExpressionsMenu.Control controlItem = new VRCExpressionsMenu.Control();

            controlItem.name = toggleObjects[i].name;
            controlItem.type = VRCExpressionsMenu.Control.ControlType.Toggle;
            controlItem.parameter = new VRCExpressionsMenu.Control.Parameter();
            controlItem.parameter.name = toggleObjects[i].name + "Toggle";
            menutoggle = false;
            for (int j = 0; j < vrcMenu.controls.Count; j++)
            {
                if (vrcMenu.controls[j].name == controlItem.parameter.name)
                {
                    menutoggle = true;
                }
            }

            if (menutoggle == false)
            {
                vrcMenu.controls.Add(controlItem);
            }
        }

    }

    private void setSaveDir()
    {
        string controllerDir;

        controllerDir = AssetDatabase.GetAssetPath(controller);
        controllerDir = controllerDir.Substring(0, controllerDir.Length - controller.name.Length - 11);
        saveDir = controllerDir + "ToggleAnimations/";
        //Check to see if path exists. If not, create it.
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }
    }

    private void Postprocessing()
    {
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(vrcParam);
        EditorUtility.SetDirty(vrcMenu);
        AssetDatabase.SaveAssets();
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
    public AnimatorControllerLayer FindAnimationLayer(AnimatorController controller, string name)
    {
        //controller.layers[0].name;
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (layer.name == name)
            {
                return layer;
            }
        }
        return null;
    }
}

public struct ToggleObjects
{
    public GameObject ToggleObject;
    public bool saveParameter;
}

#endif
