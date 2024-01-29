using System;
using UnityEditor;
using UnityEngine;

namespace Yueby.AvatarTools.CameraFollow
{
#if UNITY_EDITOR

    public class SceneViewCameraFollow : MonoBehaviour
    {
        private Transform _mainCamTrans;

        private void Start()
        {
            if (Camera.main != null)
                _mainCamTrans = Camera.main.transform;
        }

        // private void Update()
        // {
        //     if (!_mainCamTrans) return;
        //     var sceneViewCameraTrans = SceneView.lastActiveSceneView.camera.transform;
        //     _mainCamTrans.SetPositionAndRotation(sceneViewCameraTrans.position, sceneViewCameraTrans.rotation);
        // }

        private void OnDrawGizmos()
        {
            if (Camera.main != null)
                _mainCamTrans = Camera.main.transform;
            var sceneViewCameraTrans = SceneView.lastActiveSceneView.camera.transform;
            _mainCamTrans.SetPositionAndRotation(sceneViewCameraTrans.position, sceneViewCameraTrans.rotation);
        }
    }
#endif
}