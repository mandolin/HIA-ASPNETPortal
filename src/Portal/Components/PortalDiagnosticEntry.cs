using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    /// <zh-CN>表示门户运行期的结构化诊断事件。</zh-CN>
    /// <en>Represents a structured portal runtime diagnostic event.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    /// <zh-CN>P2.4 会将新增事件写入 NDJSON 文件。所有可写入字段必须由生产者在构造或反序列化前完成净化；本 DTO 不执行净化、授权或输出控制，绝不可承载原始 Cookie、连接串、密码、Token 或请求正文。</zh-CN>
    /// <en>P2.4 writes new events to NDJSON files. Producers must sanitize every persistable field before construction or deserialization; this DTO performs no sanitization, authorization, or output control and must never carry raw cookies, connection strings, passwords, tokens, or request bodies.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalDiagnosticEntry
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>与错误页和健康检查关联的稳定事件编号；不是可从客户端信任或授权的标识。</zh-CN>
        /// <en>Stable event identifier correlated with error pages and health checks; it is not an identifier to trust or authorize from client input.</en>
        /// </lang>
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>事件创建时的 UTC 时间，供受控查询使用半开区间过滤。</zh-CN>
        /// <en>UTC time at which the event was created, used by controlled queries with half-open interval filtering.</en>
        /// </lang>
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>诊断级别，通常为 Info、Warning 或 Error；DTO 不自行验证或规范化值。</zh-CN>
        /// <en>Diagnostic level, normally Info, Warning, or Error; the DTO does not validate or normalize the value itself.</en>
        /// </lang>
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>稳定事件分类；生产者负责保证其适于持久化和受控展示。</zh-CN>
        /// <en>Stable event category; producers ensure it is suitable for persistence and controlled display.</en>
        /// </lang>
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>已净化的事件摘要；不可赋入原始异常、请求正文或其他敏感原文。</zh-CN>
        /// <en>Sanitized event summary; raw exceptions, request bodies, and other sensitive source text must not be assigned.</en>
        /// </lang>
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>已净化异常的类型名称，不应包含异常消息或堆栈详情。</zh-CN>
        /// <en>Type name of the sanitized exception, which should not contain exception message or stack detail.</en>
        /// </lang>
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>已净化异常详情，仅供受控管理员详情页使用；DTO 本身不实施该展示授权。</zh-CN>
        /// <en>Sanitized exception detail for controlled administrator detail views only; the DTO itself does not enforce that display authorization.</en>
        /// </lang>
        /// </summary>
        public string ExceptionDetail { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>不含查询字符串值的请求路径；生产者负责剥离查询数据。</zh-CN>
        /// <en>Request path without query-string values; producers are responsible for stripping query data.</en>
        /// </lang>
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>请求对应的 HTTP 方法，仅作诊断事实记录。</zh-CN>
        /// <en>HTTP method associated with the request, recorded only as a diagnostic fact.</en>
        /// </lang>
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>当前已认证用户名；匿名请求为空。该字段不是重新认证或授权的依据。</zh-CN>
        /// <en>Current authenticated user name, empty for anonymous requests. This field is not a basis for re-authentication or authorization.</en>
        /// </lang>
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>客户端地址，属于诊断中的受控访问数据；不应向普通客户端回显。</zh-CN>
        /// <en>Client address, which is controlled-access diagnostic data and must not be echoed to ordinary clients.</en>
        /// </lang>
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>应用物理路径，仅供受控管理员详情页使用；DTO 本身不执行该访问限制。</zh-CN>
        /// <en>Application physical path for controlled administrator detail views only; the DTO itself does not enforce that access restriction.</en>
        /// </lang>
        /// </summary>
        public string PhysicalPath { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>客户端 User-Agent；生产者负责先净化并按受控诊断路径处理。</zh-CN>
        /// <en>Client User-Agent; producers sanitize it first and handle it through controlled diagnostic paths.</en>
        /// </lang>
        /// </summary>
        public string UserAgent { get; set; }
    }
}
