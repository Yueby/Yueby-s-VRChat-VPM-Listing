using UnityEditor;
using UnityEngine;
using Yueby.ModalWindow;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other.ModalWindow
{
    public class PhysBoneExtractorDrawer : ModalEditorWindowDrawer<PhysBoneExtractor>
    {
        public override string Title => "PhysBoneExtractor";

        public PhysBoneExtractorDrawer()
        {
            Data = new PhysBoneExtractor();
        }

        public override void OnDraw()
        {
            EditorGUILayout.HelpBox("Drop a clothes or avatar gameObject to area below to extract.", MessageType.Info);
            // EditorGUILayout.LabelField("Drop a clothes or avatar gameObject to area below to extract.");
            Data.Target = (GameObject)EditorUI.ObjectField("Target", 50, Data.Target, typeof(GameObject), true);
        }
    }
}