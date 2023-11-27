using System.Linq;
using UnityEditor;
using UnityEngine;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other
{
    public class RemoveClothesTool : EditorWindow
    {
        private Vector2 _pos;
        private Transform _target;

        private void OnEnable()
        {
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            var selections = Selection.gameObjects;
            var isSelectedObject = selections.Length > 0;

            YuebyUtil.DrawEditorTitle("衣服删除工具");
            YuebyUtil.VerticalEGLTitled("配置", () =>
            {
                _target = (Transform)YuebyUtil.ObjectField("骨骼", 60, _target, typeof(Transform), true);
                EditorGUILayout.HelpBox("按选中的衣服对象名删除目标骨骼中同名对象", MessageType.Info);
            });

            YuebyUtil.VerticalEGLTitled("操作", () =>
            {
                if (GUILayout.Button("删除") && selections.Length > 0 && _target != null) Remove(selections, _target);
            });

            YuebyUtil.VerticalEGLTitled("选中列表", () =>
            {
                if (isSelectedObject)
                    _pos = YuebyUtil.ScrollViewEGL(() =>
                    {
                        foreach (var selection in selections) EditorGUILayout.ObjectField(selection, typeof(GameObject), true);
                    }, _pos, GUILayout.Height(200));
                else
                    EditorGUILayout.HelpBox("请在场景中选中需要删除的对象", MessageType.Error);
            });
        }

        [MenuItem("Tools/YuebyTools/Avatar/Other/Remove Clothes", false, 30)]
        private static void Open()
        {
            var window = GetWindow<RemoveClothesTool>();
            window.titleContent = new GUIContent("衣服删除工具");
            window.minSize = new Vector2(400, 600);
        }

        private void Remove(GameObject[] gameObjects, Transform target)
        {
            if (!EditorUtility.DisplayDialog("提示", "你确定这么做吗？", "OK", "Cancel")) return;

            var targetGos = target.GetComponentsInChildren<Transform>(true).ToList();

            foreach (var selection in gameObjects)
            {
                var gos = targetGos.Where(go =>
                {
                    if (go != null && go.name.Contains(selection.name))
                        return go;
                    return false;
                }).ToList();

                if (gos.Count > 0)
                {
                    foreach (var t in gos.Where(t => t != null))
                    {
                        Undo.RegisterCompleteObjectUndo(t.gameObject, "Delete Clothes Bone");
                        DestroyImmediate(t.gameObject);
                    }

                    Undo.RegisterCompleteObjectUndo(selection.gameObject, "Delete Clothes");
                    DestroyImmediate(selection.gameObject);
                }
                else
                {
                    EditorUtility.DisplayDialog("Tips", "没找到同名对象 :" + selection.name, "OK");
                }
            }

            EditorUtility.DisplayDialog("提示", "删除完成！", "OK");
        }
    }
}