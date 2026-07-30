using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    /// <zh-CN>表示诊断日志的受限查询条件。</zh-CN>
    /// <en>Represents restricted query criteria for diagnostic logs.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    /// <zh-CN>本 DTO 仅承载筛选和分页意图，不读取文件、不验证管理员权限，也不规范化调用方输入；查询服务负责这些边界。</zh-CN>
    /// <en>This DTO carries only filtering and paging intent: it neither reads files, validates administrator permission, nor normalizes caller input; the query service owns those boundaries.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalDiagnosticQuery
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>查询起始 UTC 时间，包含该边界。</zh-CN>
        /// <en>UTC query start time, inclusive of this boundary.</en>
        /// </lang>
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>查询结束 UTC 时间，不包含该边界；与起始时间组成半开区间。</zh-CN>
        /// <en>UTC query end time, exclusive of this boundary; together with start time it forms a half-open interval.</en>
        /// </lang>
        /// </summary>
        public DateTime EndUtcExclusive { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>可选日志级别筛选；空值语义由查询服务定义。</zh-CN>
        /// <en>Optional log-level filter; the query service defines empty-value semantics.</en>
        /// </lang>
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>可选日志分类筛选；DTO 不将其解释为授权范围。</zh-CN>
        /// <en>Optional log-category filter; the DTO does not interpret it as an authorization scope.</en>
        /// </lang>
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>可选精确事件编号筛选；服务负责格式、长度和可见性控制。</zh-CN>
        /// <en>Optional exact event-identifier filter; the service controls format, length, and visibility.</en>
        /// </lang>
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>从零开始的请求页码；边界限制由查询服务实施。</zh-CN>
        /// <en>Zero-based requested page number; the query service enforces bounds.</en>
        /// </lang>
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>请求页大小；服务可对其进行限制以保护诊断扫描成本。</zh-CN>
        /// <en>Requested page size; the service may limit it to protect diagnostic scanning cost.</en>
        /// </lang>
        /// </summary>
        public int PageSize { get; set; }
    }

    /// <summary>
    /// <lang>
    /// <zh-CN>表示受控诊断日志查询的当前页结果。</zh-CN>
    /// <en>Represents the current-page result of a controlled diagnostic-log query.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalDiagnosticQueryResult
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>创建诊断查询结果，并在调用方未提供集合时回退为空集合。</zh-CN>
        /// <en>Creates a diagnostic query result and falls back to an empty collection when the caller supplies none.</en>
        /// </lang>
        /// </summary>
        /// <param name="entries"><lang><zh-CN>当前页诊断条目；null 会回退为新的空集合，不生成或读取条目。</zh-CN><en>Current-page diagnostic entries; null falls back to a new empty collection without creating or reading entries.</en></lang></param>
        /// <param name="hasMore"><lang><zh-CN>是否还有下一页，由查询服务根据受限扫描结果决定。</zh-CN><en>Whether another page exists, determined by the query service from restricted scan results.</en></lang></param>
        /// <param name="wasTruncated"><lang><zh-CN>是否因服务端扫描上限截断结果，不等同于客户端筛选成功或失败。</zh-CN><en>Whether a server-side scan limit truncated results; it is not equivalent to client-filter success or failure.</en></lang></param>
        public PortalDiagnosticQueryResult(
            IList<PortalDiagnosticEntry> entries,
            bool hasMore,
            bool wasTruncated)
        {
            // <lang>
            //   <zh-CN>将 null 条目集合回退为空集合，使结果消费者可安全枚举；该回退不读取日志、不制造条目，也不隐藏服务端截断事实。</zh-CN>
            //   <en>Fall back from a null entry collection to an empty collection so result consumers can enumerate safely; this fallback neither reads logs nor fabricates entries nor hides server-side truncation facts.</en>
            // </lang>
            Entries = entries ?? new List<PortalDiagnosticEntry>();
            HasMore = hasMore;
            WasTruncated = wasTruncated;
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>当前页诊断条目；调用方仍须按管理员展示路径处理其中受控字段。</zh-CN>
        /// <en>Current-page diagnostic entries; callers still handle controlled fields through administrator display paths.</en>
        /// </lang>
        /// </summary>
        public IList<PortalDiagnosticEntry> Entries { get; private set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>是否还有下一页结果；不表示扫描范围未受服务端上限限制。</zh-CN>
        /// <en>Whether another result page is available; it does not mean the scan range was unconstrained by server limits.</en>
        /// </lang>
        /// </summary>
        public bool HasMore { get; private set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>是否因服务器扫描上限而截断；为 true 时当前页不是完整匹配集的证明。</zh-CN>
        /// <en>Whether a server scan limit truncated the result; when true, the current page is not proof of a complete match set.</en>
        /// </lang>
        /// </summary>
        public bool WasTruncated { get; private set; }
    }
}
