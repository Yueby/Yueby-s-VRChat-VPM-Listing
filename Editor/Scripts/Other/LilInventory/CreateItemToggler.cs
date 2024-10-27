#if UNITY_EDITOR
using System.Collections.Generic;
using jp.lilxyzw.lilycalinventory.runtime;
using UnityEditor;
using UnityEngine;

namespace Yueby
{
    public class CreateItemToggler
    {
        private const float DelayTime = 0.1f; // 延迟时间，单位为秒
        private static float _lastExecutionTime; // 上次执行时间

        [MenuItem("GameObject/YuebyTools/Item Toggler (Single Only)", false, 11)]
        public static void CreateSingleToggler()
        {
            var selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {

                return;
            }

            var parentTransform = selectedObject.transform.parent;
            var itemTogglerGo = CreateItemTogglerGameObject(selectedObject, parentTransform);
            ConfigureItemToggler(itemTogglerGo, selectedObject);

            Undo.RegisterCreatedObjectUndo(itemTogglerGo, "Create Item Toggler (Single)");
        }

        [MenuItem("GameObject/YuebyTools/Item Toggler (Single Only)", true)]
        public static bool CreateSingleTogglerValidate()
        {
            return Selection.gameObjects.Length == 1;
        }

        [MenuItem("GameObject/YuebyTools/Item Toggler (Multiple)", false, 10)]
        public static void CreateMultipleToggler()
        {
            if (!IsDelayExceeded())
            {
                return;
            }

            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length <= 1)
            {

                return;
            }

            Transform parentTransform = selectedObjects[0].transform.parent;
            Transform rootFolderTransform = GetOrCreateMenuFolder(parentTransform, parentTransform.name);

            List<GameObject> createdItemTogglers = new List<GameObject>(); // 存储创建的 ItemToggler

            foreach (var selectedObject in selectedObjects)
            {

                GameObject itemToggler = CreateItemTogglerForRenderer(selectedObject, rootFolderTransform);
                createdItemTogglers.Add(itemToggler);
            }

            UpdateLastExecutionTime();

            if (createdItemTogglers.Count > 0)
            {
                EditorGUIUtility.PingObject(createdItemTogglers[createdItemTogglers.Count - 1]); // Ping 最后一个创建的 ItemToggler
            }
        }

        [MenuItem("GameObject/YuebyTools/Item Toggler (Multiple)", true)]
        public static bool CreateMultipleTogglerValidate()
        {
            return Selection.gameObjects.Length > 1;
        }

        [MenuItem("GameObject/YuebyTools/Item Toggler (Root Only)", false, 12)]
        public static void CreateToggler()
        {
            var selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {

                return;
            }

            Transform folderTransform = CreateNewFolder(selectedObject.transform, selectedObject.name);

            var skinnedMeshRenderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                CreateItemTogglerForRenderer(skinnedMeshRenderer.gameObject, folderTransform);
            }
        }

        [MenuItem("GameObject/YuebyTools/Item Toggler (Root Only)", true)]
        public static bool CreateTogglerValidate()
        {
            return Selection.activeGameObject != null;
        }

        private static Transform GetOrCreateMenuFolder(Transform parent, string folderName)
        {

            Transform existingFolder = parent.Find(folderName);
            if (existingFolder != null)
            {
                return existingFolder;
            }

            var folderTrans = new GameObject(folderName).transform;
            folderTrans.parent = parent;
            folderTrans.gameObject.AddComponent<MenuFolder>();
            return folderTrans;
        }

        private static GameObject CreateItemTogglerForRenderer(GameObject go, Transform folderTrans)
        {
            var itemTogglerGo = CreateItemTogglerGameObject(go, folderTrans);
            ConfigureItemToggler(itemTogglerGo, go);
            Undo.RegisterCreatedObjectUndo(itemTogglerGo, "Create Item Toggler");
            return itemTogglerGo;
        }

        private static GameObject CreateItemTogglerGameObject(GameObject go, Transform parentTransform)
        {
            var itemTogglerGo = new GameObject(go.name)
            {
                transform =
                {
                    parent = parentTransform,
                    position = Vector3.zero
                }
            };

            EditorGUIUtility.PingObject(itemTogglerGo);
            return itemTogglerGo;
        }

        private static Transform CreateNewFolder(Transform parent, string folderName)
        {
            var folderTrans = new GameObject(folderName).transform;
            folderTrans.parent = parent;
            folderTrans.gameObject.AddComponent<MenuFolder>();
            return folderTrans;
        }

        private static void ConfigureItemToggler(GameObject itemTogglerGo, GameObject go)
        {
            var itemToggler = itemTogglerGo.AddComponent<ItemToggler>();
            itemToggler.parameter = new ParametersPerMenu
            {
                objects = new[]
                {
                    new ObjectToggler
                    {
                        obj = go,
                        value = true
                    }
                },

            };
        }

        private static bool IsDelayExceeded()
        {
            return Time.realtimeSinceStartup - _lastExecutionTime >= DelayTime;
        }

        private static void UpdateLastExecutionTime()
        {
            _lastExecutionTime = Time.realtimeSinceStartup;
        }
    }
}
#endif