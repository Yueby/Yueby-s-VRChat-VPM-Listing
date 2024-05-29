#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Yueby.Utils
{
    public class YuebyReorderableList
    {
        private SerializedProperty _serializedProperty;
        private bool _isFoldout = true;
        private readonly bool _isShowAddButton;
        private readonly bool _isShowRemoveButton;

        public bool IsDisableAddButton;
        public bool IsDisableRemoveButton;

        public Vector2 ScrollPos;
        public UnityAction OnAdd;
        public UnityAction<Object, int> OnChanged;
        public Func<Rect, int, bool, bool, float> OnDraw;
        public UnityAction<ReorderableList, Object> OnRemove;
        public UnityAction<Object, int> OnSelected;
        public UnityAction OnTitleDraw;
        public UnityAction OnHeaderBottomDraw;
        public float[] ElementHeights;
        public ReorderableList List { get; }

        private readonly UnityAction _onRepaint;

        public YuebyReorderableList(SerializedObject serializedObject, SerializedProperty serializedProperty, bool isShowAddButton, bool isShowRemoveButton, bool isPPTR = false, UnityAction repaint = null)
        {
            _serializedProperty = serializedProperty;
            _isShowAddButton = isShowAddButton;
            _isShowRemoveButton = isShowRemoveButton;
            ElementHeights = new float[serializedProperty.arraySize];
            _onRepaint = repaint;
            List = new ReorderableList(serializedObject, serializedProperty, true, false, false, false)
            {
                headerHeight = 0,
                footerHeight = 0,
                drawElementCallback = OnListDraw,
                onMouseUpCallback = list =>
                {
                    var item = isPPTR ? serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue : null;
                    OnListSelected(item, list.index);
                },

                onAddCallback = list =>
                {
                    serializedProperty.arraySize++;
                    OnListAdd();

                    if (List.count <= 1)
                    {
                        List.index = 0;
                        List.onMouseUpCallback?.Invoke(List);
                    }

                    Array.Resize(ref ElementHeights, _serializedProperty.arraySize);
                },
                onRemoveCallback = reorderableList =>
                {
                    var item = isPPTR ? serializedProperty.GetArrayElementAtIndex(reorderableList.index).objectReferenceValue : null;
                    if (isPPTR)
                        serializedProperty.GetArrayElementAtIndex(reorderableList.index).objectReferenceValue = null;
                    serializedProperty.DeleteArrayElementAtIndex(reorderableList.index);

                    OnListRemove(reorderableList, item);
                    if (reorderableList.count > 0 && reorderableList.index != 0)
                    {
                        if (reorderableList.index == reorderableList.count)
                        {
                            reorderableList.index--;
                        }

                        List.onMouseUpCallback?.Invoke(List);
                    }

                    Array.Resize(ref ElementHeights, _serializedProperty.arraySize);
                },
                onChangedCallback = list =>
                {
                    var item = isPPTR
                        ? serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue
                        : null;
                    OnListChanged(item, list.index);
                },
                elementHeightCallback = index =>
                {
                    if (index < 0 || index > ElementHeights.Length - 1) return 0;
                    _onRepaint?.Invoke();
                    var height = ElementHeights[index];
                    if (ElementHeights.Length != serializedProperty.arraySize)
                        Array.Resize(ref ElementHeights, serializedProperty.arraySize);

                    return height;
                }
            };

            // Debug.Log(serializedProperty.arraySize);

            if (serializedProperty.arraySize > 0)
            {
                List.index = 0;
                List.onMouseUpCallback?.Invoke(List);

                // EditorWindow.focusedWindow.ShowNotification(new GUIContent(""));
            }

            // List.ClearSelection();

            // EditorUtils.WaitToDo(10, "Repaint", _onRepaint);
        }

        public void DoLayout(string title, Vector2 area, bool isNoBorder = false, bool hasFoldout = true)
        {
            var maxWidth = area.x;

            if (hasFoldout)
            {
                _isFoldout = EditorUI.Foldout(_isFoldout, title, () =>
                {
                    EditorUI.VerticalEGL(() =>
                    {
                        if (isNoBorder)
                            EditorUI.VerticalEGL(DrawContent, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                        else
                            EditorUI.VerticalEGL("Badge", DrawContent, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                        EditorGUILayout.Space(5);
                    }, GUILayout.MaxHeight(area.y), maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                });
            }
            else
            {
                EditorUI.VerticalEGL(() =>
                {
                    if (!string.IsNullOrEmpty(title))
                        EditorUI.TitleLabelField(title, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));

                    if (isNoBorder)
                        EditorUI.VerticalEGL(DrawContent, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                    else
                        EditorUI.VerticalEGL("Badge", DrawContent, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                    EditorGUILayout.Space(5);
                }, GUILayout.MaxHeight(area.y), maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
            }
        }

        public void DoLayout(string title, bool isNoBorder = false, bool hasFoldout = true)
        {
            if (_serializedProperty.arraySize != ElementHeights.Length)
                Array.Resize(ref ElementHeights, _serializedProperty.arraySize);
            if (hasFoldout)
            {
                _isFoldout = EditorUI.Foldout(_isFoldout, title, () =>
                {
                    EditorUI.VerticalEGL(() =>
                    {
                        if (isNoBorder)
                            EditorUI.VerticalEGL(DrawContentNoScroll);
                        else
                            EditorUI.VerticalEGL("Badge", DrawContentNoScroll);
                        EditorGUILayout.Space(5);
                    });
                });
            }
            else
            {
                if (!string.IsNullOrEmpty(title))
                    EditorUI.TitleLabelField(title);

                if (isNoBorder)
                    EditorUI.VerticalEGL(DrawContentNoScroll);
                else
                    EditorUI.VerticalEGL("Badge", DrawContentNoScroll);
            }
        }

        private void DrawContent()
        {
            EditorUI.SpaceArea(() =>
            {
                // 绘制标题头
                EditorUI.HorizontalEGL(() =>
                {
                    EditorUI.HorizontalEGL("Badge", () =>
                    {
                        EditorGUILayout.LabelField($"{List.count}", EditorStyles.centeredGreyMiniLabel,
                            GUILayout.Width(25), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }, GUILayout.Width(25), GUILayout.Height(EditorGUIUtility.singleLineHeight));

                    if (OnTitleDraw == null)
                        EditorGUILayout.Space();
                    else
                    {
                        OnTitleDraw.Invoke();
                    }

                    if (_isShowAddButton)
                    {
                        EditorGUI.BeginDisabledGroup(IsDisableAddButton);
                        if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                        {
                            //添加
                            List.onAddCallback?.Invoke(List);
                        }

                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUI.BeginDisabledGroup(List.count == 0 || List.index == -1);

                    if (_isShowRemoveButton)
                    {
                        EditorGUI.BeginDisabledGroup(IsDisableRemoveButton);
                        if (GUILayout.Button("-", GUILayout.Width(25), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                            List.onRemoveCallback?.Invoke(List);
                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUI.EndDisabledGroup();
                });

                if (OnHeaderBottomDraw != null)
                {
                    EditorUI.Line(LineType.Horizontal, 2, 0);
                    OnHeaderBottomDraw.Invoke();
                }

                EditorUI.Line(LineType.Horizontal, 2, 0);
                // 绘制列表内容
                ScrollPos = EditorUI.ScrollViewEGL(() =>
                {
                    if (List.count == 0)
                        EditorGUILayout.HelpBox("List is null!", MessageType.Info);
                    else
                        List?.DoLayoutList();
                }, ScrollPos);
            });
        }

        private void DrawContentNoScroll()
        {
            EditorUI.SpaceArea(() =>
            {
                // 绘制标题头
                EditorUI.HorizontalEGL(() =>
                {
                    EditorUI.HorizontalEGL("Badge", () =>
                    {
                        EditorGUILayout.LabelField($"{List.count}", EditorStyles.centeredGreyMiniLabel,
                            GUILayout.Width(25), GUILayout.Height(18));
                    }, GUILayout.Width(25), GUILayout.Height(18));

                    if (OnTitleDraw == null)
                        EditorGUILayout.Space();
                    else
                    {
                        OnTitleDraw.Invoke();
                    }

                    if (_isShowAddButton)
                    {
                        EditorGUI.BeginDisabledGroup(IsDisableAddButton);
                        if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            //添加
                            List.onAddCallback?.Invoke(List);
                        }

                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUI.BeginDisabledGroup(List.count == 0 || List.index == -1);

                    if (_isShowRemoveButton)
                    {
                        EditorGUI.BeginDisabledGroup(IsDisableRemoveButton);
                        if (GUILayout.Button("-", GUILayout.Width(25), GUILayout.Height(18)))
                            List.onRemoveCallback?.Invoke(List);
                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUI.EndDisabledGroup();
                });

                if (OnHeaderBottomDraw != null)
                {
                    EditorUI.Line(LineType.Horizontal, 2, 0);
                    OnHeaderBottomDraw.Invoke();
                }

                EditorUI.Line(LineType.Horizontal, 2, 0);
                // 绘制列表内容
                if (List.count == 0)
                    EditorGUILayout.HelpBox("List is null!", MessageType.Info);
                else
                    List?.DoLayoutList();
            });
        }

        private void OnListAdd()
        {
            OnAdd?.Invoke();
        }

        private void OnListRemove(ReorderableList list, Object item)
        {
            OnRemove?.Invoke(list, item);
        }

        private void OnListChanged(Object item, int index)
        {
            OnChanged?.Invoke(item, index);
        }

        private void OnListSelected(Object item, int index)
        {
            OnSelected?.Invoke(item, index);
        }

        private void OnListDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (OnDraw == null) return;
            if (index < 0 || index > ElementHeights.Length - 1) return;
            var height = OnDraw.Invoke(rect, index, isActive, isFocused);
            ElementHeights[index] = height;
            Array.Resize(ref ElementHeights, _serializedProperty.arraySize);
        }
    }
}
#endif