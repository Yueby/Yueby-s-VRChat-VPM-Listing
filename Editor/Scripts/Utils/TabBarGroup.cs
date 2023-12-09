using System.Collections.Generic;
using UnityEngine;
using Yueby.Utils;
using EditorGUIUtility = UnityEditor.EditorGUIUtility;

namespace Yueby.AvatarTools
{
    public class TabBarGroup
    {
        private readonly List<TabBarElement> _tabBarElements;

        public TabBarGroup(List<TabBarElement> tabBarElements)
        {
            _tabBarElements = tabBarElements;
        }

        public void Add(TabBarElement element)
        {
            if (!_tabBarElements.Contains(element))
                _tabBarElements.Add(element);
        }

        public void Remove(TabBarElement element)
        {
            if (_tabBarElements.Contains(element))
                _tabBarElements.Remove(element);
        }


        public void Draw(float height)
        {
            const float width = 22;
            YuebyUtil.HorizontalEGL(() =>
            {
                GUILayout.Label("", GUILayout.Width(width), GUILayout.Height(height));
                var rect = GUILayoutUtility.GetLastRect();

                if (_tabBarElements.Count > 0)
                {
                    var elementHeight = 0f;
                    foreach (var element in _tabBarElements)
                    {
                        if (!element.IsVisible) continue;
                        var label = element.IsDraw ? "◀" : "▶";
                        float currentHeight;
                        if (!string.IsNullOrEmpty(element.Title))
                        {
                            currentHeight = (element.Title.Length + 1) * EditorGUIUtility.singleLineHeight;
                            var btnRect = new Rect(rect.x, rect.y + elementHeight + element.Space, rect.width, currentHeight);

                            var result = "";
                            foreach (var c in element.Title)
                                result += c + "\n";

                            if (GUI.Button(btnRect, result + label))
                                element.ChangeDrawState(!element.IsDraw);
                        }
                        else
                        {
                            var icon = element.Icons[1];
                            if (EditorGUIUtility.isProSkin)
                                icon = element.Icons[0];

                            currentHeight = rect.width + EditorGUIUtility.singleLineHeight + 5;
                            var btnRect = new Rect(rect.x - 2, rect.y + elementHeight + element.Space, rect.width, currentHeight);

                      
                             var toggleStyle = GUI.skin.FindStyle("buttonright");
                             element.ChangeDrawState(GUI.Toggle(btnRect, element.IsDraw, "", toggleStyle));
                            var texHeight = btnRect.width - 4;
                            var texRect = new Rect(btnRect.x + 2, btnRect.y + btnRect.height / 2 - texHeight / 2, btnRect.width - 4, texHeight);
                     
             
                            GUI.DrawTexture(texRect, icon);
                        }

                        elementHeight += currentHeight + element.Space;
                    }
                }


                GUI.Box(new Rect(rect.x + rect.width + 2, rect.y, 1, rect.height), "");
                GUI.Box(new Rect(rect.x + rect.width + 2, rect.y, 1, rect.height), "");
            }, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height));
        }
    }
}