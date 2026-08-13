namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办状态的稳定值。</zh-CN>
    ///   <en>Stable values for lightweight work-item states.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>待办状态只描述任务处理状态，不替代业务对象自身的领域状态。这里的字符串会写入数据库、审计和文档，不能随意重命名；本类也不定义合法迁移、终态、超期计算或授权规则，调用方必须在领域边界另行执行这些判断。</zh-CN>
    ///   <en>Work-item status describes task handling only and does not replace the business object's domain status. These strings are persisted to the database, audit records, and documentation, so they must not be casually renamed. This class defines neither legal transitions, terminal states, expiration calculation, nor authorization rules; callers must enforce those decisions at the domain boundary.</en>
    /// </lang>
    /// </remarks>
    public static class PortalWorkItemStatuses
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>待处理；待办已创建但尚未进入办理。</zh-CN>
        ///   <en>Open and waiting for handling; the work item has been created but not started.</en>
        /// </lang>
        /// </summary>
        public const string Open = "Open";

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理中；当前仅作为预留稳定值。</zh-CN>
        ///   <en>In progress; currently reserved as a stable value.</en>
        /// </lang>
        /// </summary>
        public const string InProgress = "InProgress";

        /// <summary>
        /// <lang>
        ///   <zh-CN>已完成；表示待办处理完毕，不必然说明业务对象已被改写。</zh-CN>
        ///   <en>Completed; the work item is handled, which does not necessarily mean the business object was rewritten.</en>
        /// </lang>
        /// </summary>
        public const string Completed = "Completed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>已取消；表示待办不再需要继续处理。</zh-CN>
        ///   <en>Cancelled; the work item no longer needs further handling.</en>
        /// </lang>
        /// </summary>
        public const string Cancelled = "Cancelled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>已过期；表示超过业务或运维设定的有效处理窗口。</zh-CN>
        ///   <en>Expired; the item is outside the valid handling window defined by business or operations rules.</en>
        /// </lang>
        /// </summary>
        public const string Expired = "Expired";
    }
}
