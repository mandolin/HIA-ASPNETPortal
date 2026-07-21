namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：轻量待办状态的稳定值。
    ///
    /// English: Stable values for lightweight work-item states.
    /// </summary>
    /// <remarks>
    /// 中文：待办状态只描述任务处理状态，不替代业务对象自身的领域状态。
    ///
    /// English: Work-item status describes task handling only and does not replace the business object's domain status.
    /// </remarks>
    public static class PortalWorkItemStatuses
    {
        /// <summary>中文：待处理。English: Open and waiting for handling.</summary>
        public const string Open = "Open";

        /// <summary>中文：处理中。English: In progress.</summary>
        public const string InProgress = "InProgress";

        /// <summary>中文：已完成。English: Completed.</summary>
        public const string Completed = "Completed";

        /// <summary>中文：已取消。English: Cancelled.</summary>
        public const string Cancelled = "Cancelled";

        /// <summary>中文：已过期。English: Expired.</summary>
        public const string Expired = "Expired";
    }
}
