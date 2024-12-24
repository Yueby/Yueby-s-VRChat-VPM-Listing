
using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace Yueby.AvatarTools.MAActionSwitch
{
    [AddComponentMenu("MA Action Switch")]
    [DisallowMultipleComponent]
    public class ActionSwitch : MonoBehaviour, IEditorOnly
    {
        public string Name;
        public Texture2D Icon;
        public List<ActionElement> Actions = new List<ActionElement>();
    }

    [Serializable]
    public class ActionElement
    {
        public AnimationClip Clip;
        public string Name;
        public bool UseCustomName;
    }
}