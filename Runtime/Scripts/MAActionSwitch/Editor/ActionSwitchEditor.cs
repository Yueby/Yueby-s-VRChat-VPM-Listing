using System;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Editor = UnityEditor.Editor;
using Object = UnityEngine.Object;

namespace Yueby.MAActionSwitch
{
    [CustomEditor(typeof(ActionSwitch))]
    public class ActionSwitchEditor : Editor
    {

        private ActionSwitch _target;
        private ReorderableList _reorderableList;
        private SerializedProperty _actionsProperty;
        private SerializedProperty _nameProperty;
        private SerializedProperty _iconProperty;
        private ModularAvatarMenuItem _menuItem;

        private void OnEnable()
        {
            _target = (ActionSwitch)target;

            _nameProperty = serializedObject.FindProperty(nameof(ActionSwitch.Name));
            _iconProperty = serializedObject.FindProperty(nameof(ActionSwitch.Icon));
            _actionsProperty = serializedObject.FindProperty(nameof(ActionSwitch.Actions));

            _reorderableList = CreateReorderableList();

            if (_menuItem == null)
            {
                var menuItem = _target.gameObject.GetComponent<ModularAvatarMenuItem>() ?? _target.gameObject.AddComponent<ModularAvatarMenuItem>();
                _menuItem = menuItem;
                menuItem.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
                menuItem.MenuSource = SubmenuSource.Children;
            }

        }

        private ReorderableList CreateReorderableList()
        {
            _reorderableList = new ReorderableList(serializedObject, _actionsProperty, true, true, true, true)
            {
                headerHeight = 0,

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = _actionsProperty.GetArrayElementAtIndex(index);
                    var clip = element.FindPropertyRelative(nameof(ActionElement.Clip));
                    var name = element.FindPropertyRelative(nameof(ActionElement.Name));
                    var useCustomName = element.FindPropertyRelative(nameof(ActionElement.UseCustomName));
                    rect.y += 2;
                    var clipFieldRect = new Rect(rect.x, rect.y, rect.width / 3, EditorGUIUtility.singleLineHeight);
                    clip.objectReferenceValue = EditorGUI.ObjectField(clipFieldRect, clip.objectReferenceValue, typeof(AnimationClip), false);

                    var lastWidth = rect.width - clipFieldRect.width;
                    var toggleWidth = 40f;

                    var nameFieldRect = new Rect(clipFieldRect.x + clipFieldRect.width + 5, rect.y, lastWidth - toggleWidth, EditorGUIUtility.singleLineHeight);
                    if (useCustomName.boolValue)
                    {
                        name.stringValue = EditorGUI.TextField(nameFieldRect, name.stringValue);
                    }
                    else
                    {
                        name.stringValue = $"Action {index + 1}";
                        EditorGUI.LabelField(nameFieldRect, name.stringValue);
                    }

                    var toggleRect = new Rect(nameFieldRect.x + nameFieldRect.width + 2, rect.y, toggleWidth, EditorGUIUtility.singleLineHeight);
                    useCustomName.boolValue = EditorGUI.Toggle(toggleRect, useCustomName.boolValue);

                    // EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element);
                },

                onAddCallback = list =>
                {
                    _actionsProperty.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                },

                onRemoveCallback = list =>
                {
                    _actionsProperty.DeleteArrayElementAtIndex(list.index);
                    serializedObject.ApplyModifiedProperties();
                }

            };

            return _reorderableList;
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

            DropAreaGUI();
            _reorderableList.DoLayoutList();
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