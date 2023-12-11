using System;
using System.Collections.Generic;
using System.IO;
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

        private ReorderableListDroppable _clothesShowRl, _clothesHideRl, _clothesBlendShapeRL;
        private ReorderableListDroppable _enterDriverRl, _exitDriverRl;

        private TabBarGroup _configureTabBarGroup;
        private TabBarElement _categoryBar, _clothesBar;
        private TabBarElement _clothesParameterBar;
        private TabBarElement _clothesDriverBar;

        private VRC_AvatarParameterDriver.Parameter _currentDriverParameter;
        private bool _isClothesPreview = true;

        private const float ConfigurePageHeight = 420;

        private readonly Texture2D[] _categoryIcons = new Texture2D[2];
        private readonly Texture2D[] _clothesIcons = new Texture2D[2];
        private readonly Texture2D[] _objectIcons = new Texture2D[2];
        private readonly Texture2D[] _listIcons = new Texture2D[2];
        private Texture2D _nextIcon;

        #endregion

        // Tools/YuebyTools/Avatar/Coming Soon.../
        [MenuItem("Tools/YuebyTools/Avatar/Clothes Manager", false, 11)]
        public static void OpenWindow()
        {
            _window = GetWindow<CMEditorWindow>();

            _window.titleContent = new GUIContent(Localization.Get("window_title"));
            _window.minSize = new Vector2(770, 600);
        }

        private void OnEnable()
        {
            GetIcons();

            _categoryBar = new TabBarElement(_categoryIcons, () => { _categoriesRl.DoLayout(Localization.Get("category"), new Vector2(150, ConfigurePageHeight + 20), null, false, false); });
            _clothesBar = new TabBarElement(_clothesIcons, () => { YuebyUtil.VerticalEGL(DrawSelectedCategory, GUILayout.MaxWidth(200), GUILayout.MaxHeight(ConfigurePageHeight), GUILayout.ExpandHeight(true)); });
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
        }

        private void GetIcons()
        {
            _objectIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/DarkMode/object.png");
            _objectIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/LightMode/object.png");

            _listIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/DarkMode/list.png");
            _listIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/LightMode/list.png");

            _clothesIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/DarkMode/clothes.png");
            _clothesIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/LightMode/clothes.png");

            _categoryIcons[0] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/DarkMode/category.png");
            _categoryIcons[1] = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/LightMode/category.png");
            _nextIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Sprites/next.png");
        }


        private void OnDisable()
        {
            if (!_isClothesPreview)
                ResetAvatarState();

            if (_isStartCapture)
            {
                StopCapture(true);
            }
        }


        private void OnGUI()
        {
            wantsMouseMove = true;

            if (_window == null)
                _window = GetWindow<CMEditorWindow>();
            _window.titleContent = new GUIContent(Localization.Get("window_title"));

            YuebyUtil.DrawEditorTitle(Localization.Get("window_title"));
            Localization.DrawLanguageUI();

            if (!Application.isPlaying)
            {
                DrawInit();

                if (_descriptor != null && _dataReference != null)
                    DrawConfigure();
            }
            else
            {
                EditorGUILayout.HelpBox(Localization.Get("in_playing_mode_tip"), MessageType.Warning);
            }
        }


        private void InitSerializedObjects()
        {
            if (!_dataReference) return;
            _serializedObject = new SerializedObject(_dataReference.Data);
            _categoriesProperty = _serializedObject.FindProperty(nameof(CMCDataSo.Categories));

            _categoriesRl = new YuebyReorderableList(_serializedObject, _categoriesProperty, 40f, true, true, true);
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


        private void OnCategoriesDraw(Rect rect, int index, bool isActive, bool isFocused)
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
        }

        private void OnCategoriesRemove(ReorderableList list, Object obj)
        {
            YuebyUtil.RemoveChildAsset(obj);
        }

        private void OnCategoriesAdd()
        {
            var categorySo = YuebyUtil.AddChildAsset<CMClothesCategorySo>(_serializedObject.targetObject);
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
            GUI.FocusControl(null);

            if (_categoryBar.IsDraw)
                _categoryBar.ChangeDrawState(false);

            _currentDriverParameter = null;

            PreviewCurrentClothes(true, false);

            if (_isGridInit)
                _isGridInit = false;
        }

        private Mesh _renderMesh;

        private void InitClothData()
        {
            _clothesShowRl = new ReorderableListDroppable(_clothes.ShowParameters, typeof(GameObject), EditorGUIUtility.singleLineHeight + 5, Repaint);
            _clothesHideRl = new ReorderableListDroppable(_clothes.HideParameters, typeof(GameObject), EditorGUIUtility.singleLineHeight + 5, Repaint);
            _clothesBlendShapeRL = new ReorderableListDroppable(_clothes.BlendShapeParameters, typeof(SkinnedMeshRenderer), EditorGUIUtility.singleLineHeight + 5, Repaint);

            _clothesShowRl.InverseRlList.AddRange(new[] { _clothesHideRl, _clothesBlendShapeRL });
            _clothesHideRl.InverseRlList.AddRange(new[] { _clothesShowRl, _clothesBlendShapeRL });
            _clothesBlendShapeRL.InverseRlList.AddRange(new[] { _clothesHideRl, _clothesShowRl });

            _clothesShowRl.AnimBool.target = true;

            _clothesShowRl.OnAdd += list =>
            {
                _clothes.ShowParameters.Add(new CMClothesData.ClothesAnimParameter()
                {
                    Type = nameof(GameObject)
                });
            };
            _clothesHideRl.OnAdd += list =>
            {
                _clothes.HideParameters.Add(new CMClothesData.ClothesAnimParameter()
                {
                    Type = nameof(GameObject)
                });
            };

            _clothesBlendShapeRL.OnAdd += list =>
            {
                _clothes.BlendShapeParameters.Add(new CMClothesData.ClothesAnimParameter()
                {
                    Type = nameof(SkinnedMeshRenderer)
                });
            };

            _clothesShowRl.OnRemove += list => { PreviewCurrentClothes(); };
            _clothesHideRl.OnRemove += list => { PreviewCurrentClothes(); };
            _clothesBlendShapeRL.OnRemove += list => { PreviewCurrentClothes(); };


            _clothesShowRl.OnDraw += (rect, index, active, focused) => { RegisterClothPathListPanel(rect, index, ref _clothes.ShowParameters); };
            _clothesHideRl.OnDraw += (rect, index, active, focused) => { RegisterClothPathListPanel(rect, index, ref _clothes.HideParameters); };
            _clothesBlendShapeRL.OnDraw += (rect, index, active, focused) => { RegisterClothPathListPanel(rect, index, ref _clothes.BlendShapeParameters); };


            // Parameter Driver ReorderableList Init
            _enterDriverRl = new ReorderableListDroppable(_clothes.EnterParameter.Parameters, typeof(VRC_AvatarParameterDriver.Parameter), EditorGUIUtility.singleLineHeight + 5, Repaint)
            {
                AnimBool =
                {
                    target = true
                }
            };
            _exitDriverRl = new ReorderableListDroppable(_clothes.ExitParameter.Parameters, typeof(VRC_AvatarParameterDriver.Parameter), EditorGUIUtility.singleLineHeight + 5, Repaint);

            _enterDriverRl.InverseRlList.Add(_exitDriverRl);
            _exitDriverRl.InverseRlList.Add(_enterDriverRl);

            _enterDriverRl.OnAdd += list =>
            {
                _clothes.EnterParameter.Parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set
                });
            };

            _exitDriverRl.OnAdd += list =>
            {
                _clothes.ExitParameter.Parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set
                });
            };

            _enterDriverRl.OnSelected += index => { _currentDriverParameter = _clothes.EnterParameter.Parameters[index]; };
            _exitDriverRl.OnSelected += index => { _currentDriverParameter = _clothes.ExitParameter.Parameters[index]; };

            _enterDriverRl.OnDrawTitle += () => { _clothes.EnterParameter.IsLocal = YuebyUtil.Toggle(_clothes.EnterParameter.IsLocal, Localization.Get("driver_is_local"), 80); };
            _exitDriverRl.OnDrawTitle += () => { _clothes.ExitParameter.IsLocal = YuebyUtil.Toggle(_clothes.ExitParameter.IsLocal, Localization.Get("driver_is_local"), 80); };

            _enterDriverRl.OnDraw += (rect, index, active, focused) => { DrawParameterDriverElement(ref _clothes.EnterParameter.Parameters, rect, index, active, focused); };
            _exitDriverRl.OnDraw += (rect, index, active, focused) => { DrawParameterDriverElement(ref _clothes.ExitParameter.Parameters, rect, index, active, focused); };
        }

        private void DrawParameterDriverElement(ref List<VRC_AvatarParameterDriver.Parameter> drivers, Rect rect, int index, bool active, bool focused)
        {
            var typeRect = new Rect(rect.x, rect.y + 2, 70, rect.height);
            var driver = drivers[index];
            driver.type = (VRC_AvatarParameterDriver.ChangeType)EditorGUI.Popup(typeRect, (int)driver.type, Enum.GetNames(typeof(VRC_AvatarParameterDriver.ChangeType)));

            var nameRect = new Rect(typeRect.x + typeRect.width + 2, typeRect.y, 120, typeRect.height);

            var names = GetParameterNames();
            if (names.Length <= 0) return;
            var nameIndex = EditorGUI.Popup(nameRect, IndexInParameterNames(driver.name), names);
            driver.name = nameIndex == -1 ? "" : names[nameIndex];

            if (nameIndex == -1) return;
            var valueRect = new Rect(nameRect.x + nameRect.width + 4, rect.y, rect.width - nameRect.width - typeRect.width - 6, nameRect.height - 2);
            switch (GetParameterType(driver.name))
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

            YuebyUtil.WaitToDo(20, "Wait To Select Grid Added", () => { _clothesGrid.Select(_clothesGrid.Count - 1); });
        }

        private void DrawClothesGridElement(Rect rect, int i)
        {
            var currentCloth = _clothesList[i];

            if (currentCloth == null) return;

            if (currentCloth.Icon != null)
                GUI.Box(rect, currentCloth.Icon);

            if (_currentClothesCategory && _currentClothesCategory.Default == i)
            {
                var defaultIconRect = new Rect(rect.x + 2, rect.y + 1, 20, 20);
                EditorGUI.LabelField(defaultIconRect, "*");
            }

            var labelRect = new Rect(rect.x, rect.y + rect.height / 2, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, currentCloth.Name);
        }

        #endregion

        #region UI

        private void DrawConfigure()
        {
            if (_serializedObject != null && _serializedObject.targetObject != null)
            {
                _serializedObject.UpdateIfRequiredOrScript();

                YuebyUtil.HorizontalEGL(() =>
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

                    YuebyUtil.VerticalEGLTitled(titleLabel, DrawClothConfigArea, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                }, GUILayout.MaxHeight(ConfigurePageHeight));
                _serializedObject.ApplyModifiedProperties();
            }
        }


        private void DrawClothConfigArea()
        {
            if (_categorySerializedObject != null && _categorySerializedObject.targetObject != null && _clothes != null)
            {
                _categorySerializedObject.UpdateIfRequiredOrScript();

                const float width = 320f;
                YuebyUtil.HorizontalEGL(() =>
                {
                    YuebyUtil.VerticalEGL(() =>
                    {
                        _clothesParameterBar.Draw();
                        _clothesDriverBar.Draw();
                    }, GUILayout.MinWidth(width));

                    if (_categoryBar.IsDraw) return;
                    YuebyUtil.Line(LineType.Vertical);

                    YuebyUtil.VerticalEGL(() =>
                    {
                        var bkgColor = GUI.backgroundColor;
                        if (_isClothesPreview)
                            GUI.backgroundColor = Color.green;
                        if (GUILayout.Button(Localization.Get("clothes_preview")))
                        {
                            _isClothesPreview = !_isClothesPreview;
                            if (!_isClothesPreview)
                            {
                                ResetAvatarState();
                            }
                            else
                            {
                                PreviewCurrentClothes();
                            }
                        }

                        GUI.backgroundColor = bkgColor;

                        var isCurrentDefault = _currentClothesCategory.Default == _clothesIndex;

                        EditorGUI.BeginDisabledGroup(isCurrentDefault);
                        if (GUILayout.Button(isCurrentDefault ? Localization.Get("clothes_already_default") : Localization.Get("clothes_set_to_default")))
                        {
                            _currentClothesCategory.Default = _clothesIndex;
                        }

                        EditorGUI.EndDisabledGroup();

                        YuebyUtil.Line();

                        YuebyUtil.HorizontalEGL(() =>
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

                        YuebyUtil.HorizontalEGL(() =>
                        {
                            if (GUILayout.Button(Localization.Get("tool_save_path_change"), GUILayout.Width(80)))
                            {
                                MoveFile();
                            }

                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.TextField(_dataReference.SavePath);
                            EditorGUI.EndDisabledGroup();
                        });


                        var backupPath = GetIDPath() + "/Backups";
                        if (Directory.Exists(backupPath) && GUILayout.Button(Localization.Get("open_backup_path")))
                        {
                            YuebyUtil.PingProject(backupPath);
                        }

                        if (GUILayout.Button(Localization.Get("apply")))
                        {
                            Apply(_dataReference.Data);
                        }

                        YuebyUtil.Line();

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

                            YuebyUtil.HorizontalEGL(() =>
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


        private RenderTexture _previewRT;
        private bool _isStartCapture;
        private GameObject _captureGo;
        private GameObject _captureCameraGo;
        private Camera _captureCamera;

        private void SetupCapture(bool needFocus = true)
        {
            if (!_captureCameraGo)
            {
                _captureCameraGo = new GameObject(Localization.Get("capture_obj_camera"));
                var follow = _captureCameraGo.AddComponent<CMCaptureCameraFollow>();
                follow.OnPositionUpdate += Repaint;

                _captureCamera = _captureCameraGo.AddComponent<Camera>();
                _captureCamera.clearFlags = CameraClearFlags.SolidColor;
                _captureCamera.backgroundColor = Color.clear;
                _captureCamera.fieldOfView = 45;
                _captureCamera.targetTexture = _previewRT;
                _captureCamera.orthographic = true;
                _captureCamera.orthographicSize = 0.5f;
            }

            _captureGo = Instantiate(_descriptor.gameObject);
            _captureGo.name = Localization.Get("capture_obj_avatar") + _clothes.Name;

            foreach (var component in _captureGo.GetComponentsInChildren<Component>(true))
            {
                if (component.GetType() == typeof(Transform) || component.GetType() == typeof(SkinnedMeshRenderer)) continue;
                DestroyImmediate(component);
            }

            var map = GetClothesParameters(_currentClothesCategory, _clothesIndex);
            var showList = map["Show"];
            var hideList = map["Hide"];

            var hideRenderers = new List<GameObject>();
            foreach (var renderer in _captureGo.GetComponentsInChildren<Renderer>())
            {
                hideRenderers.Add(renderer.gameObject);
            }

            foreach (var show in showList)
            {
                var trans = _captureGo.transform.Find(show.Path);
                if (!trans) continue;
                var childRenderers = trans.GetComponentsInChildren<Renderer>();
                foreach (var childRender in childRenderers)
                {
                    if (hideRenderers.Contains(childRender.gameObject))
                        hideRenderers.Remove(childRender.gameObject);
                }


                trans.gameObject.SetActive(true);
            }

            foreach (var hide in hideRenderers)
            {
                // hide.SetActive(false);
                DestroyImmediate(hide);
            }

            foreach (var hide in hideList)
            {
                var trans = _captureGo.transform.Find(hide.Path);
                if (!trans) continue;
                DestroyImmediate(trans.gameObject);
            }

            var capturePos = new Vector3(1000, 0, 100);
            _captureGo.transform.position = capturePos;

            if (needFocus)
                YuebyUtil.FocusTarget(_captureGo);
        }


        private Texture2D SaveRTToFile(string path)
        {
            var mRt = new RenderTexture(_previewRT.width, _previewRT.height, _previewRT.depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = _previewRT.antiAliasing
            };

            mRt.Create();

            var tex = new Texture2D(mRt.width, mRt.height, TextureFormat.ARGB32, false);
            _captureCamera.targetTexture = mRt;
            _captureCamera.Render();
            RenderTexture.active = mRt;

            tex.ReadPixels(new Rect(0, 0, mRt.width, mRt.height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            mRt.Release();


            if (File.Exists(path))
                File.Delete(path);
            File.WriteAllBytes(path, tex.EncodeToPNG());

            DestroyImmediate(tex);

            _captureCamera.targetTexture = _previewRT;
            _captureCamera.Render();


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var t2d = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (EditorUtility.DisplayDialog(Localization.Get("tips"), Localization.Get("tip_success_save"), Localization.Get("yes"), Localization.Get("no")))
            {
                YuebyUtil.PingProject(t2d);
            }

            return t2d;
        }


        private void GetClothesCapture()
        {
            var categoryPath = GetCapturePath() + "/" + _currentClothesCategory.Name + "/";
            if (!Directory.Exists(categoryPath))
                Directory.CreateDirectory(categoryPath);

            var icon = SaveRTToFile(categoryPath + _clothes.Name + ".png");
            if (_clothes != null)
                _clothes.Icon = icon;
        }

        private void StopCapture(bool destroyCam = true, bool needBack = false)
        {
            if (destroyCam && _captureCameraGo)
                DestroyImmediate(_captureCameraGo);

            if (_captureGo)
                DestroyImmediate(_captureGo);

            if (needBack)
            {
                YuebyUtil.FocusTarget(_descriptor.gameObject);
            }
        }

        private void DrawClothesAnimParameter()
        {
            const float width = 320f;
            _clothesShowRl.DoLayoutList(Localization.Get("show"), new Vector2(width, 320), false, true, true, objs =>
            {
                // Show Paths
                ListenToDrop(typeof(GameObject), ref _clothes.ShowParameters, parameter =>
                {
                    if (_clothes.ContainsInList(parameter, _clothes.HideParameters))
                        _clothes.DeleteInList(parameter, ref _clothes.HideParameters);
                }, objs);

                YuebyUtil.WaitToDo(20, "WaitToPreview", () => { PreviewCurrentClothes(true, false); });
            }, Repaint);

            _clothesHideRl.DoLayoutList(Localization.Get("hide"), new Vector2(width, 320), false, true, true, objs =>
            {
                // Hide Paths
                ListenToDrop(typeof(GameObject), ref _clothes.HideParameters, parameter =>
                {
                    if (_clothes.ContainsInList(parameter, _clothes.ShowParameters))
                        _clothes.DeleteInList(parameter, ref _clothes.ShowParameters);
                }, objs);

                YuebyUtil.WaitToDo(20, "WaitToPreview", () => { PreviewCurrentClothes(true, false); });
            }, Repaint);

            _clothesBlendShapeRL.DoLayoutList(Localization.Get("blend_shapes"), new Vector2(width, 320), false, true, true, obj =>
            {
                // BlendShapes
                ListenToDrop(typeof(SkinnedMeshRenderer), ref _clothes.BlendShapeParameters, null, obj);
            }, Repaint);
        }

        private void DrawClothesParameterDriver()
        {
            const float width = 320f;
            _clothes.HasParameterDriver = YuebyUtil.Toggle(_clothes.HasParameterDriver, Localization.Get("driver_is_using"));
            if (_clothes.HasParameterDriver)
            {
                _enterDriverRl.DoLayoutList(Localization.Get("driver_enter"), new Vector2(width, 320), false, true, false, null, Repaint);
                _exitDriverRl.DoLayoutList(Localization.Get("driver_exit"), new Vector2(width, 320), false, true, false, null, Repaint);

                YuebyUtil.Line();

                YuebyUtil.VerticalEGL("Badge", () =>
                {
                    EditorGUILayout.Space();
                    if (_currentDriverParameter == null)
                    {
                        EditorGUILayout.HelpBox(Localization.Get("driver_select_tip"), MessageType.Info);
                        return;
                    }

                    var nameIndex = IndexInParameterNames(_currentDriverParameter.name);
                    YuebyUtil.HorizontalEGL(() =>
                    {
                        _currentDriverParameter.type = (VRC_AvatarParameterDriver.ChangeType)EditorGUILayout.Popup((int)_currentDriverParameter.type, Enum.GetNames(typeof(VRC_AvatarParameterDriver.ChangeType)), GUILayout.Width(70));

                        var names = GetParameterNames();
                        if (names.Length <= 0) return;
                        nameIndex = EditorGUILayout.Popup(nameIndex, names);
                        _currentDriverParameter.name = nameIndex == -1 ? "" : names[nameIndex];
                        _currentDriverParameter.name = EditorGUILayout.TextField(_currentDriverParameter.name);
                    });
                    var type = GetParameterType(_currentDriverParameter.name);
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
                YuebyUtil.HorizontalEGL(() =>
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
                YuebyUtil.HorizontalEGL(() =>
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
                YuebyUtil.HorizontalEGL(() =>
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

                YuebyUtil.HorizontalEGL(() =>
                {
                    EditorGUILayout.LabelField(Localization.Get("driver_source"), GUILayout.Width(50));
                    sourceIndex = EditorGUILayout.Popup(sourceIndex, names);
                    _currentDriverParameter.name = EditorGUILayout.TextField(_currentDriverParameter.name);
                    if (type == AnimatorControllerParameterType.Bool)
                        _currentDriverParameter.convertRange = YuebyUtil.Toggle(_currentDriverParameter.convertRange, Localization.Get("driver_convert_range"), 60);
                });

                _currentDriverParameter.source = sourceIndex == -1 ? "" : names[sourceIndex];

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
                            YuebyUtil.HorizontalEGL(() =>
                            {
                                EditorGUILayout.LabelField(Localization.Get("driver_source"), GUILayout.Width(50));
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField(Localization.Get("driver_min_value"), GUILayout.Width(40));
                                _currentDriverParameter.sourceMin = EditorGUILayout.FloatField(_currentDriverParameter.sourceMin);
                                EditorGUILayout.LabelField(Localization.Get("driver_max_value"), GUILayout.Width(40));
                                _currentDriverParameter.sourceMax = EditorGUILayout.FloatField(_currentDriverParameter.sourceMax);
                            });

                            YuebyUtil.HorizontalEGL(() =>
                            {
                                EditorGUILayout.LabelField(Localization.Get("driver_destination"), GUILayout.Width(50));
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField(Localization.Get("driver_min_value"), GUILayout.Width(40));
                                _currentDriverParameter.destMin = EditorGUILayout.FloatField(_currentDriverParameter.destMin);
                                EditorGUILayout.LabelField(Localization.Get("driver_max_value"), GUILayout.Width(40));
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

                YuebyUtil.VerticalEGL(() =>
                {
                    var clothesLabel = Localization.Get("clothes");
                    var titleLabel = _categoryBar.IsDraw ? clothesLabel : $"{clothesLabel} : {_currentClothesCategory.Name}";
                    YuebyUtil.TitleLabelField(titleLabel);
                    YuebyUtil.VerticalEGL("Badge", () =>
                    {
                        if (_clothes != null)
                        {
                            YuebyUtil.HorizontalEGL(() =>
                            {
                                _clothes.Icon = (Texture2D)EditorGUILayout.ObjectField(_clothes.Icon, typeof(Texture2D), false, GUILayout.Width(40), GUILayout.Height(40));

                                EditorGUI.BeginChangeCheck();
                                _clothes.Name = YuebyUtil.TextFieldVertical(Localization.Get("clothes_name"), _clothes.Name, 60);
                                if (EditorGUI.EndChangeCheck())
                                    Undo.RegisterCompleteObjectUndo(_currentClothesCategory, "Category Undo Register");
                            });
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(Localization.Get("clothes_select_tip"), MessageType.Info);
                        }
                    }, GUILayout.Width(200));
                    _clothesGrid.Draw(50, new Vector2(5, 5), new Vector2(200, ConfigurePageHeight - 20));
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
            YuebyUtil.VerticalEGLTitled(Localization.Get("tool_init_label"), () =>
            {
                YuebyUtil.HorizontalEGL(() =>
                {
                    EditorGUI.BeginChangeCheck();
                    _descriptor = (VRCAvatarDescriptor)YuebyUtil.ObjectFieldVertical(_descriptor, Localization.Get("tool_init_avatar"), typeof(VRCAvatarDescriptor));
                    if (EditorGUI.EndChangeCheck())
                        OnDescriptorValueChanged();


                    if (_descriptor != null)
                    {
                        YuebyUtil.Line(LineType.Vertical);
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
                            _dataReference = (CMAvatarDataReference)YuebyUtil.ObjectFieldVertical(_dataReference, Localization.Get("tool_init_data"), typeof(CMAvatarDataReference), false);
                            EditorGUI.EndDisabledGroup();
                            if (GUILayout.Button(Localization.Get("tool_init_delete_btn"), GUILayout.Height(40)) && _dataReference.Data != null)
                            {
                                var isOk = EditorUtility.DisplayDialog(Localization.Get("warning"), Localization.Get("tool_init_tip_delete_warning"), Localization.Get("ok"), "Cancel");
                                if (isOk)
                                    DeletePersistantData();
                            }
                        }
                    }
                }, GUILayout.ExpandHeight(false), GUILayout.MaxHeight(40));

                if (_descriptor != null)
                {
                    _isFoldoutVrcConfigure = YuebyUtil.Foldout(_isFoldoutVrcConfigure, Localization.Get("tool_init_hide_options"), () =>
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