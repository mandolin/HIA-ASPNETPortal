namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办写入结果。</zh-CN>
    ///   <en>Result of a lightweight work-item write operation.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalWorkItemResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建轻量待办写入结果。</zh-CN>
        ///   <en>Creates a lightweight work-item write result.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded">
        /// <l>
        ///   <zh-CN>是否成功。</zh-CN>
        ///   <en>Whether the operation succeeded.</en>
        /// </l>
        /// </param>
        /// <param name="workItemId">
        /// <l>
        ///   <zh-CN>待办标识。</zh-CN>
        ///   <en>Work-item identifier.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>可展示说明。</zh-CN>
        ///   <en>Display-safe message.</en>
        /// </l>
        /// </param>
        public PortalWorkItemResult(bool succeeded, long workItemId, string message)
        {
            Succeeded = succeeded;
            WorkItemId = workItemId;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否成功。</zh-CN>
        ///   <en>Whether the operation succeeded.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办标识。</zh-CN>
        ///   <en>Work-item identifier.</en>
        /// </lang>
        /// </summary>
        public long WorkItemId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可展示说明。</zh-CN>
        ///   <en>Display-safe message.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }
}
