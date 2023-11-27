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

                onSelectCallback = reorderableList =>
                {
                    var item = isPPTR ? serializedProperty.GetArrayElementAtIndex(reorderableList.index).objectReferenceValue : null;
                    OnListSelected(item, reorderableList.index);
                },
                onAddCallback = _ =>
                {
                    serializedProperty.arraySize++;
                    OnListAdd();
                },
                onRemoveCallback = reorderableList =>
                {
                    var item = isPPTR ? serializedProperty.GetArrayElementAtIndex(reorderableList.index).objectReferenceValue : null;
                    if (isPPTR)
                        serializedProperty.GetArrayElementAtIndex(reorderableList.index).objectReferenceValue = null;
                    serializedProperty.DeleteArrayElementAtIndex(reorderableList.index);
                    
                    Debug.Log(serializedProperty.arraySize);
                    OnListRemove(reorderableList, item);

                    if (reorderableList.count > 0 && reorderableList.index != 0)
                        reorderableList.index--;
                    
                    
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
            }
        }

        public ReorderableList List { get; }

        public void DoLayout(string title, float maxHeight, UnityAction titleDraw = null)
        {
            YuebyUtil.SpaceArea(() =>
            {
                _isFoldout = YuebyUtil.Foldout(_isFoldout, title, () =>
                {
                    EditorGUILayout.Space();
                    YuebyUtil.VerticalEGL("Badge", () =>
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
                    }, option: GUILayout.MaxHeight(maxHeight));
                });
            });
        }

        public void DoLayoutHeaderGroup(string title, float maxHeight, UnityAction titleDraw = null)
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