namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：轻量待办事件类型的稳定值。
    ///
    /// English: Stable values for lightweight work-item event types.
    /// </summary>
    public static class PortalWorkItemEventTypes
    {
        /// <summary>中文：待办已创建。English: Work item was created.</summary>
        public const string Created = "Created";

        /// <summary>中文：待办已认领。English: Work item was claimed.</summary>
        public const string Claimed = "Claimed";

        /// <summary>中文：业务对象已批准或确认通过。English: Business object was approved or accepted.</summary>
        public const string Approved = "Approved";

        /// <summary>中文：业务对象已驳回。English: Business object was rejected.</summary>
        public const string Rejected = "Rejected";

        /// <summary>中文：待办或业务对象已取消/关闭。English: Work item or business object was cancelled or closed.</summary>
        public const string Cancelled = "Cancelled";

        /// <summary>中文：追加办理备注。English: Handling comment was added.</summary>
        public const string Commented = "Commented";

        /// <summary>中文：待办已完成。English: Work item was completed.</summary>
        public const string Completed = "Completed";

        /// <summary>中文：待办已重新打开。English: Work item was reopened.</summary>
        public const string Reopened = "Reopened";
    }
}
