using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Yueby.Utils
{
    public static class YuebyUtil
    {
        public static void DrawCheckChanged(UnityAction onDraw, UnityAction onChanged)
        {
            EditorGUI.BeginChangeCheck();
            onDraw?.Invoke();
            if (EditorGUI.EndChangeCheck()) onChanged?.Invoke();
        }

        public static string GetUtilParentDirectory()
        {
            return GetParentDirectory(GetParentDirectory(GetScriptDirectory(nameof(YuebyUtil))));
        }

        public static void FocusTarget(GameObject target)
        {
            Selection.activeGameObject = target;
            if (EditorWindow.HasOpenInstances<SceneView>())
            {
                EditorWindow.FocusWindowIfItsOpen<SceneView>();

                SceneView.FrameLastActiveSceneView();
            }
        }

        // public static GUISkin GetStyle()
        // {
        //     return AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Yueby/Utils/Style/YuebyStyle.guiskin");
        // }

        public static void GoodHelpBox(string text)
        {
            var goodHelpBoxContent = new GUIContent
            {
                text = text,
                image = AssetDatabase.LoadMainAssetAtPath("Packages/com.yueby.avatartools/Editor/Assets/Sprites/ok.png") as Texture2D
            };

            EditorGUILayout.HelpBox(goodHelpBoxContent);
        }

        public static string TextField(string label, string text, int labelWidth, bool isDisable = false)
        {
            HorizontalEGL(() =>
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
                EditorGUI.BeginDisabledGroup(isDisable);
                text = EditorGUILayout.TextField(text);
                EditorGUI.EndDisabledGroup();
            });

            return text;
        }

        public static string TextFieldVertical(string label, string text, int labelWidth, bool isDisable = false)
        {
            VerticalEGL(() =>
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
                EditorGUI.BeginDisabledGroup(isDisable);
                text = EditorGUILayout.TextField(text);
                EditorGUI.EndDisabledGroup();
            });

            return text;
        }

        public static string TextFieldVertical(string label, string text, UnityAction drawOnFieldEnd)
        {
            VerticalEGL(() =>
            {
                EditorGUILayout.LabelField(label);
                HorizontalEGL(() =>
                {
                    text = EditorGUILayout.TextField(text);
                    drawOnFieldEnd?.Invoke();
                });

                EditorGUILayout.Space(5);
            });

            return text;
        }

        public static int PopupVertical(string label, int index, int labelWidth, string[] popups)
        {
            VerticalEGL(() =>
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
                index = EditorGUILayout.Popup("", index, popups);
                EditorGUILayout.Space(5);
            });

            return index;
        }

        public static int PopupVertical(string label, int index, int labelWidth, string[] popups, UnityAction drawOnPopupEnd, UnityAction changeCallback = null)
        {
            VerticalEGL(() =>
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
                HorizontalEGL(() =>
                {
                    EditorGUI.BeginChangeCheck();
                    index = EditorGUILayout.Popup("", index, popups);
                    if (EditorGUI.EndChangeCheck())
                        changeCallback?.Invoke();
                    drawOnPopupEnd?.Invoke();
                });

                EditorGUILayout.Space(5);
            });

            return index;
        }

        public static bool Radio(bool foldout, string label)
        {
            HorizontalEGL(() =>
            {
                foldout = EditorGUILayout.Toggle(foldout, new GUIStyle("Radio"), GUILayout.Width(20));
                EditorGUILayout.LabelField(new GUIContent(label));
            });

            return foldout;
        }

        public static bool Toggle(bool foldout, string label, float labelWidth = 0)
        {
            HorizontalEGL(() =>
            {
                foldout = EditorGUILayout.Toggle(foldout, GUILayout.Width(22));
                if (labelWidth == 0)
                    EditorGUILayout.LabelField(new GUIContent(label));
                else
                    EditorGUILayout.LabelField(new GUIContent(label), GUILayout.Width(labelWidth));
            });

            return foldout;
        }

        public static bool Radio(bool foldout, string label, GUIStyle style)
        {
            HorizontalEGL(() =>
            {
                foldout = EditorGUILayout.Toggle(foldout, new GUIStyle("Radio"), GUILayout.Width(20));
                EditorGUILayout.LabelField(new GUIContent(label), style);
            });

            return foldout;
        }

        public static bool FoldoutHeaderGroup(bool foldout, string content, UnityAction action, bool isLine = false, int thickness = 2, int padding = 10)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, content);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (foldout)
            {
                action?.Invoke();
                if (isLine)
                    Line();
            }

            return foldout;
        }

        public static bool Radio(bool foldout, string content, UnityAction action)
        {
            HorizontalEGL(() =>
            {
                if (GUILayout.Button("", new GUIStyle("radio"), GUILayout.Width(20), GUILayout.Height(20))) foldout = !foldout;

                // foldout = EditorGUILayout.Foldout(foldout, con, new GUIStyle("radio"));
                EditorGUILayout.LabelField(content);
            });

            if (foldout) action?.Invoke();

            return foldout;
        }

        public static bool Foldout(bool foldout, string content, UnityAction action, bool isLine = false)
        {
            var isContentEmpty = string.IsNullOrEmpty(content);

            if (isContentEmpty)
            {
                action?.Invoke();
                if (isLine)
                    Line();
            }
            else
            {
                foldout = EditorGUILayout.Foldout(foldout, content);
                if (foldout)
                {
                    action?.Invoke();
                    if (isLine)
                        Line();
                }
            }
            
            return foldout;
        }

        public static bool FoldoutHeaderGroup(bool foldout, string content, UnityAction action, GUIStyle style, bool isLine = false, int thickness = 2, int padding = 10)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, content, style);
            if (foldout)
            {
                action?.Invoke();
                if (isLine)
                    Line();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            return foldout;
        }

        public static bool DrawFoldoutNormal(bool foldout, string label, UnityAction action, int padding = 5)
        {
            VerticalEGL(new GUIStyle("Badge"), () =>
            {
                foldout = Radio(foldout, label);
                if (foldout) VerticalEGL(new GUIStyle("Badge"), () => { action?.Invoke(); });
            });

            return foldout;
        }

        public static bool DrawFoldoutH1(bool foldout, string label, UnityAction action, int padding = 5)
        {
            EditorGUILayout.Space(padding);
            VerticalEGL(new GUIStyle("Badge"), () =>
            {
                foldout = Radio(foldout, label, new GUIStyle("IN TitleText"));
                if (foldout)
                {
                    VerticalEGL(new GUIStyle("Badge"), () => { action?.Invoke(); });
                    EditorGUILayout.Space(5);
                }
            });

            return foldout;
        }

        public static bool DrawFoldoutH2(bool foldout, string label, UnityAction action, bool hasContentBorder = true, int padding = 2)
        {
            EditorGUILayout.Space(padding);

            VerticalEGL(new GUIStyle("ShurikenEffectBg"), () =>
            {
                HorizontalEGL(() =>
                {
                    foldout = EditorGUILayout.Toggle(foldout, new GUIStyle("Radio"), GUILayout.Width(20));
                    EditorGUILayout.LabelField(new GUIContent(label));
                });
                if (foldout)
                {
                    if (hasContentBorder)
                        VerticalEGL(new GUIStyle("LODSliderRange"), () =>
                        {
                            EditorGUILayout.Space(5);
                            action?.Invoke();
                            EditorGUILayout.Space(5);
                        });
                    else
                        action?.Invoke();
                }
            });

            EditorGUILayout.Space(padding);
            return foldout;
        }

        public static void PingProject(string path)
        {
            EditorUtility.FocusProjectWindow();
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = obj;
        }

        public static void PingProject(Object obj)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = obj;
        }

        public static async void PingObjectAndBack(GameObject target, GameObject current, int millionSeconds)
        {
            PingObject(target);
            await Task.Delay(millionSeconds);
            PingObject(current);
        }

        public static GameObject CreateGameObject(GameObject gameObject, Vector3 position, Quaternion rotation, Transform parent)
        {
            var go = Object.Instantiate(gameObject, parent);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = rotation;
            Undo.RegisterCreatedObjectUndo(go, "prefab");
            return go;
        }

        public static GameObject CreatePrefabAtPath(Transform parent, string path)
        {
            var prefab = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            Undo.RegisterCreatedObjectUndo(prefab, "prefab");

            prefab.transform.localScale = Vector3.one;
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.Euler(Vector3.zero);
            return prefab;
        }

        public static GameObject CreatePrefabAtPath(Transform parent, string path, Vector3 position)
        {
            var prefab = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            Undo.RegisterCreatedObjectUndo(prefab, "prefab");

            prefab.transform.localScale = Vector3.one;
            prefab.transform.localPosition = position;
            prefab.transform.localRotation = Quaternion.Euler(Vector3.zero);
            return prefab;
        }

        public static T[] FindChildDeleteOtherType<T>(Transform parent)
        {
            var list = new List<T>();
            var deleteList = new List<GameObject>();
            if (parent == null) return list.ToArray();

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var component = child.GetComponent<T>();
                if (component != null)
                    list.Add(component);
                else
                    deleteList.Add(child.gameObject);
            }

            foreach (var delete in deleteList)
            {
                Undo.RegisterFullObjectHierarchyUndo(delete, "deleteChild");
                Undo.DestroyObjectImmediate(delete);
            }

            return list.ToArray();
        }

        public static T[] FindChildByType<T>(Transform parent)
        {
            var list = new List<T>();
            if (parent == null) return list.ToArray();

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var component = child.GetComponent<T>();
                if (component != null)
                    list.Add(component);
            }

            return list.ToArray();
        }

        public static T[] FindChild<T>(Transform parent)
        {
            return parent.GetComponentsInChildren<T>(true);
        }

        public static void PingObject(GameObject gameObject)
        {
            EditorGUIUtility.PingObject(gameObject);
            Selection.activeGameObject = gameObject;
        }

        public static Vector2 ScrollViewEGL(UnityAction action, Vector2 position, params GUILayoutOption[] options)
        {
            position = EditorGUILayout.BeginScrollView(position, options);
            action?.Invoke();
            EditorGUILayout.EndScrollView();
            return position;
        }

        public static Vector2 ScrollViewEGL(UnityAction action, Vector2 position, GUIStyle hStyle, GUIStyle vStyle, params GUILayoutOption[] options)
        {
            position = EditorGUILayout.BeginScrollView(position, hStyle, vStyle, options);
            action?.Invoke();
            EditorGUILayout.EndScrollView();
            return position;
        }

        public static void DisableGroupEGL(bool disabled, UnityAction action)
        {
            EditorGUI.BeginDisabledGroup(true);
            action?.Invoke();
            EditorGUI.EndDisabledGroup();
        }

        #region DrawArray

        public static int DrawArray<T>(int pageIndex, int countPerPage, List<T> array, UnityAction<T, int> drawElement, bool contentBorder = false)
        {
            var pageCount = (array.Count - 1) / countPerPage;
            // if (countPerPage == array.arraySize)
            //     pageCount = 0;

            if (pageIndex > pageCount)
                pageIndex = pageCount;

            HorizontalEGL(new GUIStyle("Badge"), () =>
            {
                if (pageCount != 0)
                {
                    EditorGUILayout.LabelField($"[Count : {array.Count}]");
                    EditorGUILayout.Space();
                    var lastPageIndex = pageIndex;
                    pageIndex = EditorGUILayout.IntField(pageIndex, GUILayout.Width(30f));
                    if (pageIndex > pageCount || pageIndex < 0)
                        pageIndex = lastPageIndex;
                    EditorGUILayout.LabelField($"/{pageCount}", GUILayout.Width(30f));

                    if (GUILayout.Button("<", GUILayout.Width(20f)))
                    {
                        pageIndex--;
                        if (pageIndex < 0)
                            pageIndex = pageCount;
                    }

                    if (GUILayout.Button(">", GUILayout.Width(20)))
                    {
                        pageIndex++;
                        if (pageIndex > pageCount)
                            pageIndex = 0;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No more pages");
                }
            }, GUILayout.Height(24));

            var min = countPerPage * pageIndex;
            var max = pageIndex == pageCount ? array.Count : (pageIndex + 1) * countPerPage;

            //
            if (contentBorder)
                VerticalEGL(new GUIStyle("Badge"), () =>
                {
                    EditorGUILayout.Space(5);
                    for (var i = min; i < max; i++)
                    {
                        if (i >= array.Count || array[i] == null) break;
                        drawElement?.Invoke(array[i], i);
                    }

                    EditorGUILayout.Space(5);
                });
            else
                for (var i = min; i < max; i++)
                {
                    if (i >= array.Count || array[i] == null) break;
                    drawElement?.Invoke(array[i], i);
                }

            return pageIndex;
        }

        public static int DrawArray(int pageIndex, int countPerPage, SerializedProperty array, UnityAction<SerializedObject, int> drawElement, bool contentBorder = false)
        {
            var pageCount = (array.arraySize - 1) / countPerPage;
            // if (countPerPage == array.arraySize)
            //     pageCount = 0;

            if (pageIndex > pageCount)
                pageIndex = pageCount;

            HorizontalEGL(new GUIStyle("Badge"), () =>
            {
                if (pageCount != 0)
                {
                    EditorGUILayout.LabelField($"[Count : {array.arraySize}]");
                    EditorGUILayout.Space();
                    var lastPageIndex = pageIndex;
                    pageIndex = EditorGUILayout.IntField(pageIndex, GUILayout.Width(30f));
                    if (pageIndex > pageCount || pageIndex < 0)
                        pageIndex = lastPageIndex;
                    EditorGUILayout.LabelField($"/{pageCount}", GUILayout.Width(30f));

                    if (GUILayout.Button("<", GUILayout.Width(20f)))
                    {
                        pageIndex--;
                        if (pageIndex < 0)
                            pageIndex = pageCount;
                    }

                    if (GUILayout.Button(">", GUILayout.Width(20)))
                    {
                        pageIndex++;
                        if (pageIndex > pageCount)
                            pageIndex = 0;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No more pages");
                }
            }, GUILayout.Height(24));

            var min = countPerPage * pageIndex;
            var max = pageIndex == pageCount ? array.arraySize : (pageIndex + 1) * countPerPage;

            //
            if (contentBorder)
                VerticalEGL(new GUIStyle("Badge"), () =>
                {
                    EditorGUILayout.Space(5);
                    for (var i = min; i < max; i++)
                    {
                        if (i >= array.arraySize || array.GetArrayElementAtIndex(i).objectReferenceValue == null) break;
                        var arrayElement = new SerializedObject(array.GetArrayElementAtIndex(i).objectReferenceValue);
                        drawElement?.Invoke(arrayElement, i);
                    }

                    EditorGUILayout.Space(5);
                });
            else
                for (var i = min; i < max; i++)
                {
                    if (i >= array.arraySize || array.GetArrayElementAtIndex(i).objectReferenceValue == null) break;
                    var arrayElement = new SerializedObject(array.GetArrayElementAtIndex(i).objectReferenceValue);
                    drawElement?.Invoke(arrayElement, i);
                }

            return pageIndex;
        }

        public static int DrawArray(int pageIndex, int countPerPage, SerializedProperty array, UnityAction onAdd, UnityAction<SerializedObject, int> drawElement, bool contentBorder = false)
        {
            if (array.arraySize == 0)
            {
                if (GUILayout.Button("Add")) onAdd?.Invoke();
            }
            else
            {
                var pageCount = (array.arraySize - 1) / countPerPage;

                if (pageIndex > pageCount)
                    pageIndex = pageCount;

                EditorGUILayout.Space(5);
                HorizontalEGL(new GUIStyle("Badge"), () =>
                {
                    if (pageCount != 0)
                    {
                        EditorGUILayout.LabelField($"[Count : {array.arraySize}]");
                        EditorGUILayout.Space();
                        var lastPageIndex = pageIndex;
                        pageIndex = EditorGUILayout.IntField(pageIndex, GUILayout.Width(40f));
                        if (pageIndex > pageCount || pageIndex < 0)
                            pageIndex = lastPageIndex;
                        EditorGUILayout.LabelField($"/{pageCount}", GUILayout.Width(50f));

                        if (GUILayout.Button("<", GUILayout.Width(20f)))
                        {
                            pageIndex--;
                            if (pageIndex < 0)
                                pageIndex = pageCount;
                        }

                        if (GUILayout.Button(">", GUILayout.Width(20)))
                        {
                            pageIndex++;
                            if (pageIndex > pageCount)
                                pageIndex = 0;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No more pages");
                    }
                }, GUILayout.Height(24));

                var min = countPerPage * pageIndex;
                var max = pageIndex == pageCount ? array.arraySize : (pageIndex + 1) * countPerPage;

                //
                if (contentBorder)
                    VerticalEGL(new GUIStyle("Badge"), () =>
                    {
                        EditorGUILayout.Space(5);
                        for (var i = min; i < max; i++)
                        {
                            if (i >= array.arraySize || array.GetArrayElementAtIndex(i).objectReferenceValue == null) break;
                            var arrayElement = new SerializedObject(array.GetArrayElementAtIndex(i).objectReferenceValue);

                            drawElement?.Invoke(arrayElement, i);
                        }

                        EditorGUILayout.Space(5);
                    });
                else
                    for (var i = min; i < max; i++)
                    {
                        if (i >= array.arraySize || array.GetArrayElementAtIndex(i).objectReferenceValue == null) break;

                        var arrayElement = new SerializedObject(array.GetArrayElementAtIndex(i).objectReferenceValue);
                        drawElement?.Invoke(arrayElement, i);
                    }
            }

            return pageIndex;
        }

        public static int DrawArrayNative(int pageIndex, int countPerPage, SerializedProperty array, UnityAction<SerializedProperty> drawElement, bool contentBorder = false)
        {
            if (array.arraySize == 0)
            {
                if (GUILayout.Button("Add"))
                    array.arraySize++;
            }
            else
            {
                var pageCount = (array.arraySize - 1) / countPerPage;
                if (pageIndex > pageCount)
                    pageIndex = pageCount;

                HorizontalEGL(new GUIStyle("Badge"), () =>
                {
                    if (pageCount != 0)
                    {
                        EditorGUILayout.LabelField($"[Count : {array.arraySize}]");
                        EditorGUILayout.Space();
                        var lastPageIndex = pageIndex;
                        pageIndex = EditorGUILayout.IntField(pageIndex, GUILayout.Width(40f));
                        if (pageIndex > pageCount || pageIndex < 0)
                            pageIndex = lastPageIndex;
                        EditorGUILayout.LabelField($"/{pageCount}", GUILayout.Width(50f));

                        if (GUILayout.Button("<", GUILayout.Width(20f)))
                        {
                            pageIndex--;
                            if (pageIndex < 0)
                                pageIndex = pageCount;
                        }

                        if (GUILayout.Button(">", GUILayout.Width(20)))
                        {
                            pageIndex++;
                            if (pageIndex > pageCount)
                                pageIndex = 0;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No more pages");
                    }
                }, GUILayout.Height(24));

                var min = countPerPage * pageIndex;
                var max = pageIndex == pageCount ? array.arraySize : (pageIndex + 1) * countPerPage;

                //
                if (contentBorder)
                    VerticalEGL(new GUIStyle("Badge"), () =>
                    {
                        EditorGUILayout.Space(5);
                        for (var i = min; i < max; i++)
                        {
                            if (i >= array.arraySize) break;
                            var arrayElement = array.GetArrayElementAtIndex(i);

                            DrawReorderableArrayElementNative(array, i, () => { drawElement.Invoke(arrayElement); }, (count, index) =>
                            {
                                if ((index + 1) % countPerPage == 0) pageIndex++;
                            });
                        }

                        EditorGUILayout.Space(5);
                    });
                else
                    for (var i = min; i < max; i++)
                    {
                        if (i >= array.arraySize) break;
                        var arrayElement = array.GetArrayElementAtIndex(i);

                        DrawReorderableArrayElementNative(array, i, () => { drawElement.Invoke(arrayElement); }, (count, index) =>
                        {
                            if ((index + 1) % countPerPage == 0) pageIndex++;
                        });
                    }
            }

            return pageIndex;
        }

        #endregion

        #region DrawReorderableArrayElement

        public static bool DrawReorderableArrayElement(GameObject gameObject, string prefabPath, Transform prefabParent, UnityAction onDraw, bool hasContentBorder = true)
        {
            var result = true;
            var target = gameObject;

            VerticalEGL(hasContentBorder ? new GUIStyle("Badge") : GUIStyle.none, () =>
            {
                EditorGUILayout.Space(5);
                HorizontalEGL(new GUIStyle("Badge"), () =>
                {
                    EditorGUILayout.LabelField($"[{target.transform.GetSiblingIndex()}]", GUILayout.Width(80));
                    EditorGUILayout.Space();

                    if (target.transform.GetSiblingIndex() != 0 && GUILayout.Button("↑", GUILayout.Width(30)))
                        target.transform.SetSiblingIndex(target.transform.GetSiblingIndex() - 1);

                    if (target.transform.GetSiblingIndex() != target.transform.parent.childCount - 1 && GUILayout.Button("↓", GUILayout.Width(30)))
                        target.transform.SetSiblingIndex(target.transform.GetSiblingIndex() + 1);

                    if (GUILayout.Button("+", GUILayout.Width(30)))
                    {
                        var go = CreatePrefabAtPath(prefabParent, prefabPath);
                        go.transform.SetSiblingIndex(target.transform.GetSiblingIndex() + 1);
                    }

                    if (GUILayout.Button("-", GUILayout.Width(30)))
                        Undo.DestroyObjectImmediate(target.gameObject);

                    if (Selection.activeGameObject != gameObject && GUILayout.Button("●", GUILayout.Width(30)))
                        PingObject(gameObject);
                }, GUILayout.Height(24));

                if (target == null)
                    result = false;

                if (result)
                    VerticalEGL(new GUIStyle("Badge"), () =>
                    {
                        EditorGUILayout.Space(5);
                        onDraw?.Invoke();
                        EditorGUILayout.Space(5);
                    });

                EditorGUILayout.Space(5);
            });

            EditorGUILayout.Space(2f);
            return result;
        }

        public static bool DrawReorderableArrayElement(GameObject target, UnityAction onAdd, UnityAction onDraw, bool contentBorder = true)
        {
            var result = true;

            VerticalEGL(contentBorder ? new GUIStyle("Badge") : GUIStyle.none, () =>
            {
                EditorGUILayout.Space(5);
                HorizontalEGL(new GUIStyle("Badge"), () =>
                {
                    EditorGUILayout.LabelField($"[{target.transform.GetSiblingIndex()}]", GUILayout.Width(80));
                    EditorGUILayout.Space();

                    if (target.transform.GetSiblingIndex() != 0 && GUILayout.Button("↑", GUILayout.Width(30)))
                        target.transform.SetSiblingIndex(target.transform.GetSiblingIndex() - 1);

                    if (target.transform.GetSiblingIndex() != target.transform.parent.childCount - 1 && GUILayout.Button("↓", GUILayout.Width(30)))
                        target.transform.SetSiblingIndex(target.transform.GetSiblingIndex() + 1);

                    if (GUILayout.Button("+", GUILayout.Width(30))) onAdd?.Invoke();

                    if (GUILayout.Button("-", GUILayout.Width(30)))
                        Undo.DestroyObjectImmediate(target);

                    if (Selection.activeGameObject != target && GUILayout.Button("●", GUILayout.Width(30)))
                        PingObject(target);
                }, GUILayout.Height(24));

                if (target == null)
                    result = false;

                if (result)
                    VerticalEGL(new GUIStyle("Badge"), () =>
                    {
                        EditorGUILayout.Space(5);
                        onDraw?.Invoke();
                        EditorGUILayout.Space(5);
                    });

                EditorGUILayout.Space(5);
            });

            EditorGUILayout.Space(2f);
            return result;
        }

        public static bool DrawReorderableArrayElement(GameObject target, UnityAction onAdd, UnityAction onRemove, UnityAction onDraw, bool contentBorder = true)
        {
            var result = true;

            VerticalEGL(contentBorder ? new GUIStyle("Badge") : GUIStyle.none, () =>
            {
                EditorGUILayout.Space(5);
                HorizontalEGL(new GUIStyle("Badge"), () =>
                {
                    EditorGUILayout.LabelField($"[{target.transform.GetSiblingIndex()}]", GUILayout.Width(80));
                    EditorGUILayout.Space();

                    if (target.transform.GetSiblingIndex() != 0 && GUILayout.Button("↑", GUILayout.Width(30)))
                        target.transform.SetSiblingIndex(target.transform.GetSiblingIndex() - 1);

                    if (target.transform.GetSiblingIndex() != target.transform.parent.childCount - 1 && GUILayout.Button("↓", GUILayout.Width(30)))
                        target.transform.SetSiblingIndex(target.transform.GetSiblingIndex() + 1);

                    if (GUILayout.Button("+", GUILayout.Width(30))) onAdd?.Invoke();

                    if (GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        Undo.DestroyObjectImmediate(target);
                        onRemove?.Invoke();
                    }

                    if (Selection.activeGameObject != target && GUILayout.Button("●", GUILayout.Width(30)))
                        PingObject(target);
                }, GUILayout.Height(24));

                if (target == null)
                    result = false;

                if (result)
                    VerticalEGL(new GUIStyle("Badge"), () =>
                    {
                        EditorGUILayout.Space(5);
                        onDraw?.Invoke();
                        EditorGUILayout.Space(5);
                    });

                EditorGUILayout.Space(5);
            });

            EditorGUILayout.Space(2f);
            return result;
        }

        private static void DrawReorderableArrayElementNative(SerializedProperty array, int index, UnityAction onDraw, UnityAction<int, int> onAdded)
        {
            HorizontalEGL(new GUIStyle("Badge"), () =>
            {
                EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(40), GUILayout.ExpandWidth(false), GUILayout.MaxWidth(40));
                onDraw?.Invoke();

                if (index > 0)
                {
                    if (GUILayout.Button("↑", GUILayout.Width(20)))
                        array.MoveArrayElement(index, index - 1);
                }
                else
                {
                    GUILayout.Box("x", GUILayout.Width(20));
                }

                if (index < array.arraySize - 1)
                {
                    if (GUILayout.Button("↓", GUILayout.Width(20)))
                        array.MoveArrayElement(index, index + 1);
                }
                else
                {
                    GUILayout.Box("x", GUILayout.Width(20));
                }

                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    array.InsertArrayElementAtIndex(index);
                    onAdded?.Invoke(array.arraySize, index);
                }

                if (GUILayout.Button("-", GUILayout.Width(20))) array.DeleteArrayElementAtIndex(index);
            });

            EditorGUILayout.Space(2f);
        }

        #endregion

        #region Horizontal

        public static void HorizontalEGL(GUIStyle style, UnityAction action, bool isSpace = false, float space = 5, bool isPadding = false, float padding = 5)
        {
            if (isSpace)
                EditorGUILayout.Space(space);
            EditorGUILayout.BeginHorizontal(style);

            if (isPadding)
                EditorGUILayout.Space(padding);
            action?.Invoke();
            if (isPadding)
                EditorGUILayout.Space(padding);
            EditorGUILayout.EndHorizontal();
            if (isSpace)
                EditorGUILayout.Space(space);
        }

        public static void HorizontalEGL(GUIStyle style, UnityAction action, params GUILayoutOption[] option)
        {
            EditorGUILayout.BeginHorizontal(style, option);
            action?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static void HorizontalEGL(UnityAction action, params GUILayoutOption[] option)
        {
            EditorGUILayout.BeginHorizontal(option);

            action?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        public static void HorizontalEGL(UnityAction action, float space = 5, params GUILayoutOption[] option)
        {
            EditorGUILayout.Space(space);
            EditorGUILayout.BeginHorizontal(option);
            action?.Invoke();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(space);
        }

        #endregion

        #region Vertical

        // ReSharper disable Unity.PerformanceAnalysis
        public static void VerticalEGL(UnityAction action, params GUILayoutOption[] option)
        {
            EditorGUILayout.BeginVertical(option);
            action?.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void VerticalEGL(GUIStyle style, UnityAction action, params GUILayoutOption[] option)
        {
            EditorGUILayout.BeginVertical(style, option);
            action?.Invoke();
            EditorGUILayout.EndVertical();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static void VerticalEGL(GUIStyle style, UnityAction action)
        {
            EditorGUILayout.BeginVertical(style);
            action?.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void VerticalEGL(GUIStyle style, UnityAction action, bool isSpace = false, float space = 5, bool isPadding = false, float padding = 5, params GUILayoutOption[] option)
        {
            if (isSpace)
                EditorGUILayout.Space(space);
            EditorGUILayout.BeginVertical(style, option);
            if (isPadding)
                EditorGUILayout.Space(padding);
            action?.Invoke();
            if (isPadding)
                EditorGUILayout.Space(padding);
            EditorGUILayout.EndVertical();
            if (isSpace)
                EditorGUILayout.Space(space);
        }

        public static void VerticalEGLTitled(string title, UnityAction action, params GUILayoutOption[] layout)
        {
            VerticalEGL(() =>
            {
                TitleLabelField(title);
                VerticalEGL(new GUIStyle("Badge"), () =>
                {
                    // Invoke
                    SpaceArea(() => { action?.Invoke(); }, true);
                }, layout);
                EditorGUILayout.Space(5);
            }, layout);
        }

        #endregion

        public static Object ObjectField(string label, int labelWidth, Object obj, Type type, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            HorizontalEGL(() =>
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
                obj = EditorGUILayout.ObjectField(obj, type, allowSceneObjects, options);
            });
            return obj;
        }

        // public static void Line(LineType type = LineType.Horizontal, float thickness = 2f, float topDownMargin = 5f, float height = 0, UnityAction onClick = null)
        // {
        //     EditorGUILayout.Space(topDownMargin);
        //
        //     switch (type)
        //     {
        //         case LineType.Horizontal:
        //             if (GUILayout.Button("", GUILayout.Height(thickness), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false)))
        //                 onClick?.Invoke();
        //             break;
        //         case LineType.Vertical:
        //             if (GUILayout.Button("", GUILayout.Width(thickness), GUILayout.Height(height), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
        //                 onClick?.Invoke();
        //             break;
        //     }
        //
        //     EditorGUILayout.Space(topDownMargin);
        // }

        public static void Line(LineType type = LineType.Horizontal, float thickness = 1f, float space = 6)
        {
            if (type == LineType.Horizontal)
            {
                GUILayout.Label("", GUILayout.Height(space), GUILayout.ExpandWidth(true));
                var lastRect = GUILayoutUtility.GetLastRect();
                GUI.Box(new Rect(lastRect.x, lastRect.y + space / 2, lastRect.width, thickness), "");
                GUI.Box(new Rect(lastRect.x, lastRect.y + space / 2, lastRect.width, thickness), "");
            }
            else if (type == LineType.Vertical)
            {
                GUILayout.Label("", GUILayout.Width(space), GUILayout.ExpandHeight(true));
                var lastRect = GUILayoutUtility.GetLastRect();
                GUI.Box(new Rect(lastRect.x + space / 2, lastRect.y, thickness, lastRect.height), "");
                GUI.Box(new Rect(lastRect.x + space / 2, lastRect.y, thickness, lastRect.height), "");
            }
        }

        public static object CloneObject(object o)
        {
            var t = o.GetType();
            var properties = t.GetProperties();
            var p = t.InvokeMember("", BindingFlags.CreateInstance, null, o, null);
            foreach (var pi in properties)
            {
                if (!pi.CanWrite) continue;
                var value = pi.GetValue(o, null);
                pi.SetValue(p, value, null);
            }

            return p;
        }

        public static void DrawEditorTitle(string label, string version = "v1.0")
        {
            var style = (GUIStyle)CloneObject(GUI.skin.label);

            SpaceArea(() =>
            {
                style.fontSize = 20;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField(label, style, GUILayout.Height(25));

                // style.fontStyle = FontStyle.Normal;
                // style.fontSize = 12;

                // EditorGUILayout.LabelField(version + " [Yueby]", style);

                // var rect = GUILayoutUtility.GetLastRect();
                // rect.y += EditorGUIUtility.singleLineHeight;

                // EditorGUI.LabelField(rect, version, style);
            }, true, 10);
        }

        public static void TitleLabelField(string label, params GUILayoutOption[] options)
        {
            var style = (GUIStyle)CloneObject(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleLeft;
            EditorGUILayout.LabelField(label, style, options);
        }

        public static Object ObjectFieldVertical(Object target, string label, Type type, bool allowSceneObjects = true)
        {
            VerticalEGL(() =>
            {
                EditorGUILayout.LabelField(label);
                target = EditorGUILayout.ObjectField(target, type, allowSceneObjects);
                EditorGUILayout.Space(5);
            });
            return target;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static void SpaceArea(UnityAction action, bool isMargin = false, float topDownMargin = 5)
        {
            if (isMargin)
                EditorGUILayout.Space(topDownMargin);
            else
                EditorGUILayout.Space();
            action?.Invoke();
            if (isMargin)
                EditorGUILayout.Space(topDownMargin);
            else
                EditorGUILayout.Space();
        }

        /*获取当前脚本的文件夹路径，参数为脚本的名字*/
        public static string GetScriptDirectory(string scriptName)
        {
            var guidPathArray = AssetDatabase.FindAssets(scriptName);
            // foreach (var guidPath in guidPathArray)
            // {
            //     Debug.Log(AssetDatabase.GUIDToAssetPath(guidPath));
            // }

            // if (guidPathArray.Length > 1)
            // {
            //     Debug.LogError("有同名文件" + scriptName + "获取路径失败");
            //     return null;
            // }

            //将字符串中得脚本名字和后缀统统去除掉
            var result = AssetDatabase.GUIDToAssetPath(guidPathArray[0]).Replace(@"/" + scriptName + ".cs", "");
            return result;
        }

        public static string GetParentDirectory(string directoryPath)
        {
            var splitPath = directoryPath.Split('/');
            var result = string.Empty;
            for (var i = 0; i < splitPath.Length - 1; i++)
                if (i == splitPath.Length - 2)
                    result += splitPath[i];
                else
                    result += splitPath[i] + "/";

            return result;
        }

        public static async void WaitToDo(int ms, string taskName, UnityAction action)
        {
            if (WaitToDoList.Contains(taskName))
            {
                Debug.Log("Already have " + taskName);
                return;
            }

            WaitToDoList.Add(taskName);
            await Task.Yield();
            await Task.Delay(ms);
            action?.Invoke();
            WaitToDoList.Remove(taskName);
        }

        private static readonly List<string> WaitToDoList = new List<string>();

        public static void DrawChildElement(int type, UnityAction action)
        {
            HorizontalEGL(() =>
            {
                var label = type == 0 ? "├───○" : "└───○";

                EditorGUILayout.LabelField(label, GUILayout.Width(50));
                VerticalEGL(() => { action?.Invoke(); });
            });

            if (type == 1)
                EditorGUILayout.Space();
        }

        public static T AddChildAsset<T>(Object targetAsset, bool isAutoRefresh = true) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = $"{typeof(T).Name}";
            AssetDatabase.AddObjectToAsset(asset, targetAsset);

            if (isAutoRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return asset;
        }

        public static void SaveAndRefreshAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RemoveChildAsset(Object item, bool isAutoRefresh = true)
        {
            if (item == null) return;

            AssetDatabase.RemoveObjectFromAsset(item);
            Object.DestroyImmediate(item, true);

            if (isAutoRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static void MoveFolderFromPath(ref string folderPath, string folderName)
        {
            var path = EditorUtility.OpenFolderPanel("选择保存路径", folderPath, "");
            if (string.IsNullOrEmpty(path) || path == folderPath) return;

            var targetPath = FileUtil.GetProjectRelativePath(path) + "/" + folderName;
            if (targetPath != folderPath)
            {
                var lastPath = folderPath;
                if (!Directory.Exists(lastPath))
                    return;

                if (Directory.Exists(targetPath))
                {
                    if (Directory.GetFiles(targetPath).Length > 0)
                        Debug.Log("Target Directory:" + targetPath + " Not Empty!");
                    else
                        Directory.Delete(targetPath, true);
                }

                FileUtil.MoveFileOrDirectory(lastPath, targetPath);

                if (File.Exists(lastPath + ".meta"))
                    File.Delete(lastPath + ".meta");

                folderPath = targetPath;

                AssetDatabase.Refresh();


                PingProject(targetPath);
            }
        }
    }

    public enum LineType
    {
        Horizontal,
        Vertical
    }
}