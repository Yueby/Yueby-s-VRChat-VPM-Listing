using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Yueby.Utils;
using Object = UnityEngine.Object;

namespace Yueby.AvatarTools.Other
{
    public class SyncAnimationParameter : EditorWindow
    {
        private static SyncAnimationParameter _window;
        private AnimationClip _animationClip;


        private bool _isAdditive = true;
        private bool _isPreview;


        // private readonly Dictionary<string, float> _clipPropertiesDic = new Dictionary<string, float>();
        // private readonly Dictionary<string, float> _meshBlendShapesDefaultDic = new Dictionary<string, float>();

        private SkinnedMeshRenderer _meshRenderer;
        private Vector2 _pos, _pos1;

        private AnimationBlendShapeHelper _clipAnimationBsHelper;
        private AnimationBlendShapeHelper _defaultAnimationBsHelper;

        private SerializedObject _clipBsInfoSo;
        private SerializedProperty _clipBsParameters;

        private YuebyReorderableList _bsRL;


        [MenuItem("Tools/YuebyTools/Avatar/Other/Sync Animation Parameter", false, 40)]
        public static void OpenWindow()
        {
            _window = GetWindow<SyncAnimationParameter>();
        }

        private void OnDisable()
        {
            if (_isPreview)
            {
                _isPreview = false;
                ResetToDefault();
            }

            DestroyImmediate(_clipAnimationBsHelper);
            DestroyImmediate(_defaultAnimationBsHelper);
        }

        private void OnEnable()
        {
            _clipAnimationBsHelper = CreateInstance<AnimationBlendShapeHelper>();
            _defaultAnimationBsHelper = CreateInstance<AnimationBlendShapeHelper>();

            _clipBsInfoSo = new SerializedObject(_clipAnimationBsHelper);
            _clipBsParameters = _clipBsInfoSo.FindProperty(nameof(AnimationBlendShapeHelper.Parameters));

            _bsRL = new YuebyReorderableList(_clipBsInfoSo, _clipBsParameters, EditorGUIUtility.singleLineHeight, false, true);
            _bsRL.OnRemove += OnBsRLRemove;
            _bsRL.OnDraw += OnBsRLDraw;
        }

        private void OnBsRLDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            var parameter = _clipAnimationBsHelper.Parameters[index];
            var labelRect = new Rect(rect.x, rect.y, rect.width / 2, rect.height);
            EditorGUI.LabelField(labelRect, parameter.Name);

            var sliderRect = new Rect(labelRect.x + labelRect.width + 5f, labelRect.y, rect.width - 5f - labelRect.width, labelRect.height);
            parameter.Value = EditorGUI.Slider(sliderRect, parameter.Value, 0f, 100f);
        }

        private void OnBsRLRemove(ReorderableList list, Object obj)
        {
            // OnRemove

            Debug.Log(list.count);
        }


        private void OnGUI()
        {
            _clipBsInfoSo?.Update();


            YuebyUtil.DrawEditorTitle("动画内形态键同步工具");
            YuebyUtil.VerticalEGLTitled("参数", () =>
            {
                EditorGUI.BeginChangeCheck();
                _meshRenderer = (SkinnedMeshRenderer)YuebyUtil.ObjectField("目标网格：", 50, _meshRenderer, typeof(SkinnedMeshRenderer), true);
                if (EditorGUI.EndChangeCheck())
                    GetMeshInfo();
                if (_defaultAnimationBsHelper == null || _defaultAnimationBsHelper.Parameters.Count == 0 && _meshRenderer != null)
                    GetMeshInfo();

                EditorGUI.BeginChangeCheck();
                _animationClip = (AnimationClip)YuebyUtil.ObjectField("动画片段：", 50, _animationClip, typeof(AnimationClip), true);
                if (EditorGUI.EndChangeCheck())
                    GetAnimationInfo();
                if (_clipAnimationBsHelper == null || _clipAnimationBsHelper.Parameters.Count == 0 && _animationClip != null)
                    GetAnimationInfo();
            });

            if (_animationClip != null)
            {
                _bsRL.DoLayout("动画片段内参数", 400f);
            }
            else
                EditorGUILayout.LabelField("请选择你想要的动画片段(AnimationClip)!");

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
                        if (!_isPreview)
                        {
                            ResetToDefault();
                        }
                    }

                    GUI.backgroundColor = bkgColor;

                    if (GUILayout.Button("应用"))
                    {
                        _isPreview = false;

                        Undo.RegisterCompleteObjectUndo(_meshRenderer, "Apply BlendShapes");
                        ApplyToMesh();
                        
                        ShowNotification(new GUIContent("已应用形态键！CTRL+Z撤销"));
                    }
                });
            });

            if (_isPreview)
                ApplyToMesh();


            _clipBsInfoSo?.ApplyModifiedProperties();
        }


        private void GetAnimationInfo()
        {
            if (_animationClip == null) return;
            // _clipPropertiesDic.Clear();

            _clipAnimationBsHelper.Parameters.Clear();
            var curveBindings = AnimationUtility.GetCurveBindings(_animationClip);
            foreach (var curveBinding in curveBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(_animationClip, curveBinding);

                if (!curveBinding.propertyName.Contains("blendShape.")) continue;
                _clipAnimationBsHelper.Parameters.Add(new AnimationBlendShapeHelper.Parameter
                {
                    Name = curveBinding.propertyName.Replace("blendShape.", ""),
                    Value = curve[0].value
                });
                // _clipPropertiesDic.Add(curveBinding.propertyName, curve[0].value);
            }
        }

        private void GetMeshInfo()
        {
            if (_meshRenderer == null) return;
            _defaultAnimationBsHelper.Parameters.Clear();

            for (var i = 0; i < _meshRenderer.sharedMesh.blendShapeCount; i++)
            {
                var blendShapeName = _meshRenderer.sharedMesh.GetBlendShapeName(i);
                var value = _meshRenderer.GetBlendShapeWeight(i);


                _defaultAnimationBsHelper.Parameters.Add(new AnimationBlendShapeHelper.Parameter
                {
                    Name = blendShapeName,
                    Value = value
                });

                // _meshBlendShapesDefaultDic.Add(name, value);
            }
        }

        private void ApplyToMesh()
        {
            if (_meshRenderer != null && _animationClip != null)
            {
                if (!_isAdditive)
                {
                    for (var i = 0; i < _meshRenderer.sharedMesh.blendShapeCount; i++)
                    {
                        var blendShapeName = _meshRenderer.sharedMesh.GetBlendShapeName(i);
                        var index = _meshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);

                        _meshRenderer.SetBlendShapeWeight(index, 0);
                    }
                }


                foreach (var parameter in _clipAnimationBsHelper.Parameters)
                {
                    var index = _meshRenderer.sharedMesh.GetBlendShapeIndex(parameter.Name);
                    _meshRenderer.SetBlendShapeWeight(index, parameter.Value);
                }
            }
        }

        private void ResetToDefault()
        {
            if (_meshRenderer == null || _animationClip == null) return;

            foreach (var parameter in _defaultAnimationBsHelper.Parameters)
            {
                var index = _meshRenderer.sharedMesh.GetBlendShapeIndex(parameter.Name);
                _meshRenderer.SetBlendShapeWeight(index, parameter.Value);
            }
        }
    }

    [Serializable]
    public class AnimationBlendShapeHelper : ScriptableObject
    {
        public List<Parameter> Parameters = new List<Parameter>();

        [Serializable]
        public class Parameter
        {
            public string Name;
            public float Value;
        }
    }
}