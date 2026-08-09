namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>单项系统健康检查结果。</zh-CN>
    ///   <en>Result of one system health check.</en>
    /// </lang>
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
            // <lang>
            //   <zh-CN>分类文本归一为空字符串，保持结果可展示且不把 null 传播到健康页。</zh-CN>
            //   <en>Normalize category text to an empty string so the result remains displayable without propagating null to the health page.</en>
            // </lang>
            Category = category ?? string.Empty;

            // <lang>
            //   <zh-CN>检查名称归一为空字符串；名称是展示标签，不在此解释为路径、权限或配置键。</zh-CN>
            //   <en>Normalize the check name to an empty string; the name is a display label and is not interpreted here as a path, permission, or configuration key.</en>
            // </lang>
            Name = name ?? string.Empty;

            // <lang>
            //   <zh-CN>保留调用方计算出的枚举状态，不在结果模型构造期重新推断健康级别。</zh-CN>
            //   <en>Retain the status enum calculated by the caller rather than re-inferring health level during result construction.</en>
            // </lang>
            Status = status;

            // <lang>
            //   <zh-CN>结论文本归一为空字符串；该字段面向管理员展示，不应承载秘密或异常原文。</zh-CN>
            //   <en>Normalize the summary to an empty string; this administrator-facing field must not carry secrets or raw exception text.</en>
            // </lang>
            Summary = summary ?? string.Empty;

            // <lang>
            //   <zh-CN>详细文本保持安全回退语义；诊断/健康调用方负责在传入前净化路径、请求和错误内容。</zh-CN>
            //   <en>Retain safe fallback semantics for detail text; diagnostics and health callers must sanitize paths, requests, and errors before passing them.</en>
            // </lang>
            Detail = detail ?? string.Empty;

            // <lang>
            //   <zh-CN>没有关联事件时保存稳定空字符串，避免健康结果模型产生隐式事件编号。</zh-CN>
            //   <en>Store a stable empty string when no event is associated so the result model never invents an event id.</en>
            // </lang>
            EventId = eventId ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查分类。</zh-CN>
        ///   <en>Check category.</en>
        /// </lang>
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查名称。</zh-CN>
        ///   <en>Check name.</en>
        /// </lang>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查状态。</zh-CN>
        ///   <en>Check status.</en>
        /// </lang>
        /// </summary>
        public PortalHealthStatus Status { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>简短结论。</zh-CN>
        ///   <en>Short summary.</en>
        /// </lang>
        /// </summary>
        public string Summary { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>详细信息。</zh-CN>
        ///   <en>Detail text.</en>
        /// </lang>
        /// </summary>
        public string Detail { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>关联诊断事件编号。</zh-CN>
        ///   <en>Related diagnostics event id.</en>
        /// </lang>
        /// </summary>
        public string EventId { get; private set; }
    }
}
