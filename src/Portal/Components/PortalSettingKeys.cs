namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：系统设置稳定键名的集中定义。
    ///
    /// English: Central definitions of stable system-setting keys.
    /// </summary>
    public static class PortalSettingKeys
    {
        /// <summary>
        /// 中文：当前门户的 Web Forms 主题名。
        ///
        /// English: Active Web Forms theme name for the Portal.
        /// </summary>
        public const string ThemeName = "Portal.Theme.Name";

        /// <summary>
        /// 中文：诊断日志目录设置键。
        ///
        /// English: Setting key for the diagnostics log directory.
        /// </summary>
        public const string DiagnosticsLogDirectory = "Portal.Diagnostics.LogDirectory";

        /// <summary>
        /// 中文：是否启用详细错误输出的设置键。
        ///
        /// English: Setting key for the detailed-error output switch.
        /// </summary>
        public const string DiagnosticsDetailedErrors = "Portal.Diagnostics.EnableDetailedErrors";

        /// <summary>
        /// 中文：单个结构化诊断日志文件最大字节数的设置键。
        ///
        /// English: Setting key for the maximum byte size of one structured diagnostics log file.
        /// </summary>
        public const string DiagnosticsMaxFileBytes = "Portal.Diagnostics.MaxFileBytes";

        /// <summary>
        /// 中文：结构化诊断日志保留天数的设置键。
        ///
        /// English: Setting key for structured diagnostics-log retention days.
        /// </summary>
        public const string DiagnosticsRetentionDays = "Portal.Diagnostics.RetentionDays";

        /// <summary>
        /// 中文：管理员是否可查看已净化诊断详情的设置键。
        ///
        /// English: Setting key controlling whether administrators may view sanitized diagnostic details.
        /// </summary>
        public const string DiagnosticsAllowAdminDetailView = "Portal.Diagnostics.AllowAdminDetailView";

        /// <summary>
        /// 中文：文档上传大小上限（字节）的设置键。
        ///
        /// English: Setting key for maximum document-upload size in bytes.
        /// </summary>
        public const string MaxUploadBytes = "Portal.Documents.MaxUploadBytes";

        /// <summary>
        /// 中文：文档模块允许上传扩展名列表的设置键。
        ///
        /// English: Setting key for the document-module upload-extension allowlist.
        /// </summary>
        public const string AllowedDocumentExtensions = "Portal.Documents.AllowedExtensions";

        /// <summary>
        /// 中文：是否允许用户自主注册的设置键。
        ///
        /// English: Setting key for the self-registration switch.
        /// </summary>
        public const string AllowSelfRegistration = "Portal.Security.AllowSelfRegistration";

        /// <summary>
        /// 中文：自主注册后是否需要管理员审核的设置键。
        ///
        /// English: Setting key for whether self-registration requires administrator approval.
        /// </summary>
        public const string RequireRegistrationApproval = "Portal.Security.RequireRegistrationApproval";

        /// <summary>
        /// 中文：登录密码提交是否必须使用前端加密的设置键。
        ///
        /// English: Setting key for whether login-password submission must use client-side encryption.
        /// </summary>
        public const string RequireEncryptedLoginPassword = "Portal.Security.RequireEncryptedLoginPassword";

        /// <summary>
        /// 中文：密码策略最小长度的设置键。
        ///
        /// English: Setting key for password-policy minimum length.
        /// </summary>
        public const string PasswordMinimumLength = "Portal.Security.Password.MinimumLength";

        /// <summary>
        /// 中文：密码策略要求的字符类别数量设置键。
        ///
        /// English: Setting key for the required password character-category count.
        /// </summary>
        public const string PasswordRequiredCategoryCount = "Portal.Security.Password.RequiredCategoryCount";

        /// <summary>
        /// 中文：是否启用常见弱口令字典检测的设置键。
        ///
        /// English: Setting key for enabling common weak-password dictionary checks.
        /// </summary>
        public const string PasswordWeakDictionaryEnabled = "Portal.Security.Password.WeakDictionaryEnabled";

        /// <summary>
        /// 中文：是否禁止密码包含账号上下文词的设置键。
        ///
        /// English: Setting key for disallowing account-context terms in passwords.
        /// </summary>
        public const string PasswordDisallowContextTerms = "Portal.Security.Password.DisallowContextTerms";

        /// <summary>
        /// 中文：临时注册链接默认有效天数的设置键。
        ///
        /// English: Setting key for default validity days of temporary registration invite links.
        /// </summary>
        public const string RegistrationInviteDefaultExpiryDays = "Portal.Registration.InviteDefaultExpiryDays";

        /// <summary>
        /// 中文：员工号暂未绑定的注册是否仍可进入待审核的设置键。
        ///
        /// English: Setting key for whether registrations with pending employee binding may continue to approval.
        /// </summary>
        public const string AllowPendingEmployeeBinding = "Portal.Registration.AllowPendingEmployeeBinding";

        /// <summary>
        /// 中文：HIA 外围能力描述使用的部署级门户实例标识设置键。
        ///
        /// English: Setting key for the deployment-level Portal instance identifier used by HIA peripheral capability descriptors.
        /// </summary>
        public const string HiaPortalInstanceId = "Portal.Hia.InstanceId";
    }
}
