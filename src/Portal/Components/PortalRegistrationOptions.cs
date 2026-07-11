namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 读取注册相关的最小配置。本阶段只控制是否允许自主注册，审核和邀请链接后续再扩展。
    /// </summary>
    public static class PortalRegistrationOptions
    {
        /// <summary>
        /// 控制是否开放自主注册的 appSettings 键名。
        /// </summary>
        public const string AllowSelfRegistrationKey = PortalSettingKeys.AllowSelfRegistration;

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
    }
}
