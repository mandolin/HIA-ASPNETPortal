namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 系统设置键名集中定义。
    /// Central place for stable system setting keys.
    /// </summary>
    public static class PortalSettingKeys
    {
        /// <summary>
        /// 当前门户主题名。
        /// Active WebForms theme name.
        /// </summary>
        public const string ThemeName = "Portal.Theme.Name";

        /// <summary>
        /// 诊断日志目录。
        /// Diagnostics log directory.
        /// </summary>
        public const string DiagnosticsLogDirectory = "Portal.Diagnostics.LogDirectory";

        /// <summary>
        /// 是否启用详细错误输出。
        /// Detailed error output switch.
        /// </summary>
        public const string DiagnosticsDetailedErrors = "Portal.Diagnostics.EnableDetailedErrors";

        /// <summary>
        /// 文档上传大小上限，单位为字节。
        /// Maximum document upload size in bytes.
        /// </summary>
        public const string MaxUploadBytes = "Portal.Documents.MaxUploadBytes";

        /// <summary>
        /// 是否允许用户自主注册。
        /// Self-registration switch.
        /// </summary>
        public const string AllowSelfRegistration = "Portal.Security.AllowSelfRegistration";

        /// <summary>
        /// 自主注册后是否需要管理员审核。
        /// Whether self-registration requires administrator approval.
        /// </summary>
        public const string RequireRegistrationApproval = "Portal.Security.RequireRegistrationApproval";

        /// <summary>
        /// 临时注册链接默认有效天数。
        /// Default validity days for temporary registration invite links.
        /// </summary>
        public const string RegistrationInviteDefaultExpiryDays = "Portal.Registration.InviteDefaultExpiryDays";

        /// <summary>
        /// 是否允许员工号暂未绑定的注册继续进入待审核。
        /// Whether registrations with pending employee binding may continue to approval.
        /// </summary>
        public const string AllowPendingEmployeeBinding = "Portal.Registration.AllowPendingEmployeeBinding";
    }
}
