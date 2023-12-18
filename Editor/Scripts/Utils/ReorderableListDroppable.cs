using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using Yueby.Utils;
using Object = UnityEngine.Object;

namespace Yueby.AvatarTools
{
    public class ReorderableListDroppable
    {
        private readonly bool _isShowAddButton;
        private readonly bool _isShowRemoveButton;

        public AnimBool AnimBool { get; set; } = new AnimBool { speed = 3.0f };

        private Vector2 _scrollPos;
        public UnityAction<ReorderableList> OnAdd;
        public UnityAction<int> OnChanged;
        public Func<Rect, int, bool, bool, float> OnDraw;
        public UnityAction<ReorderableList> OnRemove;
        public UnityAction<int> OnRemoveBefore;
        public UnityAction<int> OnSelected;
        public UnityAction OnDrawTitle;
        public UnityAction OnHeaderBottomDraw;
        public float[] ElementHeights;
        private IList _elements;

        public UnityAction<int> OnElementHeightCallback;

        public UnityAction<bool> OnChangeAnimBoolTarget;
        public ReorderableList List { get; }
        private bool _isEnterListArea;

        private UnityAction _onRepaint;

        public readonly List<ReorderableListDroppable> InverseRlList = new List<ReorderableListDroppable>();

        public ReorderableListDroppable(IList elements, Type elementType, float elementHeight, UnityAction animBoolValueChangedRepaint, bool isShowAddButton = true, bool isShowRemoveButton = true, UnityAction onRepaint = null)
        {
            _elements = elements;
            _isShowAddButton = isShowAddButton;
            _isShowRemoveButton = isShowRemoveButton;
            ElementHeights = new float[elements.Count];

            _onRepaint = onRepaint;


            List = new ReorderableList(elements, elementType, true, false, false, false)
            {
                headerHeight = 0,
                footerHeight = 0,
                elementHeight = elementHeight,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    if (OnDraw == null) return;
                    if (index < 0 || index > ElementHeights.Length - 1) return;

                    Array.Resize(ref ElementHeights, elements.Count);
                    var height = OnDraw.Invoke(rect, index, active, focused);
                    ElementHeights[index] = height;
                },

                onSelectCallback = reorderableList => OnSelected?.Invoke(reorderableList.index),
                onAddCallback = list =>
                {
                    OnAdd?.Invoke(list);
                    Array.Resize(ref ElementHeights, elements.Count);
                },
                onRemoveCallback = reorderableList =>
                {
                    OnRemoveBefore?.Invoke(reorderableList.index);
                    elements.RemoveAt(reorderableList.index);
                    OnRemove?.Invoke(reorderableList);
                    if (reorderableList.count > 0 && reorderableList.index != 0)
                        reorderableList.index--;

                    Array.Resize(ref ElementHeights, elements.Count);
                },
                onChangedCallback = list => { OnChanged?.Invoke(list.index); },
                elementHeightCallback = index =>
                {
                    if (index < 0 || index > ElementHeights.Length - 1) return 0;
                    _onRepaint?.Invoke();

                    OnElementHeightCallback?.Invoke(index);
                    Array.Resize(ref ElementHeights, elements.Count);
                    var height = ElementHeights[index];

                    return height;
                }
            };

            if (elements.Count > 0)
            {
                List.index = 0;
            }

            AnimBool.valueChanged.RemoveAllListeners();
            AnimBool.valueChanged.AddListener(animBoolValueChangedRepaint);
        }

        private Rect _dropRect;
        private Rect _foldoutRect;

        public void ChangeAnimBool(bool value)
        {
            var lastBool = AnimBool.target;
            AnimBool.target = value;
            if (AnimBool.target != lastBool && AnimBool.target)
            {
                foreach (var inverse in InverseRlList)
                    inverse.AnimBool.target = false;
            }

            OnChangeAnimBoolTarget?.Invoke(AnimBool.target);
        }

        public void DoLayoutList(string title, Vector2 area, bool isNoBorder = false, bool hasFoldout = true, bool allowDrop = true, UnityAction<Object[]> onDropped = null, UnityAction repaint = null)
        {
            var listRect = new Rect();
            var maxWidth = area.x;

            if (hasFoldout)
            {
                ChangeAnimBool(EditorGUILayout.Foldout(AnimBool.target, title));

                _foldoutRect = GUILayoutUtility.GetLastRect();

                if (EditorGUILayout.BeginFadeGroup(AnimBool.faded))
                {
                    EditorUI.HorizontalEGL(() =>
                    {
                        EditorGUILayout.Space(5);
                        if (isNoBorder)
                            EditorUI.VerticalEGL(() => { DrawContent(OnDrawTitle); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                        else
                            EditorUI.VerticalEGL("Badge", () => { DrawContent(OnDrawTitle); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                        EditorGUILayout.Space(5);
                    }, GUILayout.MaxHeight(area.y), maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                    listRect = GUILayoutUtility.GetLastRect();
                }

                EditorGUILayout.EndFadeGroup();
            }
            else
            {
                EditorUI.VerticalEGL(() =>
                {
                    if (!string.IsNullOrEmpty(title))
                        EditorUI.TitleLabelField(title, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));

                    if (isNoBorder)
                        EditorUI.VerticalEGL(() => { DrawContent(OnDrawTitle); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                    else
                        EditorUI.VerticalEGL("Badge", () => { DrawContent(OnDrawTitle); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                    EditorGUILayout.Space(5);
                }, GUILayout.MaxHeight(area.y), maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                listRect = GUILayoutUtility.GetLastRect();
            }

            if (!allowDrop) return;

            if (!AnimBool.target)
            {
                if (Event.current.type == EventType.DragUpdated && _foldoutRect.Contains(Event.current.mousePosition))
                    ChangeAnimBool(true);

                return;
            }

            if (_isEnterListArea)
            {
                var label = "→";
                if (_dropRect.Contains(Event.current.mousePosition))
                {
                    label = "↓";
                }

                GUI.Box(_dropRect, label);
            }

            if (Event.current.type == EventType.DragUpdated)
            {
                _isEnterListArea = listRect.Contains(Event.current.mousePosition);
                repaint?.Invoke();
            }
            else if (Event.current.type == EventType.DragExited)
            {
                _isEnterListArea = false;
                if (_dropRect.Contains(Event.current.mousePosition))
                {
                    onDropped?.Invoke(DragAndDrop.objectReferences);
                    Array.Resize(ref ElementHeights, _elements.Count);
                    repaint?.Invoke();
                }
            }
        }

        private void DrawContent(UnityAction titleDraw = null)
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


                    EditorGUILayout.Space();
                    titleDraw?.Invoke();
                    if (_isShowAddButton && GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(18)))
                        //添加
                        List.onAddCallback?.Invoke(List);

                    EditorGUI.BeginDisabledGroup(List.count == 0 || List.index == -1);
                    if (_isShowRemoveButton && GUILayout.Button("-", GUILayout.Width(25), GUILayout.Height(18))) List.onRemoveCallback?.Invoke(List);

                    EditorGUI.EndDisabledGroup();
                });
                _dropRect = GUILayoutUtility.GetLastRect();

                EditorUI.Line(LineType.Horizontal, 2, 0);
                // 绘制列表内容
                _scrollPos = EditorUI.ScrollViewEGL(() =>
                {
                    if (List.count == 0)
                        EditorGUILayout.HelpBox("List is null!", MessageType.Info);
                    else
                        List?.DoLayoutList();
                }, _scrollPos);
            });
        }
    }
}