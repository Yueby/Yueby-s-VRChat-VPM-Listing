using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yueby.Utils;

namespace Yueby.AvatarTools.VRCEditorOptimize
{
    public class VRCExMenuLocalization : Localization
    {
        public VRCExMenuLocalization()
        {
            Languages = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "English", new Dictionary<string, string>
                    {
                        { "no_active_avatar", "No active avatar descriptor found in scene." },
                        { "controls", "Controls" },
                        { "name", "Name" },
                        { "type", "Type" },
                        { "parameter", "Parameter" },
                        { "value", "Value" },
                        { "parameter_rotation", "Parameter Rotation" },
                        { "control_button_tip", "Click or hold to activate. The button remains active for a minimum 0.2s.\nWhile active the (Parameter) is set to (Value).\nWhen inactive the (Parameter) is reset to zero." },
                        { "control_toggle_tip", "Click to toggle on or off.\nWhen turned on the (Parameter) is set to (Value).\nWhen turned off the (Parameter) is reset to zero." },
                        { "control_submenu_tip", "Opens another expression menu.\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero." },
                        { "control_two_axis_tip", "Puppet menu that maps the joystick to two parameters (-1 to +1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero." },
                        { "control_four_axis_tip", "Puppet menu that maps the joystick to four parameters (0 to 1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero." },
                        { "control_radial_tip", "Puppet menu that sets a value based on joystick rotation. (0 to 1)\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero." },
                        { "up", "Up" },
                        { "down", "Down" },
                        { "left", "Left" },
                        { "right", "Right" },
                        { "horizontal", "Horizontal" },
                        { "vertical", "Vertical" },
                        { "submenu", "Sub Menu" },
                        { "ex_menu_show", "Expression Menu Show" },
                        { "active_avatar", "Active Avatar" },
                        { "none", "None" },
                        { "parameter_not_found", "Parameter not found on the active avatar descriptor." },
                        { "parameter_bool_not_valid", "Bool parameters not valid for this choice." },
                    }
                },
                {
                    "中文", new Dictionary<string, string>
                    {
                        { "no_active_avatar", "未在场景中找到活动的Avatar" },
                        { "controls", "控件" },
                        { "name", "名字" },
                        { "type", "类型" },
                        { "parameter", "参数" },
                        { "value", "值" },
                        { "parameter_rotation", "旋转参数" },
                        { "control_button_tip", "单击或按住以激活。按钮保持激活状态至少0.2秒。\n激活时，（参数）设置为（值）。\n当处于非激活状态时，（参数）将重置为0。" },
                        { "control_toggle_tip", "单击以打开或关闭。\n打开时，（参数）设置为（值）。\n关闭时，（参数）重置为0。" },
                        { "control_submenu_tip", "打开其他子菜单。\n打开时，（参数）设置为（值）。\n关闭时（参数）重置为0。" },
                        { "control_two_axis_tip", "将操纵杆映射到两个参数（-1到+1）的弹出菜单。\n打开时，（参数）设置为（值）。\n关闭时（参数）重置为0。" },
                        { "control_four_axis_tip", "将操纵杆映射到四个参数（0到1）的弹出菜单。\n打开时，（参数）设置为（值）。\n关闭时（参数）重置为0。" },
                        { "control_radial_tip", "根据操纵杆旋转设置值的弹出菜单。（0到1）\n打开时，（参数）设置为（值）。\n关闭时（参数）重置为0。" },
                        { "up", "上" },
                        { "down", "下" },
                        { "left", "左" },
                        { "right", "右" },
                        { "horizontal", "横向" },
                        { "vertical", "纵向" },
                        { "submenu", "子菜单" },
                        { "ex_menu_show", "菜单中显示内容" },
                        { "active_avatar", "活动的Avatar" },
                        { "none", "无" },
                        { "parameter_not_found", "在活动的Avatar上未找到该参数。" },
                        { "parameter_bool_not_valid", "该选项不可使用Bool参数。" },
                    }
                }
            };
        }
    }
}