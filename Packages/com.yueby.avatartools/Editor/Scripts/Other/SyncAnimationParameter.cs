using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other
{
    public class SyncAnimationParameter : EditorWindow
    {
        private static SyncAnimationParameter _window;
        private AnimationClip _animationClip;

        private readonly Dictionary<string, float> _clipPropertiesDic = new Dictionary<string, float>();
        private bool _isAdditive = true;
        private bool _isPreview;
        private readonly Dictionary<string, float> _meshBlendShapesDefaultDic = new Dictionary<string, float>();
        private SkinnedMeshRenderer _meshRenderer;
        private Vector2 _pos, _pos1;

        private void OnDisable()
        {
            if (_isPreview)
            {
                _isPreview = false;
                ResetToDefault();
            }
        }

        private void OnGUI()
        {
            YuebyUtil.DrawEditorTitle("动画内形态键同步工具");
            YuebyUtil.VerticalEGLTitled("参数", () =>
            {
                EditorGUI.BeginChangeCheck();
                _animationClip = (AnimationClip)YuebyUtil.ObjectField("动画片段：", 50, _animationClip, typeof(AnimationClip), true);
                if (EditorGUI.EndChangeCheck())
                    GetAnimationInfo();
                if (_clipPropertiesDic.Count == 0 && _animationClip != null)
                    GetAnimationInfo();

                EditorGUI.BeginChangeCheck();
                _meshRenderer = (SkinnedMeshRenderer)YuebyUtil.ObjectField("目标网格：", 50, _meshRenderer, typeof(SkinnedMeshRenderer), true);
                if (EditorGUI.EndChangeCheck()) GetMeshInfo();
                if (_meshBlendShapesDefaultDic.Count == 0 && _meshRenderer != null)
                    GetMeshInfo();
            });

            YuebyUtil.VerticalEGLTitled("动画片段内参数", () =>
            {
                if (_animationClip != null)
                    _pos = YuebyUtil.ScrollViewEGL(() =>
                    {
                        foreach (var clipProperties in _clipPropertiesDic)
                        {
                            var propertyNames = clipProperties.Key.Split('.');
                            var type = propertyNames[0];
                            var name = propertyNames[1];
                            EditorGUILayout.LabelField("[" + type + "]" + " " + name + " - " + clipProperties.Value);
                        }
                    }, _pos, GUILayout.Height(100));
                else
                    EditorGUILayout.LabelField("请选择你想要的动画片段(AnimationClip)!");
            });

            YuebyUtil.VerticalEGLTitled("设置", () =>
            {
                _isAdditive = YuebyUtil.Radio(_isAdditive, "IsAdditive");
                YuebyUtil.HorizontalEGL(() =>
                {
                    var bkgColor = GUI.backgroundColor;
                    if (_isPreview)
                        GUI.backgroundColor = Color.green;

                    if (GUILayout.Button("预览"))
                    {
                        _isPreview = !_isPreview;

                        if (_isPreview)
                            ApplyToMesh();
                        else
                            ResetToDefault();
                    }

                    GUI.backgroundColor = bkgColor;

                    if (GUILayout.Button("应用"))
                    {
                        _isPreview = false;
                        ApplyToMesh();
                    }
                });
            });
        }

        [MenuItem("Tools/YuebyTools/Avatar/Other/Sync Animation Parameter")]
        public static void OpenWindow()
        {
            _window = GetWindow<SyncAnimationParameter>();
        }

        private void GetAnimationInfo()
        {
            if (_animationClip == null) return;
            _clipPropertiesDic.Clear();
            var curveBindings = AnimationUtility.GetCurveBindings(_animationClip);
            foreach (var curveBinding in curveBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(_animationClip, curveBinding);
                _clipPropertiesDic.Add(curveBinding.propertyName, curve[0].value);
            }
        }

        private void GetMeshInfo()
        {
            if (_meshRenderer == null) return;
            _meshBlendShapesDefaultDic.Clear();
            for (var i = 0; i < _meshRenderer.sharedMesh.blendShapeCount; i++)
            {
                var name = _meshRenderer.sharedMesh.GetBlendShapeName(i);
                var value = _meshRenderer.GetBlendShapeWeight(i);

                _meshBlendShapesDefaultDic.Add(name, value);
            }
        }

        private void ApplyToMesh()
        {
            if (_meshRenderer != null && _animationClip != null)
            {
                if (!_isAdditive)
                    for (var i = 0; i < _meshRenderer.sharedMesh.blendShapeCount; i++)
                    {
                        var name = _meshRenderer.sharedMesh.GetBlendShapeName(i);
                        var index = _meshRenderer.sharedMesh.GetBlendShapeIndex(name);

                        _meshRenderer.SetBlendShapeWeight(index, 0);
                    }

                foreach (var item in _clipPropertiesDic)
                {
                    var name = item.Key.Split('.')[1];
                    var value = item.Value;

                    var index = _meshRenderer.sharedMesh.GetBlendShapeIndex(name);
                    _meshRenderer.SetBlendShapeWeight(index, value);
                }
            }
        }

        private void ResetToDefault()
        {
            if (_meshRenderer != null && _animationClip != null)
                foreach (var item in _meshBlendShapesDefaultDic)
                {
                    var name = item.Key;
                    var value = item.Value;

                    var index = _meshRenderer.sharedMesh.GetBlendShapeIndex(name);
                    _meshRenderer.SetBlendShapeWeight(index, value);
                }
        }
    }
}