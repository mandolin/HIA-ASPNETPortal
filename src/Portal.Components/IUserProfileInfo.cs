namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户用户资料扩展的只读视图。
    ///
    /// English: Read-only view of the Portal user-profile extension.
    /// </summary>
    /// <remarks>
    /// 中文：此视图不包含密码、Cookie、Token 或证件号等敏感内容。旧库尚未部署
    /// <c>PortalBiz_UserProfiles</c> 时，实现应返回兼容视图，并通过 <see cref="IsAvailable"/>
    /// 告知调用方资料扩展表不可用。
    ///
    /// English: This view does not contain passwords, cookies, tokens, identity numbers, or similar sensitive
    /// content. When a legacy database has not deployed <c>PortalBiz_UserProfiles</c>, implementations should
    /// return a compatibility view and signal the missing extension table through <see cref="IsAvailable"/>.
    /// </remarks>
    public interface IUserProfileInfo
    {
        /// <summary>
        /// 中文：旧门户用户标识。
        ///
        /// English: Legacy Portal user identifier.
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// 中文：旧 <c>Portal_Users.Name</c>，P6.2.3 中只读展示。
        ///
        /// English: Legacy <c>Portal_Users.Name</c>, displayed as read-only in P6.2.3.
        /// </summary>
        string LegacyName { get; }

        /// <summary>
        /// 中文：新的稳定登录名。
        ///
        /// English: New stable login name.
        /// </summary>
        string LoginName { get; }

        /// <summary>
        /// 中文：正式显示名。
        ///
        /// English: Formal display name.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 中文：昵称或偏好称呼。
        ///
        /// English: Nickname or preferred name.
        /// </summary>
        string Nickname { get; }

        /// <summary>
        /// 中文：偏好邮箱；非空时可作为登录标识。
        ///
        /// English: Preferred email; when non-empty it may be used as a sign-in identifier.
        /// </summary>
        string PreferredEmail { get; }

        /// <summary>
        /// 中文：账号资料状态。
        ///
        /// English: Account profile status.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// 中文：非敏感状态说明。
        ///
        /// English: Non-sensitive status reason.
        /// </summary>
        string StatusReason { get; }

        /// <summary>
        /// 中文：资料扩展表是否已部署并成功读取。
        ///
        /// English: Whether the profile extension table is deployed and was read successfully.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 中文：资料来源说明。
        ///
        /// English: Profile source description.
        /// </summary>
        string Source { get; }
    }
}
