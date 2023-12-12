using System.Linq;
using UnityEditor;
#if UNITY_2022
using UnityEditor.Build;
#endif
using UnityEditor.Compilation;

using UnityEngine;

namespace Yueby.AvatarTools.VRCEditorOptimize
{
    public static class VRCEditorOptimizer
    {
        private const string Path = "Tools/YuebyTools/Utils/Change Style (VRC Expressions Inspector)";
        private static bool _isEnabled;
        private const string StyleSymbol = "YUEBY_AVATAR_STYLE";

        [MenuItem(Path, priority = 10)]
        public static void Execute()
        {
#if UNITY_2019
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#elif UNITY_2022
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#endif
            var list = symbols.Split(';').ToList();
            var result = "";
            if (_isEnabled)
            {
                if (list.Contains(StyleSymbol))
                    list.Remove(StyleSymbol);
            }
            else
            {
                if (!list.Contains(StyleSymbol))
                    list.Add(StyleSymbol);
            }

            foreach (var item in list)
                result += item + ";";

#if UNITY_2019
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, result);
#elif UNITY_2022
             PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, result);
#endif

            EditorUtility.DisplayDialog("Tips", "Waiting for editor recompile scripts.\n请等待编辑器重新编译脚本。", "Ok");
            CompilationPipeline.RequestScriptCompilation();
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
#if UNITY_2019
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#elif UNITY_2022
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#endif
            var list = symbols.Split(';').ToList();
            return list.Contains(StyleSymbol);
        }
    }
}