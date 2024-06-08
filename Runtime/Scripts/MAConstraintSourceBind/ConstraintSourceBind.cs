using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDKBase;

namespace Yueby.AvatarTools.MAConstraintSourceBind
{
    [AddComponentMenu("MA Constraint Source Bind")]
    [DisallowMultipleComponent]
    public class ConstraintSourceBind : MonoBehaviour, IEditorOnly
    {
        public bool ActiveSource = true;
        [Range(0, 1)] public float Weight = 1;
        public List<SourceInfo> SourceInfos;
    }

    [Serializable]
    public class SourceInfo
    {
        public bool UseRoot = true;
        public Transform CustomSource;
        [Range(0, 1)] public float Weight;
    }
}