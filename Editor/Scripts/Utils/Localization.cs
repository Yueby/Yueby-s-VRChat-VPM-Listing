using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yueby.Utils
{
    public class Localization
    {
        private Texture2D _darkLanguageIcon;
        private Texture2D _lightLanguageIcon;
        private int _selectedIndex;
        protected Dictionary<string, Dictionary<string, string>> Languages;

        public string Get(string label)
        {
            var current = GetCurrentLocalization();
            return current.TryGetValue(label, out var value) ? value : string.Empty;
        }

        public void DrawLanguageUI(float x = 10f, float y = 10f)
        {
            if (Languages == null || Languages.Count == 0) return;

            if (!_darkLanguageIcon)
                _darkLanguageIcon = AssetDatabase.LoadMainAssetAtPath("Packages/com.yueby.avatartools/Editor/Assets/Sprites/LanguageIconDark.png") as Texture2D;
            if (!_lightLanguageIcon)
                _lightLanguageIcon = AssetDatabase.LoadMainAssetAtPath("Packages/com.yueby.avatartools/Editor/Assets/Sprites/LanguageIconLight.png") as Texture2D;

            var rect = new Rect(x, y, 18, 18);

            GUI.DrawTexture(rect, EditorGUIUtility.isProSkin ? _darkLanguageIcon : _lightLanguageIcon);
            rect.x += rect.width + 5;
            rect.width = 80;
            rect.height = 20;

            _selectedIndex = EditorGUI.Popup(rect, _selectedIndex, GetKeys());
        }

        private Dictionary<string, string> GetCurrentLocalization()
        {
            return Languages[GetKeys()[_selectedIndex]];
        }

        private string[] GetKeys()
        {
            return Languages.Keys.ToArray();
        }
    }
}