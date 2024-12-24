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
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var list = symbols.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            var result = "";
            if (_isEnabled)
            {
                if (list.Contains(STYLE_TAG))
                    list.Remove(STYLE_TAG);
            }
            else
            {
                if (!list.Contains(STYLE_TAG))
                    list.Add(STYLE_TAG);
            }

            result = string.Join(";", list.Where(x => !string.IsNullOrWhiteSpace(x)));

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, result);
    
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
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            
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