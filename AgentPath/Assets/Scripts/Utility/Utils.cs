using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace CR
{
    public static class Utils
    {
        public delegate void VoidDelegate(GameObject go);
        public delegate void BoolDelegate(GameObject go, bool state);
        public delegate void FloatDelegate(GameObject go, float delta);
        public delegate void VectorDelegate(GameObject go, Vector2 delta);
        public delegate void ObjectDelegate(GameObject go, GameObject obj);
        public delegate void KeyCodeDelegate(GameObject go, KeyCode key);

        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();

            var dst = destination.GetComponent(type) as T;
            if (!dst) dst = destination.AddComponent(type) as T;

            var fields = GetAllFields(type);
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(original));
            }

            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
                prop.SetValue(dst, prop.GetValue(original, null), null);
            }

            return dst as T;
        }

        public static IEnumerable<FieldInfo> GetAllFields(System.Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<FieldInfo>();
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
        }

        public static string GetHierarchyPath(Transform current)
        {
            if (current == null)
            {
                return "";
            }

            if (current.parent == null)
                return current.name;
            return GetHierarchyPath(current.parent) + "/" + current.name;
        }
        
        public static float GetEaseValue(float currentValue, float targetValue, float speed, float dt, ref bool isFinished)
        {
            isFinished = false;
            if (currentValue < targetValue)
            {
                currentValue += speed * dt;
                if (currentValue >= targetValue)
                {
                    isFinished = true;
                    return targetValue;
                }
                return currentValue;
            }
            else if (currentValue > targetValue)
            {
                currentValue -= speed * dt;
                if (currentValue <= targetValue)
                {
                    isFinished = true;
                    return targetValue;
                }
                return currentValue;
            }
            isFinished = true;
            return currentValue;
        }

        public static float SmoothStep(float a, float b, float x)
        {
            if (a < b)
            {
                if (x <= a)
                {
                    return 0f;
                }
                if (x >= b)
                {
                    return 1f;
                }
                float y = (x - a) / (b - a);
                return y * y * (3 - 2 * y);
            }
            else if (a > b)
            {
                if (x >= a)
                {
                    return 0f;
                }
                if (x <= b)
                {
                    return 1;
                }
                float y = (a - x) / (a - b);
                return y * y * (3 - 2 * y);
            }
            if (x < a)
            {
                return 0;
            }
            else if (x >= a)
            {
                return 1;
            }
            return 0f;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        public static float Repeat(float t, float length)
        {
            return Clamp(t - Mathf.Floor(t / length) * length, 0.0f, length);
        }

        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Repeat((b - a), 360);
            if (delta > 180)
                delta -= 360;
            return a + delta * Mathf.Clamp01(t);
        }

        public static float LerpAngle(float a, float b, float t, bool increase)
        {
            a = a % 360f;
            b = b % 360f;

            float res = a;
            if (increase)
            {
                if (a < b)
                {
                    res = Mathf.Lerp(a, b, t);
                }
                else if (a > b)
                {
                    b = b + 360f;
                    res = Mathf.Lerp(a, b, t);
                }
            }    
            else
            {
                if (a > b)
                {
                    res = Mathf.Lerp(a, b, t);
                }
                else
                {
                    b = b - 360f;
                    res = Mathf.Lerp(a, b, t);
                }
            }

            return res;
        }


        public static float LerpAngle(float a, float b, float byPass, float lerpStrength)
        {
            //a = Repeat(a, 360f);
            //b = Repeat(b, 360f);
            //byPass = Repeat(byPass, 360f);
            //float deltaAB = Mathf.DeltaAngle(a, b);
            //if (Mathf.Abs(deltaAB) > 150f)
            //{
            //    float deltaA = Mathf.DeltaAngle(a, byPass);
            //    if (deltaA > 0f)
            //    {
            //        if (a <= b)
            //        {
            //            return LerpAngle(a, b, lerpStrength);
            //        }
            //        else
            //        {
            //            a = a - 360f;
            //            return Mathf.Lerp(a, b, lerpStrength);
            //                //LerpAngle(a, b, lerpStrength);
            //        }
            //    }
            //    else if (deltaA < 0f)
            //    {
            //        if (a >= b)
            //        {
            //            return LerpAngle(a, b, lerpStrength);
            //        }
            //        else
            //        {
            //            a += 360f;
            //            //return LerpAngle(a, b, lerpStrength);
            //            return Mathf.Lerp(a, b, lerpStrength);
            //        }
            //    }
            //}
            return LerpAngle(a, b, lerpStrength);
        }

        public static Transform CreateTransform(string name, Transform parent)
        {
            GameObject obj = CreateGameObject(name, parent);
            return obj.transform;
        }

        public static GameObject CreateGameObjectIfNeeded(string name, Transform parent)
        {
            if (parent == null)
            {
                return CreateGameObject(name, parent);
            }

            var trans = parent.Find(name);
            if (trans == null)
            {
                return CreateGameObject(name, parent);
            }
            return trans.gameObject;
        }

        public static Transform CreateOrGetChild(string name, Transform parent)
        {
            Transform res = parent.Find(name);
            if (res == null)
            {
                res = CreateGameObject(name, parent).transform;
            }
            return res;
        }

        public static GameObject CreateGameObject(string name, Transform parent)
        {
            GameObject obj = new GameObject();
            obj.name = name;
            Transform trans = obj.transform;
            if (parent != null)
            {
                trans.parent = parent;
            }
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
            return obj;
        }

        public static T GetOrCreateComponent<T>(Transform trans) where T : MonoBehaviour
        {
            T t = trans.GetComponent<T>();
            if (t == null)
            {
                t = trans.gameObject.AddComponent<T>();
            }
            return t;
        }

        static public float EaseOutSine(float currentTime, float startValue, float changeValue, float duration)
        {
            float res = changeValue * Mathf.Sin(currentTime / duration * (Mathf.PI / 2)) + startValue;
            return res;
        }

        static public float EaseExpoOut(float currentTime, float startValue, float changeValue, float duration)
        {
            float time = currentTime / duration;
            float rate = time == 1 ? 1 : (-Mathf.Pow(2, -10 * time / 1) + 1);
            float res = rate * changeValue + startValue;
            return res;
        }

        public static bool Approximately(this Vector3 me, Vector3 other, float allowedDifference)
        {
            var dx = me.x - other.x;
            if (dx < -allowedDifference || dx > allowedDifference)
            {
                return false;
            }

            var dy = me.y - other.y;
            if (dy < -allowedDifference || dy > allowedDifference)
            {
                return false;
            }

            var dz = me.z - other.z;

            return (dz >= -allowedDifference) && (dz <= allowedDifference);
        }

        public static float Smooth(float value)
        {
            return value * value * (3 - 2 * value);
        }


        public static float SmoothExt(float value)
        {
            return 6 * Mathf.Pow(value, 5) - 15 * Mathf.Pow(value, 4) + 10 * Mathf.Pow(value, 3);
        }

        public static float SmoothLerp(float dt, float lerpStrength)
        {
            return 1 - Mathf.Exp(-lerpStrength * dt);
        }

        /*
        public static void SetEventTrigger(Transform tran, MonoBehaviour target, string callbackName)
        {
            if (tran == null || target == null)
            {
                return;
            }
            UIEventTrigger evtListener = tran.GetComponent<UIEventTrigger>();
            if (evtListener == null)
            {
                evtListener = tran.gameObject.AddComponent<UIEventTrigger>();
            }

            EventDelegate evt = new EventDelegate(target, callbackName);
            evtListener.onClick.Add(evt);
        }


        public static void SetEventTrigger(Transform tran, MonoBehaviour target, string callbackName, int tag)
        {
            if (tran == null || target == null)
            {
                return;
            }
            UIEventTrigger evtListener = tran.GetComponent<UIEventTrigger>();
            if (evtListener == null)
            {
                evtListener = tran.gameObject.AddComponent<UIEventTrigger>();
            }

            EventDelegate evt = new EventDelegate(target, callbackName);
            evt.parameters[0] = new EventDelegate.Parameter(tag);
            evtListener.onClick.Add(evt);
        }
        */

        public static void SetGameLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;

            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                child.gameObject.layer = layer;
                SetGameLayerRecursive(child.gameObject, layer);
            }
        }

        
        #region Hierarchies
        /*
        public static void AddAttachment(Transform root, GameObject prefab, List<SkinnedMeshRenderer> smrList)
        {
            if (prefab == null || root == null)
            {
                return;
            }

            GameObject obj = GameObject.Instantiate(prefab);
            SkinnedMeshRenderer[] smrs = obj.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (smrList != null)
            {
                smrList.AddRange(smrs);
            }
            Transform targetRoot = root;
            SkinnedMeshRenderer smr = null;
            for (int i = 0; i < smrs.Length; i++)
            {
                smr = smrs[i];

                smr.gameObject.layer = LayerTools.GetMainCharLayer();

                if (smr.transform.parent == obj.transform)
                {
                    smr.transform.parent = targetRoot;
                }
                else
                {
                    Transform newSmrParent = GetTransByNameInChildren(targetRoot, smr.transform.parent.name);
                    if (newSmrParent == null)
                    {
                        smr.transform.parent = targetRoot;
                    }
                    else
                    {
                        smr.transform.parent = newSmrParent;
                    }
                }

                bool hasBoneError = false;
                List<Transform> newBoneList = new List<Transform>();
                for (int n = 0; n < smr.bones.Length; n++)
                {
                    if (smr.bones[n].name == "pelvis")
                    {
                        Debug.Log("break");
                    }

                    Transform newBone = GetTransByNameInChildren(targetRoot, smr.bones[n].name);
                    if (newBone == null)
                    {
                        Debug.Log("bone not found " + smr.bones[n].name);

                        List<Transform> hierachies = new List<Transform>();
                        GetTransHierarchyList(hierachies, obj.transform, smr.bones[n]);
                        newBone = CreateTransByHierarchy(hierachies, targetRoot);
                        if (newBone == null)
                        {
                            hasBoneError = true;
                        }
                        else
                        {
                            newBoneList.Add(newBone);
                        }
                    }
                    else
                    {
                        newBoneList.Add(newBone);
                    }
                }
                if (hasBoneError)
                {
                    Debug.LogError("smr has bones not found");
                    SafeDestroy(smr.gameObject);
                }
                else
                {
                    smr.bones = newBoneList.ToArray();

                    if (smr.rootBone != null)
                    {
                        smr.rootBone = GetTransByNameInChildren(targetRoot, smr.rootBone.name);
                    }
                }
            }
            SafeDestroy(obj);
        }
        */

        public static void SafeDestroy(GameObject obj)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(obj);
            }
            else
            {
                GameObject.DestroyImmediate(obj);
            }
        }

        public static Transform CreateTransByHierarchy(List<Transform> transList, Transform root)
        {
            Transform temp = root;
            for (int i = 0; i < transList.Count; i++)
            {
                temp = Utils.CreateOrGetChild(transList[i].name, temp);
                temp.transform.localPosition = transList[i].localPosition;
                temp.transform.localRotation = transList[i].localRotation;
                temp.transform.localScale = transList[i].localScale;
            }
            return temp;
        }

        public static void GetTransHierarchyList(List<Transform> transList, Transform root, Transform childTrans)
        {
            Transform temp = childTrans;
            while (temp != null)
            {
                transList.Add(temp);
                temp = temp.parent;
                if (temp == root || temp == null)
                {
                    break;
                }
            }
            transList.Reverse();
        }

        public static Transform GetTransByNameInChildren(Transform parent, string name)
        {

            if (parent.name == name)
                return parent;

            Transform result = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    result = child;
                    break;
                }
                else
                {
                    result = GetTransByNameInChildren(child, name);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            return result;
        }
        #endregion
    }
}
