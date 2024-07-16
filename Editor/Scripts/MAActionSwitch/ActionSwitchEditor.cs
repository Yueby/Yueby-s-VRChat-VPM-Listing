using System;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Yueby.Utils;
using Editor = UnityEditor.Editor;
using Object = UnityEngine.Object;

namespace Yueby.AvatarTools.MAActionSwitch
{
    [CustomEditor(typeof(ActionSwitch))]
    public class ActionSwitchEditor : Editor
    {
        private ActionSwitch _target;
        private SerializedProperty _actionsProperty;
        private SerializedProperty _nameProperty;
        private SerializedProperty _iconProperty;
        private ModularAvatarMenuItem _menuItem;
        private ReorderableListDroppable _reorderableListDroppable;

        private void OnEnable()
        {

            _target = (ActionSwitch)target;

            _nameProperty = serializedObject.FindProperty(nameof(ActionSwitch.Name));
            _iconProperty = serializedObject.FindProperty(nameof(ActionSwitch.Icon));
            _actionsProperty = serializedObject.FindProperty(nameof(ActionSwitch.Actions));

            // _reorderableList = CreateReorderableList();

            if (_menuItem == null)
            {
                var menuItem = _target.gameObject.GetComponent<ModularAvatarMenuItem>() ?? _target.gameObject.AddComponent<ModularAvatarMenuItem>();
                _menuItem = menuItem;
                menuItem.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
                menuItem.MenuSource = SubmenuSource.Children;
            }

            _reorderableListDroppable = CreateDroppableList();
            _reorderableListDroppable.AnimBool.value = true;

        }

        private ReorderableListDroppable CreateDroppableList()
        {
            var list = new ReorderableListDroppable(_target.Actions, typeof(ActionElement), EditorGUIUtility.singleLineHeight + 5, Repaint)
            {
                OnDraw = (rect, index, isActive, isFocused) =>
                {
                    var actionElement = _target.Actions[index];

                    rect.y += 2;
                    var clipFieldRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                    actionElement.Clip = (AnimationClip)EditorGUI.ObjectField(clipFieldRect, actionElement.Clip, typeof(AnimationClip), false);

                    var lastWidth = rect.width - clipFieldRect.width;
                    var toggleWidth = 20f;

                    var nameFieldRect = new Rect(clipFieldRect.x + clipFieldRect.width + 5, rect.y, lastWidth - toggleWidth, EditorGUIUtility.singleLineHeight);
                    if (actionElement.UseCustomName)
                    {
                        actionElement.Name = EditorGUI.TextField(nameFieldRect, actionElement.Name);
                    }
                    else
                    {
                        actionElement.Name = $"Action {index + 1}";
                        EditorGUI.LabelField(nameFieldRect, actionElement.Name);
                    }

                    var toggleRect = new Rect(nameFieldRect.x + nameFieldRect.width + 2, rect.y, toggleWidth, EditorGUIUtility.singleLineHeight);
                    actionElement.UseCustomName = EditorGUI.Toggle(toggleRect, actionElement.UseCustomName);

                    return EditorGUIUtility.singleLineHeight + 5;
                },

                OnAdd = list =>
                {
                    _target.Actions.Add(new ActionElement());
                    // _actionsProperty.arraySize++;
                    // serializedObject.ApplyModifiedProperties();
                },

                OnRemove = list =>
                {
                    // _actionsProperty.DeleteArrayElementAtIndex(list.index);
                    // serializedObject.ApplyModifiedProperties();
                },

            };

            return list;
        }

        public override void OnInspectorGUI()
        {

            serializedObject.UpdateIfRequiredOrScript();
            DrawEditor();
            serializedObject.ApplyModifiedProperties();

        }

        private void DrawEditor()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(_nameProperty);
                EditorGUILayout.PropertyField(_iconProperty);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _menuItem.gameObject.name = _nameProperty.stringValue;
                _menuItem.Control.icon = _iconProperty.objectReferenceValue as Texture2D;
            }

            EditorGUILayout.Space();

            // DropAreaGUI();
            // _reorderableList.DoLayoutList();
            _reorderableListDroppable.DoLayoutList("Actions", new Vector2(0, 0), false, true, true, DropObjects, Repaint);
        }

        private void DropObjects(Object[] objects)
        {
            foreach (Object dragged_object in objects)
            {
                // Do On Drag Stuff here
                if (dragged_object is not AnimationClip)
                    continue;

                // Debug.Log("Animation Clip Dropped");

                var actionElement = new ActionElement
                {
                    Clip = (AnimationClip)dragged_object,
                    Name = dragged_object.name,
                    UseCustomName = true
                };

                // check if the clip is already in the list
                bool alreadyExists = false;
                for (int i = 0; i < _target.Actions.Count; i++)
                {
                    var element = _target.Actions[i];

                    if (element.Clip == actionElement.Clip)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists)
                {
                    // Debug.Log("Already Exists: " + alreadyExists);
                    // serializedObject.ApplyModifiedProperties();
                    continue;
                }

                // check other element is null
                var addInNull = false;
                for (int i = 0; i < _target.Actions.Count; i++)
                {
                    var element = _target.Actions[i];

                    if (element.Clip == null)
                    {
                        element.Clip = actionElement.Clip;
                        addInNull = true;
                        break;
                    }
                }

                if (addInNull)
                {
                    // Debug.Log("Add in Null: " + addInNull);
                    // serializedObject.ApplyModifiedProperties();
                    continue;
                }

                _target.Actions.Add(actionElement);
                // _actionsProperty.arraySize++;
                // var action = _actionsProperty.GetArrayElementAtIndex(_actionsProperty.arraySize - 1);
                // action.FindPropertyRelative(nameof(ActionElement.Clip)).objectReferenceValue = actionElement.Clip;
                // action.FindPropertyRelative(nameof(ActionElement.Name)).stringValue = actionElement.Name;
                // action.FindPropertyRelative(nameof(ActionElement.UseCustomName)).boolValue = actionElement.UseCustomName;

                // // Debug.Log("Animation Clip Added");
                // serializedObject.ApplyModifiedProperties();
            }
        }

        public void DropAreaGUI()
        {
            Event evt = Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 25.0f, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, "", "Badge");
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(drop_area, "Drop Animation Clips Here", style);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object dragged_object in DragAndDrop.objectReferences)
                        {
                            // Do On Drag Stuff here
                            if (dragged_object is not AnimationClip)
                                continue;

                            // Debug.Log("Animation Clip Dropped");

                            var actionElement = new ActionElement
                            {
                                Clip = (AnimationClip)dragged_object,
                                Name = dragged_object.name,
                                UseCustomName = true
                            };

                            // check if the clip is already in the list
                            bool alreadyExists = false;
                            for (int i = 0; i < _actionsProperty.arraySize; i++)
                            {
                                var element = _actionsProperty.GetArrayElementAtIndex(i);
                                var clip = element.FindPropertyRelative(nameof(ActionElement.Clip));
                                if (clip.objectReferenceValue == dragged_object)
                                {
                                    alreadyExists = true;
                                    break;
                                }
                            }

                            if (alreadyExists)
                            {
                                // Debug.Log("Already Exists: " + alreadyExists);
                                serializedObject.ApplyModifiedProperties();
                                continue;
                            }

                            // check other element is null
                            var addInNull = false;
                            for (int i = 0; i < _actionsProperty.arraySize; i++)
                            {
                                var element = _actionsProperty.GetArrayElementAtIndex(i);
                                var clip = element.FindPropertyRelative(nameof(ActionElement.Clip));
                                if (clip.objectReferenceValue == null)
                                {
                                    clip.objectReferenceValue = dragged_object;
                                    addInNull = true;
                                    break;
                                }
                            }

                            if (addInNull)
                            {
                                // Debug.Log("Add in Null: " + addInNull);
                                serializedObject.ApplyModifiedProperties();
                                continue;
                            }

                            _actionsProperty.arraySize++;
                            var action = _actionsProperty.GetArrayElementAtIndex(_actionsProperty.arraySize - 1);
                            action.FindPropertyRelative(nameof(ActionElement.Clip)).objectReferenceValue = actionElement.Clip;
                            action.FindPropertyRelative(nameof(ActionElement.Name)).stringValue = actionElement.Name;
                            action.FindPropertyRelative(nameof(ActionElement.UseCustomName)).boolValue = actionElement.UseCustomName;

                            // Debug.Log("Animation Clip Added");
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                    break;
            }
        }
    }
}