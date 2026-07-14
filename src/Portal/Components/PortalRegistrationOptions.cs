namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：读取注册流程相关的最小运行期设置。
    ///
    /// English: Reads the minimal runtime settings used by registration flows.
    /// </summary>
    /// <remarks>
    /// 中文：此类型只提供当前注册流程已经使用的读取入口，不负责创建邀请码、员工绑定或审批工作流。
    ///
    /// English: This type provides only read access used by current registration flows; it does not create
    /// invite codes or implement employee binding or approval workflows.
    /// </remarks>
    public static class PortalRegistrationOptions
    {
        /// <summary>
        /// 中文：控制是否开放自主注册的稳定设置键。
        ///
        /// English: Stable setting key controlling whether self-registration is available.
        /// </summary>
        public const string AllowSelfRegistrationKey = PortalSettingKeys.AllowSelfRegistration;

        /// <summary>
        /// 中文：控制自主注册后是否需要审核的稳定设置键。
        ///
        /// English: Stable setting key controlling whether self-registration requires approval.
        /// </summary>
        public const string RequireRegistrationApprovalKey = PortalSettingKeys.RequireRegistrationApproval;

        /// <summary>
        /// 中文：临时注册链接默认有效天数的稳定设置键。
        ///
        /// English: Stable setting key for default temporary registration-invite validity days.
        /// </summary>
        public const string RegistrationInviteDefaultExpiryDaysKey = PortalSettingKeys.RegistrationInviteDefaultExpiryDays;

        /// <summary>
        /// 中文：员工号暂未绑定时是否可继续进入待审核的稳定设置键。
        ///
        /// English: Stable setting key for whether a registration with pending employee binding may continue to approval.
        /// </summary>
        public const string AllowPendingEmployeeBindingKey = PortalSettingKeys.AllowPendingEmployeeBinding;

        /// <summary>
        /// 中文：是否开放自主注册；默认关闭，只有有效设置显式为 <c>true</c> 时才允许访问注册流程。
        ///
        /// English: Whether self-registration is available. It is disabled by default and registration flows are allowed only when an effective setting is explicitly <c>true</c>.
        /// </summary>
        public static bool AllowSelfRegistration
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.AllowSelfRegistration);
            }
        }

        /// <summary>
        /// 中文：自主注册是否需要管理员审核；默认启用。
        ///
        /// English: Whether self-registration requires administrator approval; enabled by default.
        /// </summary>
        public static bool RequireRegistrationApproval
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.RequireRegistrationApproval);
            }
        }

        /// <summary>
        /// 中文：临时注册链接的默认有效天数。
        ///
        /// English: Default validity days for temporary registration invite links.
        /// </summary>
        public static int RegistrationInviteDefaultExpiryDays
        {
            get
            {
                return PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.RegistrationInviteDefaultExpiryDays);
            }
        }

        /// <summary>
        /// 中文：员工号暂未绑定时是否仍允许继续注册并进入待审核。
        ///
        /// English: Whether registration may continue to approval when employee binding is still pending.
        /// </summary>
        public static bool AllowPendingEmployeeBinding
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.AllowPendingEmployeeBinding);
            }
        }

        /// <summary>
        /// 中文：判断当前最小规则下是否必须填写员工号。
        ///
        /// English: Determines whether the current minimal rule requires an employee code.
        /// </summary>
        /// <param name="inviteCode">中文：注册链接携带的邀请码；空白表示非邀请注册。English: Invite code carried by the registration link; blank means non-invite registration.</param>
        /// <returns>中文：存在邀请码时为 <c>true</c>。English: <c>true</c> when an invite code is present.</returns>
        public static bool IsEmployeeCodeRequired(string inviteCode)
        {
            return !string.IsNullOrWhiteSpace(inviteCode);
        }
    }
}
