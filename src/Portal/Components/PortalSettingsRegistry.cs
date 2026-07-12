using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 系统设置元数据 registry。
    /// Registry of system setting metadata.
    /// </summary>
    /// <remarks>
    /// 本 registry 是 P2.1 的最小骨架：先集中描述设置项，再逐步接入数据库层和后台 UI。
    /// This registry is the P2.1 skeleton: centralize metadata first, then add database and admin UI later.
    /// </remarks>
    public static class PortalSettingsRegistry
    {
        public static readonly PortalSettingDefinition AllowSelfRegistration =
            new PortalSettingDefinition(
                PortalSettingKeys.AllowSelfRegistration,
                "是否允许自主注册",
                "控制是否显示注册链接并允许访问注册页；默认关闭。",
                PortalSettingValueType.Boolean,
                "false",
                true,
                false,
                "Admins",
                "Security");

        public static readonly PortalSettingDefinition RequireRegistrationApproval =
            new PortalSettingDefinition(
                PortalSettingKeys.RequireRegistrationApproval,
                "自主注册需要审核",
                "控制自主注册用户是否必须先由管理员批准才能登录；默认开启。",
                PortalSettingValueType.Boolean,
                "true",
                true,
                false,
                "Admins",
                "Security");

        public static readonly PortalSettingDefinition RegistrationInviteDefaultExpiryDays =
            new PortalSettingDefinition(
                PortalSettingKeys.RegistrationInviteDefaultExpiryDays,
                "注册链接默认有效天数",
                "临时注册链接默认有效天数；第一版用于设计与后续创建入口。",
                PortalSettingValueType.Integer,
                "7",
                true,
                false,
                "Admins",
                "Registration",
                minIntegerValue: 1);

        public static readonly PortalSettingDefinition AllowPendingEmployeeBinding =
            new PortalSettingDefinition(
                PortalSettingKeys.AllowPendingEmployeeBinding,
                "允许待绑定员工号注册",
                "员工号绑定失败时是否仍允许注册进入待审核；默认不允许。",
                PortalSettingValueType.Boolean,
                "false",
                true,
                false,
                "Admins",
                "Registration");

        public static readonly PortalSettingDefinition MaxUploadBytes =
            new PortalSettingDefinition(
                PortalSettingKeys.MaxUploadBytes,
                "文档上传大小上限",
                "控制文档模块允许上传的单文件最大字节数。",
                PortalSettingValueType.Integer,
                "10485760",
                true,
                false,
                "Admins",
                "Documents",
                minIntegerValue: 1);

        public static readonly PortalSettingDefinition ThemeName =
            new PortalSettingDefinition(
                PortalSettingKeys.ThemeName,
                "门户主题名",
                "当前 WebForms 主题名称；非法或缺失时回退 Default。",
                PortalSettingValueType.Enum,
                "Default",
                false,
                true,
                "Admins",
                "Theme");

        public static readonly PortalSettingDefinition DiagnosticsDetailedErrors =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsDetailedErrors,
                "详细错误输出",
                "控制是否允许详细错误输出；本阶段只显示状态，不允许在线修改。",
                PortalSettingValueType.Boolean,
                "false",
                false,
                true,
                "Admins",
                "Diagnostics");

        public static readonly PortalSettingDefinition DiagnosticsLogDirectory =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsLogDirectory,
                "诊断日志目录",
                "诊断日志文件目录；留空或未配置时使用 App_Data/Logs。",
                PortalSettingValueType.Path,
                "App_Data/Logs",
                false,
                true,
                "Admins",
                "Diagnostics");

        public static readonly PortalSettingDefinition DiagnosticsMaxFileBytes =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsMaxFileBytes,
                "诊断日志单文件大小上限",
                "结构化诊断日志单个文件允许的最大字节数；达到上限后自动滚动到下一个序号文件。",
                PortalSettingValueType.Integer,
                "10485760",
                false,
                true,
                "Admins",
                "Diagnostics",
                minIntegerValue: 1024,
                maxIntegerValue: 104857600);

        public static readonly PortalSettingDefinition DiagnosticsRetentionDays =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsRetentionDays,
                "诊断日志保留天数",
                "结构化诊断日志保留的天数；写入新事件时每天最多执行一次受限清理。",
                PortalSettingValueType.Integer,
                "90",
                false,
                true,
                "Admins",
                "Diagnostics",
                minIntegerValue: 1,
                maxIntegerValue: 3650);

        public static readonly PortalSettingDefinition DiagnosticsAllowAdminDetailView =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsAllowAdminDetailView,
                "允许管理员查看诊断详情",
                "控制 Admins 是否可查看已净化的异常详情、物理路径、用户名、IP 和 User-Agent。",
                PortalSettingValueType.Boolean,
                "true",
                false,
                true,
                "Admins",
                "Diagnostics");

        private static readonly IList<PortalSettingDefinition> AllDefinitions =
            new List<PortalSettingDefinition>
            {
                AllowSelfRegistration,
                RequireRegistrationApproval,
                RegistrationInviteDefaultExpiryDays,
                AllowPendingEmployeeBinding,
                MaxUploadBytes,
                ThemeName,
                DiagnosticsDetailedErrors,
                DiagnosticsLogDirectory,
                DiagnosticsMaxFileBytes,
                DiagnosticsRetentionDays,
                DiagnosticsAllowAdminDetailView
            }.AsReadOnly();

        /// <summary>
        /// 获取当前已登记的全部设置定义。
        /// Gets all registered setting definitions.
        /// </summary>
        public static IEnumerable<PortalSettingDefinition> GetAll()
        {
            return AllDefinitions;
        }

        /// <summary>
        /// 按键名查找设置定义。
        /// Finds a setting definition by key.
        /// </summary>
        public static bool TryGet(string key, out PortalSettingDefinition definition)
        {
            foreach (PortalSettingDefinition item in AllDefinitions)
            {
                if (item.Key == key)
                {
                    definition = item;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
