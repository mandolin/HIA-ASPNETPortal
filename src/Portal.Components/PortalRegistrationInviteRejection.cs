namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>临时注册链接邀请码的校验拒绝原因。数据层只返回该原因码，面向用户的文案由表现层按当前语言映射，使本地化文本不进入数据层。</zh-CN>
    ///   <en>Rejection reason for validating a temporary registration invite code. The data layer returns only this code, and the presentation layer maps it to user-facing text for the current language, keeping localized strings out of the data layer.</en>
    /// </lang>
    /// </summary>
    public enum PortalRegistrationInviteRejection
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>未拒绝：邀请码有效，或当前允许空邀请码的非邀请注册。</zh-CN>
        ///   <en>Not rejected: the invite code is valid, or an empty code is currently allowed for non-invite registration.</en>
        /// </lang>
        /// </summary>
        None = 0,

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册链接数据表尚未部署，无法校验邀请码。</zh-CN>
        ///   <en>The registration-link table has not been deployed, so the invite code cannot be validated.</en>
        /// </lang>
        /// </summary>
        InviteTableMissing,

        /// <summary>
        /// <lang>
        ///   <zh-CN>邀请码对应的注册链接不存在。</zh-CN>
        ///   <en>No registration link matches the invite code.</en>
        /// </lang>
        /// </summary>
        InviteNotFound,

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册链接已停用。</zh-CN>
        ///   <en>The registration link is disabled.</en>
        /// </lang>
        /// </summary>
        InviteDisabled,

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册链接已过期。</zh-CN>
        ///   <en>The registration link has expired.</en>
        /// </lang>
        /// </summary>
        InviteExpired,

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册链接已达到使用次数上限。</zh-CN>
        ///   <en>The registration link has reached its usage limit.</en>
        /// </lang>
        /// </summary>
        InviteUsageLimitReached,
    }
}
