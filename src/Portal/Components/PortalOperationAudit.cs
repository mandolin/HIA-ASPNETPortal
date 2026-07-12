using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using ASPNET.StarterKit.Portal.Util;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 门户运营审计记录。
    /// Portal operations audit record.
    /// </summary>
    public sealed class PortalOperationAuditEntry
    {
        /// <summary>
        /// 审计主键。
        /// Audit primary key.
        /// </summary>
        public long AuditId { get; set; }

        /// <summary>
        /// 动作发生 UTC 时间。
        /// UTC time at which the action occurred.
        /// </summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary>
        /// 运营分类。
        /// Operations category.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 动作名称。
        /// Action name.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 动作结果。
        /// Action outcome.
        /// </summary>
        public string Outcome { get; set; }

        /// <summary>
        /// 操作者用户名。
        /// Actor user name.
        /// </summary>
        public string ActorUserName { get; set; }

        /// <summary>
        /// 目标类型。
        /// Target type.
        /// </summary>
        public string TargetType { get; set; }

        /// <summary>
        /// 非敏感目标标识。
        /// Non-sensitive target identifier.
        /// </summary>
        public string TargetId { get; set; }

        /// <summary>
        /// 已净化的动作摘要。
        /// Sanitized action summary.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 关联运行时事件编号。
        /// Related runtime diagnostics event id.
        /// </summary>
        public string RelatedEventId { get; set; }

        /// <summary>
        /// 客户端地址。
        /// Client address.
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 客户端 User-Agent。
        /// Client user-agent.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// 后续请求关联预留编号。
        /// Reserved correlation id for future request association.
        /// </summary>
        public string CorrelationId { get; set; }
    }

    /// <summary>
    /// 运营审计受限查询条件。
    /// Restricted operations-audit query criteria.
    /// </summary>
    public sealed class PortalOperationAuditQuery
    {
        /// <summary>
        /// 包含边界的 UTC 查询起点。
        /// Inclusive UTC query start.
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// 不包含边界的 UTC 查询终点。
        /// Exclusive UTC query end.
        /// </summary>
        public DateTime EndUtcExclusive { get; set; }

        /// <summary>
        /// 可选分类筛选。
        /// Optional category filter.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 可选动作筛选。
        /// Optional action filter.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 可选目标标识筛选。
        /// Optional target-id filter.
        /// </summary>
        public string TargetId { get; set; }

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
    /// 运营审计查询结果。
    /// Operations-audit query result.
    /// </summary>
    public sealed class PortalOperationAuditQueryResult
    {
        /// <summary>
        /// 创建运营审计查询结果。
        /// Creates an operations-audit query result.
        /// </summary>
        public PortalOperationAuditQueryResult(
            IList<PortalOperationAuditEntry> entries,
            bool hasMore,
            bool isAvailable)
        {
            Entries = entries ?? new List<PortalOperationAuditEntry>();
            HasMore = hasMore;
            IsAvailable = isAvailable;
        }

        /// <summary>
        /// 当前页记录。
        /// Current-page records.
        /// </summary>
        public IList<PortalOperationAuditEntry> Entries { get; private set; }

        /// <summary>
        /// 是否还有下一页。
        /// Whether another page is available.
        /// </summary>
        public bool HasMore { get; private set; }

        /// <summary>
        /// 审计表是否已部署并可查询。
        /// Whether the audit table is deployed and queryable.
        /// </summary>
        public bool IsAvailable { get; private set; }
    }

    /// <summary>
    /// 门户运营审计门面。
    /// Portal operations-audit facade.
    /// </summary>
    /// <remarks>
    /// 审计是高价值状态变更的追溯机制，不等同于运行时错误日志。旧数据库没有审计表时，
    /// 本门面会降级为只记录运行时错误，绝不阻断已经成功的业务动作。
    /// Auditing tracks high-value state changes and is distinct from runtime error logs. When legacy
    /// databases lack the audit table, this facade degrades to runtime diagnostics and never blocks
    /// an already-successful business action.
    /// </remarks>
    public static class PortalOperationAudit
    {
        private const string AuditTableName = "PortalCfg_OperationAudits";
        private const int DefaultPageSize = 50;
        private const int MaximumPageSize = 50;
        private const int MaximumPage = 99;
        private const int MaximumQueryDays = 31;

        /// <summary>
        /// 写入一条高价值状态变更审计记录。
        /// Records one high-value state-change audit entry.
        /// </summary>
        /// <param name="category">稳定分类，如 Registration 或 UserAdministration。Stable category.</param>
        /// <param name="action">稳定动作，如 Submit、Approve 或 AddRole。Stable action.</param>
        /// <param name="targetType">目标类型。Target type.</param>
        /// <param name="targetId">非敏感目标标识。Non-sensitive target id.</param>
        /// <param name="summary">不含密码或 Token 的动作摘要。Summary without passwords or tokens.</param>
        /// <param name="context">当前 HTTP 上下文。Current HTTP context.</param>
        /// <param name="relatedEventId">可选关联诊断事件编号。Optional related diagnostics event id.</param>
        public static void Record(
            string category,
            string action,
            string targetType,
            string targetId,
            string summary,
            HttpContext context = null,
            string relatedEventId = null)
        {
            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return;
                    }

                    connection.Open();
                    if (!IsAuditTableAvailable(connection))
                    {
                        return;
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
INSERT INTO [dbo].[PortalCfg_OperationAudits]
    ([OccurredUtc], [Category], [Action], [Outcome], [ActorUserName], [TargetType], [TargetId],
     [Summary], [RelatedEventId], [ClientIp], [UserAgent], [CorrelationId])
VALUES
    (@OccurredUtc, @Category, @Action, N'Success', @ActorUserName, @TargetType, @TargetId,
     @Summary, @RelatedEventId, @ClientIp, @UserAgent, @CorrelationId);";

                        command.Parameters.Add("@OccurredUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                        AddTextParameter(command, "@Category", 80, category, "General");
                        AddTextParameter(command, "@Action", 80, action, "Update");
                        AddTextParameter(command, "@ActorUserName", 100, GetActorUserName(context), "(anonymous)");
                        AddTextParameter(command, "@TargetType", 80, targetType, "Unknown");
                        AddTextParameter(command, "@TargetId", 200, targetId, string.Empty);
                        AddTextParameter(command, "@Summary", 500, summary, string.Empty);
                        AddTextParameter(command, "@RelatedEventId", 64, relatedEventId, string.Empty);
                        AddTextParameter(command, "@ClientIp", 64, GetClientIp(context), string.Empty);
                        AddTextParameter(command, "@UserAgent", 400, GetUserAgent(context), string.Empty);
                        AddTextParameter(command, "@CorrelationId", 64, string.Empty, string.Empty);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error(
                    "OperationAudit.Write",
                    "Writing an operation audit record failed. Category=" + category + "; Action=" + action,
                    exception,
                    context);
            }
        }

        /// <summary>
        /// 查询已部署审计表中的受限运营审计记录。
        /// Queries restricted operations-audit records from the deployed audit table.
        /// </summary>
        /// <param name="query">日期、分类、动作和分页等受限查询条件。Restricted date, category, action, and paging criteria.</param>
        /// <param name="context">用于记录查询失败诊断事件的当前 HTTP 上下文。Current HTTP context used when a query failure is diagnosed.</param>
        /// <returns>当前页审计记录，以及审计表可用状态。Current-page audit records and audit-table availability.</returns>
        public static PortalOperationAuditQueryResult Query(PortalOperationAuditQuery query, HttpContext context = null)
        {
            PortalOperationAuditQuery normalized = NormalizeQuery(query);
            var entries = new List<PortalOperationAuditEntry>();

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalOperationAuditQueryResult(entries, false, false);
                    }

                    connection.Open();
                    if (!IsAuditTableAvailable(connection))
                    {
                        return new PortalOperationAuditQueryResult(entries, false, false);
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT [AuditId], [OccurredUtc], [Category], [Action], [Outcome], [ActorUserName], [TargetType],
       [TargetId], [Summary], [RelatedEventId], [ClientIp], [UserAgent], [CorrelationId]
FROM [dbo].[PortalCfg_OperationAudits]
WHERE [OccurredUtc] >= @StartUtc
  AND [OccurredUtc] < @EndUtcExclusive
  AND (@Category = N'' OR [Category] = @Category)
  AND (@Action = N'' OR [Action] = @Action)
  AND (@TargetId = N'' OR [TargetId] = @TargetId)
ORDER BY [OccurredUtc] DESC, [AuditId] DESC
OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY;";

                        command.Parameters.Add("@StartUtc", SqlDbType.DateTime2).Value = normalized.StartUtc;
                        command.Parameters.Add("@EndUtcExclusive", SqlDbType.DateTime2).Value = normalized.EndUtcExclusive;
                        AddTextParameter(command, "@Category", 80, normalized.Category, string.Empty);
                        AddTextParameter(command, "@Action", 80, normalized.Action, string.Empty);
                        AddTextParameter(command, "@TargetId", 200, normalized.TargetId, string.Empty);
                        command.Parameters.Add("@Offset", SqlDbType.Int).Value = normalized.Page * normalized.PageSize;
                        command.Parameters.Add("@Take", SqlDbType.Int).Value = normalized.PageSize + 1;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                entries.Add(ReadEntry(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error("OperationAudit.Query", "Querying operation audits failed.", exception, context);
                return new PortalOperationAuditQueryResult(entries, false, false);
            }

            bool hasMore = entries.Count > normalized.PageSize;
            if (hasMore)
            {
                entries.RemoveAt(entries.Count - 1);
            }

            return new PortalOperationAuditQueryResult(entries, hasMore, true);
        }

        private static SqlConnection CreateConnection()
        {
            if (Global.Container == null)
            {
                return null;
            }

            string connectionString = Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);
            return string.IsNullOrWhiteSpace(connectionString) ? null : new SqlConnection(connectionString);
        }

        private static bool IsAuditTableAvailable(SqlConnection connection)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[" + AuditTableName + "]', N'U') IS NULL THEN 0 ELSE 1 END";
                object value = command.ExecuteScalar();
                return value != null && Convert.ToInt32(value) == 1;
            }
        }

        private static void AddTextParameter(
            SqlCommand command,
            string parameterName,
            int size,
            string value,
            string fallback)
        {
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        private static PortalOperationAuditEntry ReadEntry(SqlDataReader reader)
        {
            return new PortalOperationAuditEntry
            {
                AuditId = reader.GetInt64(0),
                OccurredUtc = reader.GetDateTime(1),
                Category = reader.GetString(2),
                Action = reader.GetString(3),
                Outcome = reader.GetString(4),
                ActorUserName = reader.GetString(5),
                TargetType = reader.GetString(6),
                TargetId = reader.GetString(7),
                Summary = reader.GetString(8),
                RelatedEventId = ReadNullableString(reader, 9),
                ClientIp = ReadNullableString(reader, 10),
                UserAgent = ReadNullableString(reader, 11),
                CorrelationId = ReadNullableString(reader, 12)
            };
        }

        private static string ReadNullableString(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static PortalOperationAuditQuery NormalizeQuery(PortalOperationAuditQuery query)
        {
            DateTime nowUtc = DateTime.UtcNow.Date;
            DateTime startUtc = query == null || query.StartUtc == DateTime.MinValue
                ? nowUtc.AddDays(-6)
                : query.StartUtc.Date;
            DateTime endUtcExclusive = query == null || query.EndUtcExclusive == DateTime.MinValue
                ? nowUtc.AddDays(1)
                : query.EndUtcExclusive.Date;

            if (endUtcExclusive <= startUtc)
            {
                endUtcExclusive = startUtc.AddDays(1);
            }

            if ((endUtcExclusive - startUtc).TotalDays > MaximumQueryDays)
            {
                endUtcExclusive = startUtc.AddDays(MaximumQueryDays);
            }

            return new PortalOperationAuditQuery
            {
                StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
                EndUtcExclusive = DateTime.SpecifyKind(endUtcExclusive, DateTimeKind.Utc),
                Category = NormalizeFilter(query == null ? null : query.Category, 80),
                Action = NormalizeFilter(query == null ? null : query.Action, 80),
                TargetId = NormalizeFilter(query == null ? null : query.TargetId, 200),
                Page = Math.Max(0, Math.Min(query == null ? 0 : query.Page, MaximumPage)),
                PageSize = Math.Max(1, Math.Min(query == null || query.PageSize <= 0 ? DefaultPageSize : query.PageSize, MaximumPageSize))
            };
        }

        private static string NormalizeFilter(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim();
            return normalized.Substring(0, Math.Min(normalized.Length, maximumLength));
        }

        private static string GetActorUserName(HttpContext context)
        {
            return context == null || context.User == null || context.User.Identity == null
                ? string.Empty
                : context.User.Identity.Name;
        }

        private static string GetClientIp(HttpContext context)
        {
            return context == null || context.Request == null ? string.Empty : context.Request.UserHostAddress;
        }

        private static string GetUserAgent(HttpContext context)
        {
            return context == null || context.Request == null ? string.Empty : context.Request.UserAgent;
        }
    }
}
