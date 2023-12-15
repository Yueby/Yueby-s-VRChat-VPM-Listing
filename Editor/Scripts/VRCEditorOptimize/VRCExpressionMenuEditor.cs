#if YUEBY_AVATAR_STYLE
using System;
using UnityEngine;
using UnityEditor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine.Events;
using Yueby.Utils;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;

namespace Yueby.AvatarTools.VRCEditorOptimize
{
    [CustomEditor(typeof(VRCExpressionsMenu))]
    public class VRCExpressionsMenuEditor : Editor
    {
        private VRC.SDK3.Avatars.Components.VRCAvatarDescriptor _activeDescriptor;
        private string[] _parameterNames;

        private YuebyReorderableList _menuRl;
        private SerializedProperty _controls;
        public readonly VRCExMenuLocalization Localization = new VRCExMenuLocalization();

        private void OnEnable()
        {
            _controls = serializedObject.FindProperty("controls");
            _menuRl = new YuebyReorderableList(serializedObject, _controls, true, true);
            _menuRl.OnDraw += OnDrawMenuElement;
            _menuRl.OnTitleDraw += () =>
            {
                GUILayout.Box("", GUILayout.ExpandWidth(true));
                var rect = GUILayoutUtility.GetLastRect();
                if (rect.size.magnitude > 40f)
                {
                    var count = ((ExpressionsMenu)target).controls.Count;
                    var value = (count * 1f) / (MAX_CONTROLS * 1f);

                    EditorGUI.ProgressBar(rect, value, $"{count}/{MAX_CONTROLS}");

                    _menuRl.IsDisableAddButton = count >= MAX_CONTROLS;
                }
            };
        }

        public void OnDisable()
        {
            SelectAvatarDescriptor(null);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Localization.DrawLanguageUI(Screen.width - 120, 55);

            YuebyUtil.VerticalEGL("Badge", () =>
            {
                SelectAvatarDescriptor();

                if (_activeDescriptor == null)
                    EditorGUILayout.HelpBox(Localization.Get("no_active_avatar"), MessageType.Error);
            }, true, 5, true, 6);


            //Controls
            _menuRl.DoLayout(Localization.Get("controls"), 0, false, false);
            serializedObject.ApplyModifiedProperties();
        }

        // Draw List Element
        private float OnDrawMenuElement(Rect rect, int i, bool arg3, bool arg4)
        {
            var control = _controls.GetArrayElementAtIndex(i);
            var name = control.FindPropertyRelative("name");
            var icon = control.FindPropertyRelative("icon");
            var type = control.FindPropertyRelative("type");
            var parameter = control.FindPropertyRelative("parameter");
            var value = control.FindPropertyRelative("value");

            rect.x += 10;
            var foldoutRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            var firstRect = foldoutRect;
            var lastRect = firstRect;

            control.isExpanded = EditorGUI.Foldout(foldoutRect, control.isExpanded, name.stringValue);
            if (control.isExpanded)
            {
                var nowRect = new Rect(foldoutRect.x + 2, foldoutRect.y + foldoutRect.height, rect.width - 4 * 2f, rect.height - foldoutRect.height);

                nowRect = DrawLine(nowRect);

                var iconRect = new Rect(nowRect.x, nowRect.y, EditorGUIUtility.singleLineHeight * 2, EditorGUIUtility.singleLineHeight * 2 + 4);
                icon.objectReferenceValue = EditorGUI.ObjectField(iconRect, icon.objectReferenceValue, typeof(Texture2D), false);

                var nameRect = new Rect(iconRect.x + iconRect.width + 2, iconRect.y, nowRect.width - iconRect.width - 4, EditorGUIUtility.singleLineHeight);
                var typeRect = new Rect(nameRect.x, nameRect.y + nameRect.height + 2, nameRect.width, nameRect.height);
                var parameterRect = new Rect(iconRect.x, iconRect.y + iconRect.height + 2, nowRect.width - 4, EditorGUIUtility.singleLineHeight);


                DrawPropertyField(nameRect, name, Localization.Get("name"));
                DrawPopup(typeRect, type, Localization.Get("type"));

                parameterRect = DrawParameterDropDownGUI(parameterRect, parameter, Localization.Get("parameter"));
                var valueRect = DrawParameterValueGUI(new Rect(parameterRect.x, parameterRect.y + parameterRect.height + 2, nowRect.width, EditorGUIUtility.singleLineHeight), parameter, value);
                lastRect = DrawType(valueRect, control, type);
            }

            var height = EditorGUIUtility.singleLineHeight;
            if (firstRect != lastRect)
            {
                var maxHeight = lastRect.y + lastRect.height;
                height = maxHeight - firstRect.y;
            }

            return height;
        }

        private void DrawPopup(Rect rect, SerializedProperty property, string label)
        {
            var guiContents = new GUIContent[property.enumNames.Length];
            for (var i = 0; i < guiContents.Length; i++)
            {
                var type = (int)Enum.GetValues(typeof(Control.ControlType)).GetValue(property.enumValueIndex);
                guiContents[i] = new GUIContent(property.enumNames[i], GetTypeToolTip(type));
            }

            DrawEditorGUI(rect, label, drawRect => { property.enumValueIndex = EditorGUI.Popup(drawRect, property.enumValueIndex, guiContents); });
        }

        private string GetTypeToolTip(int type)
        {
            var result = type switch
            {
                (int)Control.ControlType.Button => Localization.Get("control_button_tip"),
                (int)Control.ControlType.Toggle => Localization.Get("control_toggle_tip"),
                (int)Control.ControlType.SubMenu => Localization.Get("control_submenu_tip"),
                (int)Control.ControlType.TwoAxisPuppet => Localization.Get("control_two_axis_tip"),
                (int)Control.ControlType.FourAxisPuppet => Localization.Get("control_four_axis_tip"),
                (int)Control.ControlType.RadialPuppet => Localization.Get("control_radial_tip"),
                _ => ""
            };

            return result;
        }

        private Rect DrawType(Rect rect, SerializedProperty controlProperty, SerializedProperty typeProperty)
        {
            var type = (int)Enum.GetValues(typeof(Control.ControlType)).GetValue(typeProperty.enumValueIndex);

            rect.height = EditorGUIUtility.singleLineHeight;
            var lastRect = rect;
            switch (type)
            {
                case (int)Control.ControlType.Button:
                case (int)Control.ControlType.Toggle:
                    var subParameters = controlProperty.FindPropertyRelative("subParameters");
                    var labels = controlProperty.FindPropertyRelative("labels");
                    subParameters.arraySize = 0;
                    labels.arraySize = 0;
                    break;
                case (int)Control.ControlType.SubMenu:
                    lastRect = DrawSubMenuType(rect, controlProperty);
                    break;
                case (int)Control.ControlType.TwoAxisPuppet:
                    lastRect = DrawTwoAxisPuppetType(rect, controlProperty);
                    break;
                case (int)Control.ControlType.FourAxisPuppet:
                    lastRect = DrawFourAxisPuppetType(rect, controlProperty);
                    break;
                case (int)Control.ControlType.RadialPuppet:
                    lastRect = DrawRadialPuppetType(rect, controlProperty);
                    break;
            }

            if (lastRect == rect)
                lastRect.height = 0;
            else
                lastRect.height += 2;
            return lastRect;
        }

        private Rect DrawRadialPuppetType(Rect rect, SerializedProperty controlProperty)
        {
            var subParameters = controlProperty.FindPropertyRelative("subParameters");
            var labels = controlProperty.FindPropertyRelative("labels");

            subParameters.arraySize = 1;
            labels.arraySize = 0;

            rect = DrawLine(rect);
            rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(0), Localization.Get("parameter_rotation"), false);
            return rect;
        }

        private Rect DrawFourAxisPuppetType(Rect rect, SerializedProperty controlProperty)
        {
            var subParameters = controlProperty.FindPropertyRelative("subParameters");
            var labels = controlProperty.FindPropertyRelative("labels");

            subParameters.arraySize = 4;
            labels.arraySize = 4;

            rect = DrawLine(rect);


            rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(0), Localization.Get("parameter") + " " + Localization.Get("up"), false);
            rect = DrawParameterDropDownGUI(new Rect(rect.x, rect.y + rect.height + 2, rect.width, rect.height), subParameters.GetArrayElementAtIndex(1), Localization.Get("parameter") + " " + Localization.Get("right"), false);
            rect = DrawParameterDropDownGUI(new Rect(rect.x, rect.y + rect.height + 2, rect.width, rect.height), subParameters.GetArrayElementAtIndex(2), Localization.Get("parameter") + " " + Localization.Get("down"), false);
            rect = DrawParameterDropDownGUI(new Rect(rect.x, rect.y + rect.height + 2, rect.width, rect.height), subParameters.GetArrayElementAtIndex(3), Localization.Get("parameter") + " " + Localization.Get("left"), false);

            var dirRect = DrawFourDirection(new Rect(rect.x, rect.y + rect.height + 2, rect.width, rect.height), labels, new Vector2(50, 50), new Vector2(70, 70));

            return dirRect;
        }

        private Rect DrawTwoAxisPuppetType(Rect rect, SerializedProperty controlProperty)
        {
            var subParameters = controlProperty.FindPropertyRelative("subParameters");
            var labels = controlProperty.FindPropertyRelative("labels");

            subParameters.arraySize = 2;
            labels.arraySize = 4;

            rect = DrawLine(rect);

            rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(0), Localization.Get("parameter") + " " + Localization.Get("horizontal"), false);
            rect = DrawParameterDropDownGUI(new Rect(rect.x, rect.y + rect.height + 2, rect.width, rect.height), subParameters.GetArrayElementAtIndex(1), Localization.Get("parameter") + " " + Localization.Get("vertical"), false);

            var dirRect = DrawFourDirection(new Rect(rect.x, rect.y + rect.height + 2, rect.width, rect.height), labels, new Vector2(50, 50), new Vector2(70, 70));

            return dirRect;
        }

        private Rect DrawSubMenuType(Rect rect, SerializedProperty controlProperty)
        {
            var subMenu = controlProperty.FindPropertyRelative("subMenu");

            rect = DrawLine(rect);

            var subMenuRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            DrawEditorGUI(subMenuRect, Localization.Get("submenu"), drawRect =>
            {
                //
                EditorGUI.PropertyField(drawRect, subMenu, new GUIContent(""));
            });

            return subMenuRect;
        }

        private Rect DrawFourDirection(Rect rect, SerializedProperty dirControls, Vector2 size, Vector2 offset)
        {
            var labelRect = rect;
            labelRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(labelRect, Localization.Get("ex_menu_show"));
            rect.y += labelRect.height + 2;

            const float height = 240f;
            rect.height = height;

            GUI.Box(rect, "", "Badge");
            var center = new Rect(rect.center.x, rect.center.y - size.y / 4f, 10, 10);

            var upRect = new Rect(center.x - size.x / 2f, center.y - size.y / 2 - offset.y, size.x, size.y);
            var downRect = new Rect(center.x - size.x / 2f, center.y - size.y / 2 + offset.y, size.x, size.y);
            var leftRect = new Rect(center.x - size.x / 2f - offset.x, center.y - size.y / 2, size.x, size.y);
            var rightRect = new Rect(center.x - size.x / 2f + offset.x, center.y - size.y / 2, size.x, size.y);

            DrawLabelGUI(upRect, dirControls.GetArrayElementAtIndex(0), Localization.Get("up"));
            DrawLabelGUI(leftRect, dirControls.GetArrayElementAtIndex(1), Localization.Get("right"));
            DrawLabelGUI(downRect, dirControls.GetArrayElementAtIndex(2), Localization.Get("down"));
            DrawLabelGUI(rightRect, dirControls.GetArrayElementAtIndex(3), Localization.Get("left"));

            rect.height += 2;
            return rect;
        }

        private void DrawLabelGUI(Rect rect, SerializedProperty subControl, string name)
        {
            var nameProp = subControl.FindPropertyRelative("name");
            var icon = subControl.FindPropertyRelative("icon");

            icon.objectReferenceValue = EditorGUI.ObjectField(rect, icon.objectReferenceValue, typeof(Texture2D), false);
            rect.y += rect.height + 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, nameProp, new GUIContent(""));

            if (nameProp.stringValue.Length <= 0)
                EditorGUI.LabelField(rect, new GUIContent(name), EditorStyles.centeredGreyMiniLabel);
        }


        private Rect DrawLine(Rect rect)
        {
            const float height = 2f;
            rect.y += height;

            var boxRect = new Rect(rect.x, rect.y, rect.width - 5f, height);
            GUI.Box(boxRect, "");
            rect.y = boxRect.y + boxRect.height + height * 2;

            return rect;
        }

        private Rect DrawHelpBox(Rect rect, string text, MessageType messageType)
        {
            rect.y += rect.height + 4;
            var textSize = GUI.skin.label.CalcSize(new GUIContent(text));

            var helpBoxRect = new Rect(rect.x, rect.y, rect.width, textSize.y + EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(helpBoxRect, text, messageType);
            rect.height = helpBoxRect.height + 2;

            return rect;
        }

        private void DrawPropertyField(Rect rect, SerializedProperty serializedProperty, string label, string tooltip = "")
        {
            DrawEditorGUI(rect, label, drawRect => { EditorGUI.PropertyField(drawRect, serializedProperty, new GUIContent("")); }, tooltip);
        }

        private void DrawEditorGUI(Rect rect, string label, UnityAction<Rect> onDraw, string tooltip = "")
        {
            var size = GUI.skin.label.CalcSize(new GUIContent(label + "\t"));
            var labelRect = new Rect(rect.x, rect.y, size.x, rect.height);
            var pRect = new Rect(labelRect.x + labelRect.width + 2, labelRect.y, rect.width - labelRect.width - 2 * 2, labelRect.height);
            EditorGUI.LabelField(labelRect, new GUIContent(label, tooltip));
            onDraw?.Invoke(pRect);
        }


        private void SelectAvatarDescriptor()
        {
            var descriptors = FindObjectsOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (descriptors.Length > 0)
            {
                //Compile list of names
                var names = new string[descriptors.Length];
                for (var i = 0; i < descriptors.Length; i++)
                    names[i] = descriptors[i].gameObject.name;

                //Select
                var currentIndex = Array.IndexOf(descriptors, _activeDescriptor);
                var nextIndex = EditorGUILayout.Popup(Localization.Get("active_avatar"), currentIndex, names);
                if (nextIndex < 0)
                    nextIndex = 0;
                if (nextIndex != currentIndex)
                    SelectAvatarDescriptor(descriptors[nextIndex]);
            }
            else
                SelectAvatarDescriptor(null);
        }

        private void SelectAvatarDescriptor(VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc)
        {
            if (desc == _activeDescriptor)
                return;

            _activeDescriptor = desc;
            if (_activeDescriptor != null)
            {
                //Init stage parameters
                var paramCount = desc.GetExpressionParameterCount();
                _parameterNames = new string[paramCount + 1];
                _parameterNames[0] = $"[{Localization.Get("none")}]";
                for (var i = 0; i < paramCount; i++)
                {
                    var param = desc.GetExpressionParameter(i);
                    var name = $"[{Localization.Get("none")}]";
                    if (param != null && !string.IsNullOrEmpty(param.name))
                        name = string.Format("{0}, {1}", param.name, param.valueType.ToString(), i + 1);
                    _parameterNames[i + 1] = name;
                }
            }
            else
            {
                _parameterNames = null;
            }
        }

        private int GetExpressionParametersCount()
        {
            if (_activeDescriptor != null && _activeDescriptor.expressionParameters != null && _activeDescriptor.expressionParameters.parameters != null)
                return _activeDescriptor.expressionParameters.parameters.Length;
            return 0;
        }

        private ExpressionParameters.Parameter GetExpressionParameter(int i)
        {
            return _activeDescriptor != null ? _activeDescriptor.GetExpressionParameter(i) : null;
        }

        private Rect DrawParameterDropDownGUI(Rect rect, SerializedProperty parameter, string name, bool allowBool = true)
        {
            var parameterName = parameter.FindPropertyRelative("name");
            VRCExpressionParameters.Parameter param = null;
            var value = parameterName.stringValue;

            var parameterFound = false;
            rect.height = EditorGUIUtility.singleLineHeight;
            var popupRect = new Rect(rect.x, rect.y, rect.width / 1.5f - 1f, rect.height);
            var textRect = new Rect(popupRect.x + popupRect.width + 1, rect.y, rect.width - popupRect.width - 2f, rect.height);

            if (_activeDescriptor != null)
            {
                //Dropdown
                int currentIndex;
                if (string.IsNullOrEmpty(value))
                {
                    currentIndex = -1;
                    parameterFound = true;
                }
                else
                {
                    currentIndex = -2;
                    for (var i = 0; i < GetExpressionParametersCount(); i++)
                    {
                        var item = _activeDescriptor.GetExpressionParameter(i);
                        if (item.name == value)
                        {
                            param = item;
                            parameterFound = true;
                            currentIndex = i;
                            break;
                        }
                    }
                }

                //Dropdown
                EditorGUI.BeginChangeCheck();
                DrawEditorGUI(popupRect, name, drawRect => { currentIndex = EditorGUI.Popup(drawRect, currentIndex + 1, _parameterNames); });

                if (EditorGUI.EndChangeCheck())
                {
                    parameterName.stringValue = currentIndex == 0 ? "" : GetExpressionParameter(currentIndex - 1).name;
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(popupRect, 0, new string[0]);
                EditorGUI.EndDisabledGroup();
            }

            //Text field
            parameterName.stringValue = EditorGUI.TextField(textRect, parameterName.stringValue);


            if (!parameterFound)
            {
                rect = DrawHelpBox(rect, Localization.Get("parameter_not_found"), MessageType.Warning);
            }

            if (!allowBool && param != null && param.valueType == ExpressionParameters.ValueType.Bool)
            {
                rect = DrawHelpBox(rect, Localization.Get("parameter_bool_not_valid"), MessageType.Error);
            }

            return rect;
        }

        private Rect DrawParameterValueGUI(Rect rect, SerializedProperty parameter, SerializedProperty value)
        {
            var paramName = parameter.FindPropertyRelative("name").stringValue;
            rect.height = EditorGUIUtility.singleLineHeight;
            var height = 0f;
            if (!string.IsNullOrEmpty(paramName))
            {
                var paramDef = FindExpressionParameterDef(paramName);
                if (paramDef != null)
                {
                    if (paramDef.valueType == ExpressionParameters.ValueType.Int)
                    {
                        DrawEditorGUI(rect, Localization.Get("value"), drawRect =>
                        {
                            height = drawRect.height;
                            value.floatValue = EditorGUI.IntField(drawRect, Mathf.Clamp((int)value.floatValue, 0, 255));
                        });
                    }
                    else if (paramDef.valueType == ExpressionParameters.ValueType.Float)
                    {
                        DrawEditorGUI(rect, Localization.Get("value"), drawRect =>
                        {
                            height = drawRect.height;
                            value.floatValue = EditorGUI.FloatField(drawRect, Mathf.Clamp(value.floatValue, -1f, 1f));
                        });
                    }
                    else if (paramDef.valueType == ExpressionParameters.ValueType.Bool)
                    {
                        value.floatValue = 1f;
                    }
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    DrawEditorGUI(rect, Localization.Get("value"), drawRect =>
                    {
                        height = drawRect.height;
                        value.floatValue = EditorGUI.FloatField(drawRect, value.floatValue);
                    });

                    EditorGUI.EndDisabledGroup();
                }
            }

            rect.y += height + 2;
            rect.height = height;
            return rect;
        }

        private ExpressionParameters.Parameter FindExpressionParameterDef(string name)
        {
            if (_activeDescriptor == null || string.IsNullOrEmpty(name))
                return null;

            //Find
            var length = GetExpressionParametersCount();
            for (var i = 0; i < length; i++)
            {
                var item = GetExpressionParameter(i);
                if (item != null && item.name == name)
                    return item;
            }

            return null;
        }
    }
}
#endif