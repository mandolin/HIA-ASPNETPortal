namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>用户注册审核状态的稳定常量。</zh-CN>
    ///   <en>Stable constants for user registration-review statuses.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些字符串写入注册审核元数据并参与登录门禁判断。系统默认不开放自主注册；开放注册时，只有状态为 <see cref="Approved"/> 的用户才能在审核元数据可用时继续登录。</zh-CN>
    ///   <en>These strings are written to registration-review metadata and participate in login-gate decisions. Self-registration is disabled by default; when registration is opened, only users in <see cref="Approved"/> state may continue signing in when review metadata is available.</en>
    /// </lang>
    /// </remarks>
    public static class PortalUserRegistrationStatuses
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>已批准，在注册审核元数据可读取时允许登录。</zh-CN>
        ///   <en>Approved and allowed to sign in when registration-review metadata is available.</en>
        /// </lang>
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// <lang>
        ///   <zh-CN>等待管理员审核，不允许登录。</zh-CN>
        ///   <en>Waiting for administrator approval and not allowed to sign in.</en>
        /// </lang>
        /// </summary>
        public const string PendingApproval = "PendingApproval";

        /// <summary>
        /// <lang>
        ///   <zh-CN>已拒绝，不允许登录；管理员之后仍可批准以恢复访问。</zh-CN>
        ///   <en>Rejected and not allowed to sign in; an administrator may approve later to restore access.</en>
        /// </lang>
        /// </summary>
        public const string Rejected = "Rejected";
    }
}
