using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEngine.Animations;
using Yueby.AvatarTools.MAConstraintSourceBind;

[assembly: ExportsPlugin(typeof(ConstraintSourceBindPlugin))]
namespace Yueby.AvatarTools.MAConstraintSourceBind
{

    public class ConstraintSourceBindPlugin : Plugin<ConstraintSourceBindPlugin>
    {
        public override string DisplayName => "MA Constraint Source Bind";

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Build ConstraintSourceBind", ctx =>
                {
                    if (ctx.AvatarRootObject.GetComponentsInChildren<ConstraintSourceBind>().Length > 0)
                    {

                        ConstraintSourceBuilder.Build(ctx.AvatarRootObject);
                    }
                });
        }

    }

    public class ConstraintSourceBuilder
    {
        public static void Build(GameObject avatarRootObject)
        {
            var constraintSourceBindArray = avatarRootObject.GetComponentsInChildren<ConstraintSourceBind>();
            foreach (var constraintSourceBind in constraintSourceBindArray)
            {
                var constraintArray = constraintSourceBind.GetComponents<IConstraint>();
                foreach (var constraint in constraintArray)
                {
                    foreach (var info in constraintSourceBind.SourceInfos)
                    {
                        var source = new ConstraintSource();
                        if (info.UseRoot)
                            source.sourceTransform = avatarRootObject.transform;
                        else
                            source.sourceTransform = info.CustomSource;

                        source.weight = info.Weight;
                        constraint.AddSource(source);
                    }
                    constraint.weight = constraintSourceBind.Weight;
                    if (constraintSourceBind.ActiveSource)
                        constraint.constraintActive = true;
                }
            }
        }
    }
}