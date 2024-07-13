using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other
{
    public class PhysBoneCheck : EditorWindow
    {
        public Transform Target;
        public GameObject[] SelectedClothes;
        public bool IsUseByOtherTools;
        private Vector2 _checkPos;
        private int _checkType;
        private Dictionary<string, List<PBCheckParameter>> _pbCheckedDic = new Dictionary<string, List<PBCheckParameter>>();
        private Vector2 _selectPos;
        private int _toolBarIndex;

        private void OnEnable()
        {
            if (!IsUseByOtherTools)
            {
                FindArmature();
                Selection.selectionChanged += Repaint;
            }
        }

        private void OnDisable()
        {
            if (!IsUseByOtherTools) Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            if (!IsUseByOtherTools)
                SelectedClothes = Selection.gameObjects;

            var isSelectedObject = SelectedClothes.Length > 0;

            EditorUI.DrawEditorTitle("动骨组件检查");
            EditorUI.VerticalEGLTitled("配置", () =>
            {
                EditorGUI.BeginChangeCheck();
                _checkType = EditorUI.PopupVertical("检测类型", _checkType, 80, new[] { "全部", "骨架", "自身" });

                if (EditorGUI.EndChangeCheck())
                {
                    if (_checkType == 2)
                        Target = null;
                    else
                        FindArmature();
                }

                EditorGUILayout.HelpBox("全部：检测骨架中与选中列表所有对象自身的PhysBone组件\n骨架：仅检测骨架中的PhysBone组件\n自身：仅检测选中列表所有对象自身的PhysBone组件\n\n检测VRCPhysBone、VRCPhysBoneCollider、VRCContactReceiver/Sender", MessageType.Info);

                if (_checkType != 2)
                {
                    Target = (Transform)EditorUI.ObjectField("骨架（Armature）", 100, Target, typeof(Transform), true);

                    if (IsUseByOtherTools)
                        EditorUI.DisableGroupEGL(IsUseByOtherTools, () =>
                        {
                            EditorUI.Radio(IsUseByOtherTools, "被其他工具使用");
                            EditorGUILayout.HelpBox("当显示此选项时，该工具被其他工具使用", MessageType.Info);
                        });
                }
            });

            EditorUI.VerticalEGLTitled("操作", () =>
            {
                if (GUILayout.Button("检查") && SelectedClothes.Length > 0) Check(SelectedClothes, Target);
            });

            EditorUI.VerticalEGLTitled("结果", () =>
            {
                _toolBarIndex = GUILayout.Toolbar(_toolBarIndex, new[] { "选中列表", "检查结果" });
                EditorGUILayout.Space();
                if (_toolBarIndex == 0)
                {
                    if (isSelectedObject)
                        _selectPos = EditorUI.ScrollViewEGL(() =>
                        {
                            foreach (var show in SelectedClothes) EditorGUILayout.ObjectField(show, typeof(GameObject), true);
                        }, _selectPos, GUILayout.Height(200));
                    else
                        EditorGUILayout.HelpBox("请选择对象！", MessageType.Info);
                }
                else
                {
                    if (_pbCheckedDic != null && _pbCheckedDic.Count > 0)
                        _checkPos = EditorUI.ScrollViewEGL(() =>
                        {
                            foreach (var pbCheck in _pbCheckedDic)
                            {
                                if (pbCheck.Value.Count == 0) continue;

                                var title = $"<b><i>{pbCheck.Key}</i></b>";
                                var style = UnityEngine.GUI.skin.label;
                                style.richText = true;
                                style.alignment = TextAnchor.MiddleLeft;

                                EditorUI.HorizontalEGL(() =>
                                {
                                    EditorUI.HorizontalEGL("Badge", () => { EditorGUILayout.LabelField($"{pbCheck.Value.Count}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(25), GUILayout.Height(18)); }, GUILayout.Width(25), GUILayout.Height(18));
                                    EditorGUILayout.LabelField(title, style);
                                });

                                for (var i = 0; i < pbCheck.Value.Count; i++)
                                {
                                    var type = i == pbCheck.Value.Count - 1 ? 1 : 0;

                                    var parameter = pbCheck.Value[i];
                                    EditorUI.DrawChildElement(type, () =>
                                    {
                                        EditorUI.HorizontalEGL(() =>
                                        {
                                            EditorGUILayout.LabelField($"{parameter.Tag} : {parameter.Target.GetType().Name}");
                                            EditorGUILayout.ObjectField(parameter.Target, typeof(GameObject), true);
                                        });
                                    });
                                }
                            }
                        }, _checkPos, GUILayout.Height(200));
                    else
                        EditorGUILayout.HelpBox("未找到骨骼！", MessageType.Info);

                    if (_toolBarIndex == 1)
                    {
                        if (_pbCheckedDic.Count <= 0) return;

                        var style = UnityEngine.GUI.skin.label;
                        style.richText = true;

                        EditorUI.VerticalEGL(new GUIStyle("Badge"), () =>
                        {
                            EditorGUILayout.LabelField($"找到共<b>{GetDicListCount()}</b>个PhysBone组件!", style);
                            EditorGUILayout.LabelField(GetResultString(), style);
                        }, true);
                    }
                }
            });
        }

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/PhysBone Check", false, 20)]
        private static void Open()
        {
            var window = GetWindow<PhysBoneCheck>();
            window.titleContent = new GUIContent("动骨组件检查");
            window.minSize = new Vector2(500, 650);
        }

        private void FindArmature()
        {
            var avatar = FindObjectsOfType<VRCAvatarDescriptor>()[0];
            Target = avatar.transform.Find("Armature");
            if (Target == null)
                Target = avatar.transform.Find("armature");
            if (Target == null)
                Debug.Log("[PhysBone Check]：未自动找到Armature，请手动放置！");
        }

        private int GetDicListCount()
        {
            var count = 0;
            foreach (var i in _pbCheckedDic.Values) count += i.Count;
            return count;
        }

        private void Check(GameObject[] gameObjects, Transform target)
        {
            if (_pbCheckedDic == null)
                _pbCheckedDic = new Dictionary<string, List<PBCheckParameter>>();
            else
                _pbCheckedDic.Clear();

            // 获取Target下所有的Transform
            if (target != null)
            {
                List<Transform> targetGos;
                targetGos = target.GetComponentsInChildren<Transform>(true).ToList();

                // 遍历选中的衣服GameObjects
                foreach (var selection in gameObjects)
                {
                    // 获取当前选中衣服的所有带其后缀名的骨骼
                    var gos = targetGos.Where(go =>
                    {
                        if (go != null && go.name.Contains(selection.name))
                            return go;
                        return false;
                    }).ToList();

                    // 如果骨骼数量大于0，就检测
                    if (gos.Count > 0)
                        // 遍历骨骼
                        foreach (var t in gos.Where(t => t != null))
                        {
                            var list = AddPBCheck(t, selection, false);
                            if (list.Count != 0)
                                AddRangeToDic(selection.name, list);
                        }

                    if (_checkType == 0)
                    {
                        var selfList = AddPBCheck(selection.transform, selection, true);

                        if (selfList.Count != 0)
                            AddRangeToDic(selection.name, selfList);
                    }
                }
            }
            else
            {
                foreach (var selection in gameObjects)
                {
                    var list = AddPBCheck(selection.transform, selection, true);
                    if (list.Count != 0)
                        AddRangeToDic(selection.name, list);
                }
            }

            if (EditorUtility.DisplayDialog("提示", "检查完成！", "OK")) _toolBarIndex = 1;
        }

        private void AddRangeToDic(string key, List<PBCheckParameter> list)
        {
            if (!_pbCheckedDic.ContainsKey(key))
                _pbCheckedDic.Add(key, list);
            else
                _pbCheckedDic[key].AddRange(list);
        }

        private List<PBCheckParameter> AddPBCheck(Transform t, GameObject selection, bool isSelfCheck)
        {
            var list = new List<PBCheckParameter>();
            var physBones = t.GetComponentsInChildren<VRCPhysBoneBase>(true);
            var physBoneColliders = t.GetComponentsInChildren<VRCPhysBoneColliderBase>(true);
            var contacts = t.GetComponentsInChildren<ContactBase>(true);

            var tag = isSelfCheck ? "[Self]" : "[Bone]";
            foreach (var pb in physBones)
            {
                var parameter = new PBCheckParameter(tag, pb);
                if (!PBCheckContains(list, parameter))
                    list.Add(parameter);
            }

            foreach (var pc in physBoneColliders)
            {
                var parameter = new PBCheckParameter(tag, pc);
                if (!PBCheckContains(list, parameter))
                    list.Add(parameter);
            }

            foreach (var ct in contacts)
            {
                var parameter = new PBCheckParameter(tag, ct);
                if (!PBCheckContains(list, parameter))
                    list.Add(parameter);
            }

            return list;
        }

        private string GetResultString()
        {
            var pbCount = 0;
            var pcCount = 0;
            var ctCount = 0;

            foreach (var pair in _pbCheckedDic)
            foreach (var item in pair.Value)
                switch (item.Target)
                {
                    case VRCPhysBoneBase _:
                        pbCount++;
                        break;
                    case VRCPhysBoneColliderBase _:
                        pcCount++;
                        break;
                    case ContactBase _:
                        ctCount++;
                        break;
                }

            return $"PhysBones: <b>{pbCount}</b>, PhysBoneCollider: <b>{pcCount}</b>, Contacts: <b>{ctCount}</b>";
        }

        public bool PBCheckContains(List<PBCheckParameter> list, PBCheckParameter pbCheckParameter)
        {
            foreach (var pbChecked in list)
                if (pbChecked.Target.Equals(pbCheckParameter.Target))
                    return true;

            return false;
        }
    }

    public class PBCheckParameter
    {
        public string Tag;
        public Component Target;

        public PBCheckParameter(string tag, Component target)
        {
            Tag = tag;
            Target = target;
        }

        public GameObject GameObject => Target.gameObject;
    }
}