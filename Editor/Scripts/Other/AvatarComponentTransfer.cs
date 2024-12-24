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
using System.Linq;

namespace Yueby.AvatarTools.Other
{
    public class AvatarComponentTransfer : EditorWindow
    {
        private Vector2 _pos;
        private static Transform _origin;
        private static bool _isTransferMAMergeArmature = true;
        private static bool _isTransferMAMergeAnimator = true;
        private static bool _isTransferMABoneProxy = true;
        private static bool _isTransferMaterials = true;
        private static bool _isTransferParticleSystems = true;
        private static bool _isTransferMaterialSwitcher = true;

        private Dictionary<string, GameObject> _pathCache = new Dictionary<string, GameObject>();

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/Component Transfer", false, 21)]
        public static void OpenWindow()
        {
            var window = CreateWindow<AvatarComponentTransfer>();
            window.titleContent = new GUIContent("Avatar组件转移");
            window.minSize = new Vector2(500, 450);
            window.Show();
        }

        private void OnGUI()
        {
            var selections = Selection.gameObjects;
            var isSelectedObject = selections.Length > 0;

            EditorUI.DrawEditorTitle("Avatar组件转移");
            EditorUI.VerticalEGLTitled("配置", () =>
            {
                EditorUI.HorizontalEGL(() =>
                {
                    _origin = (Transform)EditorUI.ObjectFieldVertical(_origin, "原Armature", typeof(Transform));
                    EditorUI.Line(LineType.Vertical);
                    // _current = (Transform)EditorUI.ObjectFieldVertical(_current, "现Armature", typeof(Transform));
                }, GUILayout.MaxHeight(40));
            });

            EditorUI.VerticalEGLTitled("设置", () =>
            {
                _isTransferMAMergeArmature = EditorUI.Toggle(_isTransferMAMergeArmature, "转移ModularAvatarMergeArmature组件");
                _isTransferMAMergeAnimator = EditorUI.Toggle(_isTransferMAMergeAnimator, "转移ModularAvatarMergeAnimator组件");
                _isTransferMABoneProxy = EditorUI.Toggle(_isTransferMABoneProxy, "转移ModularAvatarBoneProxy组件");
                _isTransferMaterials = EditorUI.Toggle(_isTransferMaterials, "转移材质");
                _isTransferParticleSystems = EditorUI.Toggle(_isTransferParticleSystems, "转移粒子特效");
                _isTransferMaterialSwitcher = EditorUI.Toggle(_isTransferMaterialSwitcher, "转移MaterialSwitcher组件");

                if (GUILayout.Button("转移"))
                {
                    Execute();
                }
            });
            EditorUI.VerticalEGLTitled("选中列表", () =>
            {
                if (isSelectedObject)
                    _pos = EditorUI.ScrollViewEGL(() =>
                    {
                        foreach (var selection in selections)
                            EditorGUILayout.ObjectField(selection, typeof(GameObject), true);
                    }, _pos, GUILayout.Height(200));
                else
                    EditorGUILayout.HelpBox("请在场景中选中需要删除的对象", MessageType.Error);
            });
        }

        private void Execute()
        {
            try
            {
                _pathCache.Clear();
                if (!ValidateSetup())
                    return;

                // 创建撤销组
                Undo.IncrementCurrentGroup();
                var undoGroupIndex = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Transfer Avatar Components");

                var stats = new TransferStats();
                try
                {
                    EditorUtility.DisplayProgressBar("转移组件", "准备中...", 0f);

                    for (int i = 0; i < Selection.gameObjects.Length; i++)
                    {
                        var selection = Selection.gameObjects[i];
                        float progress = (float)i / Selection.gameObjects.Length;
                        EditorUtility.DisplayProgressBar("转移组件", $"正在处理: {selection.name}", progress);

                        Recursive(_origin, selection.transform, stats);

                        if (_isTransferMaterials)
                            TransferMaterials(selection.transform, stats);
                        if (_isTransferMaterialSwitcher)
                            TransferMaterialSwitcher(selection, stats);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                // 关闭撤销组
                Undo.CollapseUndoOperations(undoGroupIndex);

                // 显示结果
                EditorUtility.DisplayDialog("转移完成", $"转移结果统计:\n{stats}", "确定");

                // stats.LogDetailedStats();
            }
            catch (Exception e)
            {
                _pathCache.Clear();
                Debug.LogError($"转移过程中发生错误: {e}");
                EditorUtility.DisplayDialog("错误", $"转移过程中发生错误:\n{e.Message}", "确定");
            }
        }

        private void OnDestroy()
        {
            _pathCache.Clear();
        }

        private bool ValidateSetup()
        {
            var selections = Selection.gameObjects;
            if (selections == null || selections.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择目标对象", "确定");
                return false;
            }

            if (_origin == null)
            {
                EditorUtility.DisplayDialog("错误", "请先设置原始Armature", "确定");
                return false;
            }

            var originArmature = FindArmatureTransform(_origin);
            if (originArmature == null)
            {
                EditorUtility.DisplayDialog("错误", $"在源对象 {_origin.name} 中找不到Armature", "确定");
                return false;
            }

            foreach (var selection in selections)
            {
                var targetArmature = FindArmatureTransform(selection.transform);
                if (targetArmature == null)
                {
                    EditorUtility.DisplayDialog("错误", $"在目标对象 {selection.name} 中找不到Armature", "确定");
                    return false;
                }
            }

            return true;
        }

        private void TransferMaterials(Transform current, TransferStats stats)
        {
            var originRenderers = _origin.GetComponentsInChildren<Renderer>();
            var targetRenderers = current.GetComponentsInChildren<Renderer>();

            if (originRenderers.Length <= 0 || targetRenderers.Length <= 0) return;

            // 遍历源对象的所有Renderer
            foreach (var originRenderer in originRenderers)
            {
                // 在目标对象中查找同名的Renderer
                var targetRenderer = targetRenderers.FirstOrDefault(r => r.name == originRenderer.name);
                if (targetRenderer == null) continue;

                targetRenderer.sharedMaterials = originRenderer.sharedMaterials;
                Undo.RegisterFullObjectHierarchyUndo(targetRenderer.gameObject, "TransferMaterial");

                stats.MaterialsTransferred++;
            }
        }

        private void Recursive(Transform parent, Transform current, TransferStats stats)
        {
            foreach (Transform child in parent)
            {
                TransferPhysBone(child, current, stats);
                TransferConstraint(child, current, stats);
                TransferComponents(child, current, stats);
                if (_isTransferParticleSystems)
                    TransferParticleSystems(child, current, stats);
                Recursive(child, current, stats);
                // Debug.Log(child);
            }
        }

        private void CopyComponentByType<T>(GameObject sourceGo, Transform current, TransferStats stats) where T : Component
        {
            var components = sourceGo.GetComponents<T>();
            foreach (var component in components)
            {
                if (component == null) continue;

                var targetGo = FindTargetObject(component.transform, current);
                if (targetGo == null)
                {
                    targetGo = CreateTargetObject(component.transform, current.transform);
                }
                if (targetGo == null) continue;

                TransferComponent(targetGo, component, current);
                stats.MAComponentsTransferred++;
            }
        }

        private void TransferComponents(Transform child, Transform current, TransferStats stats)
        {
            if (_isTransferMABoneProxy)
                CopyComponentByType<ModularAvatarBoneProxy>(child.gameObject, current, stats);
            if (_isTransferMAMergeArmature)
                CopyComponentByType<ModularAvatarMergeArmature>(child.gameObject, current, stats);
            if (_isTransferMAMergeAnimator)
                CopyComponentByType<ModularAvatarMergeAnimator>(child.gameObject, current, stats);
        }

        private void TransferConstraint(Transform child, Transform current, TransferStats stats)
        {
            var constraints = child.GetComponents<IConstraint>();
            foreach (var constraint in constraints)
            {
                var targetGo = FindTargetObject(child, current);
                if (targetGo == null)
                {
                    targetGo = CreateTargetObject(child, current.transform);
                }
                if (targetGo == null) continue;

                var target = targetGo.GetComponent(constraint.GetType()) as IConstraint;
                if (target == null)
                {
                    target = targetGo.AddComponent(constraint.GetType()) as IConstraint;
                }

                // 转移约束源
                for (int i = 0; i < constraint.sourceCount; i++)
                {
                    var source = constraint.GetSource(i);
                    if (source.sourceTransform == null) continue;

                    var targetSourceObject = FindTargetObject(source.sourceTransform, current);
                    if (targetSourceObject == null) continue;

                    target.SetSource(i, new ConstraintSource()
                    {
                        sourceTransform = targetSourceObject.transform,
                        weight = source.weight
                    });
                }

                target.constraintActive = constraint.constraintActive;
                stats.ConstraintsTransferred++;
            }
        }

        private void TransferPhysBone(Transform child, Transform current, TransferStats stats)
        {
            var physBone = child.GetComponent<VRCPhysBone>();
            if (physBone == null) return;

            var targetGo = FindTargetObject(child, current);
            if (targetGo == null)
            {
                targetGo = CreateTargetObject(child, current.transform);
            }
            if (targetGo == null) return;

            var targetPhysBone = TransferComponent(targetGo, physBone, current);
            if (targetPhysBone == null) return;

            targetPhysBone.colliders = GetColliders(targetPhysBone.colliders, current);
            Undo.RegisterFullObjectHierarchyUndo(targetGo, "CopyComponent");

            if (targetPhysBone != null)
                stats.PhysBonesTransferred++;
        }

        private Transform FindTransformByName(Transform root, string targetName)
        {
            if (root.name == targetName)
                return root;

            foreach (Transform child in root)
            {
                var result = FindTransformByName(child, targetName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private List<VRCPhysBoneColliderBase> GetColliders(List<VRCPhysBoneColliderBase> colliders, Transform current)
        {
            List<VRCPhysBoneColliderBase> list = new List<VRCPhysBoneColliderBase>();
            foreach (var originCollider in colliders)
            {
                if (originCollider == null) continue;

                // 先尝试找到目标对象
                var existingCollider = FindTargetObject(originCollider.transform, current);
                if (existingCollider != null)
                {
                    var colliderComponent = TransferComponent(existingCollider, originCollider, current);
                    if (colliderComponent != null)
                    {
                        list.Add(colliderComponent);
                    }
                    continue;
                }

                // 如果找不到，确定父对象
                var colRootTransGo = FindTargetObject(originCollider.transform.parent, current);
                if (colRootTransGo == null) continue;

                // 创建新的碰撞器对象
                var colliderGo = CreateTargetObject(originCollider.transform, colRootTransGo.transform);
                var colBase = TransferComponent(colliderGo, originCollider, current);
                if (colBase != null)
                {
                    list.Add(colBase);
                }
            }

            return list;
        }

        private void TransferParticleSystems(Transform child, Transform current, TransferStats stats)
        {
            var particleSystem = child.GetComponent<ParticleSystem>();
            if (particleSystem == null) return;

            // 先尝试找到目标对象
            var targetGo = FindTargetObject(child, current);
            if (targetGo == null)
            {
                // 如果找不到，确定父对象
                var parentGo = FindTargetObject(child.parent, current);
                if (parentGo != null)
                {
                    // 在父对象下创建新的粒子系统对象
                    targetGo = CreateTargetObject(child, parentGo.transform);
                }
            }

            if (targetGo == null) return;

            // 转移组件
            TransferComponent(targetGo, particleSystem, current);
            TransferComponent(targetGo, child.GetComponent<ParticleSystemRenderer>(), current);

            // 处理子粒子系统
            var childParticleSystems = child.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var childPS in childParticleSystems)
            {
                if (childPS.transform == child.transform) continue;

                // 先尝试找到目标子对象
                var childTargetGo = FindTargetObject(childPS.transform, current);
                if (childTargetGo == null)
                {
                    // 如果找不到，在父对象下创建
                    childTargetGo = CreateTargetObject(childPS.transform, targetGo.transform);
                }
                if (childTargetGo == null) continue;

                TransferComponent(childTargetGo, childPS, current);
                TransferComponent(childTargetGo, childPS.GetComponent<ParticleSystemRenderer>(), current);
            }

            Undo.RegisterFullObjectHierarchyUndo(targetGo, "Transfer ParticleSystem");
            stats.ParticleSystemsTransferred++;
        }

        private Transform FindArmatureTransform(Transform root)
        {
            // 先检查当前对象名称
            if (root.name.Contains("Armature", StringComparison.OrdinalIgnoreCase))
                return root;

            // 递归查找子对象
            foreach (Transform child in root)
            {
                if (child.name.Contains("Armature", StringComparison.OrdinalIgnoreCase))
                    return child;

                var result = FindArmatureTransform(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 转移组件到标对象
        /// </summary>
        private T TransferComponent<T>(GameObject targetGo, T sourceComponent, Transform current) where T : Component
        {
            if (targetGo == null || sourceComponent == null) return null;

            // 记录原始的rootTransform
            Transform originalRootTransform = null;
            bool shouldResetRootTransform = false;

            if (sourceComponent is VRCPhysBoneColliderBase collider)
            {
                originalRootTransform = collider.rootTransform;
                shouldResetRootTransform = collider.rootTransform == null || 
                                         collider.rootTransform == collider.transform;
            }
            else if (sourceComponent is VRCPhysBone physBone)
            {
                originalRootTransform = physBone.rootTransform;
                shouldResetRootTransform = physBone.rootTransform == null || 
                                         physBone.rootTransform == physBone.transform;
            }

            ComponentUtility.CopyComponent(sourceComponent);
            var targetComponent = targetGo.GetComponent<T>();

            if (targetComponent != null)
                ComponentUtility.PasteComponentValues(targetComponent);
            else
                ComponentUtility.PasteComponentAsNew(targetGo);

            var result = targetGo.GetComponent<T>();

            // 处理rootTransform
            if (shouldResetRootTransform)
            {
                // 如果原来是null或指向自身，设为null
                if (result is VRCPhysBoneColliderBase resultCollider)
                    resultCollider.rootTransform = null;
                else if (result is VRCPhysBone resultPhysBone)
                    resultPhysBone.rootTransform = null;
            }
            else if (originalRootTransform != null)
            {
                // 使用FindTargetObject找到对应的目标对象
                var mappedRootTransform = FindTargetObject(originalRootTransform, current);
                if (mappedRootTransform != null)
                {
                    if (result is VRCPhysBoneColliderBase resultCollider)
                        resultCollider.rootTransform = mappedRootTransform.transform;
                    else if (result is VRCPhysBone resultPhysBone)
                        resultPhysBone.rootTransform = mappedRootTransform.transform;
                }
            }

            return result;
        }

        /// <summary>
        /// 在目标层级中查找对应对象
        /// </summary>
        private GameObject FindTargetObject(Transform source, Transform current)
        {
            if (source == null || current == null)
            {
                Debug.LogWarning("Source or current transform is null");
                return null;
            }

            // 获取源Armature和目标Armature
            var originArmature = FindArmatureTransform(_origin);
            var targetArmature = FindArmatureTransform(current);
            
            if (originArmature == null || targetArmature == null)
                return null;

            // 获取源对象相对于Armature的路径
            var originPath = VRC.Core.ExtensionMethods.GetHierarchyPath(source);
            var originArmaturePath = VRC.Core.ExtensionMethods.GetHierarchyPath(originArmature);

            // 确保路径是相对于Armature的
            if (!originPath.StartsWith(originArmaturePath))
                return null;

            // 只获取从Armature开始的相对路径
            var relativePath = originPath.Substring(originArmaturePath.Length).TrimStart('/');
            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(targetArmature) + "/" + relativePath;

            // 使用缓存
            if (_pathCache.TryGetValue(targetPath, out var cachedGo))
            {
                if (cachedGo != null)
                    return cachedGo;
                _pathCache.Remove(targetPath);
            }

            // 使用路径查找目标对象
            var targetGo = GameObject.Find(targetPath);
            if (targetGo != null)
            {
                _pathCache[targetPath] = targetGo;
                return targetGo;
            }

            // 如果通过路径找不到，尝试通过名字在目标层级中查找
            var foundByName = FindTransformByName(current, source.name);
            if (foundByName != null)
            {
                _pathCache[targetPath] = foundByName.gameObject;
                return foundByName.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 在指定父对象下创建新对象
        /// </summary>
        private GameObject CreateTargetObject(Transform source, Transform parent)
        {
            if (source == null || parent == null)
                return null;

            var newGo = new GameObject(source.name);
            newGo.transform.parent = parent;
            newGo.transform.localPosition = source.localPosition;
            newGo.transform.localRotation = source.localRotation;
            newGo.transform.localScale = source.localScale;
            Undo.RegisterCreatedObjectUndo(newGo, "Create Target Object");

            return newGo;
        }

        // 添加统计类
        private class TransferStats
        {
            public int PhysBonesTransferred;
            public int ConstraintsTransferred;
            public int MaterialsTransferred;
            public int ParticleSystemsTransferred;
            public int MAComponentsTransferred;
            public int MaterialSwitchersTransferred;

            public override string ToString()
            {
                var stats = new List<string>();
                if (PhysBonesTransferred > 0) stats.Add($"物理骨骼: {PhysBonesTransferred}");
                if (ConstraintsTransferred > 0) stats.Add($"约束组件: {ConstraintsTransferred}");
                if (MaterialsTransferred > 0) stats.Add($"材质: {MaterialsTransferred}");
                if (ParticleSystemsTransferred > 0) stats.Add($"粒子系统: {ParticleSystemsTransferred}");
                if (MAComponentsTransferred > 0) stats.Add($"ModularAvatar组件: {MAComponentsTransferred}");
                if (MaterialSwitchersTransferred > 0) stats.Add($"MaterialSwitcher: {MaterialSwitchersTransferred}");

                return string.Join("\n", stats);
            }

            public void LogDetailedStats()
            {
                Debug.Log("=== 转移结果详细统计 ===");
                if (PhysBonesTransferred > 0) Debug.Log($"- 物理骨骼: {PhysBonesTransferred}");
                if (ConstraintsTransferred > 0) Debug.Log($"- 约束组件: {ConstraintsTransferred}");
                if (MaterialsTransferred > 0) Debug.Log($"- 材质: {MaterialsTransferred}");
                if (ParticleSystemsTransferred > 0) Debug.Log($"- 粒子系统: {ParticleSystemsTransferred}");
                if (MAComponentsTransferred > 0) Debug.Log($"- ModularAvatar组件: {MAComponentsTransferred}");
                if (MaterialSwitchersTransferred > 0) Debug.Log($"- MaterialSwitcher: {MaterialSwitchersTransferred}");
                Debug.Log("=====================");
            }
        }

        private void TransferMaterialSwitcher(GameObject targetRoot, TransferStats stats)
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
            stats.MaterialSwitchersTransferred++;
        }
    }
}