using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Yueby.Utils
{
    public class SelectionGrid
    {
        private readonly bool _isShowAddButton;
        private readonly bool _isShowRemoveButton;
        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _serializedProperty;
        private List<SelectionGridButton> _selectionGridButtons;

        public event UnityAction OnAdd;
        public event UnityAction<int, Object> OnRemove;
        public event UnityAction<SerializedProperty, int> OnChangeSelected;
        public event UnityAction<Rect, int> OnElementDraw;
        public event UnityAction OnHeaderBottomDraw;

        private static Vector2 _scrollPos;
        public event UnityAction OnTitleDraw;

        private int _selectedIndex = -1;
        private SelectionGridButton _selectedButton;

        private readonly bool _isPPTR;

        public SerializedProperty CurrentSelection => _selectedButton?.SerializedProperty;

        public int Count => _selectionGridButtons?.Count ?? 0;

        public int Index
        {
            get => _selectedIndex;
            set
            {
                if (value < 0)
                    value = 0;

                _selectedIndex = value;
                PlayerPrefs.SetInt("SelectionGridIndex", _selectedIndex);
            }
        }


        public SelectionGrid(SerializedObject serializedObject, SerializedProperty serializedProperty, UnityAction<SerializedProperty, int> onSelected = null, bool isShowAddButton = true, bool isShowRemoveButton = true, bool isPPTR = false)
        {
            _serializedObject = serializedObject;
            _serializedProperty = serializedProperty;
            _isShowAddButton = isShowAddButton;
            _isShowRemoveButton = isShowRemoveButton;
            _isPPTR = isPPTR;

            OnChangeSelected += onSelected;
            GenerateButtons();
        }

        public void Clear()
        {
            if (PlayerPrefs.HasKey("SelectionGridIndex"))
            {
                PlayerPrefs.DeleteKey("SelectionGridIndex");
            }
        }

        private void GenerateButtons()
        {
            if (_selectionGridButtons == null)
                _selectionGridButtons = new List<SelectionGridButton>();
            else
                _selectionGridButtons.Clear();

            if (_serializedProperty.arraySize <= 0) return;

            for (var i = 0; i < _serializedProperty.arraySize; i++)
            {
                var item = _serializedProperty.GetArrayElementAtIndex(i);
                var selectionGridButton = new SelectionGridButton(item, i);
                selectionGridButton.OnClick += Select;
                _selectionGridButtons.Add(selectionGridButton);
            }

            if (PlayerPrefs.HasKey("SelectionGridIndex") && Count > 0)
            {
                Index = PlayerPrefs.GetInt("SelectionGridIndex");
                if (Index < 0)
                    Index = 0;
                else if (Index > Count - 1)
                    Index = Count - 1;
            }
            else
                Index = 0;

            Select(Index);
        }

        public void Select(int index)
        {
            Index = index;

            _selectedButton?.Deselect();

            _selectedButton = _selectionGridButtons[index];
            _selectedButton.Select();

            OnChangeSelected?.Invoke(_selectedButton.SerializedProperty, index);
        }

        private void Add()
        {
            _serializedProperty.arraySize++;
            _serializedObject.ApplyModifiedProperties();
            OnAdd?.Invoke();

            GenerateButtons();
        }

        private void Remove()
        {
            var item = _isPPTR ? _serializedProperty.GetArrayElementAtIndex(Index).objectReferenceValue : null;

            OnRemove?.Invoke(Index, item);

            if (_isPPTR)
                _serializedProperty.GetArrayElementAtIndex(Index).objectReferenceValue = null;
            _serializedProperty.DeleteArrayElementAtIndex(Index);

            _serializedObject.ApplyModifiedProperties();


            if (_serializedProperty.arraySize > 1)
            {
                if (Index == Count - 1)
                    Index--;
                Select(Index);
            }

            GenerateButtons();
        }

        private void MoveLeft()
        {
            if (Index > 0)
            {
                _serializedProperty.MoveArrayElement(Index, Index - 1);
                Index--;
                Select(Index);
            }

            GenerateButtons();
        }

        private void MoveRight()
        {
            if (Index < _serializedProperty.arraySize - 1)
            {
                _serializedProperty.MoveArrayElement(Index, Index + 1);
                Index++;
                Select(Index);
            }

            GenerateButtons();
        }

        public void Draw(float elementEdgeLength, Vector2 padding, Vector2 area)
        {
            EditorUI.VerticalEGL("Badge", () =>
            {
                EditorUI.SpaceArea(() =>
                {
                    // 绘制标题头
                    EditorUI.HorizontalEGL(() =>
                    {
                        EditorUI.HorizontalEGL("Badge", () => { EditorGUILayout.LabelField($"{_serializedProperty.arraySize}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(25), GUILayout.Height(18)); }, GUILayout.Width(25), GUILayout.Height(18));

                        EditorGUILayout.Space();
                        OnTitleDraw?.Invoke();

                        if (_selectedButton != null && GUILayout.Button("<", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            MoveLeft();
                        }

                        if (_selectedButton != null && GUILayout.Button(">", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            MoveRight();
                        }

                        if (_isShowAddButton && GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            Add();
                        }

                        EditorGUI.BeginDisabledGroup(_serializedProperty.arraySize == 0);
                        if (_isShowRemoveButton && GUILayout.Button("-", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            Remove();
                        }

                        EditorGUI.EndDisabledGroup();
                    });

                    OnHeaderBottomDraw?.Invoke();
                    EditorUI.Line(LineType.Horizontal, 2, 0);
                    
                    
                    if (_selectionGridButtons.Count > 0)
                    {
                        // 绘制列表内容
                        _scrollPos = EditorUI.ScrollViewEGL(() =>
                        {
                            var width = area.x - 30;
                            var count = Mathf.Floor((width - padding.x) / (elementEdgeLength + padding.x));

                            var maxHeight = (elementEdgeLength + padding.y) * Mathf.Ceil(_serializedProperty.arraySize / count);

                            if (maxHeight > 0)
                            {
                                var gridRect = GUILayoutUtility.GetRect(elementEdgeLength, width, elementEdgeLength, maxHeight);

                                GUILayout.Label("", GUILayout.Width(0), GUILayout.Height(maxHeight - elementEdgeLength));

                                for (var i = 0; i < _selectionGridButtons.Count; i++)
                                {
                                    var xLine = i % (int)count;
                                    var yLine = i / (int)count;
                                    var x = gridRect.x + padding.x + (elementEdgeLength + padding.x) * xLine;
                                    var y = gridRect.y + padding.y + (elementEdgeLength + padding.y) * yLine;


                                    var btnRect = new Rect(x, y, elementEdgeLength, elementEdgeLength);

                                    var gridButton = _selectionGridButtons[i];
                                    gridButton.Draw(btnRect, (rect, index) =>
                                    {
                                        if (index > 0 || _serializedProperty.arraySize != 0)
                                            OnElementDraw?.Invoke(rect, index > _serializedProperty.arraySize - 1 ? 0 : index);
                                    });
                                }
                            }
                        }, _scrollPos, GUILayout.MaxWidth(area.x - 20));
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("请添加一件物品", MessageType.Info);
                    }
                });
            }, GUILayout.MaxWidth(area.x), GUILayout.MaxHeight(area.y));
        }

        public class SelectionGridButton
        {
            public bool IsSelected { get; private set; }
            public SerializedProperty SerializedProperty { get; private set; }

            public event UnityAction<int> OnClick;
            public int Index { get; }

            public SelectionGridButton(SerializedProperty serializedProperty, int index)
            {
                SerializedProperty = serializedProperty;
                Index = index;
            }

            public void Draw(Rect rect, UnityAction<Rect, int> onArrayItemDraw)
            {
                EditorGUI.BeginChangeCheck();
                UnityEngine.GUI.Toolbar(rect, IsSelected ? 0 : -1, new[] { "" });
                if (EditorGUI.EndChangeCheck())
                {
                    OnClick?.Invoke(Index);
                }


                onArrayItemDraw?.Invoke(rect, Index);
            }


            public void Select()
            {
                IsSelected = true;
            }

            public void Deselect()
            {
                IsSelected = false;
            }
        }
    }
}