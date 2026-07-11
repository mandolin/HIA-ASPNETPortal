namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 单项系统健康检查结果。
    /// Result of one system health check.
    /// </summary>
    public sealed class PortalHealthCheckResult
    {
        public PortalHealthCheckResult(
            string category,
            string name,
            PortalHealthStatus status,
            string summary,
            string detail = "",
            string eventId = "")
        {
            Category = category ?? string.Empty;
            Name = name ?? string.Empty;
            Status = status;
            Summary = summary ?? string.Empty;
            Detail = detail ?? string.Empty;
            EventId = eventId ?? string.Empty;
        }

        /// <summary>
        /// 检查分类。
        /// Check category.
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// 检查名称。
        /// Check name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 检查状态。
        /// Check status.
        /// </summary>
        public PortalHealthStatus Status { get; private set; }

        /// <summary>
        /// 简短结论。
        /// Short summary.
        /// </summary>
        public string Summary { get; private set; }

        /// <summary>
        /// 详细信息。
        /// Detail text.
        /// </summary>
        public string Detail { get; private set; }

        /// <summary>
        /// 关联诊断事件编号。
        /// Related diagnostics event id.
        /// </summary>
        public string EventId { get; private set; }
    }
}
