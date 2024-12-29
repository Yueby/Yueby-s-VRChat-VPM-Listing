using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Yueby.Utils;

namespace Yueby.Tools.Avatar
{
    public class BlendShapeAnimationPreview : EditorWindow
    {
        private SkinnedMeshRenderer _targetRenderer;
        private List<AnimationClip> _animationClips = new List<AnimationClip>();
        private ReorderableListDroppable _clipList;
        private AnimationClip _currentPreviewClip;
        private Dictionary<int, float> _originalBlendShapeValues;

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/BlendShape Animation Preview", priority = 20)]
        private static void ShowWindow()
        {
            var window = GetWindow<BlendShapeAnimationPreview>("BlendShape预览工具");
            window.Show();
        }

        private void OnEnable()
        {
            InitializeList();
            _originalBlendShapeValues = new Dictionary<int, float>();
        }

        private void InitializeList()
        {
            _clipList = new ReorderableListDroppable(_animationClips, typeof(AnimationClip), EditorGUIUtility.singleLineHeight,
                Repaint)
            {
                OnDraw = (rect, index, active, focused) =>
                {
                    var clip = _animationClips[index];
                    var elementRect = rect;
                    elementRect.width -= 65; // 为按钮预留空间

                    EditorGUI.BeginChangeCheck();
                    clip = (AnimationClip)EditorGUI.ObjectField(elementRect, clip, typeof(AnimationClip), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _animationClips[index] = clip;
                    }

                    if (clip != null)
                    {
                        var buttonRect = new Rect(elementRect.x + elementRect.width + 5, elementRect.y, 60, elementRect.height);

                        if (_currentPreviewClip == clip)
                        {
                            if (GUI.Button(buttonRect, "停止"))
                            {
                                StopPreview();
                            }
                        }
                        else
                        {
                            if (GUI.Button(buttonRect, "预览"))
                            {
                                PreviewClip(clip);
                            }
                        }
                    }

                    return EditorGUIUtility.singleLineHeight;
                },
                OnRemoveBefore = index =>
                {
                    if (_currentPreviewClip == _animationClips[index])
                    {
                        StopPreview();
                    }
                }
            };
        }

        private void SaveOriginalBlendShapeValues()
        {
            if (_targetRenderer == null) return;

            _originalBlendShapeValues.Clear();
            var mesh = _targetRenderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                _originalBlendShapeValues[i] = _targetRenderer.GetBlendShapeWeight(i);
            }
        }

        private void RestoreOriginalBlendShapeValues()
        {
            if (_targetRenderer == null) return;

            foreach (var kvp in _originalBlendShapeValues)
            {
                _targetRenderer.SetBlendShapeWeight(kvp.Key, kvp.Value);
            }
        }

        private void PreviewClip(AnimationClip clip)
        {
            if (_targetRenderer == null) return;

            // 如果当前有在预览的动画，先停止它
            if (_currentPreviewClip != null)
            {
                StopPreview();
            }

            // 保存当前的BlendShape值
            SaveOriginalBlendShapeValues();

            _currentPreviewClip = clip;

            // 获取所有动画绑定信息
            var bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in bindings)
            {
                // 只处理BlendShape相关的曲线
                if (binding.type == typeof(SkinnedMeshRenderer) && binding.propertyName.StartsWith("blendShape."))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve != null && curve.keys.Length > 0)
                    {
                        // 获取第一帧的值
                        var blendShapeName = binding.propertyName.Substring("blendShape.".Length);
                        var blendShapeIndex = _targetRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
                        if (blendShapeIndex != -1)
                        {
                            _targetRenderer.SetBlendShapeWeight(blendShapeIndex, curve.keys[0].value);
                        }
                    }
                }
            }
        }

        private void StopPreview()
        {
            if (_currentPreviewClip == null) return;

            // 恢复原始的BlendShape值
            RestoreOriginalBlendShapeValues();
            _originalBlendShapeValues.Clear();
            _currentPreviewClip = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 目标SkinnedMeshRenderer
            EditorGUI.BeginChangeCheck();
            _targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("目标模型", _targetRenderer, typeof(SkinnedMeshRenderer), true);
            if (EditorGUI.EndChangeCheck() && _currentPreviewClip != null)
            {
                StopPreview();
            }

            EditorGUILayout.Space(10);

            // 动画列表
            _clipList.DoLayout("动画列表", new Vector2(0, 0), false, false, true,
                objects =>
                {
                    foreach (var obj in objects)
                    {
                        if (obj is AnimationClip clip)
                        {
                            _animationClips.Add(clip);
                        }
                    }
                }, Repaint);

            // 当前预览的动画信息
            if (_currentPreviewClip != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField($"当前预览: {_currentPreviewClip.name}");
            }
        }

        private void OnDisable()
        {
            StopPreview();
        }
    }
}