using UnityEditor;
using UnityEngine;
using Yueby.Utils;

namespace Yueby.AvatarTools.WorldConstraints
{
    public class WorldSpaceConstraintEditorWindow : EditorWindow
    {
        private static readonly WcLocalization _localization = new WcLocalization();
        private static WorldSpaceConstraintEditorWindow _window;
        private bool _isAutoRename = true;

        private bool _isHideOption = true;

        private bool _isUseParent;
        private Transform _parentTransform;
        private GameObject _prefab;
        private GameObject _targetItem;

        private void OnEnable()
        {
            if (_prefab == null)
                _prefab = AssetDatabase.LoadMainAssetAtPath("Packages/com.yueby.avatartools/Editor/Assets/WorldConstraints/Prefabs/WorldSpaceItem.prefab") as GameObject;
        }

        private void OnGUI()
        {
            if (_window == null)
                _window = GetWindow<WorldSpaceConstraintEditorWindow>();
            _window.titleContent = new GUIContent(_localization.Get("title_main_label"));

            YuebyUtil.DrawEditorTitle(_localization.Get("title_main_label"));
            _localization.DrawLanguageUI();

            DrawStandard();
            DrawOption();
        }

        [MenuItem("Tools/YuebyTools/Avatar/WorldSpaceConstraint")]
        public static void OpenWindow()
        {
            if (_window == null)
                _window = GetWindow<WorldSpaceConstraintEditorWindow>();
            _window.titleContent = new GUIContent(_localization.Get("title_main_label"));
            _window.minSize = new Vector2(500, 500);
        }

        private void DrawStandard()
        {
            YuebyUtil.VerticalEGLTitled(_localization.Get("standard_title_label"), () =>
            {
                YuebyUtil.HorizontalEGL(() =>
                {
                    YuebyUtil.DrawCheckChanged(() =>
                    {
                        // Draw
                        _targetItem = (GameObject)YuebyUtil.ObjectFieldVertical(_targetItem, _localization.Get("standard_target_item_field"), typeof(GameObject));

                        if (_targetItem && !_targetItem.activeSelf)
                            _targetItem.SetActive(true);
                    }, () =>
                    {
                        // Action
                        _isHideOption = _targetItem is null;
                    });
                });
            });
        }

        private void DrawOption()
        {
            if (_isHideOption) return;
            YuebyUtil.VerticalEGLTitled(_localization.Get("option_title_label"), () =>
            {
                _isUseParent = YuebyUtil.Radio(_isUseParent, _localization.Get("option_use_parent_radio"));

                if (_isUseParent)
                    YuebyUtil.DrawChildElement(1, () =>
                    {
                        //
                        _parentTransform = (Transform)YuebyUtil.ObjectField(_localization.Get("option_parent_field"), 60, _parentTransform, typeof(Transform), true);
                    });

                _isAutoRename = YuebyUtil.Radio(_isAutoRename, _localization.Get("option_auto_rename_radio"));
            });

            if (GUILayout.Button(_localization.Get("setup_apply_button"))) Apply();
        }

        private void Apply()
        {
            Transform targetParent = null;
            if (_isUseParent)
                targetParent = _parentTransform;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(_prefab, targetParent);
            var clonedTarget = Instantiate(_targetItem);
            clonedTarget.name = _targetItem.name;

            _targetItem.SetActive(false);

            if (_isAutoRename)
            {
                go.name = clonedTarget.name + "_WorldSpaceConstraint";
                clonedTarget.name = "Item";
            }

            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Undo.RegisterCreatedObjectUndo(go, "Create WorldSpaceConstraint Prefab");
            var worldSpaceItem = go.GetComponent<WorldSpaceItem>();
            worldSpaceItem.SetParent(clonedTarget);
            YuebyUtil.PingObject(clonedTarget);

            _targetItem = null;
        }
    }
}