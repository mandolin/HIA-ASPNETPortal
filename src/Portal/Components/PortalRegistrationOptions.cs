namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 读取注册相关的最小配置。
    /// Reads minimal registration-related runtime settings.
    /// </summary>
    public static class PortalRegistrationOptions
    {
        /// <summary>
        /// 控制是否开放自主注册的 appSettings 键名。
        /// </summary>
        public const string AllowSelfRegistrationKey = PortalSettingKeys.AllowSelfRegistration;

        /// <summary>
        /// 控制自主注册后是否需要审核的 appSettings 键名。
        /// </summary>
        public const string RequireRegistrationApprovalKey = PortalSettingKeys.RequireRegistrationApproval;

        /// <summary>
        /// 临时注册链接默认有效天数的 appSettings 键名。
        /// </summary>
        public const string RegistrationInviteDefaultExpiryDaysKey = PortalSettingKeys.RegistrationInviteDefaultExpiryDays;

        /// <summary>
        /// 员工号绑定失败时是否允许继续进入待审核的 appSettings 键名。
        /// </summary>
        public const string AllowPendingEmployeeBindingKey = PortalSettingKeys.AllowPendingEmployeeBinding;

        /// <summary>
        /// 默认不开放自主注册；只有显式配置为 true 时才显示注册链接并允许访问 Register.aspx。
        /// </summary>
        public static bool AllowSelfRegistration
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.AllowSelfRegistration);
            }
        }

        /// <summary>
        /// 自主注册默认仍需要管理员审核。
        /// </summary>
        public static bool RequireRegistrationApproval
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.RequireRegistrationApproval);
            }
        }

        /// <summary>
        /// 临时注册链接默认有效天数。
        /// </summary>
        public static int RegistrationInviteDefaultExpiryDays
        {
            get
            {
                return PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.RegistrationInviteDefaultExpiryDays);
            }
        }

        /// <summary>
        /// 员工号绑定失败时是否允许继续注册。
        /// </summary>
        public static bool AllowPendingEmployeeBinding
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.AllowPendingEmployeeBinding);
            }
        }

        /// <summary>
        /// 当前最小规则：带 invite 参数的企业注册必须填写员工号。
        /// </summary>
        public static bool IsEmployeeCodeRequired(string inviteCode)
        {
            return !string.IsNullOrWhiteSpace(inviteCode);
        }
    }
}
