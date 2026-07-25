namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办事件类型的稳定值。</zh-CN>
    ///   <en>Stable values for lightweight work-item event types.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些字符串会写入待办事件表和审计记录，属于跨版本可读数据的一部分；新增事件类型应追加新常量，不应重命名已有值。</zh-CN>
    ///   <en>These strings are persisted into work-item event tables and audit records, so they are part of cross-version readable data. Add new constants for new event types instead of renaming existing values.</en>
    /// </lang>
    /// </remarks>
    public static class PortalWorkItemEventTypes
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>待办已创建。</zh-CN>
        ///   <en>Work item was created.</en>
        /// </lang>
        /// </summary>
        public const string Created = "Created";

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办已认领。</zh-CN>
        ///   <en>Work item was claimed.</en>
        /// </lang>
        /// </summary>
        public const string Claimed = "Claimed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象已批准或确认通过。</zh-CN>
        ///   <en>Business object was approved or accepted.</en>
        /// </lang>
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象已驳回。</zh-CN>
        ///   <en>Business object was rejected.</en>
        /// </lang>
        /// </summary>
        public const string Rejected = "Rejected";

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办或业务对象已取消/关闭。</zh-CN>
        ///   <en>Work item or business object was cancelled or closed.</en>
        /// </lang>
        /// </summary>
        public const string Cancelled = "Cancelled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>追加办理备注。</zh-CN>
        ///   <en>Handling comment was added.</en>
        /// </lang>
        /// </summary>
        public const string Commented = "Commented";

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办已完成。</zh-CN>
        ///   <en>Work item was completed.</en>
        /// </lang>
        /// </summary>
        public const string Completed = "Completed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办已重新打开。</zh-CN>
        ///   <en>Work item was reopened.</en>
        /// </lang>
        /// </summary>
        public const string Reopened = "Reopened";
    }
}
