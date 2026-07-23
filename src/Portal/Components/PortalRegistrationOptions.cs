namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>读取注册流程相关的最小运行期设置。</zh-CN>
    ///   <en>Reads the minimal runtime settings used by registration flows.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此类型只提供当前注册流程已经使用的读取入口，不负责创建邀请码、员工绑定或审批工作流。</zh-CN>
    ///   <en>This type provides only read access used by current registration flows; it does not create invite codes or implement employee binding or approval workflows.</en>
    /// </lang>
    /// </remarks>
    public static class PortalRegistrationOptions
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>控制是否开放自主注册的稳定设置键。</zh-CN>
        ///   <en>Stable setting key controlling whether self-registration is available.</en>
        /// </lang>
        /// </summary>
        public const string AllowSelfRegistrationKey = PortalSettingKeys.AllowSelfRegistration;

        /// <summary>
        /// <lang>
        ///   <zh-CN>控制自主注册后是否需要审核的稳定设置键。</zh-CN>
        ///   <en>Stable setting key controlling whether self-registration requires approval.</en>
        /// </lang>
        /// </summary>
        public const string RequireRegistrationApprovalKey = PortalSettingKeys.RequireRegistrationApproval;

        /// <summary>
        /// <lang>
        ///   <zh-CN>临时注册链接默认有效天数的稳定设置键。</zh-CN>
        ///   <en>Stable setting key for default temporary registration-invite validity days.</en>
        /// </lang>
        /// </summary>
        public const string RegistrationInviteDefaultExpiryDaysKey = PortalSettingKeys.RegistrationInviteDefaultExpiryDays;

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号暂未绑定时是否可继续进入待审核的稳定设置键。</zh-CN>
        ///   <en>Stable setting key for whether a registration with pending employee binding may continue to approval.</en>
        /// </lang>
        /// </summary>
        public const string AllowPendingEmployeeBindingKey = PortalSettingKeys.AllowPendingEmployeeBinding;

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否开放自主注册；默认关闭，只有有效设置显式为 <c>true</c> 时才允许访问注册流程。</zh-CN>
        ///   <en>Whether self-registration is available. It is disabled by default and registration flows are allowed only when an effective setting is explicitly <c>true</c>.</en>
        /// </lang>
        /// </summary>
        public static bool AllowSelfRegistration
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.AllowSelfRegistration);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>自主注册是否需要管理员审核；默认启用。</zh-CN>
        ///   <en>Whether self-registration requires administrator approval; enabled by default.</en>
        /// </lang>
        /// </summary>
        public static bool RequireRegistrationApproval
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.RequireRegistrationApproval);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>临时注册链接的默认有效天数。</zh-CN>
        ///   <en>Default validity days for temporary registration invite links.</en>
        /// </lang>
        /// </summary>
        public static int RegistrationInviteDefaultExpiryDays
        {
            get
            {
                return PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.RegistrationInviteDefaultExpiryDays);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号暂未绑定时是否仍允许继续注册并进入待审核。</zh-CN>
        ///   <en>Whether registration may continue to approval when employee binding is still pending.</en>
        /// </lang>
        /// </summary>
        public static bool AllowPendingEmployeeBinding
        {
            get
            {
                return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.AllowPendingEmployeeBinding);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前最小规则下是否必须填写员工号。</zh-CN>
        ///   <en>Determines whether the current minimal rule requires an employee code.</en>
        /// </lang>
        /// </summary>
        /// <param name="inviteCode">
        /// <l>
        ///   <zh-CN>注册链接携带的邀请码；空白表示非邀请注册。</zh-CN>
        ///   <en>Invite code carried by the registration link; blank means non-invite registration.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>存在邀请码时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when an invite code is present.</en>
        /// </l>
        /// </returns>
        public static bool IsEmployeeCodeRequired(string inviteCode)
        {
            return !string.IsNullOrWhiteSpace(inviteCode);
        }
    }
}
