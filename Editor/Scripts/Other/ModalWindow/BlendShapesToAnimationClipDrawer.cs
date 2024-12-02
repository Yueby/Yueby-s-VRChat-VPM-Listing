using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Yueby.ModalWindow;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other.ModalWindow
{
    public class BlendShapesToAnimationClipDrawer
        : ModalEditorWindowDrawer<BlendShapesToAnimationClip>
    {
        public override string Title => "BlendShapesToAnimationClip";
        private string _path = "Assets/Animations/";
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        public SkinnedMeshRenderer SkinnedMeshRenderer => _skinnedMeshRenderer;
        public string Path => _path;

        private GameObject _activeGameObject;

        public BlendShapesToAnimationClipDrawer(GameObject activeGameObject)
        {
            Data = new BlendShapesToAnimationClip();
            _activeGameObject = activeGameObject;

            _skinnedMeshRenderer = _activeGameObject.GetComponent<SkinnedMeshRenderer>();
        }

        public override void OnDraw()
        {
            EditorUI.HorizontalEGL(() =>
            {
                _path = EditorUI.TextField("Path", _path, 50);
                if (GUILayout.Button("..."))
                {
                    // Show select folder path
                    var folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        var relativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                        _path = relativePath;
                    }
                }
            });

            _skinnedMeshRenderer = (SkinnedMeshRenderer)
                EditorUI.ObjectField(
                    "Target",
                    50,
                    _skinnedMeshRenderer,
                    typeof(SkinnedMeshRenderer),
                    true
                );
        }
    }
}
