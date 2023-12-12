using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Yueby.Utils;
using Object = UnityEngine.Object;
using SkinnedMeshRenderer = UnityEngine.SkinnedMeshRenderer;

namespace Yueby.AvatarTools.ClothesManager
{
    public partial class CMEditorWindow
    {
        private const string DescriptorPrefs = "Descriptor";

        private static string GenerateId()
        {
            return GUID.Generate().ToString();
        }

        /// <summary>
        /// 通过EditorPrefs获得Descriptor
        /// </summary>
        /// <returns></returns>
        private bool GetDescriptorByEditorPrefs()
        {
            if (EditorPrefs.HasKey(DescriptorPrefs))
            {
                var descriptorId = EditorPrefs.GetInt(DescriptorPrefs);
                var descriptor = EditorUtility.InstanceIDToObject(descriptorId);
                Debug.Log(descriptor);
                if (descriptor != null)
                {
                    _descriptor = (VRCAvatarDescriptor)descriptor;

                    GetTargetParameter();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在一开始时获得Descriptor
        /// </summary>
        private void GetDescriptorOnEnable()
        {
            if (_descriptor == null)
            {
                if (!GetDescriptorByEditorPrefs())
                {
                    VRCAvatarDescriptor selectionDescriptor = null;
                    if (Selection.activeGameObject != null)
                        selectionDescriptor = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();

                    if (selectionDescriptor != null)
                        _descriptor = selectionDescriptor;
                    else
                    {
                        var descriptors = FindObjectsOfType<VRCAvatarDescriptor>();
                        if (descriptors.Length > 0)
                            _descriptor = descriptors[0];
                    }

                    if (_descriptor != null)
                    {
                        GetTargetParameter();
                    }
                }
            }
        }

        private void CreatePersistantData()
        {
            _dataReference = _descriptor.gameObject.AddComponent<CMAvatarDataReference>();
            _dataReference.ID = GenerateId();


            var asset = CreateInstance<CMCDataSo>();

            _dataReference.Data = asset;
            var targetPath = GetIDPath();
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            var path = AssetDatabase.GenerateUniqueAssetPath(targetPath + "/CMData.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            OnEnable();
        }

        private void DeletePersistantData()
        {
            if (string.IsNullOrEmpty(_dataReference.ID))
                return;

            if (_dataReference.Data.Categories.Count > 0)
            {
                foreach (var parameterName in _dataReference.Data.Categories.Select(category => $"YCM/{category.Name}/Switch"))
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

                var newParameters = _parameters.parameters.ToList();

                var removeList = new List<VRCExpressionParameters.Parameter>();
                foreach (var parameterName in _dataReference.Data.Categories.Select(category => $"YCM/{category.Name}/Switch"))
                {
                    foreach (var par in newParameters.Where(par => par.name == parameterName))
                    {
                        removeList.Add(par);
                        break;
                    }
                }

                foreach (var remove in removeList)
                    newParameters.Remove(remove);
                _parameters.parameters = newParameters.ToArray();
            }


            var currentExMenu = _expressionsMenu;
            if (currentExMenu.controls.Count >= 8)
            {
                var menuDir = GetIDPath() + "/Expressions";
                currentExMenu = GetLastNextSubMenu(currentExMenu, $"{menuDir}", _expressionsMenu.name, 0);
            }

            var control = currentExMenu.controls.FirstOrDefault(c => c.name == Localization.Get("window_title"));
            if (control != null)
                currentExMenu.controls.Remove(control);

            var path = GetIDPath();
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                if (File.Exists(path + ".meta"))
                    File.Delete(path + ".meta");
                EditorUtility.DisplayDialog("提示", "删除成功!", "Ok");

                DestroyImmediate(_dataReference);

                AssetDatabase.Refresh();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            else
                EditorUtility.DisplayDialog("提示", "删除失败!不存在数据目录", "Ok");


            EditorUtility.SetDirty(_expressionsMenu);
            EditorUtility.SetDirty(_parameters);
            EditorUtility.SetDirty(_fxLayer);

            OnEnable();
        }

        private string GetGeneratedPath()
        {
            var path = _dataReference.SavePath + "/Generated";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// 获取ID路径
        /// </summary>
        /// <returns></returns>
        private string GetIDPath()
        {
            var idPath = GetGeneratedPath() + "/" + _dataReference.ID;
            if (!Directory.Exists(idPath))
                Directory.CreateDirectory(idPath);
            return idPath;
        }

        private string GetCapturePath()
        {
            var capturePath = GetIDPath() + "/" + "Captures";
            if (!Directory.Exists(capturePath))
                Directory.CreateDirectory(capturePath);
            return capturePath;
        }

        /// <summary>
        /// 获取备份路径
        /// </summary>
        /// <returns></returns>
        private string GetBackupsPath()
        {
            var timePath = GetIDPath() + "/" + "Backups/" + DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss");
            if (!Directory.Exists(timePath))
                Directory.CreateDirectory(timePath);
            return timePath;
        }

        private string GetPreviewPath()
        {
            var path = "Packages/com.yueby.avatartools/Editor/Assets/ClothesManager/Preview.renderTexture";
            return path;
        }

        /// <summary>
        /// 当Descriptor改变
        /// </summary>
        private void OnDescriptorValueChanged()
        {
            if (_descriptor == null)
            {
                EditorPrefs.DeleteKey(DescriptorPrefs);
                return;
            }

            YuebyUtil.FocusTarget(_descriptor.gameObject);
            GetTargetParameter();
        }

        /// <summary>
        /// 获得各种对象
        /// </summary>
        private void GetTargetParameter()
        {
            _expressionsMenu = _descriptor.expressionsMenu;
            _parameters = _descriptor.expressionParameters;
            _fxLayer = _descriptor.baseAnimationLayers[4].animatorController as AnimatorController;


            _dataReference = _descriptor.GetComponent<CMAvatarDataReference>();
            if (_dataReference != null)
            {
                if (_dataReference.Data == null)
                {
                    _dataReference.Data = AssetDatabase.LoadAssetAtPath<CMCDataSo>(GetIDPath() + "/" + "CMData.asset");
                }
            }

            RecordAvatarState();
            _previewRT = AssetDatabase.LoadAssetAtPath<RenderTexture>(GetPreviewPath());
        }

        private void ListenToDrop(Type allowType, ref List<CMClothesData.ClothesAnimParameter> parameters, UnityAction<CMClothesData.ClothesAnimParameter> handler, Object[] objects)
        {
            foreach (var showAnimParameter in parameters)
            {
                if (string.IsNullOrEmpty(showAnimParameter.Path))
                    parameters.Remove(showAnimParameter);
            }

            // 自动获取后缀为 (objectName) 的对象
            if (parameters.Count <= 0 && objects != null && objects.Length == 1)
            {
                var objName = objects[0].name;
                var list = objects.ToList();
                foreach (var transform in _descriptor.transform.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name.Contains($"({objName})"))
                    {
                        list.Add(transform.gameObject);
                    }
                }

                objects = list.ToArray();
            }


            var paths = new List<CMClothesData.ClothesAnimParameter>();
            if (objects != null)
            {
                foreach (var objReference in objects)
                {
                    var obj = objReference;
                    var path = string.Empty;
                    var type = string.Empty;

                    if (allowType == typeof(GameObject))
                    {
                        if (obj is GameObject gameObject)
                        {
                            path = VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject.transform).Replace(_descriptor.name + "/", "");
                            type = nameof(GameObject);
                        }
                    }
                    else if (allowType == typeof(SkinnedMeshRenderer))
                    {
                        if (obj is GameObject gameObject)
                        {
                            var sm = gameObject.GetComponent<SkinnedMeshRenderer>();
                            if (sm == null)
                            {
                                EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_none_bs_tip"), obj.name), Localization.Get("ok"));
                                return;
                            }

                            obj = sm;
                        }

                        if (obj is SkinnedMeshRenderer skinnedMeshRenderer)
                        {
                            if (skinnedMeshRenderer.sharedMesh.blendShapeCount == 0)
                            {
                                var message = string.Format(Localization.Get("clothes_none_bs_tip"), $"{nameof(SkinnedMeshRenderer)}:{skinnedMeshRenderer.name}");
                                EditorUtility.DisplayDialog(Localization.Get("tips"), message, Localization.Get("ok"));
                                continue;
                            }

                            path = VRC.Core.ExtensionMethods.GetHierarchyPath(skinnedMeshRenderer.transform).Replace(_descriptor.name + "/", "");
                            type = nameof(SkinnedMeshRenderer);

                            var count = parameters.Count(animParameter => animParameter.Type == nameof(SkinnedMeshRenderer) && animParameter.Path == path);

                            if (count == skinnedMeshRenderer.sharedMesh.blendShapeCount)
                            {
                                EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_all_bs_tip"), path), Localization.Get("ok"));
                                continue;
                            }
                        }
                    }

                    var parameter = new CMClothesData.ClothesAnimParameter()
                    {
                        Path = path,
                        Type = type
                    };

                    handler?.Invoke(parameter);

                    if (!_clothes.ContainsInList(parameter, parameters))
                        paths.Add(parameter);
                }
            }

            parameters.AddRange(paths);
        }

        private void RegisterClothPathListPanel(Rect rect, int index, ref List<CMClothesData.ClothesAnimParameter> animParameters)
        {
            GameObject obj = null;

            if (index > animParameters.Count - 1) return;
            var target = animParameters[index];

            if (!string.IsNullOrEmpty(target.Path))
            {
                var pathTrans = _descriptor.transform.Find(target.Path);
                if (pathTrans)
                    obj = pathTrans.gameObject;
            }

            var objFieldRect = new Rect(rect.x, rect.y + 2, rect.width / 2 - 1, EditorGUIUtility.singleLineHeight);
            if (target.Type == nameof(SkinnedMeshRenderer))
            {
                var skinnedMeshRenderer = obj != null ? obj.GetComponent<SkinnedMeshRenderer>() : null;


                EditorGUI.BeginChangeCheck();
                skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUI.ObjectField(objFieldRect, skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
                if (EditorGUI.EndChangeCheck())
                {
                    if (skinnedMeshRenderer)
                    {
                        var path = VRC.Core.ExtensionMethods.GetHierarchyPath(skinnedMeshRenderer.transform).Replace(_descriptor.name + "/", "");

                        var count = animParameters.Count(animParameter => animParameter.Type == nameof(SkinnedMeshRenderer) && animParameter.Path == path);
                        if (count == skinnedMeshRenderer.sharedMesh.blendShapeCount)
                        {
                            if (EditorUtility.DisplayDialog(Localization.Get("tips"), Localization.Get("clothes_all_bs_tip"), Localization.Get("ok")))
                            {
                                return;
                            }
                        }

                        target.Path = path;
                        target.SmrParameter.Index = -1;
                        target.SmrParameter.BlendShapeValue = 0;
                    }
                }

                if (skinnedMeshRenderer)
                {
                    var typeRect = new Rect(objFieldRect.x + objFieldRect.width + 1, objFieldRect.y, objFieldRect.width, objFieldRect.height);
                    target.SmrParameter.Type = (CMClothesData.ClothesAnimParameter.SMRParameter.SMRType)EditorGUI.EnumPopup(typeRect, target.SmrParameter.Type);
                    var firstRect = new Rect(objFieldRect.x, objFieldRect.y + objFieldRect.height + 1, objFieldRect.width, objFieldRect.height);
                    var secondRect = new Rect(firstRect.x + firstRect.width + 5, firstRect.y, rect.width - 7 - firstRect.width, firstRect.height);

                    switch (target.SmrParameter.Type)
                    {
                        case CMClothesData.ClothesAnimParameter.SMRParameter.SMRType.ShapeKey:
                            var blendShapeNames = new List<string>();
                            for (var i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                                blendShapeNames.Add(skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i));

                            EditorGUI.BeginChangeCheck();
                            target.SmrParameter.Index = EditorGUI.Popup(firstRect, target.SmrParameter.Index, blendShapeNames.ToArray());

                            if (target.SmrParameter.Index != -1)
                                target.SmrParameter.BlendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(target.SmrParameter.Index);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (_clothes.ContainsInList(target, animParameters))
                                    target.SmrParameter.Index = -1;

                                if (target.SmrParameter.Index == -1)
                                    target.SmrParameter.BlendShapeValue = 0;
                            }

                            if (target.SmrParameter.Index != -1)
                            {
                                if (target.SmrParameter.Index >= 0)
                                {
                                    EditorGUI.BeginChangeCheck();
                                    target.SmrParameter.BlendShapeValue = EditorGUI.Slider(secondRect, target.SmrParameter.BlendShapeValue, 0, 100f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RegisterCompleteObjectUndo(_currentClothesCategory, "Category SliderValueChanged");
                                        PreviewSMR(_clothes);
                                    }
                                }
                            }

                            break;
                        case CMClothesData.ClothesAnimParameter.SMRParameter.SMRType.Material:

                            var count = skinnedMeshRenderer.sharedMaterials.Length;
                            var popups = new string[count];
                            for (var i = 0; i < count; i++)
                                popups[i] = i.ToString();

                            EditorGUI.BeginChangeCheck();
                            target.SmrParameter.Index = EditorGUI.Popup(firstRect, target.SmrParameter.Index, popups);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (_clothes.ContainsInList(target, animParameters))
                                {
                                    target.SmrParameter.Index = -1;
                                    target.SmrParameter.Type = CMClothesData.ClothesAnimParameter.SMRParameter.SMRType.ShapeKey;
                                    return;
                                }
                            }

                            if (target.SmrParameter.Index >= 0)
                            {
                                EditorGUI.BeginChangeCheck();
                                target.SmrParameter.Material = (Material)EditorGUI.ObjectField(secondRect, target.SmrParameter.Material, typeof(Material), false);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RegisterCompleteObjectUndo(_currentClothesCategory, "SMR Material Change");
                                    PreviewSMR(_clothes);
                                }
                            }

                            break;
                    }
                }
            }
            else if (target.Type == nameof(GameObject))
            {
                EditorGUI.BeginChangeCheck();
                objFieldRect.width = rect.width;
                obj = (GameObject)EditorGUI.ObjectField(objFieldRect, obj, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                {
                    target.Path = VRC.Core.ExtensionMethods.GetHierarchyPath(obj.transform).Replace(_descriptor.name + "/", "");
                    PreviewAll();
                }
            }

            EditorUtility.SetDirty(_currentClothesCategory);
        }

        private void Apply(CMCDataSo data)
        {
            var backupPath = GetBackupsPath();
            BackupFile(backupPath, _expressionsMenu);
            BackupFile(backupPath, _parameters);
            BackupFile(backupPath, _fxLayer);
            PrefabUtility.SaveAsPrefabAsset(_descriptor.gameObject, backupPath + "/" + _descriptor.name + ".prefab");


            if (_parameters == null || _parameters.parameters == null)
            {
                EditorUtility.DisplayDialog("提示", $"Expression Parameters为空！", "Ok");
                return;
            }

            var newParameters = _parameters.parameters.ToList();

            var removeList = new List<VRCExpressionParameters.Parameter>();
            foreach (var parameterName in _dataReference.Data.Categories.Select(category => $"YCM/{category.Name}/Switch"))
            {
                foreach (var par in newParameters.Where(par => par.name == parameterName))
                {
                    removeList.Add(par);
                    break;
                }
            }

            foreach (var remove in removeList)
                newParameters.Remove(remove);


            foreach (var category in data.Categories)
            {
                if (category.Clothes.Count == 0) continue;
                newParameters.Add(new VRCExpressionParameters.Parameter
                {
                    name = $"YCM/{category.Name}/Switch",
                    valueType = VRCExpressionParameters.ValueType.Int,
                    defaultValue = category.Default,
                    saved = true
                });
            }

            _parameters.parameters = newParameters.ToArray();

            // 配置 ExpressionMenu
            // 删除文件夹重新创建文件
            var menuDir = GetIDPath() + "/Expressions";
            if (Directory.Exists(menuDir))
                Directory.Delete(menuDir, true);
            Directory.CreateDirectory(menuDir);

            // 生成衣服菜单
            var mainMenu = CreateSubMenuAssets($"{menuDir}/ClothesMenu.asset");
            var currentCategoryMenu = mainMenu;

            var expressionMenuPath = AssetDatabase.GetAssetPath(_expressionsMenu).Replace(_expressionsMenu.name + ".asset", "");
            var currentExMenu = _dataReference.ParentMenu == null ? _expressionsMenu : _dataReference.ParentMenu;

            if (_expressionsMenu.controls.Count >= 8)
                currentExMenu = GetLastNextSubMenu(_expressionsMenu, expressionMenuPath, _expressionsMenu.name, 0);

            if (currentExMenu != _expressionsMenu)
                BackupFile(backupPath, currentExMenu);

            foreach (var category in data.Categories)
            {
                if (category.Clothes.Count == 0) continue;

                if (currentCategoryMenu.controls.Count >= 8)
                {
                    currentCategoryMenu = GetLastNextSubMenu(mainMenu, $"{menuDir}", mainMenu.name, 0, true, mainMenu);
                }

                var categoryMenu = CreateSubMenuAssets($"{menuDir}/Category_{category.Name}.asset", currentCategoryMenu, category.Name, category.Icon);
                var parameterName = $"YCM/{category.Name}/Switch";

                var currentClothesMenu = categoryMenu;
                var clothesPageIndex = 0;

                foreach (var clothes in category.Clothes)
                {
                    if (currentClothesMenu.controls.Count >= 7)
                    {
                        currentClothesMenu = CreateSubMenuAssets("", currentClothesMenu, "下一页", null, true);
                        currentClothesMenu.name = $"{category.Name}_Page {++clothesPageIndex}";
                    }


                    currentClothesMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = clothes.Name,
                        icon = clothes.Icon,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRCExpressionsMenu.Control.Parameter
                        {
                            name = parameterName
                        },
                        value = category.Clothes.IndexOf(clothes)
                    });

                    EditorUtility.SetDirty(currentClothesMenu);
                }
            }


            var isFindMenu = false;
            foreach (var control in currentExMenu.controls)
            {
                if (control.name != Localization.Get("window_title")) continue;

                isFindMenu = true;
                if (EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("apply_menu_find_tip"), Localization.Get("window_title")), Localization.Get("yes"), Localization.Get("no")))
                {
                    control.subMenu = mainMenu;
                }

                break;
            }

            if (!isFindMenu)
            {
                currentExMenu.controls.Add(new VRCExpressionsMenu.Control()
                {
                    name = Localization.Get("window_title"),
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = mainMenu
                });
            }


            EditorUtility.SetDirty(currentExMenu);


            // 创建动画文件
            // 删除旧文件，替换为新文件

            var animDir = GetIDPath() + "/Animations";
            if (Directory.Exists(animDir))
                Directory.Delete(animDir, true);
            Directory.CreateDirectory(animDir);

            _descriptor.customizeAnimationLayers = true;

            foreach (var category in data.Categories)
            {
                var clothesAnimDir = animDir + "/" + category.Name;
                if (!Directory.Exists(clothesAnimDir))
                    Directory.CreateDirectory(clothesAnimDir);

                var clothesAnimClipList = new List<AnimationClip>();
                for (var i = 0; i < category.Clothes.Count; i++)
                {
                    var clothes = category.Clothes[i];
                    var clip = GetClothesClip(category, i);
                    clothesAnimClipList.Add(clip);
                    AssetDatabase.CreateAsset(clip, $"{clothesAnimDir}/{category.Name}_{clothes.Name}.anim");
                }

                var parameterName = $"YCM/{category.Name}/Switch";
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

                if (category.Clothes.Count > 0)
                {
                    _fxLayer.AddParameter(new AnimatorControllerParameter
                    {
                        name = parameterName,
                        type = AnimatorControllerParameterType.Int,
                        defaultInt = 0
                    });
                    var categoryStateMachine = new AnimatorStateMachine
                    {
                        name = parameterName,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    AssetDatabase.AddObjectToAsset(categoryStateMachine, _fxLayer); // 必须放这，我也不知道为什么 *那就放这里吧

                    for (var i = 0; i < clothesAnimClipList.Count; i++)
                    {
                        var clip = clothesAnimClipList[i];

                        var state = categoryStateMachine.AddState(clip.name.Replace(".", "_"));
                        state.motion = clip;
                        var transition = categoryStateMachine.AddAnyStateTransition(state);
                        transition.duration = 0;
                        transition.AddCondition(AnimatorConditionMode.Equals, i, parameterName);
                    }

                    _fxLayer.AddLayer(new AnimatorControllerLayer
                    {
                        name = categoryStateMachine.name,
                        defaultWeight = 1f,
                        stateMachine = categoryStateMachine
                    });

                    // 添加Parameter Driver
                    if (category.HasParameterDriver())
                    {
                        var driverStateMachine = new AnimatorStateMachine()
                        {
                            name = parameterName + "_ParameterDriver",
                            hideFlags = HideFlags.HideInHierarchy
                        };
                        AssetDatabase.AddObjectToAsset(driverStateMachine, _fxLayer);

                        var idleState = driverStateMachine.AddState("Idle");

                        for (var i = 0; i < category.Clothes.Count; i++)
                        {
                            var clothes = category.Clothes[i];
                            if (!clothes.HasRealParameterDriver()) continue;

                            var enterState = driverStateMachine.AddState($"{clothes.Name}_Enter");
                            var exitState = driverStateMachine.AddState($"{clothes.Name}_Exit");

                            var idleToEnterTransition = idleState.AddTransition(enterState);
                            var enterToExitTransition = enterState.AddTransition(exitState);
                            var exitTransition = exitState.AddExitTransition();

                            idleToEnterTransition.AddCondition(AnimatorConditionMode.Equals, i, parameterName);
                            enterToExitTransition.AddCondition(AnimatorConditionMode.NotEqual, i, parameterName);
                            exitTransition.AddCondition(AnimatorConditionMode.NotEqual, i, parameterName);

                            idleToEnterTransition.duration = 0;
                            enterToExitTransition.duration = 0;
                            exitTransition.duration = 0;


                            if (clothes.EnterParameter.Parameters.Count > 0)
                            {
                                var showDriver = enterState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                                showDriver.parameters = clothes.EnterParameter.Parameters;
                                showDriver.localOnly = clothes.EnterParameter.IsLocal;
                            }

                            if (clothes.ExitParameter.Parameters.Count > 0)
                            {
                                var exitDriver = exitState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                                exitDriver.parameters = clothes.ExitParameter.Parameters;
                                exitDriver.localOnly = clothes.ExitParameter.IsLocal;
                            }
                        }

                        _fxLayer.AddLayer(new AnimatorControllerLayer()
                        {
                            name = driverStateMachine.name,
                            defaultWeight = 1f,
                            stateMachine = driverStateMachine
                        });
                    }
                }
            }

            EditorUtility.SetDirty(_fxLayer);
            EditorUtility.SetDirty(_expressionsMenu);
            EditorUtility.SetDirty(_parameters);


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(Localization.Get("tips"), Localization.Get("apply_success_tip"), Localization.Get("ok"));
        }

        private void BackupFile(string path, Object targetFile)
        {
            var sourcePath = AssetDatabase.GetAssetPath(targetFile);
            var fileInfo = new FileInfo(sourcePath);

            // Debug.Log($"{sourcePath}\n{path + fileInfo.Name}");
            var destPath = path + "/" + fileInfo.Name;
            if (!File.Exists(destPath))
                FileUtil.CopyFileOrDirectory(sourcePath, destPath);
        }


        private VRCExpressionsMenu GetLastNextSubMenu(VRCExpressionsMenu current, string path, string menuName, int index, bool isAddChild = false, VRCExpressionsMenu parent = null)
        {
            if (current.controls.Count < 8) return current;

            var currentName = $"{menuName} ({index})";
            var createPath = path + $"/{currentName}.asset";

            var control = current.controls[current.controls.Count - 1];
            if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
            {
                VRCExpressionsMenu subMenu = null;
                if (control.name == "下一页" || control.name == "下一个" || control.name == "Next" || control.name == "Next Page")
                    subMenu = control.subMenu;

                if (subMenu != null)
                {
                    return GetLastNextSubMenu(control.subMenu, path, menuName, ++index, isAddChild, parent);
                }
            }

            current.controls.Remove(control);

            var currentExMenu = CreateSubMenuAssets(createPath, current, Localization.Get("apply_next_page"), _nextIcon, isAddChild, parent);
            currentExMenu.name = currentName;
            currentExMenu.controls.Add(control);

            return currentExMenu;
        }

        private VRCExpressionsMenu CreateSubMenuAssets(string path, VRCExpressionsMenu parentMenu = null, string createdMenuName = "", Texture2D icon = null, bool isAddToChild = false, VRCExpressionsMenu parentMenuToAdd = null)
        {
            var createdMenu = CreateInstance<VRCExpressionsMenu>();

            if (!isAddToChild)
            {
                if (File.Exists(path))
                    File.Delete(path);
                AssetDatabase.CreateAsset(createdMenu, path);
            }

            if (parentMenu != null)
            {
                parentMenu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = createdMenuName,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = createdMenu,
                    icon = icon
                });

                if (isAddToChild)
                {
                    if (parentMenuToAdd == null)
                        parentMenuToAdd = parentMenu;
                    AssetDatabase.AddObjectToAsset(createdMenu, parentMenuToAdd);
                }

                EditorUtility.SetDirty(parentMenu);
            }

            AssetDatabase.SaveAssets();
            return createdMenu;
        }

        private AnimationClip GetClothesClip(CMClothesCategorySo category, int index)
        {
            var clip = new AnimationClip { name = category.Clothes[index].Name };
            var parametersDic = GetClothesParameters(category, index);

            var showList = parametersDic["Show"];
            var hideList = parametersDic["Hide"];
            var smrList = category.Clothes[index].GetNotEmptyParameters(category.Clothes[index].SMRParameters);


            foreach (var showParameter in showList)
            {
                var curve = new AnimationCurve { keys = new[] { new Keyframe { time = 0, value = 1 } } };
                var bind = new EditorCurveBinding
                {
                    path = showParameter.Path,
                    propertyName = "m_IsActive",
                    type = typeof(GameObject)
                };
                AnimationUtility.SetEditorCurve(clip, bind, curve);
            }

            foreach (var hideParameter in hideList)
            {
                var curve = new AnimationCurve { keys = new[] { new Keyframe { time = 0, value = 0 } } };
                var bind = new EditorCurveBinding
                {
                    path = hideParameter.Path,
                    propertyName = "m_IsActive",
                    type = typeof(GameObject)
                };
                AnimationUtility.SetEditorCurve(clip, bind, curve);
            }

            foreach (var smr in smrList)
            {
                if (string.IsNullOrEmpty(smr.Path)) continue;


                if (smr.SmrParameter.Index < 0) continue;

                EditorCurveBinding bind;
                switch (smr.SmrParameter.Type)
                {
                    case CMClothesData.ClothesAnimParameter.SMRParameter.SMRType.ShapeKey:
                        if (string.IsNullOrEmpty(smr.SmrParameter.BlendShapeName)) continue;
                        var curve = new AnimationCurve { keys = new[] { new Keyframe { time = 0, value = smr.SmrParameter.BlendShapeValue } } };
                        bind = new EditorCurveBinding
                        {
                            path = smr.Path,
                            propertyName = "blendShape." + smr.SmrParameter.BlendShapeName,
                            type = typeof(SkinnedMeshRenderer),
                        };
                        AnimationUtility.SetEditorCurve(clip, bind, curve);
                        break;
                    case CMClothesData.ClothesAnimParameter.SMRParameter.SMRType.Material:
                        var objCurve = new ObjectReferenceKeyframe { time = 0, value = smr.SmrParameter.Material };

                        bind = EditorCurveBinding.PPtrCurve(smr.Path, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{smr.SmrParameter.Index}]");
                        AnimationUtility.SetObjectReferenceCurve(clip, bind, new[] { objCurve });
                        break;
                }
            }


            return clip;
        }


        private Dictionary<string, List<CMClothesData.ClothesAnimParameter>> GetClothesParameters(CMClothesCategorySo category, int index)
        {
            if (index < 0 || index > category.Clothes.Count)
                return null;
            var showList = new List<CMClothesData.ClothesAnimParameter>();
            var hideList = new List<CMClothesData.ClothesAnimParameter>();

            for (var i = 0; i < category.Clothes.Count; i++)
            {
                if (i == index)
                    continue;
                var clothes = category.Clothes[i];

                hideList = CombineCAPList(hideList, clothes.GetNotEmptyParameters(clothes.ShowParameters));
                showList = CombineCAPList(showList, clothes.GetNotEmptyParameters(clothes.HideParameters));
            }

            var currentClothes = category.Clothes[index];
            showList = CombineCAPList(showList, currentClothes.GetNotEmptyParameters(currentClothes.ShowParameters));
            hideList = CombineCAPList(hideList, currentClothes.GetNotEmptyParameters(currentClothes.HideParameters));


            foreach (var showParameter in currentClothes.ShowParameters)
            {
                var removeList = hideList.Where(hide => hide.Path == showParameter.Path).ToList();

                foreach (var r in removeList)
                {
                    hideList.Remove(r);
                }
            }

            foreach (var hideParameter in currentClothes.HideParameters)
            {
                var removeList = showList.Where(hide => hide.Path == hideParameter.Path).ToList();
                foreach (var r in removeList)
                {
                    showList.Remove(r);
                }
            }


            var dic = new Dictionary<string, List<CMClothesData.ClothesAnimParameter>>()
            {
                { "Show", showList },
                { "Hide", hideList }
            };


            return dic;
        }

        private List<CMClothesData.ClothesAnimParameter> CombineCAPList(List<CMClothesData.ClothesAnimParameter> list1, List<CMClothesData.ClothesAnimParameter> list2)
        {
            list1.AddRange(list2);
            return list1.Union(list2).ToList();
        }

        private AnimatorControllerParameterType GetParameterType(string driverName)
        {
            foreach (var parameter in GetAnimatorParametersWithTool())
            {
                if (parameter.name == driverName)
                    return parameter.type;
            }

            return AnimatorControllerParameterType.Bool;
        }

        private int IndexInParameterNames(string driverName)
        {
            var names = GetParameterNames();
            for (var i = 0; i < names.Length; i++)
            {
                var currentName = names[i];
                if (currentName == driverName)
                    return i;
            }

            return -1;
        }

        private string[] GetParameterNames()
        {
            return GetAnimatorParametersWithTool().Select(param => param.name).ToArray();
        }

        private List<AnimatorControllerParameter> GetAnimatorParametersWithTool()
        {
            var parameters = _fxLayer.parameters.ToList();
            parameters.AddRange(_dataReference.Data.Categories.Select(category => new AnimatorControllerParameter() { defaultInt = 0, name = $"YCM/{category.Name}/Switch", type = AnimatorControllerParameterType.Int }));
            return parameters;
        }


        private void PreviewAll()
        {
            if (!_isClothesPreview) return;

            PreviewGameObject();
            PreviewSMR(_clothes);

            if (_isStartCapture)
            {
                _isStartCapture = false;
                StopCapture(false);

                YuebyUtil.WaitToDo(20, "Wait to setup capture", () =>
                {
                    _isStartCapture = true;
                    SetupCapture(false);
                });
            }
        }

        private void PreviewGameObject()
        {
            foreach (var category in _dataReference.Data.Categories)
            {
                if (category.Clothes.Count == 0) continue;


                var parameters = GetClothesParameters(category, category.Selected);
                var showList = parameters["Show"];
                var hideList = parameters["Hide"];

                Undo.RegisterFullObjectHierarchyUndo(_descriptor.gameObject, "Record Descriptor GameObjects State");

                foreach (var trans in hideList.Select(parameter => _descriptor.transform.Find(parameter.Path)).Where(trans => trans))
                {
                    // Undo.RegisterCompleteObjectUndo(trans.gameObject, "Preview Hide GameObject");
                    trans.gameObject.SetActive(false);

                    // Debug.Log(parameter.Path);
                }


                foreach (var trans in showList.Select(parameter => _descriptor.transform.Find(parameter.Path)).Where(trans => trans))
                {
                    // Undo.RegisterCompleteObjectUndo(trans.gameObject, "Preview Show GameObject");
                    trans.gameObject.SetActive(true);
                }
            }
        }

        private void PreviewSMR(CMClothesData clothes)
        {
            ResetAvatarSMRs();

            var smrParameters = clothes.SMRParameters;
            foreach (var parameter in smrParameters)
            {
                var trans = _descriptor.transform.Find(parameter.Path);
                if (!trans) continue;
                var skinnedMeshRenderer = trans.GetComponent<SkinnedMeshRenderer>();
                if (!skinnedMeshRenderer) continue;

                if (parameter.SmrParameter.Index < 0) continue;
                Undo.RegisterCompleteObjectUndo(skinnedMeshRenderer, "Preview SMR");
                switch (parameter.SmrParameter.Type)
                {
                    case CMClothesData.ClothesAnimParameter.SMRParameter.SMRType.ShapeKey:
                        skinnedMeshRenderer.SetBlendShapeWeight(parameter.SmrParameter.Index, parameter.SmrParameter.BlendShapeValue);
                        break;
                    case CMClothesData.ClothesAnimParameter.SMRParameter.SMRType.Material:
                        var mats = skinnedMeshRenderer.sharedMaterials;

                        mats[parameter.SmrParameter.Index] = parameter.SmrParameter.Material;
                        skinnedMeshRenderer.sharedMaterials = mats;
                        break;
                }
            }
        }

        private CMAvatarState _avatarState;

        private void RecordAvatarState()
        {
            _avatarState = new CMAvatarState(_descriptor.gameObject);
        }

        private void ResetAvatarState()
        {
            _avatarState?.Reset();
        }

        private void ResetAvatarGameObjects()
        {
            _avatarState?.ResetGameObjects();
        }

        private void ResetAvatarSMRs()
        {
            _avatarState?.ResetSMRs();
        }

        private void MoveFile()
        {
            var path = EditorUtility.OpenFolderPanel("选择保存路径", _dataReference.SavePath, "");
            if (string.IsNullOrEmpty(path) || path == _dataReference.SavePath) return;

            var targetPath = FileUtil.GetProjectRelativePath(path) + "/ClothesManager";
            if (targetPath != _dataReference.SavePath)
            {
                var lastPath = _dataReference.SavePath;
                if (!Directory.Exists(lastPath))
                    return;

                if (Directory.Exists(targetPath))
                {
                    if (Directory.GetFiles(targetPath).Length > 0)
                        Debug.Log("Target Directory:" + targetPath + " Not Empty!");
                    else
                        Directory.Delete(targetPath, true);
                }

                FileUtil.MoveFileOrDirectory(lastPath, targetPath);

                if (File.Exists(lastPath + ".meta"))
                    File.Delete(lastPath + ".meta");

                _dataReference.SavePath = targetPath;

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(Localization.Get("tips"), Localization.Get("tool_save_path_change_success"), Localization.Get("ok"));
                YuebyUtil.PingProject(targetPath);
            }
        }
    }

    public class CMAvatarState
    {
        private readonly List<CMAvatarGameObjectState> _gameObjectStates = new List<CMAvatarGameObjectState>();
        private readonly List<CMAvatarSMRState> _smrStates = new List<CMAvatarSMRState>();

        public CMAvatarState(GameObject gameObject)
        {
            var transforms = gameObject.GetComponentsInChildren<Transform>(true);
            foreach (var trans in transforms)
                _gameObjectStates.Add(new CMAvatarGameObjectState(trans.gameObject));

            var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer == null) continue;
                _smrStates.Add(new CMAvatarSMRState(renderer));
            }
        }

        public void Reset()
        {
            ResetGameObjects();
            ResetSMRs();
        }

        public void ResetGameObjects()
        {
            foreach (var gameObjectState in _gameObjectStates)
                gameObjectState.Reset();
        }

        public void ResetSMRs()
        {
            foreach (var smr in _smrStates)
                smr.Reset();
        }


        private class CMAvatarGameObjectState
        {
            private readonly GameObject _gameObject;
            private readonly bool _isActive;

            public CMAvatarGameObjectState(GameObject gameObject)
            {
                _gameObject = gameObject;
                _isActive = gameObject.activeSelf;
            }

            public void Reset()
            {
                if (_gameObject)
                    _gameObject.SetActive(_isActive);
            }
        }

        private class CMAvatarSMRState
        {
            private readonly SkinnedMeshRenderer _skinnedMeshRenderer;
            private readonly Dictionary<int, float> _blendShapes = new Dictionary<int, float>();
            private readonly Material[] _materials;

            public CMAvatarSMRState(SkinnedMeshRenderer skinnedMeshRenderer)
            {
                _skinnedMeshRenderer = skinnedMeshRenderer;
                if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
                {
                    Debug.Log("ClothesManager: SkinnedMeshRenderer is null？");
                    return;
                }

                for (var i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                {
                    var weight = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    _blendShapes.Add(i, weight);
                }

                _materials = skinnedMeshRenderer.sharedMaterials;
            }

            public void Reset()
            {
                foreach (var item in _blendShapes)
                    _skinnedMeshRenderer.SetBlendShapeWeight(item.Key, item.Value);

                _skinnedMeshRenderer.sharedMaterials = _materials;
            }
        }
    }
}