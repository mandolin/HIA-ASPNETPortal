namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户用户资料扩展的只读视图。</zh-CN>
    ///   <en>Read-only view of the Portal user-profile extension.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此视图不包含密码、Cookie、Token 或证件号等敏感内容。旧库尚未部署 <c>PortalBiz_UserProfiles</c> 时，实现应返回兼容视图，并通过 <see cref="IsAvailable"/> 告知调用方资料扩展表不可用。</zh-CN>
    ///   <en>This view does not contain passwords, cookies, tokens, identity numbers, or similar sensitive content. When a legacy database has not deployed <c>PortalBiz_UserProfiles</c>, implementations should return a compatibility view and signal the missing extension table through <see cref="IsAvailable"/>.</en>
    /// </lang>
    /// </remarks>
    public interface IUserProfileInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户用户标识。</zh-CN>
        ///   <en>Legacy Portal user identifier.</en>
        /// </lang>
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧 <c>Portal_Users.Name</c>，P6.2.3 中只读展示。</zh-CN>
        ///   <en>Legacy <c>Portal_Users.Name</c>, displayed as read-only in P6.2.3.</en>
        /// </lang>
        /// </summary>
        string LegacyName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新的稳定登录名。</zh-CN>
        ///   <en>New stable login name.</en>
        /// </lang>
        /// </summary>
        string LoginName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>正式显示名。</zh-CN>
        ///   <en>Formal display name.</en>
        /// </lang>
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>昵称或偏好称呼。</zh-CN>
        ///   <en>Nickname or preferred name.</en>
        /// </lang>
        /// </summary>
        string Nickname { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好邮箱；非空时可作为登录标识。</zh-CN>
        ///   <en>Preferred email; when non-empty it may be used as a sign-in identifier.</en>
        /// </lang>
        /// </summary>
        string PreferredEmail { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号资料状态。</zh-CN>
        ///   <en>Account profile status.</en>
        /// </lang>
        /// </summary>
        string Status { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非敏感状态说明。</zh-CN>
        ///   <en>Non-sensitive status reason.</en>
        /// </lang>
        /// </summary>
        string StatusReason { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>资料扩展表是否已部署并成功读取。</zh-CN>
        ///   <en>Whether the profile extension table is deployed and was read successfully.</en>
        /// </lang>
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>资料来源说明。</zh-CN>
        ///   <en>Profile source description.</en>
        /// </lang>
        /// </summary>
        string Source { get; }
    }
}
