#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace Yueby.Utils
{
    public class YuebyReorderableList
    {
        private bool _isFoldout = true;
        private readonly bool _isShowAddButton;
        private readonly bool _isShowRemoveButton;

        private Vector2 _scrollPos;
        public UnityAction OnAdd;
        public UnityAction<Object, int> OnChanged;
        public UnityAction<Rect, int, bool, bool> OnDraw;
        public UnityAction<ReorderableList, Object> OnRemove;
        public UnityAction<Object, int> OnSelected;
        public ReorderableList List { get; }

        public YuebyReorderableList(SerializedObject serializedObject, SerializedProperty serializedProperty, float elementHeight, bool isShowAddButton, bool isShowRemoveButton, bool isPPTR = false)
        {
            _isShowAddButton = isShowAddButton;
            _isShowRemoveButton = isShowRemoveButton;

            List = new ReorderableList(serializedObject, serializedProperty, true, false, false, false)
            {
                headerHeight = 0,
                footerHeight = 0,
                elementHeight = elementHeight,
                drawElementCallback = OnListDraw,
                onMouseUpCallback = list =>
                {
                    var item = isPPTR ? serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue : null;
                    OnListSelected(item, list.index);
                },


                onAddCallback = _ =>
                {
                    serializedProperty.arraySize++;
                    OnListAdd();

                    if (List.count <= 1)
                    {
                        List.index = 0;
                        List.onMouseUpCallback?.Invoke(List);
                    }
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
                },
                onChangedCallback = list =>
                {
                    var item = isPPTR
                        ? serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue
                        : null;
                    OnListChanged(item, list.index);
                }
            };

            if (serializedProperty.arraySize > 0)
            {
                List.index = 0;
                List.onMouseUpCallback?.Invoke(List);
            }
        }


        public void DoLayout(string title, Vector2 area, UnityAction titleDraw = null, bool isNoBorder = false, bool hasFoldout = true)
        {
            var maxWidth = area.x;

            if (hasFoldout)
            {
                _isFoldout = YuebyUtil.Foldout(_isFoldout, title, () =>
                {
                    YuebyUtil.VerticalEGL(() =>
                    {
                        if (isNoBorder)
                            YuebyUtil.VerticalEGL(() => { DrawContent(titleDraw); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                        else
                            YuebyUtil.VerticalEGL("Badge", () => { DrawContent(titleDraw); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                        EditorGUILayout.Space(5);
                    }, GUILayout.MaxHeight(area.y), maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                });
            }
            else
            {
                YuebyUtil.VerticalEGL(() =>
                {
                    if (!string.IsNullOrEmpty(title))
                        YuebyUtil.TitleLabelField(title, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));

                    if (isNoBorder)
                        YuebyUtil.VerticalEGL(() => { DrawContent(titleDraw); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                    else
                        YuebyUtil.VerticalEGL("Badge", () => { DrawContent(titleDraw); }, maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
                    EditorGUILayout.Space(5);
                }, GUILayout.MaxHeight(area.y), maxWidth > 0 ? GUILayout.MaxWidth(maxWidth) : GUILayout.ExpandWidth(true));
            }
        }

        private void DrawContent(UnityAction titleDraw = null)
        {
            YuebyUtil.SpaceArea(() =>
            {
                // 绘制标题头
                YuebyUtil.HorizontalEGL(() =>
                {
                    YuebyUtil.HorizontalEGL("Badge", () =>
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

                YuebyUtil.Line(LineType.Horizontal, 2, 0);
                // 绘制列表内容
                _scrollPos = YuebyUtil.ScrollViewEGL(() =>
                {
                    if (List.count == 0)
                        EditorGUILayout.HelpBox("列表为空！", MessageType.Info);
                    else
                        List?.DoLayoutList();
                }, _scrollPos);
            });
        }

        public void DoLayoutHeaderGroup(string title, float maxHeight, UnityAction titleDraw = null, params GUILayoutOption[] options)
        {
            YuebyUtil.VerticalEGL(() =>
            {
                YuebyUtil.SpaceArea(() =>
                {
                    _isFoldout = YuebyUtil.FoldoutHeaderGroup(_isFoldout, title, () =>
                    {
                        EditorGUILayout.Space();
                        YuebyUtil.VerticalEGL("Badge", () =>
                        {
                            YuebyUtil.SpaceArea(() =>
                            {
                                // 绘制标题头
                                YuebyUtil.HorizontalEGL(() =>
                                {
                                    YuebyUtil.HorizontalEGL("Badge",
                                        () =>
                                        {
                                            EditorGUILayout.LabelField($"{List.count}", EditorStyles.centeredGreyMiniLabel,
                                                GUILayout.Width(25), GUILayout.Height(18));
                                        }, GUILayout.Width(25), GUILayout.Height(18));

                                    EditorGUILayout.Space();
                                    titleDraw?.Invoke();
                                    if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(18)))
                                        //添加
                                        List.onAddCallback?.Invoke(List);

                                    EditorGUI.BeginDisabledGroup(List.count == 0 || List.index == -1);
                                    if (GUILayout.Button("-", GUILayout.Width(25), GUILayout.Height(18))) List.onRemoveCallback?.Invoke(List);

                                    EditorGUI.EndDisabledGroup();
                                });

                                YuebyUtil.Line(LineType.Horizontal, 2, 0);
                                // 绘制列表内容
                                _scrollPos = YuebyUtil.ScrollViewEGL(() =>
                                {
                                    if (List.count == 0)
                                        EditorGUILayout.HelpBox("列表为空！", MessageType.Info);
                                    else
                                        List?.DoLayoutList();
                                }, _scrollPos);
                            });
                        }, option: GUILayout.MaxHeight(maxHeight));
                    });
                });
            }, options);
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
            OnDraw?.Invoke(rect, index, isActive, isFocused);
        }
    }
}
#endif