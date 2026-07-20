using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：系统设置元数据 registry。
    ///
    /// English: Registry of system-setting metadata.
    /// </summary>
    /// <remarks>
    /// 中文：registry 集中描述设置项，再由运行时解析器、数据库覆盖层和后台管理界面按其契约工作。
    /// 已登记的定义是可受控读取和在线管理的唯一入口，不能以任意键绕过元数据校验。
    ///
    /// English: The registry centralizes setting definitions, which are then used by the runtime resolver,
    /// database override layer, and administration UI. Registered definitions are the only controlled entry
    /// point for reads and online management; arbitrary keys must not bypass metadata validation.
    /// </remarks>
    public static class PortalSettingsRegistry
    {
        /// <summary>
        /// 中文：控制是否开放用户自主注册的设置定义，默认关闭。
        ///
        /// English: Setting definition controlling whether user self-registration is available; disabled by default.
        /// </summary>
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

        /// <summary>
        /// 中文：控制自主注册用户是否需要管理员审核的设置定义，默认启用。
        ///
        /// English: Setting definition controlling whether self-registered users require administrator approval; enabled by default.
        /// </summary>
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

        /// <summary>
        /// 中文：控制登录密码提交是否必须使用前端一次性公钥加密的设置定义。
        ///
        /// English: Setting definition controlling whether login-password submission must use one-time public-key encryption on the client.
        /// </summary>
        /// <remarks>
        /// 中文：P10.3 当前默认开启。后续会按客户端浏览器环境选择加密强度，但该开关仍表达“是否必须加密提交”的业务策略。
        ///
        /// English: Enabled by default in P10.3. Later work will select encryption strength by client browser capability,
        /// while this setting continues to represent whether encrypted submission is required.
        /// </remarks>
        public static readonly PortalSettingDefinition RequireEncryptedLoginPassword =
            new PortalSettingDefinition(
                PortalSettingKeys.RequireEncryptedLoginPassword,
                "登录密码必须前端加密",
                "控制登录密码是否必须通过一次性 RSA 公钥加密后提交；默认开启。",
                PortalSettingValueType.Boolean,
                "true",
                true,
                false,
                "Admins",
                "Security");

        /// <summary>
        /// 中文：临时注册链接默认有效天数的设置定义。
        ///
        /// English: Setting definition for the default validity period of temporary registration invite links.
        /// </summary>
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

        /// <summary>
        /// 中文：员工号尚未绑定时是否可继续进入待审核注册流程的设置定义。
        ///
        /// English: Setting definition controlling whether a registration with pending employee binding may continue to approval.
        /// </summary>
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

        /// <summary>
        /// 中文：文档模块允许上传的单个文件最大字节数设置定义。
        ///
        /// English: Setting definition for the maximum bytes allowed for one document-module upload.
        /// </summary>
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
                minIntegerValue: 1,
                maxIntegerValue: PortalDocumentPolicy.InfrastructureMaximumUploadBytes);

        /// <summary>
        /// 中文：文档模块允许上传扩展名列表的设置定义。
        ///
        /// English: Setting definition for the document-module upload-extension allowlist.
        /// </summary>
        /// <remarks>
        /// 中文：该文本设置只用于从 <see cref="PortalDocumentPolicy"/> 的硬允许集中选择子集；即使数据库覆盖值
        /// 包含其他扩展名，也不会使脚本、页面、配置或可执行文件可上传。
        ///
        /// English: This text setting selects a subset of <see cref="PortalDocumentPolicy"/>'s hard allowlist only;
        /// a database override containing other extensions never makes scripts, pages, configuration files, or executables uploadable.
        /// </remarks>
        public static readonly PortalSettingDefinition AllowedDocumentExtensions =
            new PortalSettingDefinition(
                PortalSettingKeys.AllowedDocumentExtensions,
                "文档上传允许扩展名",
                "以逗号分隔的上传允许扩展名；只能收紧内置安全类型集合。",
                PortalSettingValueType.String,
                "pdf,txt,csv,json,doc,docx,xls,xlsx,ppt,pptx,zip",
                true,
                false,
                "Admins",
                "Documents");

        /// <summary>
        /// 中文：门户 Web Forms 主题名称的设置定义。
        ///
        /// English: Setting definition for the Portal Web Forms theme name.
        /// </summary>
        public static readonly PortalSettingDefinition ThemeName =
            new PortalSettingDefinition(
                PortalSettingKeys.ThemeName,
                "门户主题名",
                "当前 WebForms 主题名称；只允许选择已部署且通过清单校验的主题，非法或缺失时回退 Default。",
                PortalSettingValueType.Enum,
                "EnterpriseLight",
                true,
                false,
                "Admins",
                "Theme");

        /// <summary>
        /// 中文：详细错误输出状态的只读设置定义。
        ///
        /// English: Read-only setting definition for detailed-error output status.
        /// </summary>
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

        /// <summary>
        /// 中文：诊断日志目录的部署级只读设置定义。
        ///
        /// English: Deployment-level read-only setting definition for the diagnostics log directory.
        /// </summary>
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

        /// <summary>
        /// 中文：结构化诊断日志单文件大小上限的部署级只读设置定义。
        ///
        /// English: Deployment-level read-only setting definition for the structured diagnostics log-file size limit.
        /// </summary>
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

        /// <summary>
        /// 中文：结构化诊断日志保留天数的部署级只读设置定义。
        ///
        /// English: Deployment-level read-only setting definition for structured diagnostics-log retention days.
        /// </summary>
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

        /// <summary>
        /// 中文：管理员查看已净化诊断详情权限的部署级只读设置定义。
        ///
        /// English: Deployment-level read-only setting definition for administrator access to sanitized diagnostic details.
        /// </summary>
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

        /// <summary>
        /// 中文：HIA 外围契约使用的部署级门户实例标识设置定义。
        ///
        /// English: Deployment-level Portal instance identifier setting definition for the HIA peripheral contract.
        /// </summary>
        public static readonly PortalSettingDefinition HiaPortalInstanceId =
            new PortalSettingDefinition(
                PortalSettingKeys.HiaPortalInstanceId,
                "HIA 门户实例标识",
                "用于未来 HIA 外围能力描述的非敏感稳定实例标识；留空时不启用任何对外适配器。",
                PortalSettingValueType.String,
                string.Empty,
                false,
                true,
                "Admins",
                "HiaBoundary",
                sourceLevel: "AppSettings");

        private static readonly IList<PortalSettingDefinition> AllDefinitions =
            new List<PortalSettingDefinition>
            {
                AllowSelfRegistration,
                RequireRegistrationApproval,
                RequireEncryptedLoginPassword,
                RegistrationInviteDefaultExpiryDays,
                AllowPendingEmployeeBinding,
                MaxUploadBytes,
                AllowedDocumentExtensions,
                ThemeName,
                DiagnosticsDetailedErrors,
                DiagnosticsLogDirectory,
                DiagnosticsMaxFileBytes,
                DiagnosticsRetentionDays,
                DiagnosticsAllowAdminDetailView,
                HiaPortalInstanceId
            }.AsReadOnly();

        /// <summary>
        /// 中文：获取当前已登记的全部设置定义。
        ///
        /// English: Gets all currently registered setting definitions.
        /// </summary>
        /// <returns>
        /// 中文：只读定义集合，调用方不应修改其中的定义。
        /// English: Read-only definition collection; callers must not modify the contained definitions.
        /// </returns>
        public static IEnumerable<PortalSettingDefinition> GetAll()
        {
            return AllDefinitions;
        }

        /// <summary>
        /// 中文：按稳定键名查找设置定义。
        ///
        /// English: Finds a setting definition by its stable key.
        /// </summary>
        /// <param name="key">中文：要查找的稳定设置键。English: Stable setting key to find.</param>
        /// <param name="definition">中文：找到时返回对应定义；否则为 <c>null</c>。English: Matching definition when found; otherwise <c>null</c>.</param>
        /// <returns>中文：找到已登记定义时为 <c>true</c>。English: <c>true</c> when a registered definition is found.</returns>
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
