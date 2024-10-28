﻿#if YUEBY_AVATAR_STYLE
using System;
using UnityEngine;
using UnityEditor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using ExpressionParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;
using Yueby.Utils;
using Editor = UnityEditor.Editor;

namespace Yueby.AvatarTools.VRCEditorOptimize
{
    [CustomEditor(typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters))]
    public class VRCExpressionParametersEditor : Editor
    {
        private YuebyReorderableList _paramRl;
        private SerializedProperty _parametersProperty;

        private static readonly VRCExParameterLocalization Localization = new VRCExParameterLocalization();
        private static Vector2 _scrollPos;

        private Rect _syncedRect;
        private Rect _savedRect;
        private Rect _defaultRect;
        private Rect _typeRect;
        private Rect _nameRect;

        public void OnEnable()
        {
            //Init parameters
            var expressionParameters = target as ExpressionParameters;
            if (expressionParameters != null && expressionParameters.parameters == null)
                InitExpressionParameters(true);

            // Init Reorderable List
            _parametersProperty = serializedObject.FindProperty("parameters");
            _paramRl = new YuebyReorderableList(serializedObject, _parametersProperty, true, true, false, Repaint);
            _paramRl.OnDraw += OnListDraw;
            _paramRl.OnHeaderBottomDraw += OnListHeaderBottomDraw;
            _paramRl.OnTitleDraw += OnTitleDraw;
            if (_scrollPos != Vector2.zero)
                _paramRl.ScrollPos = _scrollPos;
        }

        private void OnTitleDraw()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true));
            var rect = GUILayoutUtility.GetLastRect();
            if (rect.size.magnitude > 40f)
            {
                var cost = ((ExpressionParameters)target).CalcTotalCost();
                var value = (cost * 1f) / (ExpressionParameters.MAX_PARAMETER_COST * 1f);


                EditorGUI.ProgressBar(rect, value, $"{cost}/{ExpressionParameters.MAX_PARAMETER_COST}");
            }
        }

        private void OnListHeaderBottomDraw()
        {
            var style = new GUIStyle("Badge");
            style.clipping = TextClipping.Overflow;

            EditorUI.HorizontalEGL(style, () =>
            {
                // EditorGUILayout.Space(30);
                GUILayout.Space(20);

                EditorGUILayout.LabelField(Localization.Get("parameters_name"), GUILayout.MinWidth(100), GUILayout.MinWidth(10), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                _nameRect = GUILayoutUtility.GetLastRect();
                EditorGUILayout.LabelField(Localization.Get("parameters_type"), GUILayout.MaxWidth(70), GUILayout.MinWidth(10), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                _typeRect = GUILayoutUtility.GetLastRect();
                EditorGUILayout.LabelField(Localization.Get("parameters_default"), GUILayout.MaxWidth(60), GUILayout.MinWidth(10), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                _defaultRect = GUILayoutUtility.GetLastRect();
                EditorGUILayout.LabelField(Localization.Get("parameters_saved"), GUILayout.MaxWidth(60), GUILayout.MinWidth(10), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                _savedRect = GUILayoutUtility.GetLastRect();
                EditorGUILayout.LabelField(Localization.Get("parameters_synced"), GUILayout.MaxWidth(60), GUILayout.MinWidth(10), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                _syncedRect = GUILayoutUtility.GetLastRect();
            }, space: 0f);
        }


        private float OnListDraw(Rect rect, int index, bool arg2, bool arg3)
        {
            if (_parametersProperty.arraySize < index + 1)
                _parametersProperty.InsertArrayElementAtIndex(index);
            var item = _parametersProperty.GetArrayElementAtIndex(index);

            var itemName = item.FindPropertyRelative("name");
            var valueType = item.FindPropertyRelative("valueType");
            var defaultValue = item.FindPropertyRelative("defaultValue");
            var saved = item.FindPropertyRelative("saved");
            var synced = item.FindPropertyRelative("networkSynced");

            rect.y += 2;
            rect.height -= 4;

            var offset = -28;
            var syncedRect = new Rect(_syncedRect.x + offset + 3, rect.y, _savedRect.width, rect.height);
            var savedRect = new Rect(_savedRect.x + offset + 3, rect.y, _savedRect.width, rect.height);
            var defaultRect = new Rect(_defaultRect.x + offset + 3, rect.y + 2, _defaultRect.width - 3, EditorGUIUtility.singleLineHeight);
            var typeRect = new Rect(_typeRect.x + offset, rect.y + 2, _typeRect.width, rect.height);
            var nameRect = new Rect(_nameRect.x + offset, rect.y + 2, _nameRect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(syncedRect, synced, new GUIContent(""));
            EditorGUI.PropertyField(savedRect, saved, new GUIContent(""));


            if (valueType.intValue == (int)ExpressionParameters.ValueType.Bool)
            {
                var value = EditorGUI.Toggle(defaultRect, defaultValue.floatValue > 0f);
                defaultValue.floatValue = value ? 1f : 0f;
            }
            else if (valueType.intValue == (int)ExpressionParameters.ValueType.Int)
            {
                defaultValue.floatValue = EditorGUI.IntField(defaultRect, (int)defaultValue.floatValue);
            }
            else
                EditorGUI.PropertyField(defaultRect, defaultValue, new GUIContent(""));

            EditorGUI.PropertyField(typeRect, valueType, new GUIContent(""));
            EditorGUI.PropertyField(nameRect, itemName, new GUIContent(""));

            return EditorGUIUtility.singleLineHeight + 5;
        }


        public override void OnInspectorGUI()
        {
            Localization.DrawLanguageUI(Screen.width - 120);
            EditorGUILayout.Space(10);
            serializedObject.Update();
            {
                _paramRl.DoLayout(Localization.Get("parameters"), new Vector2(0, 600), false, false);

                _scrollPos = _paramRl.ScrollPos;


                EditorUI.VerticalEGL(() =>
                {
                    EditorUI.VerticalEGL("Badge", () =>
                    {
                        var cost = ((ExpressionParameters)target).CalcTotalCost();
                        if (cost > ExpressionParameters.MAX_PARAMETER_COST)
                        {
                            _paramRl.IsDisableAddButton = true;
                            EditorGUILayout.HelpBox(Localization.Get("parameters_out_of_memory"), MessageType.Error);
                        }
                        else
                            _paramRl.IsDisableAddButton = false;

                        //Info
                        EditorGUILayout.HelpBox(Localization.Get("parameters_tip_1"), MessageType.Info);
                        EditorGUILayout.HelpBox(Localization.Get("parameters_tip_2"), MessageType.Info);
                        EditorGUILayout.HelpBox(Localization.Get("parameters_tip_3"), MessageType.Info);

                        EditorUI.HorizontalEGL(() =>
                        {
                            //Clear
                            if (GUILayout.Button(Localization.Get("parameters_clear")))
                            {
                                if (EditorUtility.DisplayDialogComplex(Localization.Get("warning"), Localization.Get("parameters_clear_tip"), Localization.Get("yes"), Localization.Get("no"), "") == 0)
                                {
                                    InitExpressionParameters(false);
                                }

                                GUIUtility.ExitGUI();
                            }

                            if (GUILayout.Button(Localization.Get("parameters_to_default")))
                            {
                                if (EditorUtility.DisplayDialogComplex(Localization.Get("warning"), Localization.Get("parameters_reset_tip"), Localization.Get("yes"), Localization.Get("no"), "") == 0)
                                {
                                    InitExpressionParameters(true);
                                }

                                GUIUtility.ExitGUI();
                            }
                        });
                    }, true, 2, true);
                });
            }
            serializedObject.ApplyModifiedProperties();
        }


        void InitExpressionParameters(bool populateWithDefault)
        {
            var expressionParameters = target as ExpressionParameters;
            serializedObject.Update();
            {
                if (expressionParameters != null)
                {
                    if (populateWithDefault)
                    {
                        expressionParameters.parameters = new ExpressionParameter[3];

                        expressionParameters.parameters[0] = new ExpressionParameter();
                        expressionParameters.parameters[0].name = "VRCEmote";
                        expressionParameters.parameters[0].valueType = ExpressionParameters.ValueType.Int;

                        expressionParameters.parameters[1] = new ExpressionParameter();
                        expressionParameters.parameters[1].name = "VRCFaceBlendH";
                        expressionParameters.parameters[1].valueType = ExpressionParameters.ValueType.Float;

                        expressionParameters.parameters[2] = new ExpressionParameter();
                        expressionParameters.parameters[2].name = "VRCFaceBlendV";
                        expressionParameters.parameters[2].valueType = ExpressionParameters.ValueType.Float;
                    }
                    else
                    {
                        //Empty
                        expressionParameters.parameters = new ExpressionParameter[0];
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif