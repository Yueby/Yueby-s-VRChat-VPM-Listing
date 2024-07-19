using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using Yueby.AvatarTools.Other.ModalWindow;
using Yueby.ModalWindow;
using Yueby.Utils;
using Object = UnityEngine.Object;

namespace Yueby.AvatarTools.Other
{
    public class PhysBoneExtractor
    {
        public GameObject Target;
        public const string PhyBoneRootName = "Physbone_Extract";

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/PhysBone Extractor", false, 20)]
        public static void Extract()
        {
            var drawer = new PhysBoneExtractorDrawer();
            Extract(drawer);
        }

        public static void Extract(PhysBoneExtractorDrawer drawer, bool isModel = false)
        {
            if (isModel)
                ModalEditorWindow.Show(drawer, () => { StartExtract(drawer); });
            else
                ModalEditorWindow.ShowUtility(drawer, () => { StartExtract(drawer); });
        }

        private static void StartExtract(PhysBoneExtractorDrawer drawer)
        {
            var target = drawer.Data.Target;

            if (target == null)
            {
                EditorUtils.WaitToDo(40, "ShowTips", () => { ModalEditorWindow.ShowTip("Target is null!"); });
                return;
            }

            var colliderMappers = new List<PhysBoneColliderMapper>();

            var physBones = target.GetComponentsInChildren<VRCPhysBone>(true).ToList();
            var colliders = target.GetComponentsInChildren<VRCPhysBoneColliderBase>(true).ToList();
            var senders = target.GetComponentsInChildren<VRCContactSender>(true).ToList();
            var receivers = target.GetComponentsInChildren<VRCContactReceiver>(true).ToList();

            GameObject root;
            GameObject physBoneParent = null;
            GameObject colliderParent = null;
            GameObject senderParent = null;
            GameObject receiverParent = null;

            if (physBones.Count > 0 || colliders.Count > 0 || senders.Count > 0 || receivers.Count > 0)
            {
                root = new GameObject(PhyBoneRootName) { transform = { parent = target.transform } };
                Undo.RegisterCreatedObjectUndo(root, "Extract PhysBone");
            }
            else
            {
                // EditorUtils.WaitToDo(40, "ShowTips", () => { });
                ModalEditorWindow.ShowTip("No physbones or contacts found!");
                return;
            }

            Undo.RegisterCompleteObjectUndo(target, "Extract PhysBone");
            // Colliders and physbones
            foreach (var pb in physBones)
            {
                pb.rootTransform = pb.transform;
                var pbColliders = pb.colliders;

                for (var i = 0; i < pbColliders.Count; i++)
                {
                    var col = pbColliders[i];
                    if (col == null) continue;

                    if (col.rootTransform == null)
                        col.rootTransform = col.transform;

                    var id = col.GetInstanceID();

                    if (IsInMapper(id, colliderMappers))
                    {
                        var mapper = GetMapper(id, colliderMappers);
                        pb.colliders[i] = mapper.New;
                        continue;
                    }

                    if (colliders.Contains(col))
                        colliders.Remove(col);

                    if (colliderParent == null)
                        colliderParent = new GameObject("Colliders") { transform = { parent = root.transform } };

                    var component = CopyComponentToNewGameObject<VRCPhysBoneColliderBase>(col, colliderParent.transform, false);
                    colliderMappers.Add(new PhysBoneColliderMapper(id, col, component));
                    pb.colliders[i] = component;
                }


                if (physBoneParent == null)
                    physBoneParent = new GameObject("PhysBones") { transform = { parent = root.transform } };

                if (pb.rootTransform == null)
                    pb.rootTransform = pb.transform;
                CopyComponentToNewGameObject<VRCPhysBoneBase>(pb, physBoneParent.transform);
            }

            foreach (var col in colliders)
            {
                if (colliderParent == null)
                    colliderParent = new GameObject("Colliders") { transform = { parent = root.transform } };

                if (col.rootTransform == null)
                    col.rootTransform = col.transform;
                var component = CopyComponentToNewGameObject<VRCPhysBoneColliderBase>(col, colliderParent.transform, false);
                colliderMappers.Add(new PhysBoneColliderMapper(col.GetInstanceID(), col, component));
            }

            foreach (var mapper in colliderMappers)
            {
                Undo.RegisterCompleteObjectUndo(mapper.Old.gameObject, "Destroy Collider");
                Object.DestroyImmediate(mapper.Old);
            }

            // Senders
            foreach (var sender in senders)
            {
                if (senderParent == null)
                    senderParent = new GameObject("Senders") { transform = { parent = root.transform } };
                if (sender.rootTransform == null)
                    sender.rootTransform = sender.transform;
                CopyComponentToNewGameObject<VRCContactSender>(sender, senderParent.transform);
            }


            // Receivers
            foreach (var receiver in receivers)
            {
                if (receiverParent == null)
                    receiverParent = new GameObject("Receivers") { transform = { parent = root.transform } };
                if (receiver.rootTransform == null)
                    receiver.rootTransform = receiver.transform;
                CopyComponentToNewGameObject<VRCContactReceiver>(receiver, receiverParent.transform);
            }

            ModalEditorWindow.ShowTip("Extracted PhysBones and Contacts will be placed under a new GameObject named 'Physbone_Extract'.");
        }

        private static T CopyComponentToNewGameObject<T>(Component component, Transform parent, bool destroyOriginal = true) where T : Component
        {
            var go = new GameObject(component.gameObject.name);
            go.transform.SetParent(parent);

            Undo.RegisterCompleteObjectUndo(go.gameObject, "Copy Component");

            ComponentUtility.CopyComponent(component);
            ComponentUtility.PasteComponentAsNew(go);


            if (destroyOriginal)
                Object.DestroyImmediate(component);
            return go.GetComponents<T>()[^1];
        }

        private static bool IsInMapper(int id, List<PhysBoneColliderMapper> mapper)
        {
            return mapper.Count != 0 && mapper.Any(m => m.InstanceID == id);
        }

        private static PhysBoneColliderMapper GetMapper(int id, List<PhysBoneColliderMapper> mapper)
        {
            return mapper.Find(m => m.InstanceID == id);
        }


        public class PhysBoneColliderMapper
        {
            public int InstanceID;
            public VRCPhysBoneColliderBase Old;
            public VRCPhysBoneColliderBase New;

            public PhysBoneColliderMapper(int instanceID, VRCPhysBoneColliderBase oldComponent, VRCPhysBoneColliderBase newComponent)
            {
                InstanceID = instanceID;
                Old = oldComponent;
                New = newComponent;
            }
        }
    }
}