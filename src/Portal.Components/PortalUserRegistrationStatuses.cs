namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：用户注册审核状态的稳定常量。
    ///
    /// English: Stable constants for user registration-review statuses.
    /// </summary>
    public static class PortalUserRegistrationStatuses
    {
        /// <summary>
        /// 中文：已批准，在注册审核元数据可读取时允许登录。
        ///
        /// English: Approved and allowed to sign in when registration-review metadata is available.
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// 中文：等待管理员审核，不允许登录。
        ///
        /// English: Waiting for administrator approval and not allowed to sign in.
        /// </summary>
        public const string PendingApproval = "PendingApproval";

        /// <summary>
        /// 中文：已拒绝，不允许登录；管理员后续仍可批准以恢复访问。
        ///
        /// English: Rejected and not allowed to sign in; an administrator may approve later to restore access.
        /// </summary>
        public const string Rejected = "Rejected";
    }
}
