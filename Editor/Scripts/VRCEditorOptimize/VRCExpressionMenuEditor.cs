#if YUEBY_AVATAR_STYLE
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Reflection.Emit;
using UnityEngine.Events;
using Yueby.Utils;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;

[CustomEditor(typeof(VRCExpressionsMenu))]
public class VRCExpressionsMenuEditor : Editor
{
    static string[] ToggleStyles = { "Pip-Slot", "Animation" };

    List<UnityEngine.Object> foldoutList = new List<UnityEngine.Object>();
    VRC.SDK3.Avatars.Components.VRCAvatarDescriptor activeDescriptor = null;
    string[] parameterNames;

    private YuebyReorderableList _menuRl;
    private SerializedProperty controls;

    private void OnEnable()
    {
        controls = serializedObject.FindProperty("controls");
        _menuRl = new YuebyReorderableList(serializedObject, controls, true, true);
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

        _menuRl.OnElementHeightCallback += arg0 => { Repaint(); };
    }

    public void OnDisable()
    {
        SelectAvatarDescriptor(null);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SelectAvatarDescriptor();

        if (activeDescriptor == null)
        {
            EditorGUILayout.HelpBox("No active avatar descriptor found in scene.", MessageType.Error);
        }

        EditorGUILayout.Space();

        //Controls

        _menuRl.DoLayout("Controls", false, false);
        serializedObject.ApplyModifiedProperties();
    }

    // Draw List Element
    private float OnDrawMenuElement(Rect rect, int i, bool arg3, bool arg4)
    {
        var control = controls.GetArrayElementAtIndex(i);
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
            var parameterRect = new Rect(iconRect.x, iconRect.y + iconRect.height + 2, nowRect.width, EditorGUIUtility.singleLineHeight);


            DrawPropertyField(nameRect, name, "Name");
            DrawPopup(typeRect, type, "Type");

            parameterRect = DrawParameterDropDownGUI(parameterRect, parameter, "Parameter");
            var valueRect = DrawParameterValueGUI(parameterRect, parameter, value);
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
            (int)Control.ControlType.Button => "Click or hold to activate. The button remains active for a minimum 0.2s.\nWhile active the (Parameter) is set to (Value).\nWhen inactive the (Parameter) is reset to zero.",
            (int)Control.ControlType.Toggle => "Click to toggle on or off.\nWhen turned on the (Parameter) is set to (Value).\nWhen turned off the (Parameter) is reset to zero.",
            (int)Control.ControlType.SubMenu => "Opens another expression menu.\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
            (int)Control.ControlType.TwoAxisPuppet => "Puppet menu that maps the joystick to two parameters (-1 to +1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
            (int)Control.ControlType.FourAxisPuppet => "Puppet menu that maps the joystick to four parameters (0 to 1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
            (int)Control.ControlType.RadialPuppet => "Puppet menu that sets a value based on joystick rotation. (0 to 1)\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
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
        DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(0), "Paramater Rotation", false);
        return rect;
    }

    private Rect DrawFourAxisPuppetType(Rect rect, SerializedProperty controlProperty)
    {
        var subParameters = controlProperty.FindPropertyRelative("subParameters");
        var labels = controlProperty.FindPropertyRelative("labels");

        subParameters.arraySize = 4;
        labels.arraySize = 4;

        rect = DrawLine(rect);


        rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(0), "Parameter Up", false);
        rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(1), "Parameter Right", false);
        rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(2), "Parameter Down", false);
        rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(3), "Parameter Left", false);

        var dirRect = DrawFourDirection(rect, labels, new Vector2(50, 50), new Vector2(70, 70));

        return dirRect;
    }

    private Rect DrawTwoAxisPuppetType(Rect rect, SerializedProperty controlProperty)
    {
        var subParameters = controlProperty.FindPropertyRelative("subParameters");
        var labels = controlProperty.FindPropertyRelative("labels");

        subParameters.arraySize = 2;
        labels.arraySize = 4;

        rect = DrawLine(rect);

        rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(0), "Parameter Horizontal", false);
        rect = DrawParameterDropDownGUI(rect, subParameters.GetArrayElementAtIndex(1), "Parameter Vertical", false);

        var dirRect = DrawFourDirection(rect, labels, new Vector2(50, 50), new Vector2(70, 70));

        return dirRect;
    }

    private Rect DrawSubMenuType(Rect rect, SerializedProperty controlProperty)
    {
        var subMenu = controlProperty.FindPropertyRelative("subMenu");

        rect = DrawLine(rect);

        var subMenuRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

        DrawEditorGUI(subMenuRect, "SubMenu", drawRect =>
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
        EditorGUI.LabelField(labelRect, "Expression Menu Show");
        rect.y += labelRect.height + 2;

        var height = 240f;
        rect.height = height;

        GUI.Box(rect, "", "Badge");
        var center = new Rect(rect.center.x, rect.center.y - size.y / 4f, 10, 10);

        var upRect = new Rect(center.x - size.x / 2f, center.y - size.y / 2 - offset.y, size.x, size.y);
        var downRect = new Rect(center.x - size.x / 2f, center.y - size.y / 2 + offset.y, size.x, size.y);
        var leftRect = new Rect(center.x - size.x / 2f - offset.x, center.y - size.y / 2, size.x, size.y);
        var rightRect = new Rect(center.x - size.x / 2f + offset.x, center.y - size.y / 2, size.x, size.y);

        DrawLabelGUI(upRect, dirControls.GetArrayElementAtIndex(0), "Up");
        DrawLabelGUI(leftRect, dirControls.GetArrayElementAtIndex(1), "Right");
        DrawLabelGUI(downRect, dirControls.GetArrayElementAtIndex(2), "Down");
        DrawLabelGUI(rightRect, dirControls.GetArrayElementAtIndex(3), "Left");

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
        var height = 2f;
        rect.y += height;

        var boxRect = new Rect(rect.x, rect.y, rect.width - 5f, height);
        GUI.Box(boxRect, "");
        rect.y = boxRect.y + boxRect.height + height * 2;
        // rect.height += boxRect.height;
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


    void SelectAvatarDescriptor()
    {
        var descriptors = FindObjectsOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
        if (descriptors.Length > 0)
        {
            //Compile list of names
            string[] names = new string[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
                names[i] = descriptors[i].gameObject.name;

            //Select
            var currentIndex = Array.IndexOf(descriptors, activeDescriptor);
            var nextIndex = EditorGUILayout.Popup("Active Avatar", currentIndex, names);
            if (nextIndex < 0)
                nextIndex = 0;
            if (nextIndex != currentIndex)
                SelectAvatarDescriptor(descriptors[nextIndex]);
        }
        else
            SelectAvatarDescriptor(null);
    }

    void SelectAvatarDescriptor(VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc)
    {
        if (desc == activeDescriptor)
            return;

        activeDescriptor = desc;
        if (activeDescriptor != null)
        {
            //Init stage parameters
            int paramCount = desc.GetExpressionParameterCount();
            parameterNames = new string[paramCount + 1];
            parameterNames[0] = "[None]";
            for (int i = 0; i < paramCount; i++)
            {
                var param = desc.GetExpressionParameter(i);
                string name = "[None]";
                if (param != null && !string.IsNullOrEmpty(param.name))
                    name = string.Format("{0}, {1}", param.name, param.valueType.ToString(), i + 1);
                parameterNames[i + 1] = name;
            }
        }
        else
        {
            parameterNames = null;
        }
    }

    int GetExpressionParametersCount()
    {
        if (activeDescriptor != null && activeDescriptor.expressionParameters != null && activeDescriptor.expressionParameters.parameters != null)
            return activeDescriptor.expressionParameters.parameters.Length;
        return 0;
    }

    ExpressionParameters.Parameter GetExpressionParameter(int i)
    {
        if (activeDescriptor != null)
            return activeDescriptor.GetExpressionParameter(i);
        return null;
    }

    private Rect DrawParameterDropDownGUI(Rect rect, SerializedProperty parameter, string name, bool allowBool = true)
    {
        var parameterName = parameter.FindPropertyRelative("name");
        VRCExpressionParameters.Parameter param = null;
        string value = parameterName.stringValue;

        bool parameterFound = false;
        rect.height = EditorGUIUtility.singleLineHeight;
        var popupRect = new Rect(rect.x, rect.y, rect.width / 1.5f - 1f, rect.height);
        var textRect = new Rect(popupRect.x + popupRect.width + 1, rect.y, rect.width - popupRect.width - 2f, rect.height);

        if (activeDescriptor != null)
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
                for (int i = 0; i < GetExpressionParametersCount(); i++)
                {
                    var item = activeDescriptor.GetExpressionParameter(i);
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
            DrawEditorGUI(popupRect, name, drawRect => { currentIndex = EditorGUI.Popup(drawRect, currentIndex + 1, parameterNames); });

            if (EditorGUI.EndChangeCheck())
            {
                if (currentIndex == 0)
                    parameterName.stringValue = "";
                else
                    parameterName.stringValue = GetExpressionParameter(currentIndex - 1).name;
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
            var helpRect = DrawHelpBox(rect, "Parameter not found on the active avatar descriptor.", MessageType.Warning);
            rect.height += helpRect.height + 2;
        }

        if (!allowBool && param != null && param.valueType == ExpressionParameters.ValueType.Bool)
        {
            var helpRect = DrawHelpBox(rect, "Bool parameters not valid for this choice.", MessageType.Error);
            rect.height += helpRect.height + 2;
        }

        rect.y += rect.height + 2;
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
                    DrawEditorGUI(rect, "Value", drawRect =>
                    {
                        height = drawRect.height;
                        value.floatValue = EditorGUI.IntField(drawRect, Mathf.Clamp((int)value.floatValue, 0, 255));
                    });
                }
                else if (paramDef.valueType == ExpressionParameters.ValueType.Float)
                {
                    DrawEditorGUI(rect, "Value", drawRect =>
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
                DrawEditorGUI(rect, "Value", drawRect =>
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

    ExpressionParameters.Parameter FindExpressionParameterDef(string name)
    {
        if (activeDescriptor == null || string.IsNullOrEmpty(name))
            return null;

        //Find
        int length = GetExpressionParametersCount();
        for (int i = 0; i < length; i++)
        {
            var item = GetExpressionParameter(i);
            if (item != null && item.name == name)
                return item;
        }

        return null;
    }
}
#endif