namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 用户注册审核状态常量。
    /// User registration review status constants.
    /// </summary>
    public static class PortalUserRegistrationStatuses
    {
        /// <summary>
        /// 已批准，允许登录。
        /// Approved and allowed to sign in.
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// 等待管理员审核。
        /// Waiting for administrator approval.
        /// </summary>
        public const string PendingApproval = "PendingApproval";

        /// <summary>
        /// 已拒绝。
        /// Rejected.
        /// </summary>
        public const string Rejected = "Rejected";
    }
}
