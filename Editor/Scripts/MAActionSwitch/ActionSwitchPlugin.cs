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
                .Run("Build ActionSwitch", ctx =>
                {
                    if (ctx.AvatarRootObject.GetComponentsInChildren<ActionSwitch>().Length > 0)
                    {
                        ActionSwitchBuilder.GeneratePath();
                        ActionSwitchBuilder.Build(ctx.AvatarRootObject);
                    }
                });
        }
    }

    internal static class ActionSwitchBuilder
    {
        private static string _toolLabel = "MA Action Switch/";
        private static string AssetsPath => "Packages/yueby.tools.avatar-tools/Editor/Assets/MAActionSwitch";
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
            var parameterName = _toolLabel + actionSwitch.Name;
            var emptyClip = new AnimationClip();
            var animatorController = AnimatorController.CreateAnimatorControllerAtPath($"{AssetsPath}/Generated/{actionSwitch.name}.controller");

            animatorController.AddParameter(name: parameterName, AnimatorControllerParameterType.Int);

            var layer = new AnimatorControllerLayer
            {
                name = parameterName,
                defaultWeight = 1,
                stateMachine = new AnimatorStateMachine
                {
                    name = parameterName
                }
            };

            var waitState = CreateAnimatorState(layer.stateMachine, "Wait", emptyClip, writeDefault);
            var readyState = CreateAnimatorState(layer.stateMachine, "Ready", emptyClip, writeDefault);
            var endState = CreateAnimatorState(layer.stateMachine, "End", emptyClip, writeDefault);
            var overState = CreateAnimatorState(layer.stateMachine, "Over", emptyClip, writeDefault);

            AddTrackingControlStateBehaviour(readyState, VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation);
            AddTrackingControlStateBehaviour(endState, VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking);
            AddPlayableLayerControl(readyState, 1);
            AddPlayableLayerControl(endState, 0);

            overState.AddStateMachineBehaviour<VRCAvatarParameterDriver>().parameters.Add(new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter
            {
                name = parameterName,
                value = 0,
                type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set
            });
            // var driver = endState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            // driver.parameters.Add(new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter
            // {
            //     name = parameterName,
            //     value = 0,
            //     type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set
            // });

            var anyToOverTransition = layer.stateMachine.AddAnyStateTransition(overState);
            anyToOverTransition.AddCondition(AnimatorConditionMode.NotEqual, 0, parameterName);
            anyToOverTransition.AddCondition(AnimatorConditionMode.If, 1, "Seated");
            anyToOverTransition.hasExitTime = false;
            anyToOverTransition.duration = transitionDuration;

            var overToEndTransition = overState.AddTransition(endState);
            overToEndTransition.hasExitTime = true;
            overToEndTransition.exitTime = 0.9f;
            overToEndTransition.duration = transitionDuration;
            overToEndTransition.hasFixedDuration = true;

            layer.stateMachine.defaultState = waitState;
            AddIntTransition(waitState, readyState, AnimatorConditionMode.NotEqual, parameterName, 0, transitionDuration);
            var exitTransition = endState.AddExitTransition();
            exitTransition.hasExitTime = true;
            exitTransition.exitTime = 0;
            exitTransition.duration = transitionDuration;

            animatorController.AddLayer(layer);
            for (int i = 0; i < actionSwitch.Actions.Count; i++)
            {
                ActionElement actionElement = actionSwitch.Actions[i];
                var actionState = CreateAnimatorState(layer.stateMachine, actionElement.Name, actionElement.Clip, writeDefault);

                AddIntTransition(readyState, actionState, AnimatorConditionMode.Equals, parameterName, i + 1, transitionDuration);
                AddIntTransition(actionState, endState, AnimatorConditionMode.NotEqual, parameterName, i + 1, transitionDuration);
                // add anyState transition
                // var transition = layer.stateMachine.AddAnyStateTransition(state);
                // transition.AddCondition(AnimatorConditionMode.Equals, i + 1, parameterName);
                // transition.duration = transitionDuration;
            }

            animatorController.RemoveLayer(0);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return animatorController;

        }

        private static void AddTrackingControlStateBehaviour(AnimatorState state, VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType type)
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

        private static AnimatorState CreateAnimatorState(AnimatorStateMachine stateMachine, string name, AnimationClip clip, bool writeDefault)
        {
            var state = stateMachine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = writeDefault;
            return state;
        }

        private static AnimatorState AddPlayableLayerControl(AnimatorState state, float weight)
        {
            var playable = state.AddStateMachineBehaviour<VRCPlayableLayerControl>();
            playable.layer = VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.Action;
            playable.goalWeight = weight;
            return state;
        }

        private static AnimatorState AddIntTransition(AnimatorState state, AnimatorState destination, AnimatorConditionMode mode, string parameterName, int value, float duration, bool hasExitTime = false)
        {
            var transition = state.AddTransition(destination);
            transition.AddCondition(mode, value, parameterName);
            transition.duration = duration;
            transition.hasExitTime = hasExitTime;
            transition.hasFixedDuration = true;
            return destination;
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
            var parameterName = _toolLabel + actionSwitch.Name;
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
            maMenuItem.Control.name = parameterName;

            CreateChild("None", 0, maMenuItem.transform);

            for (int i = 0; i < actionSwitch.Actions.Count; i++)
            {
                ActionElement actionElement = actionSwitch.Actions[i];
                CreateChild(actionElement.Name, i + 1, maMenuItem.transform);
            }

            var mergeAnimator = actionSwitch.GetComponent<ModularAvatarMergeAnimator>() ?? actionSwitch.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            mergeAnimator.animator = animator;
            mergeAnimator.layerType = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.Action;
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