using UnityEditor;
using UnityEngine;
using Yueby.Utils;

namespace Yueby.AvatarTools.ToggleManager
{
    public class TMEditorWindow : EditorWindow
    {
        private static TMEditorWindow _window;

        [MenuItem("Tools/YuebyTools/Avatar/Toggle Manager", false, 12)]
        public static void ShowWindow()
        {
            _window = GetWindow<TMEditorWindow>();
            _window.titleContent = new GUIContent("Toggle Manager");
            _window.minSize = new Vector2(420, 550);
        }

        private void OnGUI()
        {
            if (_window == null)
                _window = GetWindow<TMEditorWindow>();
            _window.titleContent = new GUIContent("Toggle Manager");
            EditorUI.DrawEditorTitle("开关管理");

            DrawInit();
            DrawConfigure();
        }

        private void DrawInit()
        {
            EditorUI.VerticalEGLTitled("初始设置", () =>
            {
                EditorGUILayout.HelpBox("初始设置",MessageType.Info);
            });
        }

        private void DrawConfigure()
        {
            EditorUI.VerticalEGLTitled("配置页面", () =>
            {
                EditorGUILayout.HelpBox("配置页面",MessageType.Info);
            });
        }
    }
}