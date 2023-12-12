using UnityEditor;
using UnityEngine;

namespace Yueby.AvatarTools.CameraFollow
{
    public class SceneViewCameraFollowEditorWindow : EditorWindow
    {
        private const string Path = "Tools/YuebyTools/Utils/Follow SceneView Camera %&X";
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
            }
            else
            {
                DestroyImmediate(follow);
                EditorUtility.DisplayDialog("SceneView Camera Follow - Tips", "Disabled!", "OK");
            }
        }

        [MenuItem(Path, true)]
        public static bool SettingValidate()
        {
            if (Camera.main != null)
                IsEnabled = Camera.main.GetComponent<SceneViewCameraFollow>();
            Menu.SetChecked(Path, IsEnabled);
            return true;
        }
    }
}