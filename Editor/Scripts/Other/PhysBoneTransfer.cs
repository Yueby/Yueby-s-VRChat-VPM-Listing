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
using Yueby.Utils;

namespace YuebyAvatarTools.PhysBoneTransfer.Editor
{
    public class PhysboneTransfer : EditorWindow
    {
        private Vector2 _pos;
        private Transform _origin;
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
            window.minSize = new Vector2(500, 450);
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
                _isTransferMAMergeArmature = EditorUI.Toggle(_isTransferMAMergeArmature, "Transfer ModularAvatarMergeArmature");
                _isTransferMAMergeAnimator = EditorUI.Toggle(_isTransferMAMergeAnimator, "Transfer ModularAvatarMergeAnimator");
                _isTransferMABoneProxy = EditorUI.Toggle(_isTransferMABoneProxy, "Transfer ModularAvatarBoneProxy");
                _isTransferMaterials = EditorUI.Toggle(_isTransferMaterials, "Transfer Materials");
                _isTransferParticleSystems = EditorUI.Toggle(_isTransferParticleSystems, "Transfer Particle Systems");
                _isTransferConstraints = EditorUI.Toggle(_isTransferConstraints, "Transfer Constraints");
                _isTransferMaterialSwitcher = EditorUI.Toggle(_isTransferMaterialSwitcher, "Transfer MaterialSwitcher");

                if (GUILayout.Button("Transfer"))
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
            var originRenderers = _origin.GetComponentsInChildren<Renderer>();

            if (originRenderers.Length <= 0) return;
            foreach (var originRenderer in originRenderers)
            {
                var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originRenderer.transform).Replace($"{_origin.name}", $"{current.name}");
                var targetGo = GameObject.Find(targetPath);
                if (!targetGo) continue;
                var targetRenderer = targetGo.GetComponent<Renderer>();

                var materials = originRenderer.sharedMaterials;

                targetRenderer.sharedMaterials = materials;
                Undo.RegisterFullObjectHierarchyUndo(targetGo, "TransferMaterial");
                // Debug.Log("...Transfer Materials");
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
                // Debug.Log(child);
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
            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(child).Replace($"{_origin.name}", $"{current.name}");
            var targetGo = GameObject.Find(targetPath);
            if (targetGo == null) return;

            if (_isTransferMABoneProxy)
                CopyComponentByType<ModularAvatarBoneProxy>(targetGo, child);
            if (_isTransferMAMergeArmature)
                CopyComponentByType<ModularAvatarMergeArmature>(targetGo, child);
            if (_isTransferMAMergeAnimator)
                CopyComponentByType<ModularAvatarMergeAnimator>(targetGo, child);
        }

        private void TransferConstraint(Transform child, Transform current)
        {
            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(child).Replace($"{_origin.name}", $"{current.name}");
            var targetGo = GameObject.Find(targetPath);
            if (targetGo == null) return;

            var parentConstraints = child.GetComponents<ParentConstraint>();
            var rotationConstraints = child.GetComponents<RotationConstraint>();
            var positionConstraints = child.GetComponents<PositionConstraint>();
            CopyConstraint(parentConstraints, (c, t) =>
            {
                // t.rotationAtRest = c.rotationAtRest;
                // t.translationAtRest = c.translationAtRest;

                // t.rotationOffsets = c.rotationOffsets;
                // t.translationOffsets = c.translationOffsets;
                // t.weight = c.weight;

            });

            CopyConstraint(rotationConstraints, (c, t) =>
            {
                // t.rotationAtRest = c.rotationAtRest;
                // t.rotationOffset = c.rotationOffset;
                // t.weight = c.weight;

            });

            CopyConstraint(positionConstraints, (c, t) =>
            {
                // t.translationAtRest = c.translationAtRest;
                // t.translationOffset = c.translationOffset;
                // t.weight = c.weight;

            });

            return;

            void CopyConstraint<T>(T[] constraints, System.Action<T, T> action) where T : Component
            {
                foreach (var constraint in constraints)
                {
                    if (constraint == null)
                        continue;

                    var constraintCurrent = constraint as IConstraint;

                    var lastLocked = constraintCurrent.locked;
                    var lastActive = constraintCurrent.constraintActive;

                    constraintCurrent.locked = false;
                    constraintCurrent.constraintActive = false;

                    ComponentUtility.CopyComponent(constraint);
                    var targetConstraint = targetGo.GetComponent<T>();
                    if (targetConstraint != null)
                    {
                        ComponentUtility.PasteComponentValues(targetConstraint);
                    }
                    else
                    {
                        ComponentUtility.PasteComponentAsNew(targetGo);
                        targetConstraint = targetGo.GetComponent<T>();
                    }

                    constraintCurrent.locked = lastLocked;
                    constraintCurrent.constraintActive = lastActive;
                    action?.Invoke(constraint, targetConstraint);
                    ApplySource(constraint as IConstraint, targetConstraint as IConstraint);
                }
            }

            void ApplySource(IConstraint currentConstraint, IConstraint targetConstarint)
            {
                if (currentConstraint.sourceCount <= 0) return;

                //clear target source
                for (var i = 0; i < currentConstraint.sourceCount; i++)
                {
                    var source = currentConstraint.GetSource(i);
                    var targetSourceObject = GameObject.Find(VRC.Core.ExtensionMethods.GetHierarchyPath(source.sourceTransform).Replace($"{_origin.name}", $"{current.name}"));
                    if (targetSourceObject == null) continue;

                    // Debug.Log(targetSourceObject.name);

                    // target.RemoveSource(i);
                    targetConstarint.SetSource(i, new ConstraintSource()
                    {
                        sourceTransform = targetSourceObject.transform,
                        weight = source.weight
                    });
                }

                targetConstarint.constraintActive = currentConstraint.constraintActive;
                // target.locked = current.locked;
            }
        }

        private void TransferPhysBone(Transform child, Transform current)
        {
            var physBone = child.GetComponent<VRCPhysBone>();
            if (physBone == null) return;

            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(child).Replace($"{_origin.name}", $"{current.name}");
            var targetGo = GameObject.Find(targetPath);

            if (targetGo != null)
            {
                ComponentUtility.CopyComponent(physBone);

                if (targetGo.GetComponent<VRCPhysBone>() != null)
                    ComponentUtility.PasteComponentValues(targetGo.GetComponent<VRCPhysBone>());
                else
                    ComponentUtility.PasteComponentAsNew(targetGo);

                var targetPhysBone = targetGo.GetComponent<VRCPhysBone>();

                // 只有当原始的rootTransform不为空时才进行映射
                if (physBone.rootTransform != null)
                {
                    var rootTransPath = VRC.Core.ExtensionMethods.GetHierarchyPath(physBone.rootTransform).Replace($"{_origin.name}", $"{current.name}");
                    var rootTransGo = GameObject.Find(rootTransPath);
                    targetPhysBone.rootTransform = rootTransGo != null ? rootTransGo.transform : null;
                }

                targetPhysBone.colliders = GetColliders(targetPhysBone.colliders, current);
                Undo.RegisterFullObjectHierarchyUndo(targetGo, "CopyComponent");
            }
        }

        private List<VRCPhysBoneColliderBase> GetColliders(List<VRCPhysBoneColliderBase> colliders, Transform current)
        {
            List<VRCPhysBoneColliderBase> list = new List<VRCPhysBoneColliderBase>();
            foreach (var originCollider in colliders)
            {
                if (originCollider == null) continue;

                var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originCollider.transform).Replace($"{_origin.name}", $"{current.name}");
                var targetGo = GameObject.Find(targetPath);

                if (targetGo != null)
                {
                    var colBase = targetGo.GetComponent<VRCPhysBoneColliderBase>();
                    if (colBase == null)
                    {
                        ComponentUtility.CopyComponent(originCollider);
                        ComponentUtility.PasteComponentAsNew(targetGo);
                        colBase = targetGo.GetComponent<VRCPhysBoneColliderBase>();
                    }
                    else
                    {
                        ComponentUtility.CopyComponent(originCollider);
                        ComponentUtility.PasteComponentValues(colBase);
                    }

                    // 只有当原始的rootTransform不为空时才进行映射
                    if (originCollider.rootTransform != null)
                    {
                        var rootTransPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originCollider.rootTransform).Replace($"{_origin.name}", $"{current.name}");
                        var rootTransGo = GameObject.Find(rootTransPath);
                        colBase.rootTransform = rootTransGo != null ? rootTransGo.transform : null;
                    }

                    list.Add(colBase);
                    Undo.RegisterFullObjectHierarchyUndo(targetGo, "CopyComponent");
                }
                else
                {
                    var parentPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originCollider.transform.parent).Replace($"{_origin.name}", $"{current.name}");
                    var parent = GameObject.Find(parentPath);

                    if (parent != null)
                    {
                        var newGo = new GameObject(originCollider.name);
                        newGo.transform.parent = parent.transform;
                        newGo.transform.localPosition = originCollider.transform.localPosition;
                        newGo.transform.localRotation = originCollider.transform.localRotation;
                        newGo.transform.localScale = originCollider.transform.localScale;

                        ComponentUtility.CopyComponent(originCollider);
                        ComponentUtility.PasteComponentAsNew(newGo);
                        var colBase = newGo.GetComponent<VRCPhysBoneColliderBase>();

                        // 只有当原始的rootTransform不为空时才进行映射
                        if (originCollider.rootTransform != null)
                        {
                            var rootTransPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originCollider.rootTransform).Replace($"{_origin.name}", $"{current.name}");
                            var rootTransGo = GameObject.Find(rootTransPath);
                            colBase.rootTransform = rootTransGo != null ? rootTransGo.transform : null;
                        }

                        list.Add(colBase);
                        Undo.RegisterCreatedObjectUndo(newGo, "Create Collider");
                    }
                }
            }

            return list;
        }

        private void TransferParticleSystems(Transform child, Transform current)
        {
            var particleSystem = child.GetComponent<ParticleSystem>();
            if (particleSystem == null) return;

            // 先尝试找到目标对象
            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(child).Replace($"{_origin.name}", $"{current.name}");
            var targetGo = GameObject.Find(targetPath);
            if (targetGo == null)
            {
                // 如果找不到，尝试找父对象
                var parentPath = VRC.Core.ExtensionMethods.GetHierarchyPath(child.parent).Replace($"{_origin.name}", $"{current.name}");
                var parentGo = GameObject.Find(parentPath);
                if (parentGo != null)
                {
                    // 在父对象下创建新的粒子系统对象
                    targetGo = new GameObject(child.name);
                    targetGo.transform.parent = parentGo.transform;
                    targetGo.transform.localPosition = child.localPosition;
                    targetGo.transform.localRotation = child.localRotation;
                    targetGo.transform.localScale = child.localScale;
                }
            }
            if (targetGo == null) return;

            // 转移组件
            ComponentUtility.CopyComponent(particleSystem);
            if (targetGo.GetComponent<ParticleSystem>() != null)
                ComponentUtility.PasteComponentValues(targetGo.GetComponent<ParticleSystem>());
            else
                ComponentUtility.PasteComponentAsNew(targetGo);

            var renderer = child.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                ComponentUtility.CopyComponent(renderer);
                if (targetGo.GetComponent<ParticleSystemRenderer>() != null)
                    ComponentUtility.PasteComponentValues(targetGo.GetComponent<ParticleSystemRenderer>());
                else
                    ComponentUtility.PasteComponentAsNew(targetGo);
            }

            // 处理子粒子系统
            var childParticleSystems = child.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var childPS in childParticleSystems)
            {
                if (childPS.transform == child.transform) continue;

                // 先尝试找到目标子对象
                var childTargetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(childPS.transform).Replace($"{_origin.name}", $"{current.name}");
                var childTargetGo = GameObject.Find(childTargetPath);
                if (childTargetGo == null)
                {
                    // 如果找不到，在父对象下创建
                    var childParentPath = VRC.Core.ExtensionMethods.GetHierarchyPath(childPS.transform.parent).Replace($"{_origin.name}", $"{current.name}");
                    var childParentGo = GameObject.Find(childParentPath);
                    if (childParentGo != null)
                    {
                        childTargetGo = new GameObject(childPS.name);
                        childTargetGo.transform.parent = childParentGo.transform;
                        childTargetGo.transform.localPosition = childPS.transform.localPosition;
                        childTargetGo.transform.localRotation = childPS.transform.localRotation;
                        childTargetGo.transform.localScale = childPS.transform.localScale;
                    }
                }
                if (childTargetGo == null) continue;

                ComponentUtility.CopyComponent(childPS);
                if (childTargetGo.GetComponent<ParticleSystem>() != null)
                    ComponentUtility.PasteComponentValues(childTargetGo.GetComponent<ParticleSystem>());
                else
                    ComponentUtility.PasteComponentAsNew(childTargetGo);

                var childRenderer = childPS.GetComponent<ParticleSystemRenderer>();
                if (childRenderer != null)
                {
                    ComponentUtility.CopyComponent(childRenderer);
                    if (childTargetGo.GetComponent<ParticleSystemRenderer>() != null)
                        ComponentUtility.PasteComponentValues(childTargetGo.GetComponent<ParticleSystemRenderer>());
                    else
                        ComponentUtility.PasteComponentAsNew(childTargetGo);
                }
            }

            Undo.RegisterFullObjectHierarchyUndo(targetGo, "Transfer ParticleSystem");
        }

        private void TransferMaterialSwitcher(GameObject targetRoot)
        {
            var sourceComponent = _origin.GetComponent<MaterialSwitcher>();
            if (sourceComponent == null) return;

            var targetComponent = targetRoot.GetComponent<MaterialSwitcher>();
            if (targetComponent == null)
            {
                targetComponent = targetRoot.AddComponent<MaterialSwitcher>();
                Undo.RegisterCreatedObjectUndo(targetComponent, "Create MaterialSwitcher");
            }

            // 使用ComponentUtility复制组件值
            ComponentUtility.CopyComponent(sourceComponent);
            ComponentUtility.PasteComponentValues(targetComponent);

            Undo.RegisterCompleteObjectUndo(targetComponent, "Transfer MaterialSwitcher");
            EditorUtility.SetDirty(targetComponent);
        }
    }
}