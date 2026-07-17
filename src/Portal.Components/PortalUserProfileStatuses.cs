namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：P6.2 用户资料扩展表使用的账号生命周期状态常量。
    ///
    /// English: Account lifecycle status constants used by the P6.2 user-profile extension table.
    /// </summary>
    public static class PortalUserProfileStatuses
    {
        /// <summary>
        /// 中文：账号可进行常规登录验证。
        ///
        /// English: The account may continue through normal sign-in validation.
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// 中文：账号注册已提交但仍待管理员审核。
        ///
        /// English: The account registration has been submitted and is awaiting administrator approval.
        /// </summary>
        public const string PendingApproval = "PendingApproval";

        /// <summary>
        /// 中文：账号已存在，但尚未确认员工绑定。
        ///
        /// English: The account exists, but employee binding has not yet been confirmed.
        /// </summary>
        public const string PendingEmployeeBinding = "PendingEmployeeBinding";

        /// <summary>
        /// 中文：账号被管理员禁用。
        ///
        /// English: The account has been disabled by an administrator.
        /// </summary>
        public const string Disabled = "Disabled";

        /// <summary>
        /// 中文：账号因员工离职或等价业务状态而不可登录。
        ///
        /// English: The account cannot sign in because the employee has left or reached an equivalent business state.
        /// </summary>
        public const string Left = "Left";

        /// <summary>
        /// 中文：预留的临时锁定状态。
        ///
        /// English: Reserved temporary lockout status.
        /// </summary>
        public const string Locked = "Locked";
    }
}
