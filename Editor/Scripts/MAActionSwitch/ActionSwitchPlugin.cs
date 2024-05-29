using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Yueby.AvatarTools.MAActionSwitch;

[assembly: ExportsPlugin(typeof(ActionSwitchPlugin))]
namespace Yueby.AvatarTools.MAActionSwitch
{

    public class ActionSwitchPlugin : Plugin<ActionSwitchPlugin>
    {
        public override string DisplayName => "MA Action Switch";

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Build AddAction", ctx =>
                {
                    if (ctx.AvatarRootObject.GetComponentsInChildren<ActionSwitch>().Length > 0)
                    {
                        ActionSwitchBuilder.GeneratePath();
                        ActionSwitchBuilder.Build(ctx.AvatarRootObject);
                    }
                });
        }
    }

    class ActionSwitchBuilder
    {

        private static string AssetsPath => "Packages/com.yueby.avatartools/Editor/Assets/MAActionSwitch";
        private static string EmptyClipPath => AssetsPath + "/Animations/Empty.anim";

        public static void Build(GameObject avatarRootObject)
        {
            var actionSwitches = avatarRootObject.GetComponentsInChildren<ActionSwitch>().ToList();

            while (actionSwitches.Count > 0)
            {
                var actionSwitch = actionSwitches.First();

                var animator = BuildAnimator(actionSwitch);
                MakeMAComponents(avatarRootObject, animator, actionSwitch);

                actionSwitches.Remove(actionSwitch);
            }
        }

        private static RuntimeAnimatorController BuildAnimator(ActionSwitch actionSwitch)
        {
            var transitionDuration = 0.1f;
            var writeDefault = true;
            var parameterName = actionSwitch.Name;
            var animatorController = AnimatorController.CreateAnimatorControllerAtPath($"{AssetsPath}/Generated/{actionSwitch.name}.controller");
            animatorController.AddParameter(name: parameterName, AnimatorControllerParameterType.Int);

            var layer = new AnimatorControllerLayer
            {
                name = actionSwitch.Name,
                defaultWeight = 1,
                stateMachine = new AnimatorStateMachine
                {
                    name = actionSwitch.Name
                }
            };

            var defaultState = layer.stateMachine.AddState("Default");
            var defaultStateTransition = layer.stateMachine.AddAnyStateTransition(defaultState);
            defaultStateTransition.AddCondition(AnimatorConditionMode.Equals, 0, parameterName);
            defaultState.writeDefaultValues = writeDefault;
            defaultState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(EmptyClipPath);
            defaultStateTransition.duration = transitionDuration;
            AddTrackingControlStateBehaviour(defaultState, VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking);
            animatorController.AddLayer(layer);
            for (int i = 0; i < actionSwitch.Actions.Count; i++)
            {
                ActionElement actionElement = actionSwitch.Actions[i];
                var state = layer.stateMachine.AddState(actionElement.Name);
                state.motion = actionElement.Clip;
                state.writeDefaultValues = writeDefault;
                AddTrackingControlStateBehaviour(state, VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation);

                // add anyState transition
                var transition = layer.stateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Equals, i + 1, parameterName);
                transition.duration = transitionDuration;
            }

            animatorController.RemoveLayer(0);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return animatorController;

            void AddTrackingControlStateBehaviour(AnimatorState state, VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType type)
            {
                var control = state.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
                control.trackingEyes = type;
                control.trackingHead = type;
                control.trackingLeftHand = type;
                control.trackingRightHand = type;
                control.trackingMouth = type;
                control.trackingHip = type;
                control.trackingLeftFingers = type;
                control.trackingRightFingers = type;
                control.trackingLeftFoot = type;
                control.trackingRightFoot = type;
            }
        }

        public static void GeneratePath()
        {

            if (!AssetDatabase.IsValidFolder(AssetsPath + "/Generated"))
            {
                AssetDatabase.CreateFolder(AssetsPath, "Generated");
            }
        }

        private static void MakeMAComponents(GameObject avatarRootObject, RuntimeAnimatorController animator, ActionSwitch actionSwitch)
        {
            var parameterName = actionSwitch.Name;
            var maParam = avatarRootObject.GetComponent<ModularAvatarParameters>() ?? avatarRootObject.AddComponent<ModularAvatarParameters>();
            maParam.parameters.Add(new ParameterConfig
            {
                nameOrPrefix = parameterName,
                remapTo = "",
                internalParameter = false,
                isPrefix = false,
                syncType = ParameterSyncType.Int,
                localOnly = false,
                defaultValue = 0,
                saved = false
            });

            var maMenuItem = actionSwitch.GetComponent<ModularAvatarMenuItem>() ?? actionSwitch.gameObject.AddComponent<ModularAvatarMenuItem>();
            maMenuItem.Control.name = actionSwitch.Name;

            CreateChild("None", 0, maMenuItem.transform);

            for (int i = 0; i < actionSwitch.Actions.Count; i++)
            {
                ActionElement actionElement = actionSwitch.Actions[i];
                CreateChild(actionElement.Name, i + 1, maMenuItem.transform);
            }

            var mergeAnimator = actionSwitch.GetComponent<ModularAvatarMergeAnimator>() ?? actionSwitch.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            mergeAnimator.animator = animator;
            mergeAnimator.layerType = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Base;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            mergeAnimator.matchAvatarWriteDefaults = true;

            ModularAvatarMenuItem CreateChild(string name, int index, Transform parent)
            {
                var child = new GameObject(name);
                child.transform.SetParent(maMenuItem.transform);
                var maMenuItemChild = child.AddComponent<ModularAvatarMenuItem>();

                maMenuItemChild.Control.name = name;
                maMenuItemChild.Control.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle;
                maMenuItemChild.Control.value = index;
                maMenuItemChild.Control.parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter
                {
                    name = parameterName
                };

                return maMenuItemChild;
            }
        }
    }
}