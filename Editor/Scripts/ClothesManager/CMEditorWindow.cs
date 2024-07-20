using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using Yueby.Utils;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using Object = UnityEngine.Object;

namespace Yueby.AvatarTools.ClothesManager
{
    public partial class CMEditorWindow : EditorWindow
    {
        #region Variables

        private static CMEditorWindow _window;

        private static readonly CMLocalization Localization = new CMLocalization();

        // Init
        private VRCAvatarDescriptor _descriptor;
        private CMAvatarDataReference _dataReference;

        private bool _isFoldoutVrcConfigure;

        private AnimatorController _fxLayer;
        private VRCExpressionsMenu _expressionsMenu;
        private VRCExpressionParameters _parameters;

        // Serialized Objects
        private SerializedObject _serializedObject;
        private SerializedProperty _categoriesProperty;
        private YuebyReorderableList _categoriesRl;

        private SerializedObject _categorySerializedObject;

        private SelectionGrid _clothesGrid;

        private SerializedProperty _clothesProperty;
        private CMClothesCategorySo _currentClothesCategory;
        private List<CMClothesData> _clothesList;
        private CMClothesData _clothes;
        private int _clothesIndex = -1;

        private ReorderableListDroppable _clothesShowRl, _clothesHideRl, _clothesSmrRL;
        private ReorderableListDroppable _enterDriverRl, _exitDriverRl;

        private TabBarGroup _configureTabBarGroup;
        private TabBarElement _categoryBar, _clothesBar;
        private TabBarElement _clothesParameterBar;
        private TabBarElement _clothesDriverBar;

        private Parameter _currentDriverParameter;
        private bool IsClothesPreview => _previewIndex > 0;
        private static int _previewIndex = 1;

        private const float ConfigurePageHeight = 480;
        private const float ConfigureListHeight = ConfigurePageHeight - 100;
        private const float ConfigureListWidth = 380f;

        private RenderTexture _previewRT;
        private bool _isStartCapture;
        private GameObject _captureGo;
        private GameObject _captureCameraGo;
        private Camera _captureCamera;

        private static int _clothesFoldoutIndex;
        private static int _driverFoldoutIndex;

        private readonly Texture2D[] _categoryIcons = new Texture2D[2];
        private readonly Texture2D[] _clothesIcons = new Texture2D[2];
        private readonly Texture2D[] _objectIcons = new Texture2D[2];
        private readonly Texture2D[] _listIcons = new Texture2D[2];
        private readonly Texture2D[] _titleIcons = new Texture2D[2];
        private Texture2D _nextIcon;

        private List<CMClothesData.ClothesAnimParameter> _copiedSMRData;

        private CMAvatarState _avatarState;

        #endregion

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/Clothes Manager", false, -22)]
        public static void OpenWindow()
        {
            _window = GetWindow<CMEditorWindow>();

            _window.titleContent = new GUIContent(Localization.Get("window_title"));
            _window.minSize = new Vector2(840, 650);
        }

        private void OnEnable()
        {
            GetIcons();

            _categoryBar = new TabBarElement(_categoryIcons, () => { _categoriesRl.DoLayout(Localization.Get("category"), new Vector2(150, ConfigurePageHeight + 20), false, false); });
            _clothesBar = new TabBarElement(_clothesIcons, () => { EditorUI.VerticalEGL(DrawSelectedCategory, GUILayout.MaxWidth(200), GUILayout.MaxHeight(ConfigurePageHeight), GUILayout.ExpandHeight(true)); });
            _clothesParameterBar = new TabBarElement(_objectIcons, DrawClothesAnimParameter, true, 5f)
            {
                IsVisible = false,
            };

            _clothesDriverBar = new TabBarElement(_listIcons, DrawClothesParameterDriver, false)
            {
                IsVisible = false,
            };

            _clothesParameterBar.InvertElements.Add(_clothesDriverBar);
            _clothesDriverBar.InvertElements.Add(_clothesParameterBar);

            _configureTabBarGroup = new TabBarGroup(new List<TabBarElement> { _categoryBar, _clothesBar, _clothesParameterBar, _clothesDriverBar });
            GetDescriptorOnEnable();
            InitSerializedObjects();
            if (_avatarState == null)
                _avatarState = new CMAvatarState(_descriptor.gameObject);
        }

        private void GetIcons()
        {
            _objectIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/DarkMode/object.png");
            _objectIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/LightMode/object.png");

            _listIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/DarkMode/list.png");
            _listIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/LightMode/list.png");

            _clothesIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/DarkMode/clothes.png");
            _clothesIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/LightMode/clothes.png");

            _categoryIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/DarkMode/category.png");
            _categoryIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/LightMode/category.png");

            _titleIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/DarkMode/icon.png");
            _titleIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/LightMode/icon.png");

            _nextIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Sprites/next.png");
        }

        private void OnDisable()
        {
            if (!IsClothesPreview)
                ResetAvatarState();

            if (_isStartCapture)
            {
                _isStartCapture = false;
                StopCapture(true, true);
            }
        }

        private void OnGUI()
        {
            if (_descriptor == null)
                GetDescriptorOnEnable();

            wantsMouseMove = true;

            if (_window == null)
                _window = GetWindow<CMEditorWindow>();
            _window.titleContent = new GUIContent(Localization.Get("window_title"), _titleIcons[!EditorGUIUtility.isProSkin ? 1 : 0]);

            EditorUI.DrawEditorTitle(Localization.Get("window_title"));
            Localization.DrawLanguageUI();

            DrawInit();

            if (_descriptor != null && _dataReference != null)
                DrawConfigure();
        }

        private void InitSerializedObjects()
        {
            if (!_dataReference) return;
            _serializedObject = new SerializedObject(_dataReference.Data);
            _categoriesProperty = _serializedObject.FindProperty(nameof(CMCDataSo.Categories));

            _categoriesRl = new YuebyReorderableList(_serializedObject, _categoriesProperty, true, true, true);
            _categoriesRl.OnAdd += OnCategoriesAdd;
            _categoriesRl.OnRemove += OnCategoriesRemove;
            _categoriesRl.OnDraw += OnCategoriesDraw;
            _categoriesRl.OnSelected += OnCategoriesSelected;

            if (_categoriesRl.List.count > 0)
            {
                var obj = _categoriesProperty.GetArrayElementAtIndex(_categoriesRl.List.index).objectReferenceValue;
                _categoriesRl.OnSelected?.Invoke(obj, _categoriesRl.List.index);
            }
        }

        #region Category Events

        private void OnCategoriesSelected(Object obj, int index)
        {
            _categorySerializedObject = new SerializedObject(obj);
            _currentClothesCategory = _categorySerializedObject.targetObject as CMClothesCategorySo;

            if (_currentClothesCategory != null)
            {
                _clothesList = _currentClothesCategory.Clothes;
                _clothes = null;
            }

            GetSelectedClothesData();

            if (!_clothesBar.IsDraw)
                _clothesBar.ChangeDrawState(true);
        }

        private float OnCategoriesDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            var categorySo = new SerializedObject(_categoriesProperty.GetArrayElementAtIndex(index).objectReferenceValue);

            var categoryIcon = categorySo.FindProperty(nameof(CMClothesCategorySo.Icon));
            var categoryName = categorySo.FindProperty(nameof(CMClothesCategorySo.Name));

            categorySo.UpdateIfRequiredOrScript();

            var iconRect = new Rect(rect.x, rect.y + 1, rect.height - 2, rect.height - 2);
            categoryIcon.objectReferenceValue = EditorGUI.ObjectField(iconRect, categoryIcon.objectReferenceValue, typeof(Texture2D), false);

            var nameLabelRect = new Rect(rect.x + iconRect.width + 1, iconRect.y, rect.width - iconRect.width - 1, EditorGUIUtility.singleLineHeight);
            var nameRect = new Rect(rect.x + iconRect.width + 1, nameLabelRect.y + nameLabelRect.height + 1, rect.width - iconRect.width - 1, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(nameLabelRect, Localization.Get("category_name"));

            EditorGUI.BeginChangeCheck();
            categoryName.stringValue = EditorGUI.TextField(nameRect, categoryName.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(categorySo.targetObject, "Category Undo Register");
            }

            if (categorySo.ApplyModifiedProperties())
            {
                if (categorySo.targetObject.name != categoryName.stringValue)
                {
                    categorySo.targetObject.name = categoryName.stringValue;
                    // YuebyUtil.SaveAndRefreshAssets();
                }
            }

            return 40f;
        }

        private void OnCategoriesRemove(ReorderableList list, Object obj)
        {
            var categorySo = obj as CMClothesCategorySo;
            if (categorySo != null)
            {
                var parameterName = $"YCM/{categorySo.Name}/Switch";
                if (_fxLayer != null)
                {
                    for (var i = 0; i < _fxLayer.parameters.Length; i++)
                    {
                        if (_fxLayer.parameters[i].name != parameterName) continue;
                        _fxLayer.RemoveParameter(i);
                        i--;
                    }

                    for (var i = 0; i < _fxLayer.layers.Length; i++)
                    {
                        if (_fxLayer.layers[i].name != parameterName && _fxLayer.layers[i].name != parameterName + "_ParameterDriver") continue;
                        _fxLayer.RemoveLayer(i);
                        i--;
                    }
                }

                if (_parameters != null)
                {
                    var newParameters = _parameters.parameters.ToList();

                    var removeList = new List<VRCExpressionParameters.Parameter>();
                    foreach (var par in newParameters.Where(par => par.name == parameterName))
                    {
                        removeList.Add(par);
                        break;
                    }

                    foreach (var remove in removeList)
                        newParameters.Remove(remove);
                    _parameters.parameters = newParameters.ToArray();
                }
            }

            EditorUtils.RemoveChildAsset(obj);
        }

        private void OnCategoriesAdd()
        {
            var categorySo = EditorUtils.AddChildAsset<CMClothesCategorySo>(_serializedObject.targetObject);
            categorySo.Clear();
            categorySo.Name = $"{Localization.Get("new_category_name")} " + (_categoriesProperty.arraySize - 1);
            categorySo.name = categorySo.Name;

            _categoriesProperty.GetArrayElementAtIndex(_categoriesProperty.arraySize - 1).objectReferenceValue = categorySo;
        }

        #endregion

        #region Clothes Grid Events

        private bool _isGridInit;

        private void GetSelectedClothesData()
        {
            _clothesProperty = _categorySerializedObject.FindProperty(nameof(CMClothesCategorySo.Clothes));
            _isGridInit = true;
            _clothesGrid = new SelectionGrid(_categorySerializedObject, _clothesProperty, OnClothesGridChangeSelected);
            _clothesGrid.OnAdd += OnClothesGridAdd;
            _clothesGrid.OnRemove += OnClothesGridRemove;
            _clothesGrid.OnElementDraw += DrawClothesGridElement;
            _clothesGrid.OnHeaderBottomDraw += () =>
            {
                if (_clothes == null) return;
                EditorUI.Line();

                EditorUI.HorizontalEGL(() =>
                {
                    _clothes.Icon = (Texture2D)EditorGUILayout.ObjectField(_clothes.Icon, typeof(Texture2D), false, GUILayout.Width(40), GUILayout.Height(40));

                    EditorGUI.BeginChangeCheck();
                    _clothes.Name = EditorUI.TextFieldVertical(Localization.Get("clothes_name"), _clothes.Name, 100);
                    if (EditorGUI.EndChangeCheck())
                        Undo.RegisterCompleteObjectUndo(_currentClothesCategory, "Category Undo Register");
                });
            };
            if (_clothesGrid.Count > 0 && _currentClothesCategory.Selected < _clothesGrid.Count)
                _clothesGrid.Select(_currentClothesCategory.Selected);
        }

        private void OnClothesGridChangeSelected(SerializedProperty sp, int i)
        {
            _clothesIndex = i;
            _clothes = _clothesList[i];

            if (!_isGridInit)
                _currentClothesCategory.Selected = i;

            InitClothData();
            UnityEngine.GUI.FocusControl(null);

            if (_categoryBar.IsDraw)
                _categoryBar.ChangeDrawState(false);

            _currentDriverParameter = null;
            // if (!_isGridInit)
            //     ResetAvatarState();
            PreviewConfig();

            if (_isGridInit)
                _isGridInit = false;
        }

        private void InitClothData()
        {
            _clothesShowRl = new ReorderableListDroppable(_clothes.ShowParameters, typeof(GameObject), EditorGUIUtility.singleLineHeight + 5, Repaint);
            _clothesHideRl = new ReorderableListDroppable(_clothes.HideParameters, typeof(GameObject), EditorGUIUtility.singleLineHeight + 5, Repaint);
            _clothesSmrRL = new ReorderableListDroppable(_clothes.SMRParameters, typeof(SkinnedMeshRenderer), EditorGUIUtility.singleLineHeight * 2 + 5, Repaint);
            _clothesShowRl.InverseRlList.AddRange(new[] { _clothesHideRl, _clothesSmrRL });
            _clothesHideRl.InverseRlList.AddRange(new[] { _clothesShowRl, _clothesSmrRL });
            _clothesSmrRL.InverseRlList.AddRange(new[] { _clothesHideRl, _clothesShowRl });

            if (_clothesFoldoutIndex == 0) _clothesShowRl.ChangeAnimBool(true);
            else if (_clothesFoldoutIndex == 1) _clothesHideRl.ChangeAnimBool(true);
            else if (_clothesFoldoutIndex == 2) _clothesSmrRL.ChangeAnimBool(true);

            _clothesShowRl.OnChangeAnimBoolTarget += value =>
            {
                if (value) _clothesFoldoutIndex = 0;
            };
            _clothesHideRl.OnChangeAnimBoolTarget += value =>
            {
                if (value) _clothesFoldoutIndex = 1;
            };
            _clothesSmrRL.OnChangeAnimBoolTarget += value =>
            {
                if (value) _clothesFoldoutIndex = 2;
            };

            _clothesShowRl.OnAdd += _ =>
            {
                _clothes.ShowParameters.Add(new CMClothesData.ClothesAnimParameter()
                {
                    Type = nameof(GameObject)
                });
            };
            _clothesHideRl.OnAdd += _ =>
            {
                _clothes.HideParameters.Add(new CMClothesData.ClothesAnimParameter()
                {
                    Type = nameof(GameObject)
                });
            };

            _clothesSmrRL.OnAdd += _ =>
            {
                _clothes.SMRParameters.Add(new CMClothesData.ClothesAnimParameter()
                {
                    Type = nameof(SkinnedMeshRenderer)
                });
            };

            _clothesShowRl.OnRemove += _ => { PreviewConfig(); };
            _clothesHideRl.OnRemove += _ => { PreviewConfig(); };
            _clothesSmrRL.OnRemove += _ => { PreviewConfig(); };
            _clothesSmrRL.OnRemoveBefore += index =>
            {
                var target = _clothes.SMRParameters[index];
                if (target.SmrParameter.Index == -1) return;
                var smrList = GetOtherSMRParameters(target);
                if (smrList.Count > 0 && EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_smr_remove_tip"), smrList.Count), Localization.Get("ok"), Localization.Get("cancel")))
                {
                    RemoveSMRParameterToOther(target);
                }
            };

            _clothesShowRl.OnDraw += (rect, index, a, b) => { return RegisterClothPathListPanel(rect, index, ref _clothes.ShowParameters); };
            _clothesHideRl.OnDraw += (rect, index, a, b) => { return RegisterClothPathListPanel(rect, index, ref _clothes.HideParameters); };
            _clothesSmrRL.OnDraw += (rect, index, a, b) => { return RegisterClothPathListPanel(rect, index, ref _clothes.SMRParameters); };

            // Parameter Driver ReorderableList Init
            _enterDriverRl = new ReorderableListDroppable(_clothes.EnterParameter.Parameters, typeof(Parameter), EditorGUIUtility.singleLineHeight + 5, Repaint);
            _exitDriverRl = new ReorderableListDroppable(_clothes.ExitParameter.Parameters, typeof(Parameter), EditorGUIUtility.singleLineHeight + 5, Repaint);
            _enterDriverRl.InverseRlList.Add(_exitDriverRl);
            _exitDriverRl.InverseRlList.Add(_enterDriverRl);

            if (_driverFoldoutIndex == 0) _enterDriverRl.ChangeAnimBool(true);
            else if (_driverFoldoutIndex == 1) _exitDriverRl.ChangeAnimBool(true);

            _enterDriverRl.OnChangeAnimBoolTarget += value =>
            {
                if (value) _driverFoldoutIndex = 0;
            };
            _exitDriverRl.OnChangeAnimBoolTarget += value =>
            {
                if (value) _driverFoldoutIndex = 1;
            };

            _enterDriverRl.OnAdd += _ =>
            {
                _clothes.EnterParameter.Parameters.Add(new Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set
                });
            };

            _exitDriverRl.OnAdd += _ =>
            {
                _clothes.ExitParameter.Parameters.Add(new Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set
                });
            };

            _enterDriverRl.OnSelected += index => { _currentDriverParameter = _clothes.EnterParameter.Parameters[index]; };
            _exitDriverRl.OnSelected += index => { _currentDriverParameter = _clothes.ExitParameter.Parameters[index]; };

            _enterDriverRl.OnDrawTitle += () => { _clothes.EnterParameter.IsLocal = EditorUI.Toggle(_clothes.EnterParameter.IsLocal, Localization.Get("driver_is_local"), 80); };
            _exitDriverRl.OnDrawTitle += () => { _clothes.ExitParameter.IsLocal = EditorUI.Toggle(_clothes.ExitParameter.IsLocal, Localization.Get("driver_is_local"), 80); };

            _enterDriverRl.OnDraw += (rect, index, active, focused) => { return DrawParameterDriverElement(ref _clothes.EnterParameter.Parameters, rect, index); };
            _exitDriverRl.OnDraw += (rect, index, active, focused) => { return DrawParameterDriverElement(ref _clothes.ExitParameter.Parameters, rect, index); };

            _clothesSmrRL.OnDrawTitle += () =>
            {
                if (_clothes != null && _clothes.SMRParameters.Count > 0 && GUILayout.Button(Localization.Get("clothes_smr_copy"), GUILayout.Width(50)))
                {
                    CopyCurrentSMR();
                }

                if (_clothes != null && _copiedSMRData != null && _copiedSMRData.Count > 0 && GUILayout.Button(Localization.Get("clothes_smr_paste"), GUILayout.Width(50)))
                {
                    // create the menu and add items to it
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent(Localization.Get("clothes_smr_paste_overwrite")), false, PasteCurrentSMRAll);
                    menu.AddItem(new GUIContent(Localization.Get("clothes_smr_paste_additive")), false, PasteUniqueSMR);

                    menu.ShowAsContext();
                }
            };
        }

        private void PasteUniqueSMR()
        {
            var addList = new List<CMClothesData.ClothesAnimParameter>();

            foreach (var data in _copiedSMRData)
            {
                if (!_clothes.ContainsInList(data, _clothes.SMRParameters))
                    addList.Add(data);
            }

            _clothes.SMRParameters.AddRange(addList);

            _copiedSMRData = null;
            OnClothesGridChangeSelected(null, _clothesIndex);
        }

        private void PasteCurrentSMRAll()
        {
            _clothes.SMRParameters = _copiedSMRData;
            _copiedSMRData = null;
            OnClothesGridChangeSelected(null, _clothesIndex);
        }

        private void CopyCurrentSMR()
        {
            _copiedSMRData = new List<CMClothesData.ClothesAnimParameter>();
            foreach (var parameter in _clothes.SMRParameters)
            {
                _copiedSMRData.Add(new CMClothesData.ClothesAnimParameter(parameter));
            }
        }

        private float DrawParameterDriverElement(ref List<Parameter> drivers, Rect rect, int index)
        {
            var height = EditorGUIUtility.singleLineHeight + 2;
            var typeRect = new Rect(rect.x, rect.y + 2, 70, rect.height);
            var driver = drivers[index];
            driver.type = (VRC_AvatarParameterDriver.ChangeType)EditorGUI.Popup(typeRect, (int)driver.type, Enum.GetNames(typeof(VRC_AvatarParameterDriver.ChangeType)));

            var nameRect = new Rect(typeRect.x + typeRect.width + 2, typeRect.y, 120, typeRect.height);

            var names = GetParameterNames();
            if (names.Length <= 0) return height;
            var indexInParameterNames = IndexInParameterNames(driver.name);
            if (indexInParameterNames == 0 && !string.IsNullOrEmpty(driver.name))
                names[0] = driver.name;

            EditorGUI.BeginChangeCheck();
            var nameIndex = EditorGUI.Popup(nameRect, indexInParameterNames, names);
            if (EditorGUI.EndChangeCheck())
                driver.name = names[nameIndex];

            if (nameIndex == 0 && driver.customType == 0)
            {
                return height;
            }
            var valueRect = new Rect(nameRect.x + nameRect.width + 4, rect.y, rect.width - nameRect.width - typeRect.width - 6, nameRect.height - 2);
            var type = GetParameterType(driver.name);
            if (type == 0)
                type = driver.customType;
            switch (type)
            {
                case AnimatorControllerParameterType.Int:
                    driver.value = EditorGUI.IntField(valueRect, (int)driver.value);
                    break;
                case AnimatorControllerParameterType.Float:
                    driver.value = EditorGUI.FloatField(valueRect, driver.value);
                    break;
                case AnimatorControllerParameterType.Bool:
                    var valueBool = Math.Abs(driver.value - 1f) <= 0f;
                    valueRect.width = 20;
                    valueBool = EditorGUI.Toggle(valueRect, valueBool);
                    driver.value = valueBool ? 1f : 0f;
                    break;
            }

            return height;
        }

        private void OnClothesGridRemove(int index, Object obj)
        {
            if (_clothesList[index] == _clothes)
                _clothes = null;
        }

        private void OnClothesGridAdd()
        {
            var cloth = _clothesList[_clothesList.Count - 1];
            cloth.Clear();
            cloth.Name = _currentClothesCategory.Name + " " + (_clothesList.Count - 1);

            EditorUtils.WaitToDo(20, "Wait To Select Grid Added", () => { _clothesGrid.Select(_clothesGrid.Count - 1); });
        }

        private void DrawClothesGridElement(Rect rect, int i)
        {
            var currentCloth = _clothesList[i];

            if (currentCloth == null) return;

            rect.x += 2;
            rect.y += 2;
            rect.width -= 4;
            rect.height -= 4;
            if (currentCloth.Icon != null)
                UnityEngine.GUI.DrawTexture(rect, currentCloth.Icon);

            if (_currentClothesCategory && _currentClothesCategory.Default == i)
            {
                var defaultIconRect = new Rect(rect.x + 2, rect.y + 1, 20, 20);
                EditorGUI.LabelField(defaultIconRect, "*");
            }

            var labelRect = new Rect(rect.x, rect.y + rect.height / 1.5f, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, currentCloth.Name);
        }

        #endregion

        #region UI

        private void DrawConfigure()
        {
            if (_serializedObject != null && _serializedObject.targetObject != null)
            {
                _serializedObject.UpdateIfRequiredOrScript();

                EditorUI.HorizontalEGL(() =>
                {
                    _configureTabBarGroup.Draw(ConfigurePageHeight);

                    _categoryBar.Draw();
                    _clothesBar.Draw();

                    _clothesParameterBar.IsVisible = !_categoryBar.IsDraw;
                    _clothesDriverBar.IsVisible = _clothesParameterBar.IsVisible;

                    string titleLabel;
                    if (_clothes != null)
                        titleLabel = _clothesBar.IsDraw ? Localization.Get("clothes_configure") : $"{Localization.Get("clothes_configure")} : {_currentClothesCategory.Name} | {_clothes.Name}";
                    else
                        titleLabel = _clothesBar.IsDraw ? Localization.Get("clothes_configure") : $"{Localization.Get("clothes_configure")} : {_currentClothesCategory.Name} | {Localization.Get("clothes_configure_none_clothes")}";

                    EditorUI.VerticalEGLTitled(titleLabel, DrawClothConfigArea, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                }, GUILayout.MaxHeight(ConfigurePageHeight));
                _serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawClothConfigArea()
        {
            if (_categorySerializedObject != null && _categorySerializedObject.targetObject != null && _clothes != null)
            {
                _categorySerializedObject.UpdateIfRequiredOrScript();

                EditorUI.HorizontalEGL(() =>
                {
                    EditorUI.VerticalEGL(() =>
                    {
                        _clothesParameterBar.Draw();
                        _clothesDriverBar.Draw();
                    }, GUILayout.MinWidth(ConfigureListWidth));

                    if (_categoryBar.IsDraw) return;
                    EditorUI.Line(LineType.Vertical);

                    EditorUI.VerticalEGL(() =>
                    {
                        // var bkgColor = GUI.backgroundColor;
                        // if (IsClothesPreview)
                        //     GUI.backgroundColor = Color.green;

                        EditorGUILayout.LabelField(Localization.Get("clothes_preview"), GUILayout.Width(80));
                        EditorGUI.BeginChangeCheck();
                        var previewContents = new[] { Localization.Get("clothes_preview_none"), Localization.Get("clothes_preview_all"), Localization.Get("clothes_preview_current") };
                        _previewIndex = GUILayout.Toolbar(_previewIndex, previewContents);

                        if (EditorGUI.EndChangeCheck())
                        {
                            PreviewConfig();
                        }
                        // if (GUILayout.Button(Localization.Get("clothes_preview")))
                        // {
                        //     // IsClothesPreview = !IsClothesPreview;
                        //    
                        // }

                        // GUI.backgroundColor = bkgColor;

                        var isCurrentDefault = _currentClothesCategory.Default == _clothesIndex;

                        EditorGUI.BeginDisabledGroup(isCurrentDefault);
                        if (GUILayout.Button(isCurrentDefault ? Localization.Get("clothes_already_default") : Localization.Get("clothes_set_to_default")))
                        {
                            _currentClothesCategory.Default = _clothesIndex;
                        }

                        EditorGUI.EndDisabledGroup();

                        EditorUI.Line();

                        EditorUI.HorizontalEGL(() =>
                        {
                            EditorGUILayout.LabelField(Localization.Get("parent_menu"), GUILayout.Width(40));
                            EditorGUI.BeginChangeCheck();
                            _dataReference.ParentMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(_dataReference.ParentMenu, typeof(VRCExpressionsMenu), false);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RegisterCompleteObjectUndo(_dataReference, "RegisterDataReference");
                            }
                        });

                        if (_dataReference.ParentMenu == null)
                        {
                            EditorGUILayout.HelpBox(Localization.Get("parent_menu_tip"), MessageType.Warning);
                        }

                        EditorUI.HorizontalEGL(() =>
                        {
                            if (GUILayout.Button(Localization.Get("tool_save_path_change"), GUILayout.Width(80)))
                            {
                                EditorUtils.MoveFolderFromPath(ref _dataReference.SavePath, "ClothesManager");
                                EditorUtility.DisplayDialog(Localization.Get("tips"), Localization.Get("tool_save_path_change_success"), Localization.Get("ok"));
                            }

                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.TextField(_dataReference.SavePath);
                            EditorGUI.EndDisabledGroup();
                        });

                        var backupPath = GetIDPath() + "/Backups";
                        if (Directory.Exists(backupPath) && GUILayout.Button(Localization.Get("open_backup_path")))
                        {
                            EditorUtils.PingProject(backupPath);
                        }

                        if (GUILayout.Button(Localization.Get("apply")))
                        {
                            Apply(_dataReference.Data);
                        }

                        EditorUI.Line();

                        if (!_isStartCapture && GUILayout.Button(Localization.Get("capture_start")))
                        {
                            _isStartCapture = true;
                            SetupCapture();
                        }

                        if (_isStartCapture)
                        {
                            GUILayout.Box("", GUILayout.Width(160), GUILayout.Height(160));

                            var rect = GUILayoutUtility.GetLastRect();
                            EditorGUI.DrawTextureTransparent(rect, _previewRT);

                            EditorUI.HorizontalEGL(() =>
                            {
                                if (GUILayout.Button(Localization.Get("capture")))
                                {
                                    GetClothesCapture();
                                }

                                if (GUILayout.Button(Localization.Get("cancel")))
                                {
                                    _isStartCapture = false;
                                    StopCapture(true, true);
                                }

                                if (GUILayout.Button(EditorGUIUtility.IconContent("d_SceneViewCamera"), GUILayout.Height(EditorGUIUtility.singleLineHeight - 1)))
                                {
                                    if (_captureCamera)
                                    {
                                        _captureCamera.orthographic = !_captureCamera.orthographic;
                                    }
                                }
                            });

                            if (!GetWindow<SceneView>().drawGizmos)
                            {
                                EditorGUILayout.HelpBox(Localization.Get("capture_tip_warning"), MessageType.Warning);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(Localization.Get("capture_tip_info"), MessageType.Info);
                            }
                        }
                    });
                });

                _categorySerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_categorySerializedObject.targetObject);
            }
        }

        private void DrawClothesAnimParameter()
        {
            _clothesShowRl.DoLayoutList(Localization.Get("show"), new Vector2(ConfigureListWidth, ConfigureListHeight), false, true, true, objs =>
            {
                // Show Paths
                ListenToDrop(typeof(GameObject), ref _clothes.ShowParameters, parameter =>
                {
                    if (_clothes.ContainsInList(parameter, _clothes.HideParameters))
                        _clothes.DeleteInList(parameter, ref _clothes.HideParameters);
                }, objs);

                EditorUtils.WaitToDo(20, "WaitToPreview", () => { PreviewGameObject(); });
            }, Repaint);

            _clothesHideRl.DoLayoutList(Localization.Get("hide"), new Vector2(ConfigureListWidth, ConfigureListHeight), false, true, true, objs =>
            {
                // Hide Paths
                ListenToDrop(typeof(GameObject), ref _clothes.HideParameters, parameter =>
                {
                    if (_clothes.ContainsInList(parameter, _clothes.ShowParameters))
                        _clothes.DeleteInList(parameter, ref _clothes.ShowParameters);
                }, objs);

                EditorUtils.WaitToDo(20, "WaitToPreview", () => { PreviewGameObject(); });
            }, Repaint);

            _clothesSmrRL.DoLayoutList(Localization.Get("skinned_mesh_renderer"), new Vector2(ConfigureListWidth, ConfigureListHeight), false, true, true, obj => { ListenToDrop(typeof(SkinnedMeshRenderer), ref _clothes.SMRParameters, null, obj); }, Repaint);
        }

        private void DrawClothesParameterDriver()
        {
            _clothes.HasParameterDriver = EditorUI.Toggle(_clothes.HasParameterDriver, Localization.Get("driver_is_using"));
            if (_clothes.HasParameterDriver)
            {
                _enterDriverRl.DoLayoutList(Localization.Get("driver_enter"), new Vector2(ConfigureListWidth, ConfigureListHeight), false, true, false, null, Repaint);
                _exitDriverRl.DoLayoutList(Localization.Get("driver_exit"), new Vector2(ConfigureListWidth, ConfigureListHeight), false, true, false, null, Repaint);

                EditorUI.Line();

                EditorUI.VerticalEGL("Badge", () =>
                {
                    EditorGUILayout.Space();
                    if (_currentDriverParameter == null)
                    {
                        EditorGUILayout.HelpBox(Localization.Get("driver_select_tip"), MessageType.Info);
                        return;
                    }

                    var nameIndex = IndexInParameterNames(_currentDriverParameter.name);

                    var type = AnimatorControllerParameterType.Bool;
                    EditorUI.HorizontalEGL(() =>
                    {
                        _currentDriverParameter.type = (VRC_AvatarParameterDriver.ChangeType)EditorGUILayout.Popup((int)_currentDriverParameter.type, Enum.GetNames(typeof(VRC_AvatarParameterDriver.ChangeType)), GUILayout.Width(70));

                        var names = GetParameterNames();
                        if (names.Length <= 0) return;
                        if (nameIndex == 0 && !string.IsNullOrEmpty(_currentDriverParameter.name))
                            names[0] = _currentDriverParameter.name;

                        EditorGUI.BeginChangeCheck();
                        nameIndex = EditorGUILayout.Popup(nameIndex, names);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _currentDriverParameter.name = names[nameIndex];
                        }

                        _currentDriverParameter.name = EditorGUILayout.TextField(_currentDriverParameter.name);

                        if (nameIndex == 0)
                        {
                            _currentDriverParameter.customType = (AnimatorControllerParameterType)EditorGUILayout.EnumPopup(_currentDriverParameter.customType);
                            type = _currentDriverParameter.customType;
                        }
                        else
                            type = GetParameterType(_currentDriverParameter.name);
                    });

                    switch (_currentDriverParameter.type)
                    {
                        case VRC_AvatarParameterDriver.ChangeType.Set:
                            DrawSet(type);
                            break;
                        case VRC_AvatarParameterDriver.ChangeType.Add:
                            DrawAdd(type);
                            break;
                        case VRC_AvatarParameterDriver.ChangeType.Random:
                            DrawRandom(type);
                            break;
                        case VRC_AvatarParameterDriver.ChangeType.Copy:
                            DrawCopy(type);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    EditorGUILayout.Space();
                }, GUILayout.Height(100));
            }

            return;

            void DrawSet(AnimatorControllerParameterType type)
            {
                EditorUI.HorizontalEGL(() =>
                {
                    EditorGUILayout.LabelField(Localization.Get("driver_dest_value"), GUILayout.Width(60));

                    switch (type)
                    {
                        case AnimatorControllerParameterType.Int:
                            _currentDriverParameter.value = EditorGUILayout.IntField((int)_currentDriverParameter.value);
                            break;
                        case AnimatorControllerParameterType.Float:
                            _currentDriverParameter.value = EditorGUILayout.FloatField(_currentDriverParameter.value);
                            break;
                        case AnimatorControllerParameterType.Bool:
                            var valueBool = Math.Abs(_currentDriverParameter.value - 1f) <= 0f;
                            valueBool = EditorGUILayout.Toggle(valueBool);
                            _currentDriverParameter.value = valueBool ? 1f : 0f;
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            break;
                        default:
                            EditorGUILayout.HelpBox(string.Format(Localization.Get("driver_not_suit_warning"), _currentDriverParameter.name, type, _currentDriverParameter.type), MessageType.Warning);
                            break;
                    }
                });
            }

            void DrawAdd(AnimatorControllerParameterType type)
            {
                EditorUI.HorizontalEGL(() =>
                {
                    switch (type)
                    {
                        case AnimatorControllerParameterType.Int:
                            EditorGUILayout.LabelField(Localization.Get("driver_dest_value"), GUILayout.Width(60));
                            _currentDriverParameter.value = EditorGUILayout.IntField((int)_currentDriverParameter.value);
                            break;
                        case AnimatorControllerParameterType.Float:
                            EditorGUILayout.LabelField(Localization.Get("driver_dest_value"), GUILayout.Width(60));
                            _currentDriverParameter.value = EditorGUILayout.FloatField(_currentDriverParameter.value);
                            break;
                        default:
                            EditorGUILayout.HelpBox(string.Format(Localization.Get("driver_not_suit_warning"), _currentDriverParameter.name, type, _currentDriverParameter.type), MessageType.Warning);
                            break;
                    }
                });
            }

            void DrawRandom(AnimatorControllerParameterType type)
            {
                EditorUI.HorizontalEGL(() =>
                {
                    switch (type)
                    {
                        case AnimatorControllerParameterType.Trigger:
                        case AnimatorControllerParameterType.Bool:
                            EditorGUILayout.LabelField(Localization.Get("driver_chance"), GUILayout.Width(60));
                            _currentDriverParameter.chance = EditorGUILayout.Slider(_currentDriverParameter.chance, 0, 1f);

                            break;
                        case AnimatorControllerParameterType.Int:
                            EditorGUILayout.LabelField(Localization.Get("driver_min_value"), GUILayout.Width(60));
                            _currentDriverParameter.valueMin = EditorGUILayout.IntField((int)_currentDriverParameter.valueMin);
                            EditorGUILayout.LabelField(Localization.Get("driver_max_value"), GUILayout.Width(60));
                            _currentDriverParameter.valueMax = EditorGUILayout.IntField((int)_currentDriverParameter.valueMax);
                            break;
                        case AnimatorControllerParameterType.Float:
                            EditorGUILayout.LabelField(Localization.Get("driver_min_value"), GUILayout.Width(60));
                            _currentDriverParameter.valueMin = EditorGUILayout.FloatField(_currentDriverParameter.valueMin);
                            EditorGUILayout.LabelField(Localization.Get("driver_max_value"), GUILayout.Width(60));
                            _currentDriverParameter.valueMax = EditorGUILayout.FloatField(_currentDriverParameter.valueMax);
                            break;
                    }
                });
            }

            void DrawCopy(AnimatorControllerParameterType type)
            {
                var parameters = GetAnimatorParametersWithTool();

                var names = GetParameterNames();
                var sourceIndex = IndexInParameterNames(_currentDriverParameter.source);
                if (sourceIndex == 0 && !string.IsNullOrEmpty(_currentDriverParameter.source))
                    names[0] = _currentDriverParameter.source;
                EditorUI.HorizontalEGL(() =>
                {
                    EditorGUILayout.LabelField(Localization.Get("driver_source"), GUILayout.Width(50));
                    sourceIndex = EditorGUILayout.Popup(sourceIndex, names);
                    _currentDriverParameter.name = EditorGUILayout.TextField(_currentDriverParameter.name);
                    if (type == AnimatorControllerParameterType.Bool)
                        _currentDriverParameter.convertRange = EditorUI.Toggle(_currentDriverParameter.convertRange, Localization.Get("driver_convert_range"), 60);
                });

                _currentDriverParameter.source = names[sourceIndex];

                var sourceValueType = sourceIndex >= 0 ? parameters[sourceIndex].type : AnimatorControllerParameterType.Float;

                switch (type)
                {
                    case AnimatorControllerParameterType.Float:
                    case AnimatorControllerParameterType.Int:
                    case AnimatorControllerParameterType.Bool:
                        if (sourceIndex >= 0)
                        {
                            if (sourceValueType == AnimatorControllerParameterType.Trigger)
                                EditorGUILayout.HelpBox(Localization.Get("driver_tip_not_allow_trigger"), MessageType.Warning);
                            else if (sourceValueType != type)
                                EditorGUILayout.HelpBox(string.Format(Localization.Get("driver_tip_convert"), sourceValueType, type), MessageType.Info);
                        }

                        if (_currentDriverParameter.convertRange)
                        {
                            EditorGUI.indentLevel += 1;
                            EditorUI.HorizontalEGL(() =>
                            {
                                EditorGUILayout.LabelField(Localization.Get("driver_source"), GUILayout.Width(50));
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField(Localization.Get("driver_min_value"), GUILayout.Width(50));
                                _currentDriverParameter.sourceMin = EditorGUILayout.FloatField(_currentDriverParameter.sourceMin);
                                EditorGUILayout.LabelField(Localization.Get("driver_max_value"), GUILayout.Width(50));
                                _currentDriverParameter.sourceMax = EditorGUILayout.FloatField(_currentDriverParameter.sourceMax);
                            });

                            EditorUI.HorizontalEGL(() =>
                            {
                                EditorGUILayout.LabelField(Localization.Get("driver_destination"), GUILayout.Width(50));
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField(Localization.Get("driver_min_value"), GUILayout.Width(50));
                                _currentDriverParameter.destMin = EditorGUILayout.FloatField(_currentDriverParameter.destMin);
                                EditorGUILayout.LabelField(Localization.Get("driver_max_value"), GUILayout.Width(50));
                                _currentDriverParameter.destMax = EditorGUILayout.FloatField(_currentDriverParameter.destMax);
                            });
                            EditorGUI.indentLevel -= 1;
                        }

                        break;
                    case AnimatorControllerParameterType.Trigger:
                        break;
                    default:
                        EditorGUILayout.HelpBox(string.Format(Localization.Get("driver_not_suit_warning"), _currentDriverParameter.name, type, _currentDriverParameter.type), MessageType.Warning);
                        break;
                }
            }
        }

        private void DrawSelectedCategory()
        {
            if (_categorySerializedObject != null && _categorySerializedObject.targetObject != null)
            {
                _categorySerializedObject.UpdateIfRequiredOrScript();

                EditorUI.VerticalEGL(() =>
                {
                    var clothesLabel = Localization.Get("clothes");
                    var titleLabel = _categoryBar.IsDraw ? clothesLabel : $"{clothesLabel} : {_currentClothesCategory.Name}";
                    EditorUI.TitleLabelField(titleLabel);
                    EditorUI.VerticalEGL("Badge", () =>
                    {
                        if (_clothes != null)
                        {
                            var parentMenu = _categorySerializedObject.FindProperty(nameof(CMClothesCategorySo.ParentMenu));
                            parentMenu.objectReferenceValue = EditorUI.ObjectFieldVertical(parentMenu.objectReferenceValue, Localization.Get("parent_menu"), typeof(VRCExpressionsMenu), false);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(Localization.Get("clothes_select_tip"), MessageType.Info);
                        }
                    }, GUILayout.Width(200));
                    _clothesGrid?.Draw(50, new Vector2(5, 5), new Vector2(200, ConfigurePageHeight - 20));
                });

                // Debug.Log("1");
                _categorySerializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(_categorySerializedObject.targetObject);
            }
        }

        /// <summary>
        /// 绘制配置UI
        /// </summary>
        private void DrawInit()
        {
            EditorUI.VerticalEGLTitled(Localization.Get("tool_init_label"), () =>
            {
                EditorUI.HorizontalEGL(() =>
                {
                    EditorGUI.BeginChangeCheck();
                    _descriptor = (VRCAvatarDescriptor)EditorUI.ObjectFieldVertical(_descriptor, Localization.Get("tool_init_avatar"), typeof(VRCAvatarDescriptor));
                    if (EditorGUI.EndChangeCheck())
                        OnDescriptorValueChanged();

                    if (_descriptor != null)
                    {
                        EditorUI.Line(LineType.Vertical);
                        if (_dataReference == null)
                        {
                            EditorGUILayout.HelpBox(Localization.Get("tool_init_none_data_tip"), MessageType.Info);
                            if (GUILayout.Button(Localization.Get("tool_init_create_btn"), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
                            {
                                CreatePersistantData();
                            }
                        }
                        else
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            _dataReference = (CMAvatarDataReference)EditorUI.ObjectFieldVertical(_dataReference, Localization.Get("tool_init_data"), typeof(CMAvatarDataReference), false);
                            EditorGUI.EndDisabledGroup();
                            if (GUILayout.Button(Localization.Get("tool_init_delete_btn"), GUILayout.Height(40)) && _dataReference.Data != null)
                            {
                                var isOk = EditorUtility.DisplayDialog(Localization.Get("warning"), Localization.Get("tool_init_tip_delete_warning"), Localization.Get("ok"), "Cancel");
                                if (isOk)
                                    DeletePersistentData();
                            }
                        }
                    }
                }, GUILayout.ExpandHeight(false), GUILayout.MaxHeight(40));

                if (_descriptor != null)
                {
                    _isFoldoutVrcConfigure = EditorUI.Foldout(_isFoldoutVrcConfigure, Localization.Get("tool_init_hide_options"), () =>
                    {
                        EditorGUI.BeginDisabledGroup(true);

                        EditorGUILayout.ObjectField(_fxLayer, typeof(RuntimeAnimatorController), false);
                        EditorGUILayout.ObjectField(_expressionsMenu, typeof(VRCExpressionsMenu), false);
                        EditorGUILayout.ObjectField(_parameters, typeof(VRCExpressionParameters), false);

                        EditorGUI.EndDisabledGroup();
                    });
                }
            });
        }
    }
}

#endregion