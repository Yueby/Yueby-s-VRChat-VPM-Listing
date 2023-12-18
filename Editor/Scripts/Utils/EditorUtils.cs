using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Yueby.Utils
{
    public class EditorUtils
    {
        public static string GetUtilParentDirectory()
        {
            return GetParentDirectory(GetParentDirectory(GetScriptDirectory(nameof(EditorUI))));
        }

        public static void FocusTarget(GameObject target)
        {
            Selection.activeGameObject = target;
            if (EditorWindow.HasOpenInstances<SceneView>())
            {
                EditorWindow.FocusWindowIfItsOpen<SceneView>();

                SceneView.FrameLastActiveSceneView();
            }
        }

        public static void PingProject(string path)
        {
            EditorUtility.FocusProjectWindow();
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = obj;
        }

        public static void PingProject(Object obj)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = obj;
        }

        public static async void PingObjectAndBack(GameObject target, GameObject current, int millionSeconds)
        {
            PingObject(target);
            await Task.Delay(millionSeconds);
            PingObject(current);
        }

        public static GameObject CreateGameObject(GameObject gameObject, Vector3 position, Quaternion rotation, Transform parent)
        {
            var go = Object.Instantiate(gameObject, parent);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = rotation;
            Undo.RegisterCreatedObjectUndo(go, "prefab");
            return go;
        }

        public static GameObject CreatePrefabAtPath(Transform parent, string path)
        {
            var prefab = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            Undo.RegisterCreatedObjectUndo(prefab, "prefab");

            prefab.transform.localScale = Vector3.one;
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.Euler(Vector3.zero);
            return prefab;
        }

        public static GameObject CreatePrefabAtPath(Transform parent, string path, Vector3 position)
        {
            var prefab = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            Undo.RegisterCreatedObjectUndo(prefab, "prefab");

            prefab.transform.localScale = Vector3.one;
            prefab.transform.localPosition = position;
            prefab.transform.localRotation = Quaternion.Euler(Vector3.zero);
            return prefab;
        }

        public static T[] FindChildDeleteOtherType<T>(Transform parent)
        {
            var list = new List<T>();
            var deleteList = new List<GameObject>();
            if (parent == null) return list.ToArray();

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var component = child.GetComponent<T>();
                if (component != null)
                    list.Add(component);
                else
                    deleteList.Add(child.gameObject);
            }

            foreach (var delete in deleteList)
            {
                Undo.RegisterFullObjectHierarchyUndo(delete, "deleteChild");
                Undo.DestroyObjectImmediate(delete);
            }

            return list.ToArray();
        }

        public static T[] FindChildByType<T>(Transform parent)
        {
            var list = new List<T>();
            if (parent == null) return list.ToArray();

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var component = child.GetComponent<T>();
                if (component != null)
                    list.Add(component);
            }

            return list.ToArray();
        }

        public static T[] FindChild<T>(Transform parent)
        {
            return parent.GetComponentsInChildren<T>(true);
        }

        public static void PingObject(GameObject gameObject)
        {
            EditorGUIUtility.PingObject(gameObject);
            Selection.activeGameObject = gameObject;
        }

        public static object CloneObject(object o)
        {
            var t = o.GetType();
            var properties = t.GetProperties();
            var p = t.InvokeMember("", BindingFlags.CreateInstance, null, o, null);
            foreach (var pi in properties)
            {
                if (!pi.CanWrite) continue;
                var value = pi.GetValue(o, null);
                pi.SetValue(p, value, null);
            }

            return p;
        }

        /*获取当前脚本的文件夹路径，参数为脚本的名字*/
        public static string GetScriptDirectory(string scriptName)
        {
            var guidPathArray = AssetDatabase.FindAssets(scriptName);
            // foreach (var guidPath in guidPathArray)
            // {
            //     Debug.Log(AssetDatabase.GUIDToAssetPath(guidPath));
            // }

            // if (guidPathArray.Length > 1)
            // {
            //     Debug.LogError("有同名文件" + scriptName + "获取路径失败");
            //     return null;
            // }

            //将字符串中得脚本名字和后缀统统去除掉
            var result = AssetDatabase.GUIDToAssetPath(guidPathArray[0]).Replace(@"/" + scriptName + ".cs", "");
            return result;
        }

        public static string GetParentDirectory(string directoryPath)
        {
            var splitPath = directoryPath.Split('/');
            var result = string.Empty;
            for (var i = 0; i < splitPath.Length - 1; i++)
                if (i == splitPath.Length - 2)
                    result += splitPath[i];
                else
                    result += splitPath[i] + "/";

            return result;
        }

        public static async void WaitToDo(int ms, string taskName, UnityAction action)
        {
            if (WaitToDoList.Contains(taskName))
            {
                Debug.Log("Already have " + taskName);
                return;
            }

            WaitToDoList.Add(taskName);
            await Task.Yield();
            await Task.Delay(ms);
            action?.Invoke();
            WaitToDoList.Remove(taskName);
        }

        private static readonly List<string> WaitToDoList = new List<string>();

        public static T AddChildAsset<T>(Object targetAsset, bool isAutoRefresh = true) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = $"{typeof(T).Name}";
            AssetDatabase.AddObjectToAsset(asset, targetAsset);

            if (isAutoRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return asset;
        }

        public static void SaveAndRefreshAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RemoveChildAsset(Object item, bool isAutoRefresh = true)
        {
            if (item == null) return;

            AssetDatabase.RemoveObjectFromAsset(item);
            Object.DestroyImmediate(item, true);

            if (isAutoRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static void MoveFolderFromPath(ref string folderPath, string folderName)
        {
            var path = EditorUtility.OpenFolderPanel("选择保存路径", folderPath, "");
            if (string.IsNullOrEmpty(path) || path == folderPath) return;

            var targetPath = FileUtil.GetProjectRelativePath(path) + "/" + folderName;
            if (targetPath != folderPath)
            {
                var lastPath = folderPath;
                if (!Directory.Exists(lastPath))
                    return;

                if (Directory.Exists(targetPath))
                {
                    if (Directory.GetFiles(targetPath).Length > 0)
                        Debug.Log("Target Directory:" + targetPath + " Not Empty!");
                    else
                        Directory.Delete(targetPath, true);
                }

                FileUtil.MoveFileOrDirectory(lastPath, targetPath);

                if (File.Exists(lastPath + ".meta"))
                    File.Delete(lastPath + ".meta");

                folderPath = targetPath;

                AssetDatabase.Refresh();


                PingProject(targetPath);
            }
        }
    }
}