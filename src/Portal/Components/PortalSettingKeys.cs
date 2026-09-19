namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>系统设置稳定键名的集中定义。</zh-CN>
    ///   <en>Central definitions of stable system-setting keys.</en>
    /// </lang>
    /// </summary>
    public static class PortalSettingKeys
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前门户的 Web Forms 主题名。</zh-CN>
        ///   <en>Active Web Forms theme name for the Portal.</en>
        /// </lang>
        /// </summary>
        public const string ThemeName = "Portal.Theme.Name";

        /// <summary>
        /// <lang>
        ///   <zh-CN>诊断日志目录设置键。</zh-CN>
        ///   <en>Setting key for the diagnostics log directory.</en>
        /// </lang>
        /// </summary>
        public const string DiagnosticsLogDirectory = "Portal.Diagnostics.LogDirectory";

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否启用详细错误输出的设置键。</zh-CN>
        ///   <en>Setting key for the detailed-error output switch.</en>
        /// </lang>
        /// </summary>
        public const string DiagnosticsDetailedErrors = "Portal.Diagnostics.EnableDetailedErrors";

        /// <summary>
        /// <lang>
        ///   <zh-CN>单个结构化诊断日志文件最大字节数的设置键。</zh-CN>
        ///   <en>Setting key for the maximum byte size of one structured diagnostics log file.</en>
        /// </lang>
        /// </summary>
        public const string DiagnosticsMaxFileBytes = "Portal.Diagnostics.MaxFileBytes";

        /// <summary>
        /// <lang>
        ///   <zh-CN>结构化诊断日志保留天数的设置键。</zh-CN>
        ///   <en>Setting key for structured diagnostics-log retention days.</en>
        /// </lang>
        /// </summary>
        public const string DiagnosticsRetentionDays = "Portal.Diagnostics.RetentionDays";

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员是否可查看已净化诊断详情的设置键。</zh-CN>
        ///   <en>Setting key controlling whether administrators may view sanitized diagnostic details.</en>
        /// </lang>
        /// </summary>
        public const string DiagnosticsAllowAdminDetailView = "Portal.Diagnostics.AllowAdminDetailView";

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档上传大小上限（字节）的设置键。</zh-CN>
        ///   <en>Setting key for maximum document-upload size in bytes.</en>
        /// </lang>
        /// </summary>
        public const string MaxUploadBytes = "Portal.Documents.MaxUploadBytes";

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档模块允许上传扩展名列表的设置键。</zh-CN>
        ///   <en>Setting key for the document-module upload-extension allowlist.</en>
        /// </lang>
        /// </summary>
        public const string AllowedDocumentExtensions = "Portal.Documents.AllowedExtensions";

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否允许用户自主注册的设置键。</zh-CN>
        ///   <en>Setting key for the self-registration switch.</en>
        /// </lang>
        /// </summary>
        public const string AllowSelfRegistration = "Portal.Security.AllowSelfRegistration";

        /// <summary>
        /// <lang>
        ///   <zh-CN>自主注册后是否需要管理员审核的设置键。</zh-CN>
        ///   <en>Setting key for whether self-registration requires administrator approval.</en>
        /// </lang>
        /// </summary>
        public const string RequireRegistrationApproval = "Portal.Security.RequireRegistrationApproval";

        /// <summary>
        /// <lang>
        ///   <zh-CN>登录密码提交是否必须使用前端加密的设置键。</zh-CN>
        ///   <en>Setting key for whether login-password submission must use client-side encryption.</en>
        /// </lang>
        /// </summary>
        public const string RequireEncryptedLoginPassword = "Portal.Security.RequireEncryptedLoginPassword";

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码策略最小长度的设置键。</zh-CN>
        ///   <en>Setting key for password-policy minimum length.</en>
        /// </lang>
        /// </summary>
        public const string PasswordMinimumLength = "Portal.Security.Password.MinimumLength";

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码策略要求的字符类别数量设置键。</zh-CN>
        ///   <en>Setting key for the required password character-category count.</en>
        /// </lang>
        /// </summary>
        public const string PasswordRequiredCategoryCount = "Portal.Security.Password.RequiredCategoryCount";

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否启用常见弱口令字典检测的设置键。</zh-CN>
        ///   <en>Setting key for enabling common weak-password dictionary checks.</en>
        /// </lang>
        /// </summary>
        public const string PasswordWeakDictionaryEnabled = "Portal.Security.Password.WeakDictionaryEnabled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否禁止密码包含账号上下文词的设置键。</zh-CN>
        ///   <en>Setting key for disallowing account-context terms in passwords.</en>
        /// </lang>
        /// </summary>
        public const string PasswordDisallowContextTerms = "Portal.Security.Password.DisallowContextTerms";

        /// <summary>
        /// <lang>
        ///   <zh-CN>强密码哈希目标 PBKDF2 迭代次数的设置键；低于当前默认值的配置会被视为无效并回落，强度只增不减。</zh-CN>
        ///   <en>Setting key for the target PBKDF2 iteration count of strong password hashes; values below the current default are treated as invalid and clamped up, so strength only increases.</en>
        /// </lang>
        /// </summary>
        public const string PasswordIterationCount = "Portal.Security.Password.IterationCount";

        /// <summary>
        /// <lang>
        ///   <zh-CN>身份与角色 Cookie 是否下发 Secure 属性的设置键；仅 HTTPS 部署方可启用。</zh-CN>
        ///   <en>Setting key for whether identity and role cookies emit the Secure attribute; only enable for HTTPS deployments.</en>
        /// </lang>
        /// </summary>
        public const string CookiesSecure = "Portal.Security.Cookies.Secure";

        /// <summary>
        /// <lang>
        ///   <zh-CN>身份与角色 Cookie 的 SameSite 模式设置键；取值 None 时必须配合 Secure 使用。</zh-CN>
        ///   <en>Setting key for the SameSite mode of identity and role cookies; the None value must be combined with Secure.</en>
        /// </lang>
        /// </summary>
        public const string CookiesSameSite = "Portal.Security.Cookies.SameSite";

        /// <summary>
        /// <lang>
        ///   <zh-CN>临时注册链接默认有效天数的设置键。</zh-CN>
        ///   <en>Setting key for default validity days of temporary registration invite links.</en>
        /// </lang>
        /// </summary>
        public const string RegistrationInviteDefaultExpiryDays = "Portal.Registration.InviteDefaultExpiryDays";

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号暂未绑定的注册是否仍可进入待审核的设置键。</zh-CN>
        ///   <en>Setting key for whether registrations with pending employee binding may continue to approval.</en>
        /// </lang>
        /// </summary>
        public const string AllowPendingEmployeeBinding = "Portal.Registration.AllowPendingEmployeeBinding";

        /// <summary>
        /// <lang>
        ///   <zh-CN>HIA 外围能力描述使用的部署级门户实例标识设置键。</zh-CN>
        ///   <en>Setting key for the deployment-level Portal instance identifier used by HIA peripheral capability descriptors.</en>
        /// </lang>
        /// </summary>
        public const string HiaPortalInstanceId = "Portal.Hia.InstanceId";
    }
}
