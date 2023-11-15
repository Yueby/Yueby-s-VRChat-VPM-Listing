using UnityEditor;
using UnityEngine;

namespace Yueby.AvatarTools.CameraFollow
{
    public class SceneViewCameraFollowEditorWindow : EditorWindow
    {
        private const string Path = "Tools/YuebyTools/Follow SceneView Camera %&X";
        private static bool IsEnabled;

        [MenuItem(Path, priority = 10)]
        public static void Execute()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Error", "Unable to find the main camera!", "OK");
                return;
            }

            var follow = cam.GetComponent<SceneViewCameraFollow>();
            if (!follow)
            {
                cam.gameObject.AddComponent<SceneViewCameraFollow>();
                EditorUtility.DisplayDialog("SceneView Camera Follow - Tips", "Enabled!", "OK");
                IsEnabled = true;
            }
            else
            {
                DestroyImmediate(follow);
                EditorUtility.DisplayDialog("SceneView Camera Follow - Tips", "Disabled!", "OK");
                IsEnabled = false;
            }
        }

        [MenuItem(Path, true)]
        public static bool SettingValidate()
        {
            Menu.SetChecked(Path, IsEnabled);
            return true;
        }

        [MenuItem("Tools/YuebyTools", priority = 5463)]
        public static void YuebyTools()
        {
        }
    }
}