namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 单项系统健康检查结果。
    /// Result of one system health check.
    /// </summary>
    public sealed class PortalHealthCheckResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建单项健康检查结果。</zh-CN>
        ///   <en>Creates one health-check result.</en>
        /// </lang>
        /// </summary>
        /// <param name="category">
        /// <l zh-CN="检查所属分类，例如数据库、文件系统或运行时。" en="Category that owns the check, such as database, file system, or runtime." />
        /// </param>
        /// <param name="name">
        /// <l zh-CN="检查项名称。" en="Check item name." />
        /// </param>
        /// <param name="status">
        /// <l zh-CN="检查结果状态。" en="Check result status." />
        /// </param>
        /// <param name="summary">
        /// <l zh-CN="可直接展示给管理员的简短结论。" en="Short conclusion that can be shown directly to administrators." />
        /// </param>
        /// <param name="detail">
        /// <l zh-CN="可选详细信息；当前面向 Admin 页面，不承载密码、Token 或连接串。" en="Optional detail text; currently intended for Admin pages and must not carry passwords, tokens, or connection strings." />
        /// </param>
        /// <param name="eventId">
        /// <l zh-CN="关联诊断事件编号；无事件时为空。" en="Related diagnostic event id; empty when no event is associated." />
        /// </param>
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
