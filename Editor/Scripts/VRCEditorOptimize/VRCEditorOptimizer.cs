using System.IO;
using System.Linq;
using UnityEditor;
#if UNITY_2022
using UnityEditor.Build;
#endif
using UnityEditor.Compilation;
using UnityEngine;
using File = UnityEngine.Windows.File;

namespace Yueby.AvatarTools.VRCEditorOptimize
{
    public static class VRCEditorOptimizer
    {
        private const string Path = "Tools/YuebyTools/VRChat/Avatar/Change Style (VRC Expressions Inspector)";
        private static bool _isEnabled;
        private const string STYLE_TAG = "YUEBY_AVATAR_STYLE";

        [MenuItem(Path, priority = 60)]
        public static void Execute()
        {
            //#if UNITY_2019
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            // #elif UNITY_2022
            //             var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            // #endif
            var list = symbols.Split(';').ToList();
            var result = "";
            if (_isEnabled)
            {
                // EditorPrefs.SetString(nameof(VRCEditorOptimizer), "");
                if (list.Contains(STYLE_TAG))
                    list.Remove(STYLE_TAG);
            }
            else
            {
                // EditorPrefs.SetString(nameof(VRCEditorOptimizer), STYLE_TAG);
                if (!list.Contains(STYLE_TAG))
                    list.Add(STYLE_TAG);
            }

            foreach (var item in list)
                result += item + ";";

            //#if UNITY_2019
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, result);
            //#elif UNITY_2022
            //            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, result);
            //#endif

            EditorUtility.DisplayDialog("Tips", "Waiting for editor recompile scripts.\n请等待编辑器重新编译脚本。", "Ok");
            CompilationPipeline.RequestScriptCompilation();

            ChangeVRCEditorFile();
            AssetDatabase.Refresh();
        }

        [MenuItem(Path, true)]
        public static bool SettingValidate()
        {
            _isEnabled = GetEnable();
            Menu.SetChecked(Path, _isEnabled);
            return true;
        }

        private static bool GetEnable()
        {
            // var tag = EditorPrefs.GetString(nameof(VRCEditorOptimizer));
            // return tag == STYLE_TAG;
            //#if UNITY_2019
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            //#elif UNITY_2022
            //            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            //#endif
            var list = symbols.Split(';').ToList();
            return list.Contains(STYLE_TAG);
        }

        private static void ChangeVRCEditorFile()
        {
            var menuEditorPath = "Packages/com.vrchat.avatars/Editor/VRCSDK/SDK3A/Components3/VRCExpressionsMenuEditor.cs";
            var parameterEditorPath = "Packages/com.vrchat.avatars/Editor/VRCSDK/SDK3A/Components3/VRCExpressionParametersEditor.cs";

            HideFile(GetEnable(), menuEditorPath);
            HideFile(GetEnable(), parameterEditorPath);
        }

        private static void HideFile(bool isEnabled, string path)
        {
            // 隐藏 （改文件后缀）
            var currentPath = isEnabled ? path : path + ".hide";
            var targetPath = isEnabled ? path + ".hide" : path;

            if (File.Exists(currentPath))
            {
                System.IO.File.Move(currentPath, targetPath);

                if (File.Exists(currentPath + ".meta"))
                    File.Delete(currentPath + ".meta");
            }
            else
            {
                Debug.Log("未找到文件:" + currentPath);
            }
        }
    }
}