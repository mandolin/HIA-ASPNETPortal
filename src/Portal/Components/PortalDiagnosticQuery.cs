using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 诊断日志受限查询条件。
    /// Restricted diagnostic-log query criteria.
    /// </summary>
    public sealed class PortalDiagnosticQuery
    {
        /// <summary>
        /// 查询起始 UTC 日期时间，包含边界。
        /// Inclusive UTC start date and time.
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// 查询结束 UTC 日期时间，不包含边界。
        /// Exclusive UTC end date and time.
        /// </summary>
        public DateTime EndUtcExclusive { get; set; }

        /// <summary>
        /// 可选日志级别筛选。
        /// Optional log-level filter.
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// 可选日志分类筛选。
        /// Optional log-category filter.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 可选精确事件编号筛选。
        /// Optional exact event-id filter.
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// 从零开始的页码。
        /// Zero-based page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 请求页大小。
        /// Requested page size.
        /// </summary>
        public int PageSize { get; set; }
    }

    /// <summary>
    /// 诊断日志查询结果。
    /// Diagnostic-log query result.
    /// </summary>
    public sealed class PortalDiagnosticQueryResult
    {
        /// <summary>
        /// 创建查询结果。
        /// Creates a query result.
        /// </summary>
        public PortalDiagnosticQueryResult(
            IList<PortalDiagnosticEntry> entries,
            bool hasMore,
            bool wasTruncated)
        {
            Entries = entries ?? new List<PortalDiagnosticEntry>();
            HasMore = hasMore;
            WasTruncated = wasTruncated;
        }

        /// <summary>
        /// 当前页日志条目。
        /// Current-page diagnostic entries.
        /// </summary>
        public IList<PortalDiagnosticEntry> Entries { get; private set; }

        /// <summary>
        /// 是否还有下一页。
        /// Whether another page is available.
        /// </summary>
        public bool HasMore { get; private set; }

        /// <summary>
        /// 是否因服务器扫描上限而截断。
        /// Whether the server scan limit truncated the result.
        /// </summary>
        public bool WasTruncated { get; private set; }
    }
}
