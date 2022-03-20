using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using System;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;

namespace AutoToggleCreator.Util
{
    public static class ATCAnimUtils
    {
        public static void makeGameObjectToggleCurve(ref AnimationClip clip, string gameobjectpath, bool enable)
        {
            float enablefloat = Convert.ToInt32(enable);
            clip.SetCurve(gameobjectpath, typeof(GameObject), "m_IsActive", TwoKeyframesWithValue(enablefloat));
        }
        public static void addBlendShapeAnimCurves(ref AnimationClip clip, string gameobjectpath, string blendshape, float value)
        {
            clip.SetCurve(gameobjectpath, typeof(SkinnedMeshRenderer), blendshape, TwoKeyframesWithValue(value));
        }
        public static AnimationCurve TwoKeyframesWithValue(float value)
        {
            return new AnimationCurve(new Keyframe(0, value), new Keyframe(1 / 60f, value));
        }
        public static VRCAvatarParameterDriver MakeVRCParameterSetDriver((string toggleName, float toggleValue)[] paramAndBool, string assetContainerPath)
        {
            VRCAvatarParameterDriver parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            parameterDriver.name = "VRC ParameterDriver";
            foreach (var parameter in paramAndBool)
            {
                VRCAvatarParameterDriver.Parameter driverParameter = new VRCAvatarParameterDriver.Parameter
                {
                    name = parameter.toggleName,
                    type = VRCAvatarParameterDriver.ChangeType.Set,
                    value = parameter.toggleValue,
                };
                parameterDriver.parameters.Add(driverParameter);
            }
            parameterDriver.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(parameterDriver, assetContainerPath);
            return parameterDriver;
        }
        public static AnimatorCondition MakeIfTrueCondition(string boolParameterName)
        {
            AnimatorCondition animatorCondition = new AnimatorCondition
            {
                parameter = boolParameterName,
                mode = AnimatorConditionMode.If,
                threshold = 0
            };
            return animatorCondition;
        }
        public static AnimatorCondition MakeIfFalseCondition(string boolParameterName)
        {
            AnimatorCondition animatorCondition = new AnimatorCondition
            {
                parameter = boolParameterName,
                mode = AnimatorConditionMode.IfNot,
                threshold = 0
            };
            return animatorCondition;
        }

        public static void MakeTransition(AnimatorState sourceState, AnimatorState destinationState, string transitionName, AnimatorCondition[] conditions, string assetContainerPath)
        {
            AnimatorStateTransition newTransition = new AnimatorStateTransition
            {
                name = transitionName,
                hasExitTime = false,
                exitTime = 0,
                offset = 0,
                hasFixedDuration = true,
                duration = 0.1f,
                destinationState = destinationState,
                hideFlags = HideFlags.HideInHierarchy,
                conditions = conditions
            };
            sourceState.AddTransition(newTransition);
            AssetDatabase.AddObjectToAsset(newTransition, assetContainerPath);
        }
        public static void AddLayerWithWeight(string layerName, float weightWhenCreating, ref AnimatorController controller, string assetContainerPath)
        {
            var layerUniqueName = controller.MakeUniqueLayerName(layerName);
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
            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, assetContainerPath);
            controller.AddLayer(newLayer);
        }
    }
}

#endif