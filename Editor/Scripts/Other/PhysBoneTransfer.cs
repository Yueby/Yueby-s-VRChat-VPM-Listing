using System;
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using Yueby;
using Yueby.Core.Utils;
using Yueby.Utils;

namespace YuebyAvatarTools.PhysBoneTransfer.Editor
{
    public class PhysboneTransfer : EditorWindow
    {
        private Vector2 _pos;
        private Transform _origin;
        private static bool _isOnlyTransferInArmature = false;
        private static bool _isTransferMAMergeArmature = true;
        private static bool _isTransferMAMergeAnimator = true;
        private static bool _isTransferMABoneProxy = true;
        private static bool _isTransferMaterials = true;
        private static bool _isTransferParticleSystems = true;
        private static bool _isTransferConstraints = true;
        private static bool _isTransferMaterialSwitcher = true;

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/PhysBone Transfer", false, 21)]
        public static void OpenWindow()
        {
            var window = CreateWindow<PhysboneTransfer>();
            window.titleContent = new GUIContent("Avatar Component Transfer");
            window.minSize = new Vector2(200, 250);
            window.Show();
        }

        private void OnGUI()
        {
            var selections = Selection.gameObjects;
            var isSelectedObject = selections.Length > 0;

            EditorUI.DrawEditorTitle("Avatar Component Transfer");
            EditorUI.VerticalEGLTitled("Configuration", () =>
            {
                EditorUI.HorizontalEGL(() =>
                {
                    _origin = (Transform)EditorUI.ObjectFieldVertical(_origin, "Source Armature", typeof(Transform));
                    EditorUI.Line(LineType.Vertical);
                }, GUILayout.MaxHeight(40));
            });

            EditorUI.VerticalEGLTitled("Settings", () =>
            {
                _isOnlyTransferInArmature = EditorUI.Toggle(_isOnlyTransferInArmature, "Only Transfer In Armature");
                EditorUI.Line(LineType.Horizontal);
                _isTransferMAMergeArmature = EditorUI.Toggle(_isTransferMAMergeArmature, "Transfer ModularAvatarMergeArmature");
                _isTransferMAMergeAnimator = EditorUI.Toggle(_isTransferMAMergeAnimator, "Transfer ModularAvatarMergeAnimator");
                _isTransferMABoneProxy = EditorUI.Toggle(_isTransferMABoneProxy, "Transfer ModularAvatarBoneProxy");
                _isTransferMaterials = EditorUI.Toggle(_isTransferMaterials, "Transfer Materials");
                _isTransferParticleSystems = EditorUI.Toggle(_isTransferParticleSystems, "Transfer Particle Systems");
                _isTransferConstraints = EditorUI.Toggle(_isTransferConstraints, "Transfer Constraints");
                _isTransferMaterialSwitcher = EditorUI.Toggle(_isTransferMaterialSwitcher, "Transfer MaterialSwitcher");

                if (GUILayout.Button("Transfer"))
                {
                    if (_origin == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Please select a source armature first.", "OK");
                        return;
                    }

                    EditorUtility.DisplayProgressBar("Transferring Components", "Processing...", 0);
                    try
                    {
                        foreach (var selection in selections)
                        {
                            Recursive(_origin, selection.transform);

                            if (_isTransferMaterials)
                                TransferMaterials(selection.transform);
                            if (_isTransferMaterialSwitcher)
                                TransferMaterialSwitcher(selection);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error during transfer: {e.Message}");
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                    EditorUtility.ClearProgressBar();
                }
            });

            EditorUI.VerticalEGLTitled("Selected Objects", () =>
            {
                if (isSelectedObject)
                    _pos = EditorUI.ScrollViewEGL(() =>
                    {
                        foreach (var selection in selections)
                            EditorGUILayout.ObjectField(selection, typeof(GameObject), true);
                    }, _pos, GUILayout.Height(200));
                else
                    EditorGUILayout.HelpBox("Please select target objects in the scene", MessageType.Error);
            });
        }

        private void TransferMaterials(Transform current)
        {
            var originRenderers = _origin.GetComponentsInChildren<Renderer>(true);

            if (originRenderers.Length <= 0) return;
            foreach (var originRenderer in originRenderers)
            {
                var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originRenderer.transform).Replace($"{_origin.name}", $"{current.name}");
                var targetGo = GameObject.Find(targetPath);
                if (!targetGo) continue;

                var targetRenderer = targetGo.GetComponent<Renderer>();
                if (targetRenderer == null)
                {
                    Debug.LogWarning($"Skipping material transfer for {targetGo.name}: No Renderer component found");
                    continue;
                }

                var materials = originRenderer.sharedMaterials;
                targetRenderer.sharedMaterials = materials;
                Undo.RegisterFullObjectHierarchyUndo(targetGo, "TransferMaterial");
            }
        }

        private void Recursive(Transform parent, Transform current)
        {
            foreach (Transform child in parent)
            {
                TransferPhysBone(child, current);
                if (_isTransferConstraints)
                    TransferConstraint(child, current);
                if (_isTransferParticleSystems)
                    TransferParticleSystems(child, current);
                TransferComponents(child, current);
                Recursive(child, current);
            }
        }

        private void CopyComponentByType<T>(GameObject targetGo, Transform child) where T : Component
        {
            var components = child.GetComponents<T>();
            foreach (var component in components)
            {
                if (component == null)
                    continue;

                ComponentUtility.CopyComponent(component);
                var targetComponent = targetGo.GetComponent<T>();
                if (targetComponent != null)
                {
                    ComponentUtility.PasteComponentValues(targetComponent);
                }
                else
                {
                    ComponentUtility.PasteComponentAsNew(targetGo);
                }
            }
        }

        private void TransferComponents(Transform child, Transform current)
        {
            var targetGo = GetOrCreateTargetObject(child, current);
            if (targetGo == null) return;

            if (_isTransferMABoneProxy)
                CopyComponentWithUndo<ModularAvatarBoneProxy>(child.gameObject, targetGo);
            if (_isTransferMAMergeArmature)
                CopyComponentWithUndo<ModularAvatarMergeArmature>(child.gameObject, targetGo);
            if (_isTransferMAMergeAnimator)
                CopyComponentWithUndo<ModularAvatarMergeAnimator>(child.gameObject, targetGo);
        }

        private void TransferConstraint(Transform child, Transform current)
        {
            var targetGo = GetOrCreateTargetObject(child, current);
            if (targetGo == null) return;

            CopyConstraintComponents<ParentConstraint>(child.gameObject, targetGo, current);
            CopyConstraintComponents<RotationConstraint>(child.gameObject, targetGo, current);
            CopyConstraintComponents<PositionConstraint>(child.gameObject, targetGo, current);
        }

        private void CopyConstraintComponents<T>(GameObject sourceObj, GameObject target, Transform current) where T : Component, IConstraint
        {
            var constraints = sourceObj.GetComponents<T>();
            foreach (var constraint in constraints)
            {
                var lastLocked = constraint.locked;
                var lastActive = constraint.constraintActive;

                constraint.locked = false;
                constraint.constraintActive = false;

                var targetConstraint = CopyComponentWithUndo<T>(sourceObj, target);
                if (targetConstraint == null) continue;

                // 恢复源约束的状态
                constraint.locked = lastLocked;
                constraint.constraintActive = lastActive;

                // 处理约束源
                for (var i = 0; i < constraint.sourceCount; i++)
                {
                    var source = constraint.GetSource(i);
                    var targetSourceObject = GetOrCreateTargetObject(source.sourceTransform, current);
                    if (targetSourceObject == null) continue;

                    targetConstraint.SetSource(i, new ConstraintSource()
                    {
                        sourceTransform = targetSourceObject.transform,
                        weight = source.weight
                    });
                }

                targetConstraint.constraintActive = constraint.constraintActive;
            }
        }

        private void TransferPhysBone(Transform child, Transform current)
        {
            var physBone = child.GetComponent<VRCPhysBone>();
            if (physBone == null) return;

            var targetGo = GetOrCreateTargetObject(child, current);
            if (targetGo == null) return;

            var targetPhysBone = CopyComponentWithUndo<VRCPhysBone>(child.gameObject, targetGo);
            if (targetPhysBone == null) return;

            // 处理 rootTransform
            if (physBone.rootTransform != null)
            {
                var rootTransGo = GetOrCreateTargetObject(physBone.rootTransform, current);
                targetPhysBone.rootTransform = rootTransGo?.transform;
            }

            targetPhysBone.colliders = GetColliders(targetPhysBone.colliders, current);
        }

        private List<VRCPhysBoneColliderBase> GetColliders(List<VRCPhysBoneColliderBase> colliders, Transform current)
        {
            List<VRCPhysBoneColliderBase> list = new List<VRCPhysBoneColliderBase>();
            foreach (var originCollider in colliders)
            {
                if (originCollider == null) continue;

                var targetGo = GetOrCreateTargetObject(originCollider.transform, current);
                if (targetGo != null)
                {
                    var colBase = CopyComponentWithUndo<VRCPhysBoneColliderBase>(originCollider.gameObject, targetGo);
                    if (colBase == null) continue;

                    if (originCollider.rootTransform != null)
                    {
                        var rootTransGo = GetOrCreateTargetObject(originCollider.rootTransform, current);
                        colBase.rootTransform = rootTransGo?.transform;
                    }

                    list.Add(colBase);
                }
            }
            return list;
        }

        private void TransferParticleSystems(Transform child, Transform current)
        {
            var targetGo = GetOrCreateTargetObject(child, current);
            if (targetGo == null) return;

            // 转移 ParticleSystem 组件
            var particleSystem = CopyComponentWithUndo<ParticleSystem>(child.gameObject, targetGo);
            if (particleSystem == null) return;

            // 转移 ParticleSystemRenderer 组件
            if (child.TryGetComponent<ParticleSystemRenderer>(out var renderer))
            {
                CopyComponentWithUndo<ParticleSystemRenderer>(child.gameObject, targetGo);
            }

            // 处理子粒子系统
            foreach (var childPS in child.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (childPS.transform == child.transform) continue;

                var childTargetGo = GetOrCreateTargetObject(childPS.transform, current);
                if (childTargetGo == null) continue;

                CopyComponentWithUndo<ParticleSystem>(childPS.gameObject, childTargetGo);
                if (childPS.TryGetComponent<ParticleSystemRenderer>(out var childRenderer))
                {
                    CopyComponentWithUndo<ParticleSystemRenderer>(childPS.gameObject, childTargetGo);
                }
            }
        }

        private void TransferMaterialSwitcher(GameObject targetRoot)
        {
            var sourceComponents = _origin.GetComponents<MaterialSwitcher>();
            if (sourceComponents == null || sourceComponents.Length == 0) return;

            // 获取目标对象上已有的MaterialSwitcher组件
            var existingComponents = targetRoot.GetComponents<MaterialSwitcher>();
            
            // 确保目标对象有足够的组件
            for (int i = existingComponents.Length; i < sourceComponents.Length; i++)
            {
                var newComponent = targetRoot.AddComponent<MaterialSwitcher>();
                Undo.RegisterCreatedObjectUndo(newComponent, "Create MaterialSwitcher");
            }

            // 重新获取目标组件（包括新创建的）
            var targetComponents = targetRoot.GetComponents<MaterialSwitcher>();

            // 复制每个组件的值
            for (int i = 0; i < sourceComponents.Length; i++)
            {
                ComponentUtility.CopyComponent(sourceComponents[i]);
                ComponentUtility.PasteComponentValues(targetComponents[i]);
                
                Undo.RegisterCompleteObjectUndo(targetComponents[i], "Transfer MaterialSwitcher");
                EditorUtility.SetDirty(targetComponents[i]);
            }
        }

        /// <summary>
        /// 根据路径创建或获取对象，确保路径上的所有对象都存在
        /// </summary>
        private GameObject EnsureGameObjectPath(string fullPath, Transform sourceRoot, Transform targetRoot)
        {
            var pathParts = fullPath.Split('/');
            if (pathParts.Length == 0) return null;

            // 优化：先尝试直接查找完整路径
            var directGo = GameObject.Find(fullPath);
            if (directGo != null) return directGo;

            Transform current = targetRoot;
            Transform sourceCurrent = sourceRoot;

            foreach (var partName in pathParts)
            {
                var child = current.Find(partName);
                if (child == null)
                {
                    var sourceChild = sourceCurrent?.Find(partName);
                    if (sourceChild == null) continue;

                    var newGo = new GameObject(partName);
                    newGo.transform.SetParent(current, false); // 使用SetParent更高效

                    // 一次性设置所有transform信息
                    var sourceLocalTRS = new TransformData(sourceChild);
                    sourceLocalTRS.ApplyTo(newGo.transform);

                    child = newGo.transform;
                    Undo.RegisterCreatedObjectUndo(newGo, $"Create Path Object: {partName}");
                    YuebyLogger.LogInfo("PhysBoneTransfer", $"Created object in path: {VRC.Core.ExtensionMethods.GetHierarchyPath(newGo.transform)}");
                }

                current = child;
                sourceCurrent = sourceCurrent?.Find(partName);
            }

            return current?.gameObject;
        }

        // 辅助类来处理Transform数据
        private struct TransformData
        {
            private Vector3 localPosition;
            private Quaternion localRotation;
            private Vector3 localScale;

            public TransformData(Transform transform)
            {
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public void ApplyTo(Transform transform)
            {
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }

        /// <summary>
        /// 统一的组件转移方法
        /// </summary>
        private GameObject GetOrCreateTargetObject(Transform sourceObj, Transform targetRoot, bool createIfNotExist = true)
        {
            var targetPath = GetTargetPath(sourceObj, targetRoot);

            if (createIfNotExist && !_isOnlyTransferInArmature)
            {
                return EnsureGameObjectPath(targetPath, _origin, targetRoot);
            }

            return GameObject.Find(targetPath);
        }

        /// <summary>
        /// 统一的组件复制方法
        /// </summary>
        private T CopyComponentWithUndo<T>(GameObject source, GameObject target, string undoName = null) where T : Component
        {
            var sourceComponent = source.GetComponent<T>();
            if (sourceComponent == null)
            {
                // YuebyLogger.LogWarning("PhysBoneTransfer", $"Source component {typeof(T).Name} not found on {source.name}");
                return null;
            }

            try
            {
                ComponentUtility.CopyComponent(sourceComponent);
                var targetComponent = target.GetComponent<T>();

                if (targetComponent != null)
                    ComponentUtility.PasteComponentValues(targetComponent);
                else
                {
                    ComponentUtility.PasteComponentAsNew(target);
                    targetComponent = target.GetComponent<T>();
                }

                Undo.RegisterCompleteObjectUndo(target, undoName ?? $"Transfer {typeof(T).Name}");
                return targetComponent;
            }
            catch (System.Exception e)
            {
                YuebyLogger.LogError("PhysBoneTransfer", $"Failed to copy {typeof(T).Name}: {e.Message}");
                return null;
            }
        }

        // 1. 统一路径处理
        private string GetTargetPath(Transform source, Transform targetRoot)
        {
            return VRC.Core.ExtensionMethods.GetHierarchyPath(source).Replace($"{_origin.name}", $"{targetRoot.name}");
        }
    }
}