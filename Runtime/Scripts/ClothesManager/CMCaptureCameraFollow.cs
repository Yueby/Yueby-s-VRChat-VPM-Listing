using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Yueby.AvatarTools.ClothesManager
{
    [ExecuteAlways]
    public class CMCaptureCameraFollow : MonoBehaviour
    {
        public event UnityAction OnPositionUpdate;

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            var sceneViewCameraTrans = SceneView.lastActiveSceneView.camera.transform;
            transform.SetPositionAndRotation(sceneViewCameraTrans.position, sceneViewCameraTrans.rotation);
            OnPositionUpdate?.Invoke();
#endif
        }
    }
}