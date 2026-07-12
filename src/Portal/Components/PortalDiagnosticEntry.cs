using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 门户运行期结构化诊断事件。
    /// Structured portal runtime diagnostic event.
    /// </summary>
    /// <remarks>
    /// P2.4 将新增事件写入 NDJSON 文件。所有可写入字段在创建时必须先经过净化，
    /// 因此本类型不应用于承载原始 Cookie、连接串、密码、Token 或请求正文。
    /// P2.4 writes new events to NDJSON files. Every persisted field must be sanitized
    /// before construction; this type must not carry raw cookies, connection strings,
    /// passwords, tokens, or request bodies.
    /// </remarks>
    public sealed class PortalDiagnosticEntry
    {
        /// <summary>
        /// 与错误页和健康检查关联的稳定事件编号。
        /// Stable event id correlated with error pages and health checks.
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// 事件发生的 UTC 时间。
        /// UTC time at which the event was created.
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// 诊断级别：Info、Warning 或 Error。
        /// Diagnostic level: Info, Warning, or Error.
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// 稳定分类名称。
        /// Stable event category.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 已净化的事件摘要。
        /// Sanitized event summary.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 已净化异常的类型名称。
        /// Type name of the sanitized exception.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// 已净化的异常详情，仅供受控管理员详情页展示。
        /// Sanitized exception detail for controlled administrator detail views only.
        /// </summary>
        public string ExceptionDetail { get; set; }

        /// <summary>
        /// 请求路径，不包含查询字符串值。
        /// Request path without query-string values.
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// HTTP 请求方法。
        /// HTTP request method.
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// 当前已认证用户名；匿名请求为空。
        /// Current authenticated user name; empty for anonymous requests.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 客户端地址。
        /// Client address.
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 应用物理路径，仅向管理员详情页展示。
        /// Application physical path, shown only on administrator detail views.
        /// </summary>
        public string PhysicalPath { get; set; }

        /// <summary>
        /// 客户端 User-Agent。
        /// Client user-agent.
        /// </summary>
        public string UserAgent { get; set; }
    }
}
