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
    }
}
