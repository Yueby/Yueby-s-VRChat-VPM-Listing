using UnityEditor;
using UnityEngine;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other
{
    public class RenameObjectTool : EditorWindow
    {
        public enum RenameType
        {
            Replace,
            Additive
        }

        private string _keyword;
        private Vector2 _pos;
        private string _renameText;
        private RenameType _renameType;

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

            EditorUI.DrawEditorTitle("重命名工具");
            EditorUI.VerticalEGLTitled("配置", () =>
            {
                EditorUI.HorizontalEGL(() =>
                {
                    EditorGUILayout.LabelField("重命名类型");
                    _renameType = (RenameType)EditorGUILayout.EnumPopup(_renameType);
                });

                switch (_renameType)
                {
                    case RenameType.Additive:
                        _renameText = EditorUI.TextField("添加的字符", _renameText, 70);
                        break;
                    case RenameType.Replace:
                        _keyword = EditorUI.TextField("关键字", _keyword, 60);
                        _renameText = EditorUI.TextField("替换为", _renameText, 60);
                        EditorGUILayout.HelpBox("按关键字替换", MessageType.Info);
                        break;
                }
            });

            EditorUI.VerticalEGLTitled("操作", () =>
            {
                if (GUILayout.Button("重命名") && selections.Length > 0)
                    switch (_renameType)
                    {
                        case RenameType.Replace:
                            RenameByKeyword(_keyword, _renameText);
                            break;
                        case RenameType.Additive:
                            RenameByAdditive(_renameText);
                            break;
                    }
            });

            EditorUI.VerticalEGLTitled("选中列表", () =>
            {
                if (isSelectedObject)
                    _pos = EditorUI.ScrollViewEGL(() =>
                    {
                        foreach (var selection in selections) EditorGUILayout.ObjectField(selection, typeof(GameObject), true);
                    }, _pos, GUILayout.Height(200));
                else
                    EditorGUILayout.HelpBox("请在场景中选中需要重命名的对象", MessageType.Error);
            });
        }

        [MenuItem("Tools/YuebyTools/Avatar/Other/Rename Object", false, 31)]
        private static void Open()
        {
            var window = GetWindow<RenameObjectTool>();
            window.titleContent = new GUIContent("重命名工具");
            window.minSize = new Vector2(400, 600);
        }

        private void RenameByAdditive(string text)
        {
            if (!EditorUtility.DisplayDialog("提示", "你确定这么做吗？", "OK", "Cancel")) return;

            foreach (var selectedObject in Selection.objects)
            {
                Undo.RegisterCompleteObjectUndo(selectedObject, "Object name change");
                selectedObject.name = selectedObject.name + text;
            }

            EditorUtility.DisplayDialog("提示", "重命名完成！", "OK");
        }

        private void RenameByKeyword(string keyword, string replace)
        {
            if (!EditorUtility.DisplayDialog("提示", "你确定这么做吗？请检查好关键字与替换字为你想要的文字哦。", "OK", "Cancel")) return;

            foreach (var selectedObject in Selection.objects)
            {
                Undo.RegisterCompleteObjectUndo(selectedObject, "Object name change");
                if (selectedObject.name.Contains(keyword))
                    selectedObject.name = selectedObject.name.Replace(keyword, replace);
            }

            EditorUtility.DisplayDialog("提示", "重命名完成！", "OK");
        }
    }
}