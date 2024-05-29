using System.Collections.Generic;
using Yueby.Utils;

namespace Yueby.AvatarTools.WorldConstraints
{
    public class WcLocalization : Localization
    {
        public WcLocalization()
        {
            Languages = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "中文", new Dictionary<string, string>
                    {
                        { "title_main_label", "世界空间约束" },
                        { "standard_target_item_field", "目标物品" },
                        { "standard_title_label", "配置" },
                        { "option_title_label", "设置" },
                        { "option_use_parent_radio", "使用父对象" },
                        { "option_parent_field", "父对象" },
                        { "setup_apply_button", "应用" },
                        { "option_auto_rename_radio", "自动重命名" }
                    }
                },
                {
                    "English", new Dictionary<string, string>
                    {
                        { "title_main_label", "World Space Constraint" },
                        { "standard_target_item_field", "Target" },
                        { "standard_title_label", "Configure" },
                        { "option_title_label", "Option" },
                        { "option_use_parent_radio", "Use parent" },
                        { "option_parent_field", "Parent" },
                        { "setup_apply_button", "Apply" },
                        { "option_auto_rename_radio", "Auto rename" }
                    }
                },
                {
                    "日本語　(Coming soon...)", new Dictionary<string, string>
                    {
                        { "title_main_label", "World Space Constraint" },
                        { "standard_target_item_field", "Target" },
                        { "standard_title_label", "Configure" },
                        { "option_title_label", "Option" },
                        { "option_use_parent_radio", "Use parent" },
                        { "option_parent_field", "Parent" },
                        { "setup_apply_button", "Apply" },
                        { "option_auto_rename_radio", "Auto rename" }
                    }
                }
            };
        }
    }
}