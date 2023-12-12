using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEditor.Graphs;
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
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
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
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, result);

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
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            var list = symbols.Split(';').ToList();
            return list.Contains(StyleSymbol);
        }
    }
}