namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：轻量待办写入结果。
    ///
    /// English: Result of a lightweight work-item write operation.
    /// </summary>
    public sealed class PortalWorkItemResult
    {
        /// <summary>
        /// 中文：创建轻量待办写入结果。
        ///
        /// English: Creates a lightweight work-item write result.
        /// </summary>
        /// <param name="succeeded">中文：是否成功。English: Whether the operation succeeded.</param>
        /// <param name="workItemId">中文：待办标识。English: Work-item identifier.</param>
        /// <param name="message">中文：可展示说明。English: Display-safe message.</param>
        public PortalWorkItemResult(bool succeeded, long workItemId, string message)
        {
            Succeeded = succeeded;
            WorkItemId = workItemId;
            Message = message ?? string.Empty;
        }

        /// <summary>中文：是否成功。English: Whether the operation succeeded.</summary>
        public bool Succeeded { get; private set; }

        /// <summary>中文：待办标识。English: Work-item identifier.</summary>
        public long WorkItemId { get; private set; }

        /// <summary>中文：可展示说明。English: Display-safe message.</summary>
        public string Message { get; private set; }
    }
}
