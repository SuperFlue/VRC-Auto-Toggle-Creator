using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;

namespace AutoToggleCreator.Util
{
    public static class ATCUtils
    {
        public static List<ObjectListConfig> MakeStateConfigList(List<GameObject> toggleObjects)
        {
            List<ObjectListConfig> list = new List<ObjectListConfig>();
            foreach (GameObject gameobj in toggleObjects)
            {
                list.Add(new ObjectListConfig(gameobj));

            }
            return list;
        }
        public static void checkSaveDir(string saveDir)
        {
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
        }
        public static void validateGameObjectList(ref List<GameObject> toggleObjects)
        {
            for (var i = toggleObjects.Count - 1; i >= 0; i--)
            {
                if (toggleObjects[i] == null)
                    toggleObjects.RemoveAt(i);
            }
        }

        public static bool doesNameExistParam(string name, AnimatorControllerParameter[] array)
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
        public static bool doesNameExistVRCParam(string name, VRCExpressionParameters.Parameter[] array)
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
        public static bool doesNameExistVRCMenu(string name, List<VRCExpressionsMenu.Control> controls)
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

        public static string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
        public static AnimatorControllerLayer FindAnimationLayer(string name, ref AnimatorController controller)
        {
            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                if (layer.name == name)
                {
                    return layer;
                }
            }
            return null;
        }
        public static int FindAnimationLayerIndex(string name, AnimatorController controller)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == name)
                {
                    return i;
                }
            }
            return 0;
        }

        public static bool vrcParamSanityCheck(VRCExpressionParameters.Parameter[] parameters)
        {
            VRCExpressionParameters vrcParamSanitycheck = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            vrcParamSanitycheck.parameters = parameters;
            int newcost = vrcParamSanitycheck.CalcTotalCost();
            if (newcost < VRCExpressionParameters.MAX_PARAMETER_COST)
            {
                return true;
            }
            return false;
        }
        public static List<BlendShapeListItem> getBlendShapes(SkinnedMeshRenderer skinnedMesh)
        {
            List<BlendShapeListItem> blendShapeList = new List<BlendShapeListItem>();
            string meshName = skinnedMesh.name;
            int blendShapeCount = skinnedMesh.sharedMesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; i++)
            {
                blendShapeList.Add(new BlendShapeListItem(meshName, skinnedMesh.sharedMesh.GetBlendShapeName(i),0));
            }
            
            return blendShapeList;
        }
    }
    public class BlendShapeListItem
    {
        public string skinnedMeshName;
        public string blendshapeName;
        public float value;
        public BlendShapeListItem(string meshName, string blendshapename)
        {
            skinnedMeshName = meshName;
            blendshapeName = blendshapename;
            value = 0;
        }
        public BlendShapeListItem(string meshName, string blendshapename, float setvalue)
        {
            skinnedMeshName = meshName;
            blendshapeName = blendshapename;
            value = setvalue;
        }
    }
    public class ObjectListConfig
    {
        public GameObject gameobject;
        public string objname;
        public string paramname;
        public string layername;
        public string menuname;
        public string gameobjectpath;
        static readonly string suffix;
        static readonly string paramprefix;
        static readonly string layerprefix;
        static readonly string menuprefix;

        static ObjectListConfig()
        {
            paramprefix = "";
            layerprefix = "";
            menuprefix = "";
            suffix = "Toggle";
        }

        public ObjectListConfig(GameObject gameobject)
        {
            this.gameobject = gameobject;
            this.objname = gameobject.name;
            this.paramname = $"{paramprefix}{objname}{suffix}";
            this.layername = $"{layerprefix}{objname.Replace(".", "_")}";
            this.menuname = $"{menuprefix}{objname} {suffix}";
            this.gameobjectpath = GetGameObjectRelativePath(this.gameobject.transform);
        }
        private string GetGameObjectRelativePath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            path = path.Substring(this.objname.Length + 1);
            return path;
        }
    }
}

#endif