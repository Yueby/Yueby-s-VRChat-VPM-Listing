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
using VRC.SDKBase;
using Yueby.Utils;
using static Yueby.AvatarTools.ClothesManager.CMClothesData.ClothesAnimParameter.SMRParameter;
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

        private void DeletePersistentData()
        {
            if (string.IsNullOrEmpty(_dataReference.ID))
                return;

            if (_dataReference.Data.Categories.Count > 0)
            {
                if (_fxLayer)
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
                }

                if (_parameters)
                {
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
            }

            if (_expressionsMenu)
            {
                var currentExMenu = _expressionsMenu;
                if (currentExMenu.controls.Count >= 8)
                {
                    var menuDir = GetIDPath() + "/Expressions";
                    currentExMenu = GetLastNextSubMenu(currentExMenu, $"{menuDir}", _expressionsMenu.name, 0);
                }

                var control = currentExMenu.controls.FirstOrDefault(c => c.name == Localization.Get("window_title"));
                if (control != null)
                    currentExMenu.controls.Remove(control);
            }

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

            if (_expressionsMenu)
                EditorUtility.SetDirty(_expressionsMenu);
            if (_parameters)
                EditorUtility.SetDirty(_parameters);
            if (_fxLayer)
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
            var path = "Packages/yueby.tools.avatar-tools/Editor/Assets/ClothesManager/Preview.renderTexture";
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

            EditorUtils.FocusTarget(_descriptor.gameObject);
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
            foreach (var showAnimParameter in parameters.ToList())
            {
                if (string.IsNullOrEmpty(showAnimParameter.Path))
                    parameters.Remove(showAnimParameter);
            }

            // 自动获取后缀为 (objectName) 的对象
            if (parameters.Count <= 0 && objects is { Length: 1 })
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
                    var type = allowType.Name;
                    var smrType = SMRType.BlendShapes;
                    if (allowType == typeof(GameObject))
                    {
                        if (obj is GameObject gameObject)
                        {
                            var hierarchyPath = VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject.transform);
                            path = hierarchyPath.Substring(_descriptor.name.Length + 1, hierarchyPath.Length - _descriptor.name.Length - 1);
                            // type = nameof(GameObject);
                        }
                    }
                    else if (allowType == typeof(SkinnedMeshRenderer))
                    {
                        if (obj is GameObject gameObject)
                        {
                            var sm = gameObject.GetComponent<SkinnedMeshRenderer>();
                            if (sm == null)
                            {
                                EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_none_smr_tip"), obj.name), Localization.Get("ok"));
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

                                smrType = SMRType.Materials;
                                // continue;
                            }

                            var hierarchyPath = VRC.Core.ExtensionMethods.GetHierarchyPath(skinnedMeshRenderer.transform);
                            path = hierarchyPath.Substring(_descriptor.name.Length + 1, hierarchyPath.Length - _descriptor.name.Length - 1);
                            // type = nameof(SkinnedMeshRenderer);

                            // var count = parameters.Count(animParameter => animParameter.Type == nameof(SkinnedMeshRenderer) && animParameter.Path == path);

                            // if (count == skinnedMeshRenderer.sharedMesh.blendShapeCount)
                            // {
                            //     EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_all_bs_tip"), path), Localization.Get("ok"));
                            //     continue;
                            // }
                        }
                    }

                    var parameter = new CMClothesData.ClothesAnimParameter()
                    {
                        Path = path,
                        Type = type,
                        SmrParameter = new CMClothesData.ClothesAnimParameter.SMRParameter
                        {
                            Type = smrType
                        }
                    };

                    handler?.Invoke(parameter);

                    if (!_clothes.ContainsInList(parameter, parameters))
                        paths.Add(parameter);

                    Debug.Log($"Add {parameter.Path} {parameter.Type} {parameter.SmrParameter.Type}");
                }
            }

            parameters.AddRange(paths);
        }

        private float RegisterClothPathListPanel(Rect rect, int index, ref List<CMClothesData.ClothesAnimParameter> animParameters)
        {
            var height = EditorGUIUtility.singleLineHeight;
            GameObject obj = null;


            if (index > animParameters.Count - 1) return height;
            var target = animParameters[index];

            // Debug.Log(animParameters.Count);

            if (!string.IsNullOrEmpty(target.Path))
            {
                var pathTrans = _descriptor.transform.Find(target.Path);
                // Debug.Log(target.Path);
                if (pathTrans)
                    obj = pathTrans.gameObject;
            }

            var objFieldRect = new Rect(rect.x, rect.y + 2, rect.width / 2 - 1, height);
            height += 4;
            if (target.Type == nameof(SkinnedMeshRenderer))
            {
                var skinnedMeshRenderer = obj != null ? obj.GetComponent<SkinnedMeshRenderer>() : null;

                // Debug.Log(skinnedMeshRenderer);

                EditorGUI.BeginChangeCheck();
                skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUI.ObjectField(objFieldRect, skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
                if (EditorGUI.EndChangeCheck())
                {
                    if (skinnedMeshRenderer)
                    {
                        var hierarchyPath = VRC.Core.ExtensionMethods.GetHierarchyPath(skinnedMeshRenderer.transform);
                        var path = hierarchyPath.Substring(_descriptor.name.Length + 1, hierarchyPath.Length - _descriptor.name.Length - 1);


                        // var path = [.._descriptor.name.Length];

                        if (skinnedMeshRenderer.sharedMesh.blendShapeCount == 0)
                        {
                            var message = string.Format(Localization.Get("clothes_none_bs_tip"), $"{nameof(SkinnedMeshRenderer)}:{skinnedMeshRenderer.name}");
                            EditorUtility.DisplayDialog(Localization.Get("tips"), message, Localization.Get("ok"));

                            target.SmrParameter.Type = SMRType.Materials;
                        }

                        target.Path = path;
                        target.SmrParameter.Index = -1;
                        target.SmrParameter.BlendShapeValue = 0;
                    }
                }

                if (skinnedMeshRenderer)
                {
                    var typeRect = new Rect(objFieldRect.x + objFieldRect.width + 1, objFieldRect.y, objFieldRect.width, objFieldRect.height);
                    EditorGUI.BeginChangeCheck();
                    var lastType = target.SmrParameter.Type;
                    target.SmrParameter.Type = (SMRType)EditorGUI.EnumPopup(typeRect, target.SmrParameter.Type);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var param = new CMClothesData.ClothesAnimParameter(target);
                        param.SmrParameter.Type = lastType;

                        var smrList = GetOtherSMRParameters(param);
                        if (smrList.Count > 0 && EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_smr_shape_key_change_to_mat_tip"), smrList.Count), Localization.Get("ok"), Localization.Get("cancel")))
                        {
                            RemoveSMRParameterToOther(param);
                        }

                        target.SmrParameter.Index = -1;
                    }

                    var addRect = new Rect(objFieldRect.x, objFieldRect.y + objFieldRect.height + 1, 20, objFieldRect.height);
                    var firstRect = new Rect(addRect.x + addRect.width + 1, objFieldRect.y + objFieldRect.height + 1, objFieldRect.width - addRect.width, objFieldRect.height);
                    var secondRect = new Rect(firstRect.x + firstRect.width + 5, firstRect.y, rect.width - firstRect.width - addRect.width - 7, firstRect.height);

                    height += addRect.height + 1;
                    EditorGUI.BeginDisabledGroup(target.SmrParameter.Index == -1);

                    if (GUI.Button(addRect, "+"))
                    {
                        var parameter = AddNextSMRParameter(target, skinnedMeshRenderer);

                        var message = "";
                        switch (parameter.SmrParameter.Type)
                        {
                            case SMRType.BlendShapes:
                                var count = _clothes.SMRParameters.Count(animParameter => animParameter.Type == nameof(SkinnedMeshRenderer) && animParameter.Path == target.Path);
                                if (count > skinnedMeshRenderer.sharedMesh.blendShapeCount)
                                {
                                    parameter.SmrParameter.Index = -1;
                                    parameter.SmrParameter.BlendShapeName = "";
                                }
                                else
                                    parameter.SmrParameter.BlendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(parameter.SmrParameter.Index);

                                message = string.Format(Localization.Get("clothes_smr_add_new_tip"), parameter.SmrParameter.BlendShapeName);
                                break;
                            case SMRType.Materials:
                                message = Localization.Get("clothes_smr_mat_add_new_tip");
                                break;
                        }

                        if (parameter.SmrParameter.Index >= 0 && EditorUtility.DisplayDialog(Localization.Get("tips"), message, Localization.Get("ok"), Localization.Get("cancel")))
                            AddSMRParameterToOther(parameter);
                        return height;
                    }

                    EditorGUI.EndDisabledGroup();

                    switch (target.SmrParameter.Type)
                    {
                        case SMRType.BlendShapes:
                            var blendShapeNames = new List<string>();
                            for (var i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                                blendShapeNames.Add(skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i));

                            if (target.SmrParameter.Index > blendShapeNames.Count - 1)
                                target.SmrParameter.Index = -1;

                            var lastIndex = target.SmrParameter.Index;
                            EditorGUI.BeginChangeCheck();
                            target.SmrParameter.Index = EditorGUI.Popup(firstRect, target.SmrParameter.Index, blendShapeNames.ToArray());

                            if (target.SmrParameter.Index != -1)
                                target.SmrParameter.BlendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(target.SmrParameter.Index);

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (_clothes.ContainsInList(target, animParameters))
                                {
                                    target.SmrParameter.Index = -1;
                                    target.SmrParameter.BlendShapeValue = 0;
                                }
                                else
                                    target.SmrParameter.BlendShapeValue = skinnedMeshRenderer.GetBlendShapeWeight(target.SmrParameter.Index);

                                if (target.SmrParameter.Index != -1)
                                {
                                    if (lastIndex < 0)
                                    {
                                        if (EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_smr_add_new_tip"), target.SmrParameter.BlendShapeName), Localization.Get("ok"), Localization.Get("cancel")))
                                        {
                                            AddSMRParameterToOther(target);
                                        }
                                    }
                                    else
                                    {
                                        var param = new CMClothesData.ClothesAnimParameter(target);
                                        param.SmrParameter.Index = lastIndex;

                                        var smrList = GetOtherSMRParameters(param);
                                        if (smrList.Count > 0 && EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_smr_change_tip"), smrList.Count), Localization.Get("ok"), Localization.Get("cancel")))
                                        {
                                            ChangeSMRParameterToOther(param, target.SmrParameter.Index);
                                        }
                                    }
                                }
                            }

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

                            break;
                        case SMRType.Materials:
                            var count = skinnedMeshRenderer.sharedMaterials.Length;
                            var popups = new string[count];
                            for (var i = 0; i < count; i++)
                                popups[i] = i.ToString();

                            if (target.SmrParameter.Index > popups.Length - 1)
                                target.SmrParameter.Index = -1;

                            lastIndex = target.SmrParameter.Index;
                            EditorGUI.BeginChangeCheck();
                            target.SmrParameter.Index = EditorGUI.Popup(firstRect, target.SmrParameter.Index, popups);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (_clothes.ContainsInList(target, animParameters))
                                {
                                    target.SmrParameter.Index = -1;
                                    target.SmrParameter.Type = SMRType.BlendShapes;
                                    target.SmrParameter.BlendShapeName = "";
                                    return height;
                                }

                                target.SmrParameter.Material = skinnedMeshRenderer.sharedMaterials[target.SmrParameter.Index];
                                if (target.SmrParameter.Index != -1)
                                {
                                    if (lastIndex < 0)
                                    {
                                        if (EditorUtility.DisplayDialog(Localization.Get("tips"), Localization.Get("clothes_smr_mat_add_new_tip"), Localization.Get("ok"), Localization.Get("cancel")))
                                        {
                                            AddSMRParameterToOther(target);
                                        }
                                    }
                                    else
                                    {
                                        var param = new CMClothesData.ClothesAnimParameter(target);
                                        param.SmrParameter.Index = lastIndex;

                                        var smrList = GetOtherSMRParameters(param);
                                        if (smrList.Count > 0 && EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("clothes_smr_mat_change_tip"), smrList.Count), Localization.Get("ok"), Localization.Get("cancel")))
                                        {
                                            ChangeSMRParameterToOther(param, target.SmrParameter.Index);
                                        }
                                    }
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
                    var hierarchyPath = VRC.Core.ExtensionMethods.GetHierarchyPath(obj.transform);
                    var path = hierarchyPath.Substring(_descriptor.name.Length + 1, hierarchyPath.Length - _descriptor.name.Length - 1);
                    target.Path = path;
                    PreviewConfig();
                }
            }

            EditorUtility.SetDirty(_currentClothesCategory);
            return height;
        }

        private CMClothesData.ClothesAnimParameter AddNextSMRParameter(CMClothesData.ClothesAnimParameter target, SkinnedMeshRenderer renderer)
        {
            var addParameter = new CMClothesData.ClothesAnimParameter(target);
            addParameter.SmrParameter.Index += 1;

            var state = _avatarState.GetSMROriginState(renderer);
            if (state != null)
            {
                switch (addParameter.SmrParameter.Type)
                {
                    case SMRType.BlendShapes:
                        addParameter.SmrParameter.BlendShapeValue = state.GetBlendShapeWeight(addParameter.SmrParameter.Index);
                        break;
                    case SMRType.Materials:
                        addParameter.SmrParameter.Material = state.GetMaterial(addParameter.SmrParameter.Index);
                        break;
                }
            }

            if (_clothes.ContainsInList(addParameter, _clothes.SMRParameters))
            {
                return AddNextSMRParameter(addParameter, renderer);
            }

            _clothes.SMRParameters.Add(addParameter);

            return addParameter;
        }

        private void AddSMRParameterToOther(CMClothesData.ClothesAnimParameter target)
        {
            foreach (var clothes in _currentClothesCategory.Clothes)
            {
                if (clothes == _clothes) continue;
                if (clothes.ContainsInList(target, clothes.SMRParameters)) continue;
                var parameter = new CMClothesData.ClothesAnimParameter(target);

                clothes.SMRParameters.Add(parameter);

                var targetTrans = _descriptor.transform.Find(target.Path);
                if (!targetTrans) continue;
                var renderer = targetTrans.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null)
                {
                    var state = _avatarState.GetSMROriginState(renderer);
                    if (state != null)
                    {
                        switch (parameter.SmrParameter.Type)
                        {
                            case SMRType.BlendShapes:
                                parameter.SmrParameter.BlendShapeValue = state.GetBlendShapeWeight(parameter.SmrParameter.Index);
                                break;
                            case SMRType.Materials:
                                parameter.SmrParameter.Material = state.GetMaterial(parameter.SmrParameter.Index);
                                break;
                        }
                    }
                }
            }
        }

        private void RemoveSMRParameterToOther(CMClothesData.ClothesAnimParameter target)
        {
            foreach (var clothes in _currentClothesCategory.Clothes)
            {
                if (clothes == _clothes) continue;

                var parameter = new CMClothesData.ClothesAnimParameter(target);

                if (!clothes.ContainsInList(target, clothes.SMRParameters)) continue;
                clothes.DeleteInList(parameter, ref clothes.SMRParameters);
            }
        }

        private void ChangeSMRParameterToOther(CMClothesData.ClothesAnimParameter target, int index)
        {
            var smrList = GetOtherSMRParameters(target);
            foreach (var smr in smrList)
            {
                smr.SmrParameter.Index = index;
                var targetTrans = _descriptor.transform.Find(target.Path);
                if (!targetTrans) continue;
                var renderer = targetTrans.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null)
                {
                    var state = _avatarState.GetSMROriginState(renderer);
                    switch (smr.SmrParameter.Type)
                    {
                        case SMRType.BlendShapes:
                            smr.SmrParameter.BlendShapeValue = state.GetBlendShapeWeight(smr.SmrParameter.Index);
                            break;
                        case SMRType.Materials:
                            var mat = state.GetMaterial(smr.SmrParameter.Index);
                            if (mat != null)
                                smr.SmrParameter.Material = mat;
                            break;
                    }
                }
            }
        }

        private List<CMClothesData.ClothesAnimParameter> GetOtherSMRParameters(CMClothesData.ClothesAnimParameter target)
        {
            var list = new List<CMClothesData.ClothesAnimParameter>();

            foreach (var clothes in _currentClothesCategory.Clothes)
            {
                if (clothes == _clothes) continue;
                if (!clothes.ContainsInList(target, clothes.SMRParameters)) continue;
                foreach (var smrParameter in clothes.SMRParameters)
                {
                    if (smrParameter.SmrParameter.IsSame(target.SmrParameter))
                    {
                        list.Add(smrParameter);
                    }
                }
            }

            // foreach (var item in list)
            // {
            //     Debug.Log(item.Path);
            // }

            return list;
        }

        private void Apply(CMCDataSo data)
        {
            var backupPath = GetBackupsPath();
            var time = new DirectoryInfo(backupPath).Name;
            BackupFile(backupPath, _expressionsMenu, time);
            BackupFile(backupPath, _parameters, time);
            BackupFile(backupPath, _fxLayer, time);
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
            {
                Directory.Delete(menuDir, true);
                Debug.Log($"Delete {menuDir}");
            }

            Directory.CreateDirectory(menuDir);
            AssetDatabase.Refresh();

            // 生成衣服菜单
            var mainMenu = CreateSubMenuAssets($"{menuDir}/ClothesMenu.asset");

            var currentExMenu = _dataReference.ParentMenu == null ? _expressionsMenu : _dataReference.ParentMenu;

            if (currentExMenu.controls.Count >= 8)
                currentExMenu = GetLastNextSubMenu(currentExMenu, AssetDatabase.GetAssetPath(currentExMenu).Replace($"/{currentExMenu.name}.asset", ""), currentExMenu.name, 0);

            if (currentExMenu != _expressionsMenu)
                BackupFile(backupPath, currentExMenu, time);

            foreach (var category in data.Categories)
            {
                if (category.Clothes.Count == 0) continue;

                var currentMainMenu = mainMenu;
                // Debug.Log(currentMainMenu.controls.Count);
                if (category.ParentMenu != null)
                {
                    currentMainMenu = category.ParentMenu;
                    BackupFile(backupPath, currentMainMenu, time);
                }

                var currentCategoryMenu = GetLastNextSubMenu(currentMainMenu, $"{menuDir}", currentMainMenu.name, 0, false, currentMainMenu);

                var parameterName = $"YCM/{category.Name}/Switch";

                VRCExpressionsMenu firstMenu;
                CreateSubMenuAssets($"{menuDir}/Category_{category.Name}.asset", menu =>
                {
                    firstMenu = menu;

                    var currentClothesMenu = menu;
                    var clothesPageIndex = 0;
                    foreach (var clothes in category.Clothes)
                    {
                        if (currentClothesMenu.controls.Count >= 7)
                        {
                            CreateSubMenuAssets($"{menuDir}/Category_{category.Name}_Page {++clothesPageIndex}.asset", nextMenu =>
                            {
                                ReplaceControlOnSubMenu(Localization.Get("apply_next_page"), _nextIcon, currentClothesMenu, nextMenu);
                                currentClothesMenu = nextMenu;
                                AddControlToSubMenu(clothes.Name, parameterName, clothes.Icon, category.Clothes.IndexOf(clothes), currentClothesMenu);
                            });
                        }
                        else
                            AddControlToSubMenu(clothes.Name, parameterName, clothes.Icon, category.Clothes.IndexOf(clothes), currentClothesMenu);
                    }


                    SetupSubMenu(currentCategoryMenu, firstMenu, category.Name, category.Icon);
                    if (currentCategoryMenu != null)
                        EditorUtility.SetDirty(currentCategoryMenu);
                    EditorUtility.SetDirty(currentClothesMenu);
                    EditorUtility.SetDirty(currentMainMenu);
                    // AssetDatabase.SaveAssets();
                });
            }

            if (mainMenu.controls.Count > 0)
            {
                SetupSubMenu(currentExMenu, mainMenu, Localization.Get("window_title"), null);
                EditorUtility.SetDirty(mainMenu);
            }
            else
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(mainMenu));

            EditorUtility.SetDirty(currentExMenu);
            // AssetDatabase.SaveAssets();

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

                // 删除动画控制器内之前的配置
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

                // 添加新的进入动画控制器
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
                    categoryStateMachine.AddState("Idle");
                    for (var i = 0; i < clothesAnimClipList.Count; i++)
                    {
                        var clip = clothesAnimClipList[i];
                        if (clip == null) continue;
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
                                showDriver.parameters = clothes.EnterParameter.Convert();
                                showDriver.localOnly = clothes.EnterParameter.IsLocal;
                            }

                            if (clothes.ExitParameter.Parameters.Count > 0)
                            {
                                var exitDriver = exitState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                                exitDriver.parameters = clothes.ExitParameter.Convert();
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

        private void ReplaceControlOnSubMenu(string replaceMenuName, Texture2D icon, VRCExpressionsMenu menu, VRCExpressionsMenu targetMenu)
        {
            var control = menu.controls[menu.controls.Count - 1];
            if (control.name == replaceMenuName && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
            {
                control.name = replaceMenuName;
                control.subMenu = targetMenu;
                control.icon = icon;
            }
            else
            {
                menu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = replaceMenuName,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = targetMenu,
                    icon = icon
                });
            }
        }

        private void AddControlToSubMenu(string controlName, string parameterName, Texture2D icon, float value, VRCExpressionsMenu parent)
        {
            var control = new VRCExpressionsMenu.Control
            {
                name = controlName,
                icon = icon,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter
                {
                    name = parameterName
                },
                value = value
            };
            parent.controls.Add(control);
            EditorUtility.SetDirty(parent);
        }

        private void DeleteNullSubMenu(VRCExpressionsMenu current, string menuName, VRCExpressionsMenu exclude)
        {
            foreach (var control in current.controls.ToList())
            {
                if (control.type != VRCExpressionsMenu.Control.ControlType.SubMenu) continue;
                if (control.subMenu == exclude) continue;

                if (control.subMenu == null)
                    current.controls.Remove(control);
            }
        }

        private void SetupSubMenu(VRCExpressionsMenu menu, VRCExpressionsMenu target, string targetName, Texture2D icon)
        {
            var isFindMenu = false;
            foreach (var control in menu.controls)
            {
                if (control.name != targetName) continue;

                isFindMenu = true;
                if (EditorUtility.DisplayDialog(Localization.Get("tips"), string.Format(Localization.Get("apply_menu_find_tip"), targetName), Localization.Get("yes"), Localization.Get("no")))
                {
                    control.subMenu = target;
                    control.icon = icon;
                }
                else
                {
                    isFindMenu = false;
                    targetName += " (1)";
                }

                break;
            }

            if (!isFindMenu)
            {
                menu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = targetName,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = target,
                    icon = icon
                });
            }

            // EditorUtility.SetDirty(menu);
            // DeleteNullSubMenu(menu, targetName, target);
        }

        private void BackupFile(string path, Object targetFile, string backupTime)
        {
            var sourcePath = AssetDatabase.GetAssetPath(targetFile);
            var fileInfo = new FileInfo(sourcePath);

            var destPath = path + "/" + fileInfo.Name;
            if (!File.Exists(destPath))
            {
                FileUtil.CopyFileOrDirectory(sourcePath, destPath);
                AssetDatabase.Refresh();
                AssetDatabase.RenameAsset(destPath, fileInfo.Name + " " + backupTime);
                AssetDatabase.Refresh();
            }
        }

        private VRCExpressionsMenu GetLastNextSubMenu(VRCExpressionsMenu current, string path, string menuName, int index, bool isAddChild = false, VRCExpressionsMenu parent = null)
        {
            if (current.controls.Count < 8) return current;

            var currentName = $"{menuName} (Next {index + 1})";
            var createPath = path + $"/{currentName}.asset";

            var control = current.controls[current.controls.Count - 1];
            bool isNullSubMenu = false;
            if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
            {
                if (control.name == "下一页" || control.name == "下一个" || control.name == "Next" || control.name == "Next Page")
                {
                    if (control.subMenu != null)
                    {
                        return GetLastNextSubMenu(control.subMenu, path, menuName, ++index, isAddChild, parent);
                    }

                    isNullSubMenu = true;
                }
            }

            current.controls.Remove(control);

            var currentExMenu = CreateSubMenuAssets(createPath, current, Localization.Get("apply_next_page"), _nextIcon, isAddChild, parent);
            currentExMenu.name = currentName;

            if (!isNullSubMenu)
            {
                currentExMenu.controls.Add(control);
            }

            EditorUtility.SetDirty(current);

            return currentExMenu;
        }

        private void CreateSubMenuAssets(string path, UnityAction<VRCExpressionsMenu> onAction)
        {
            var createdMenu = CreateInstance<VRCExpressionsMenu>();
            onAction?.Invoke(createdMenu);

            if (File.Exists(path))
                File.Delete(path);
            AssetDatabase.CreateAsset(createdMenu, path);
            AssetDatabase.SaveAssets();
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
                ReplaceControlOnSubMenu(createdMenuName, icon, parentMenu, createdMenu);

                if (isAddToChild)
                {
                    if (parentMenuToAdd == null)
                        parentMenuToAdd = parentMenu;
                    AssetDatabase.AddObjectToAsset(createdMenu, parentMenuToAdd);
                }

                // EditorUtility.SetDirty(parentMenu);
            }

            // AssetDatabase.SaveAssets();
            return createdMenu;
        }

        private AnimationClip GetClothesClip(CMClothesCategorySo category, int index)
        {
            var clip = new AnimationClip { name = category.Clothes[index].Name };
            var parametersDic = GetClothesParameters(category, index);

            var showList = parametersDic["Show"];
            var hideList = parametersDic["Hide"];
            var smrList = category.Clothes[index].GetNotEmptyParameters(category.Clothes[index].SMRParameters);

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

            foreach (var smr in smrList)
            {
                if (string.IsNullOrEmpty(smr.Path)) continue;

                if (smr.SmrParameter.Index < 0) continue;

                EditorCurveBinding bind;
                switch (smr.SmrParameter.Type)
                {
                    case SMRType.BlendShapes:
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
                    case SMRType.Materials:
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

            return default;
        }

        private int IndexInParameterNames(string driverName)
        {
            if (string.IsNullOrEmpty(driverName)) return 0;

            var names = GetParameterNames();
            for (var i = 0; i < names.Length; i++)
            {
                var currentName = names[i];
                if (currentName == driverName)
                    return i;
            }

            return 0;
        }

        private string[] GetParameterNames()
        {
            var list = GetAnimatorParametersWithTool().Select(param => param.name).ToList();
            list.Insert(0, "-");
            return list.ToArray();
        }

        private List<AnimatorControllerParameter> GetAnimatorParametersWithTool()
        {
            var parameters = _fxLayer.parameters.ToList();
            parameters.AddRange(_dataReference.Data.Categories.Select(category => new AnimatorControllerParameter() { defaultInt = 0, name = $"YCM/{category.Name}/Switch", type = AnimatorControllerParameterType.Int }));
            return parameters;
        }

        private void PreviewConfig()
        {
            switch (_previewIndex)
            {
                case 0:
                    ResetAvatarState();
                    break;
                case 1:
                    PreviewAll();
                    break;
                case 2:
                    PreviewCurrent();
                    break;
            }
        }

        private void PreviewCurrent()
        {
            PreviewGameObject(true);
            PreviewSMR(_clothes);

            if (_isStartCapture)
            {
                _isStartCapture = false;
                StopCapture(false);

                EditorUtils.WaitToDo(20, "Wait to setup capture", () =>
                {
                    _isStartCapture = true;
                    SetupCapture(false);
                });
            }
        }

        private void PreviewAll()
        {
            PreviewGameObject();
            PreviewSMR(_clothes);

            if (_isStartCapture)
            {
                _isStartCapture = false;
                StopCapture(false);

                EditorUtils.WaitToDo(20, "Wait to setup capture", () =>
                {
                    _isStartCapture = true;
                    SetupCapture(false);
                });
            }
        }

        private void PreviewGameObject(bool isCurrent = false)
        {
            if (isCurrent)
            {
                var parameters = GetClothesParameters(_currentClothesCategory, _currentClothesCategory.Selected);
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
            else
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
        }

        private void PreviewSMR(CMClothesData clothes)
        {
            ResetAvatarSMRs();

            var smrParameters = clothes.SMRParameters;
            foreach (var parameter in smrParameters)
            {
                if (string.IsNullOrEmpty(parameter.Path)) continue;
                var trans = _descriptor.transform.Find(parameter.Path);
                if (!trans) continue;
                var skinnedMeshRenderer = trans.GetComponent<SkinnedMeshRenderer>();
                if (!skinnedMeshRenderer) continue;

                if (parameter.SmrParameter.Index < 0) continue;
                Undo.RegisterCompleteObjectUndo(skinnedMeshRenderer, "Preview SMR");
                switch (parameter.SmrParameter.Type)
                {
                    case SMRType.BlendShapes:
                        skinnedMeshRenderer.SetBlendShapeWeight(parameter.SmrParameter.Index, parameter.SmrParameter.BlendShapeValue);
                        break;
                    case SMRType.Materials:
                        var mats = skinnedMeshRenderer.sharedMaterials;
                        if (parameter.SmrParameter.Index <= mats.Length - 1)
                        {
                            mats[parameter.SmrParameter.Index] = parameter.SmrParameter.Material;
                            skinnedMeshRenderer.sharedMaterials = mats;
                        }

                        break;
                }
            }
        }

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
                EditorUtils.FocusTarget(_captureGo);
        }

        private void GetClothesCapture()
        {
            var categoryPath = GetCapturePath() + "/" + _currentClothesCategory.Name + "/";
            if (!Directory.Exists(categoryPath))
                Directory.CreateDirectory(categoryPath);

            var icon = EditorUtils.SaveRTToFile(categoryPath + _clothes.Name + ".png", _previewRT, _captureCamera);

            if (EditorUtility.DisplayDialog(Localization.Get("tips"), Localization.Get("tip_success_save"), Localization.Get("yes"), Localization.Get("no")))
            {
                EditorUtils.PingProject(icon);
            }

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
                EditorUtils.FocusTarget(_descriptor.gameObject);
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

        public CMAvatarSMRState GetSMROriginState(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            foreach (var state in _smrStates)
            {
                if (state.SkinnedMeshRenderer == skinnedMeshRenderer)
                {
                    return state;
                }
            }

            return null;
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

        public class CMAvatarSMRState
        {
            public SkinnedMeshRenderer SkinnedMeshRenderer { get; }
            private readonly Dictionary<int, float> _blendShapes = new Dictionary<int, float>();
            private readonly Material[] _materials;

            public CMAvatarSMRState(SkinnedMeshRenderer skinnedMeshRenderer)
            {
                if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
                {
                    // Debug.Log("ClothesManager: SkinnedMeshRenderer is null？");
                    return;
                }

                SkinnedMeshRenderer = skinnedMeshRenderer;
                for (var i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                {
                    var weight = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    _blendShapes.Add(i, weight);
                }

                _materials = skinnedMeshRenderer.sharedMaterials;
            }

            public float GetBlendShapeWeight(int index)
            {
                if (_blendShapes.Count > 0 && _blendShapes.TryGetValue(index, out var weight))
                {
                    return weight;
                }

                return 0f;
            }

            public Material GetMaterial(int index)
            {
                if (_materials != null && index >= 0 && index < _materials.Length)
                    return _materials[index];
                return null;
            }

            public void Reset()
            {
                if (SkinnedMeshRenderer == null) return;
                foreach (var item in _blendShapes)
                    SkinnedMeshRenderer.SetBlendShapeWeight(item.Key, item.Value);
                SkinnedMeshRenderer.sharedMaterials = _materials;
            }
        }
    }
}