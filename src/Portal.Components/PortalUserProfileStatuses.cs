namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>P6.2 用户资料扩展表使用的账号生命周期状态常量。</zh-CN>
    ///   <en>Account lifecycle status constants used by the P6.2 user-profile extension table.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些状态值属于数据库和业务审计可见数据。后续如果引入更细的账号状态机，应追加新状态并保留旧值解释，而不是直接改写已有字符串。</zh-CN>
    ///   <en>These status values are visible in database data and business audits. If a richer account state machine is introduced later, add new states and preserve the meaning of existing strings instead of rewriting them.</en>
    /// </lang>
    /// </remarks>
    public static class PortalUserProfileStatuses
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>账号可进行常规登录验证。</zh-CN>
        ///   <en>The account may continue through normal sign-in validation.</en>
        /// </lang>
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号注册已提交但仍待管理员审核。</zh-CN>
        ///   <en>The account registration has been submitted and is awaiting administrator approval.</en>
        /// </lang>
        /// </summary>
        public const string PendingApproval = "PendingApproval";

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号已存在，但尚未确认员工绑定。</zh-CN>
        ///   <en>The account exists, but employee binding has not yet been confirmed.</en>
        /// </lang>
        /// </summary>
        public const string PendingEmployeeBinding = "PendingEmployeeBinding";

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号被管理员禁用。</zh-CN>
        ///   <en>The account has been disabled by an administrator.</en>
        /// </lang>
        /// </summary>
        public const string Disabled = "Disabled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号因员工离职或等价业务状态而不可登录。</zh-CN>
        ///   <en>The account cannot sign in because the employee has left or reached an equivalent business state.</en>
        /// </lang>
        /// </summary>
        public const string Left = "Left";

        /// <summary>
        /// <lang>
        ///   <zh-CN>预留的临时锁定状态。</zh-CN>
        ///   <en>Reserved temporary lockout status.</en>
        /// </lang>
        /// </summary>
        public const string Locked = "Locked";
    }
}
