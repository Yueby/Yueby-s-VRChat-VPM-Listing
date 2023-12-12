using System.Collections.Generic;
using Yueby.Utils;

public class VRCExParameterLocalization : Localization
{
    public VRCExParameterLocalization()
    {
        Languages = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "中文", new Dictionary<string, string>
                {
                    { "parameters", "参数" },
                    { "parameters_name", "名字" },
                    { "parameters_type", "类型" },
                    { "parameters_default", "默认值" },
                    { "parameters_saved", "可保存" },
                    { "parameters_synced", "可同步" },
                    { "parameters_out_of_memory", "使用了过多的参数内存，删除无用参数或使用使用内存占用较少的bool参数。" },
                    { "parameters_tip_1", "只有这里定义的参数才能被ExpressionsMenu使用，在所有可播放Layer之间同步，并通过网络同步到远程客户端。" },
                    { "parameters_tip_2", "参数名称和类型应与一个或多个动画控制器上定义的参数相匹配。" },
                    { "parameters_tip_3", "默认动画控制器使用的参数 (可选)\nVRCEmote, Int\nVRCFaceBlendH, Float\nVRCFaceBlendV, Float" },
                    { "parameters_clear", "清空参数" },
                    { "parameters_to_default", "恢复默认参数" },
                    { "warning", "警告" },
                    { "no", "否" },
                    { "yes", "是" },
                    { "parameters_clear_tip", "是否要清除所有参数？" },
                    { "parameters_reset_tip", "是否要将所有参数重置为默认值？" },
                }
            },

            {
                "English", new Dictionary<string, string>
                {
                    { "parameters", "Parameters" },
                    { "parameters_name", "Name" },
                    { "parameters_type", "Type" },
                    { "parameters_default", "Default" },
                    { "parameters_saved", "Saved" },
                    { "parameters_synced", "Synced" },
                    { "parameters_out_of_memory", "Parameters use too much memory.  Remove parameters or use bools which use less memory." },
                    { "parameters_tip_1", "Only parameters defined here can be used by expression menus, sync between all playable layers and sync across the network to remote clients." },
                    { "parameters_tip_2", "The parameter name and type should match a parameter defined on one or more of your animation controllers." },
                    { "parameters_tip_3", "Parameters used by the default animation controllers (Optional)\nVRCEmote, Int\nVRCFaceBlendH, Float\nVRCFaceBlendV, Float" },
                    { "parameters_clear", "Clear Parameters" },
                    { "parameters_to_default", "Default Parameters" },
                    { "warning", "Warning" },
                    { "no", "No" },
                    { "yes", "Yes" },
                    { "parameters_clear_tip", "Are you sure you want to clear all expression parameters?" },
                    { "parameters_reset_tip", "Are you sure you want to reset all expression parameters to default?" },
                }
            },
        };
    }
}