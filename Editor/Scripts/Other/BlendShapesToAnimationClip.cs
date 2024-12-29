using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Yueby.AvatarTools.Other.ModalWindow;
using Yueby.Core.Utils;
using Yueby.ModalWindow;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other
{
    public class BlendShapesToAnimationClip
    {
        [MenuItem("GameObject/YuebyTools/BlendShapesToAnimationClip", validate = true)]
        public static bool ValidateBlendShapesToAnimationClip() => ValidateSelection();

        private static bool ValidateSelection()
        {
            return Selection.activeGameObject != null
                && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem("GameObject/YuebyTools/BlendShapesToAnimationClip", priority = 30)]
        public static void OpenDrawer()
        {
            var drawer = new BlendShapesToAnimationClipDrawer(Selection.activeGameObject);
            ModalEditorWindow.ShowUtility(
                drawer,
                () =>
                {
                    CreateAnimationClip(drawer);
                },
                showFocusCenter: false
            );
        }

        private static void CreateAnimationClip(BlendShapesToAnimationClipDrawer drawer)
        {
            var animationClip = new AnimationClip();
            var rendererMesh = drawer.SkinnedMeshRenderer.sharedMesh;
            for (var i = 0; i < rendererMesh.blendShapeCount; i++)
            {
                var shapeName = rendererMesh.GetBlendShapeName(i);
                var weight = drawer.SkinnedMeshRenderer.GetBlendShapeWeight(i);
                if (weight == 0)
                    continue;

                var curve = new AnimationCurve
                {
                    keys = new[]
                    {
                        new Keyframe { time = 0, value = weight },
                    },
                };

                var path = RecursiveGetPath(drawer.SkinnedMeshRenderer.gameObject);

                var bind = new EditorCurveBinding
                {
                    path = path,
                    type = typeof(SkinnedMeshRenderer),
                    propertyName = "blendShape." + shapeName,
                };
                AnimationUtility.SetEditorCurve(animationClip, bind, curve);
            }

            try
            {
                if (!Directory.Exists(drawer.Path))
                {
                    Directory.CreateDirectory(drawer.Path);
                }

                var assetPath = drawer.Path + "/" + drawer.SkinnedMeshRenderer.name + ".anim";
                // 确保路径是相对于Assets文件夹的
                if (!assetPath.StartsWith("Assets/"))
                {
                    YuebyLogger.LogError($"Invalid asset path: {assetPath}. Path must be inside Assets folder.");
                    return;
                }

                AssetDatabase.CreateAsset(animationClip, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                YuebyLogger.LogInfo($"AnimationClip created: {assetPath}");

                EditorUtils.PingProject(animationClip);
            }
            catch (System.Exception ex)
            {
                YuebyLogger.LogError($"Error when create animation clip: {ex.Message}");
            }
        }

        public static string RecursiveGetPath(GameObject gameObject)
        {
            var path = gameObject.name;
            var parent = gameObject.transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<Animator>() != null)
                    return path;

                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
