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
    /// <lang>
    ///   <zh-CN>门户运营审计记录的只读传输模型；字段只承载已净化、可授权查看的审计事实。</zh-CN>
    ///   <en>Read-only transport model for a Portal operations-audit record; fields carry only sanitized audit facts that an authorized caller may view.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalOperationAuditEntry
    {
        /// <summary><lang><zh-CN>审计表的稳定主键，用于排序和关联，不表示业务对象本身。</zh-CN><en>Stable audit-table key used for ordering and correlation; it is not the business-object identifier.</en></lang></summary>
        public long AuditId { get; set; }

        /// <summary><lang><zh-CN>动作发生的 UTC 时间；查询使用其作为包含起点、排除终点的时间范围字段。</zh-CN><en>UTC instant at which the action occurred; queries use it in an inclusive-start, exclusive-end time range.</en></lang></summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary><lang><zh-CN>稳定的运营分类，用于受限筛选和事件归组。</zh-CN><en>Stable operations category used for restricted filtering and event grouping.</en></lang></summary>
        public string Category { get; set; }

        /// <summary><lang><zh-CN>稳定的动作名称，用于表示已发生的状态变化或管理操作。</zh-CN><en>Stable action name that represents the completed state change or administrative operation.</en></lang></summary>
        public string Action { get; set; }

        /// <summary><lang><zh-CN>记录时确定的动作结果；写入门面当前只写成功事实，失败会转入诊断而不伪造审计成功记录。</zh-CN><en>Outcome known when the audit is recorded; the facade currently writes successful facts only, while failures go to diagnostics instead of fabricating a successful audit record.</en></lang></summary>
        public string Outcome { get; set; }

        /// <summary><lang><zh-CN>执行动作的门户用户名；没有可用 HTTP 身份时写入受限回退值。</zh-CN><en>Portal user name that performed the action; a constrained fallback is written when no HTTP identity is available.</en></lang></summary>
        public string ActorUserName { get; set; }

        /// <summary><lang><zh-CN>被操作资源的稳定类型，避免将领域细节隐含在摘要文本中。</zh-CN><en>Stable type of the operated resource, avoiding domain detail being hidden in free-form summary text.</en></lang></summary>
        public string TargetType { get; set; }

        /// <summary><lang><zh-CN>经调用方确认可审计的非敏感目标标识；不得放入口令、令牌或业务正文。</zh-CN><en>Non-sensitive target identifier approved by the caller for auditing; it must not contain passwords, tokens, or business body text.</en></lang></summary>
        public string TargetId { get; set; }

        /// <summary><lang><zh-CN>写入前按列长度净化的动作摘要；它服务运营追溯而不是原始请求存档。</zh-CN><en>Action summary sanitized to the column limit before writing; it serves operations traceability rather than raw-request archival.</en></lang></summary>
        public string Summary { get; set; }

        /// <summary><lang><zh-CN>可选运行时诊断事件编号，用于关联失败调查而不复制诊断正文。</zh-CN><en>Optional runtime-diagnostics event identifier used to correlate failure investigation without copying diagnostic body text.</en></lang></summary>
        public string RelatedEventId { get; set; }

        /// <summary><lang><zh-CN>请求可用时的客户端地址；该值会受写入净化和数据库列长度限制。</zh-CN><en>Client address when the request is available; the value remains subject to write sanitization and database column limits.</en></lang></summary>
        public string ClientIp { get; set; }

        /// <summary><lang><zh-CN>请求可用时的客户端 User-Agent，仅作为受限运营上下文，不作为身份凭据。</zh-CN><en>Client User-Agent when the request is available, retained only as restricted operations context and never as an identity credential.</en></lang></summary>
        public string UserAgent { get; set; }

        /// <summary><lang><zh-CN>为未来请求关联预留的编号；当前写入路径保持为空，避免凭空声明关联关系。</zh-CN><en>Identifier reserved for future request correlation; the current write path keeps it empty to avoid claiming a correlation that does not exist.</en></lang></summary>
        public string CorrelationId { get; set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>运营审计的受限查询输入；门面会规范化日期、筛选值和分页，以控制查询范围。</zh-CN>
    ///   <en>Restricted input for operations-audit queries; the facade normalizes dates, filters, and paging to bound the query scope.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalOperationAuditQuery
    {
        /// <summary><lang><zh-CN>包含边界的 UTC 查询起点；缺省时门面选择最近的受限时间窗。</zh-CN><en>Inclusive UTC query start; when omitted, the facade selects a recent bounded window.</en></lang></summary>
        public DateTime StartUtc { get; set; }

        /// <summary><lang><zh-CN>不包含边界的 UTC 查询终点；与起点共同避免跨页或相邻日期重复。</zh-CN><en>Exclusive UTC query end; together with the start it avoids duplication across adjacent dates or pages.</en></lang></summary>
        public DateTime EndUtcExclusive { get; set; }

        /// <summary><lang><zh-CN>可选稳定分类筛选；空值表示不按分类缩小已受限的时间范围。</zh-CN><en>Optional stable-category filter; an empty value does not further narrow the already bounded time range.</en></lang></summary>
        public string Category { get; set; }

        /// <summary><lang><zh-CN>可选稳定动作筛选；输入会在执行 SQL 前去空白并截断。</zh-CN><en>Optional stable-action filter; input is trimmed and bounded before SQL execution.</en></lang></summary>
        public string Action { get; set; }

        /// <summary><lang><zh-CN>可选非敏感目标标识筛选；它不是自由文本搜索接口。</zh-CN><en>Optional non-sensitive target-identifier filter; it is not a free-text search interface.</en></lang></summary>
        public string TargetId { get; set; }

        /// <summary><lang><zh-CN>从零开始的请求页码；门面将其夹紧，避免极大 OFFSET 造成不可控数据库成本。</zh-CN><en>Zero-based requested page number; the facade clamps it to avoid unbounded database cost from a large OFFSET.</en></lang></summary>
        public int Page { get; set; }

        /// <summary><lang><zh-CN>请求页大小；门面保留一条探测记录以报告是否还有下一页。</zh-CN><en>Requested page size; the facade retains one probe row so it can report whether another page exists.</en></lang></summary>
        public int PageSize { get; set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>运营审计查询的受限结果与表可用状态。</zh-CN>
    ///   <en>Restricted operations-audit query result together with audit-table availability.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalOperationAuditQueryResult
    {
        /// <summary><lang><zh-CN>创建查询结果，并在调用方没有提供列表时返回空集合以保持 UI 绑定稳定。</zh-CN><en>Creates a query result and returns an empty collection when the caller supplied no list, keeping UI binding stable.</en></lang></summary>
        /// <param name="entries"><l><zh-CN>已读取且已受 SQL 范围限制的当前页记录。</zh-CN><en>Current-page records already read within the SQL range.</en></l></param>
        /// <param name="hasMore"><l><zh-CN>探测记录表明是否存在下一页。</zh-CN><en>Whether the probe record indicates that another page exists.</en></l></param>
        /// <param name="isAvailable"><l><zh-CN>审计表是否存在并成功完成本次查询。</zh-CN><en>Whether the audit table exists and this query completed successfully.</en></l></param>
        public PortalOperationAuditQueryResult(
            IList<PortalOperationAuditEntry> entries,
            bool hasMore,
            bool isAvailable)
        {
            Entries = entries ?? new List<PortalOperationAuditEntry>();
            HasMore = hasMore;
            IsAvailable = isAvailable;
        }

        /// <summary><lang><zh-CN>当前页的受限审计记录，不包含下一页探测行。</zh-CN><en>Restricted audit records for the current page, excluding the next-page probe row.</en></lang></summary>
        public IList<PortalOperationAuditEntry> Entries { get; private set; }

        /// <summary><lang><zh-CN>是否检测到下一页；它不承诺在随后的请求前审计数据不会发生变化。</zh-CN><en>Whether a next page was detected; it does not promise that audit data remains unchanged before the subsequent request.</en></lang></summary>
        public bool HasMore { get; private set; }

        /// <summary><lang><zh-CN>审计表是否在当前连接中已部署且可查询；false 是兼容降级信号，不是权限结论。</zh-CN><en>Whether the audit table is deployed and queryable on the current connection; false is a compatibility-degradation signal, not an authorization decision.</en></lang></summary>
        public bool IsAvailable { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>门户运营审计门面：在业务动作成功后尽力记录可追溯的低敏事实，并为旧 schema 提供不阻断业务的兼容降级。</zh-CN>
    ///   <en>Portal operations-audit facade: best-effort records traceable low-sensitivity facts after a business action succeeds and provides compatibility degradation for legacy schemas without blocking business.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>审计追溯高价值状态变化，但不替代运行时错误日志。旧数据库缺少审计表、连接不可用或写入失败时，本门面只记录诊断并保持已成功业务动作的结果，不回滚或重试业务动作。</zh-CN>
    ///   <en>Auditing traces high-value state changes but does not replace runtime error logging. When a legacy database lacks the audit table, the connection is unavailable, or a write fails, this facade records diagnostics only and preserves the already-successful business outcome without rolling it back or retrying it.</en>
    /// </lang>
    /// </remarks>
    public static class PortalOperationAudit
    {
        // <lang>
        //   <zh-CN>审计表名只作为受控 schema 常量参与 OBJECT_ID 检查；调用方不能将表名作为输入传入。</zh-CN>
        //   <en>The audit-table name participates in OBJECT_ID checks only as a controlled schema constant; callers cannot supply a table name.</en>
        // </lang>
        private const string AuditTableName = "PortalCfg_OperationAudits";

        // <lang>
        //   <zh-CN>默认页大小与最大页大小相同，确保缺省与显式请求不会意外扩大单次审计读取量。</zh-CN>
        //   <en>The default and maximum page sizes are equal so neither omitted nor explicit input can unexpectedly enlarge one audit read.</en>
        // </lang>
        private const int DefaultPageSize = 50;
        private const int MaximumPageSize = 50;

        // <lang>
        //   <zh-CN>最大页码限制 OFFSET 深度，最大查询天数限制单次时间范围；两者共同保护审计后台的数据库成本。</zh-CN>
        //   <en>The maximum page limits OFFSET depth and the maximum query days limit one time range; together they protect the database cost of the audit administration page.</en>
        // </lang>
        private const int MaximumPage = 99;
        private const int MaximumQueryDays = 31;

        /// <summary>
        /// <lang>
        ///   <zh-CN>尽力写入一条高价值状态变化的运营审计记录；本方法只应在主业务动作已成功后调用。</zh-CN>
        ///   <en>Best-effort writes one operations-audit record for a high-value state change; callers must invoke it only after the primary business action has succeeded.</en>
        /// </lang>
        /// </summary>
        /// <param name="category"><l><zh-CN>稳定分类，例如 Registration 或 UserAdministration。</zh-CN><en>Stable category, such as Registration or UserAdministration.</en></l></param>
        /// <param name="action"><l><zh-CN>稳定动作，例如 Submit、Approve 或 AddRole。</zh-CN><en>Stable action, such as Submit, Approve, or AddRole.</en></l></param>
        /// <param name="targetType"><l><zh-CN>被操作资源的稳定类型。</zh-CN><en>Stable type of the operated resource.</en></l></param>
        /// <param name="targetId"><l><zh-CN>可安全审计的非敏感目标标识。</zh-CN><en>Non-sensitive target identifier that is safe to audit.</en></l></param>
        /// <param name="summary"><l><zh-CN>不含口令、Token 或业务正文的低敏摘要。</zh-CN><en>Low-sensitivity summary without passwords, tokens, or business body text.</en></l></param>
        /// <param name="context"><l><zh-CN>可选 HTTP 上下文，用于受限读取动作人和请求元数据及诊断关联。</zh-CN><en>Optional HTTP context used for restricted actor/request metadata and diagnostic correlation.</en></l></param>
        /// <param name="relatedEventId"><l><zh-CN>可选的既有诊断事件编号，不复制事件正文。</zh-CN><en>Optional existing diagnostics event identifier; its body is not copied.</en></l></param>
        public static void Record(
            string category,
            string action,
            string targetType,
            string targetId,
            string summary,
            HttpContext context = null,
            string relatedEventId = null)
        {
            // <lang>
            //   <zh-CN>审计失败不得影响已经成功的主业务动作；整个写入路径因此保持尽力而为并仅在 catch 中记录诊断。</zh-CN>
            //   <en>An audit failure must not affect an already-successful primary business action, so the entire write path remains best-effort and records diagnostics only in catch.</en>
            // </lang>
            try
            {
                // <lang>
                //   <zh-CN>连接由当前 Unity 外置配置解析；返回 null 代表当前实例不具备可安全使用的审计连接。</zh-CN>
                //   <en>The connection is resolved from the current Unity external configuration; null means this instance has no audit connection safe to use.</en>
                // </lang>
                using (SqlConnection connection = CreateConnection())
                {
                    // <lang>
                    //   <zh-CN>不在缺失连接时尝试构造替代连接或猜测配置，直接静默降级以避免将审计路径变成可用性故障源。</zh-CN>
                    //   <en>Do not construct a fallback connection or guess configuration when it is missing; degrade directly so the audit path cannot become an availability-failure source.</en>
                    // </lang>
                    if (connection == null)
                    {
                        return;
                    }

                    // <lang>
                    //   <zh-CN>先打开连接并确认 table 已部署，避免对旧 schema 执行 INSERT 后把兼容缺口升级为业务异常。</zh-CN>
                    //   <en>Open the connection and confirm the table is deployed before INSERT, preventing a legacy-schema gap from escalating into a business exception.</en>
                    // </lang>
                    connection.Open();
                    if (!IsAuditTableAvailable(connection))
                    {
                        return;
                    }

                    // <lang>
                    //   <zh-CN>命令只在已验证连接生命周期内存在；所有调用方值都通过受长度约束的参数传入，不拼接到 SQL 文本。</zh-CN>
                    //   <en>The command exists only within the verified connection lifetime; every caller value is supplied through a length-bounded parameter rather than interpolated into SQL text.</en>
                    // </lang>
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        // <lang>
                        //   <zh-CN>固定 INSERT 列表使审计 schema、参数命名和低敏字段边界可审查；Outcome 固定为成功，因为本方法不记录失败业务动作。</zh-CN>
                        //   <en>The fixed INSERT column list keeps the audit schema, parameter naming, and low-sensitivity field boundary reviewable; Outcome is fixed to success because this method does not record failed business actions.</en>
                        // </lang>
                        command.CommandText = @"
INSERT INTO [dbo].[PortalCfg_OperationAudits]
    ([OccurredUtc], [Category], [Action], [Outcome], [ActorUserName], [TargetType], [TargetId],
     [Summary], [RelatedEventId], [ClientIp], [UserAgent], [CorrelationId])
VALUES
    (@OccurredUtc, @Category, @Action, N'Success', @ActorUserName, @TargetType, @TargetId,
     @Summary, @RelatedEventId, @ClientIp, @UserAgent, @CorrelationId);";

                        // <lang>
                        //   <zh-CN>发生时间在写入点以 UTC 取得，避免调用方本地时区或页面渲染时间改变审计顺序。</zh-CN>
                        //   <en>The occurrence time is taken in UTC at the write point, preventing caller-local time zones or page-render time from changing audit ordering.</en>
                        // </lang>
                        command.Parameters.Add("@OccurredUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;

                        // <lang>
                        //   <zh-CN>下列文本参数统一净化、截断并在空值时采用稳定回退；动作人、客户端元数据和摘要绝不以原始未受限文本写入。</zh-CN>
                        //   <en>The following text parameters are uniformly sanitized, truncated, and assigned stable fallbacks when empty; actor, client metadata, and summary are never written as unrestricted raw text.</en>
                        // </lang>
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
                        // <lang>
                        //   <zh-CN>执行单条审计 INSERT；未引入与业务事务的反向依赖，因此执行失败由外层降级处理。</zh-CN>
                        //   <en>Execute one audit INSERT; no reverse dependency on the business transaction is introduced, so an execution failure is handled by the outer degradation path.</en>
                        // </lang>
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>异常只进入运行时诊断，消息仅保留分类和动作稳定键；不要再尝试写审计表，以避免失败路径递归。</zh-CN>
                //   <en>The exception goes only to runtime diagnostics, whose message retains just stable category and action keys; do not retry writing the audit table, avoiding recursive failure paths.</en>
                // </lang>
                PortalDiagnostics.Error(
                    "OperationAudit.Write",
                    "Writing an operation audit record failed. Category=" + category + "; Action=" + action,
                    exception,
                    context);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>查询已部署审计表中的受限运营审计记录，并以探测行报告下一页可用性。</zh-CN>
        ///   <en>Queries restricted operations-audit records from the deployed audit table and uses a probe row to report next-page availability.</en>
        /// </lang>
        /// </summary>
        /// <param name="query"><l><zh-CN>日期、分类、动作和分页等受限查询条件；null 采用安全默认时间窗。</zh-CN><en>Restricted date, category, action, and paging criteria; null uses the safe default time window.</en></l></param>
        /// <param name="context"><l><zh-CN>查询失败时用于关联运行时诊断的可选 HTTP 上下文。</zh-CN><en>Optional HTTP context used to correlate runtime diagnostics when the query fails.</en></l></param>
        /// <returns><lang><zh-CN>当前页审计记录、下一页提示和审计表可用状态；失败或旧 schema 返回不可用结果而不泄露异常细节。</zh-CN><en>Current-page audit records, next-page indication, and audit-table availability; failures or legacy schemas return an unavailable result without exposing exception detail.</en></lang></returns>
        public static PortalOperationAuditQueryResult Query(PortalOperationAuditQuery query, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>规范化对象将不可信页面输入收敛为受限的 UTC 时间窗、精确筛选和分页上限，随后的 SQL 只消费该对象。</zh-CN>
            //   <en>The normalized object reduces untrusted page input to a bounded UTC window, exact filters, and paging limits; subsequent SQL consumes only this object.</en>
            // </lang>
            PortalOperationAuditQuery normalized = NormalizeQuery(query);

            // <lang>
            //   <zh-CN>结果列表先初始化为空，使连接、schema 或查询不可用时仍能返回稳定的只读结果契约。</zh-CN>
            //   <en>The result list starts empty so an unavailable connection, schema, or query can still return a stable read-only result contract.</en>
            // </lang>
            var entries = new List<PortalOperationAuditEntry>();

            try
            {
                // <lang>
                //   <zh-CN>查询与写入共享同一受控连接解析；没有配置时返回不可用，而不以空连接字符串触发 provider 细节泄露。</zh-CN>
                //   <en>The query shares the same controlled connection resolution as writing; without configuration it returns unavailable rather than exposing provider detail from an empty connection string.</en>
                // </lang>
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalOperationAuditQueryResult(entries, false, false);
                    }

                    // <lang>
                    //   <zh-CN>连接成功后再检查表存在性，确保旧数据库的正常兼容降级和实际 SQL 故障被区分处理。</zh-CN>
                    //   <en>Check table existence only after opening the connection so normal legacy-database degradation is distinguished from an actual SQL failure.</en>
                    // </lang>
                    connection.Open();
                    if (!IsAuditTableAvailable(connection))
                    {
                        return new PortalOperationAuditQueryResult(entries, false, false);
                    }

                    // <lang>
                    //   <zh-CN>命令由当前打开连接创建，固定选择列防止未来 schema 扩展自动暴露未审查字段。</zh-CN>
                    //   <en>The command is created from the current open connection; fixed selected columns prevent future schema extensions from automatically exposing unreviewed fields.</en>
                    // </lang>
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        // <lang>
                        //   <zh-CN>WHERE 只允许时间范围和三个精确等值筛选；排序加主键稳定化，OFFSET/FETCH 读取一条额外记录作为下一页探测。</zh-CN>
                        //   <en>WHERE permits only the time range and three exact equality filters; ordering is stabilized by the primary key, and OFFSET/FETCH reads one extra row as the next-page probe.</en>
                        // </lang>
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

                        // <lang>
                        //   <zh-CN>时间、文本筛选和分页均作为显式类型参数绑定；Offset 和 Take 已在 NormalizeQuery 限制，不能被页面直接放大。</zh-CN>
                        //   <en>Time, text filters, and paging are bound as explicitly typed parameters; Offset and Take were bounded in NormalizeQuery and cannot be enlarged directly by the page.</en>
                        // </lang>
                        command.Parameters.Add("@StartUtc", SqlDbType.DateTime2).Value = normalized.StartUtc;
                        command.Parameters.Add("@EndUtcExclusive", SqlDbType.DateTime2).Value = normalized.EndUtcExclusive;
                        AddTextParameter(command, "@Category", 80, normalized.Category, string.Empty);
                        AddTextParameter(command, "@Action", 80, normalized.Action, string.Empty);
                        AddTextParameter(command, "@TargetId", 200, normalized.TargetId, string.Empty);
                        command.Parameters.Add("@Offset", SqlDbType.Int).Value = normalized.Page * normalized.PageSize;
                        command.Parameters.Add("@Take", SqlDbType.Int).Value = normalized.PageSize + 1;

                        // <lang>
                        //   <zh-CN>reader 只读取固定选择列并逐行映射到传输模型；不把 DataReader 或连接对象泄漏给页面层。</zh-CN>
                        //   <en>The reader consumes only fixed selected columns and maps each row to the transport model; it does not leak a DataReader or connection object to the page layer.</en>
                        // </lang>
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // <lang>
                            //   <zh-CN>循环保留最多一条探测记录；ReadEntry 统一处理可空审计上下文字段。</zh-CN>
                            //   <en>The loop retains at most one probe record; ReadEntry consistently handles nullable audit-context fields.</en>
                            // </lang>
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
                // <lang>
                //   <zh-CN>查询失败仅记录固定诊断事件并返回不可用状态；管理员 UI 不能获得底层 SQL、连接串或堆栈细节。</zh-CN>
                //   <en>A query failure records only a stable diagnostics event and returns unavailable; the administration UI must not receive underlying SQL, connection-string, or stack detail.</en>
                // </lang>
                PortalDiagnostics.Error("OperationAudit.Query", "Querying operation audits failed.", exception, context);
                return new PortalOperationAuditQueryResult(entries, false, false);
            }

            // <lang>
            //   <zh-CN>多出的第 PageSize+1 条仅用于判断下一页，不属于当前页内容，必须在返回前移除。</zh-CN>
            //   <en>The extra PageSize+1 record exists only to determine the next page and must be removed before returning current-page content.</en>
            // </lang>
            bool hasMore = entries.Count > normalized.PageSize;
            if (hasMore)
            {
                entries.RemoveAt(entries.Count - 1);
            }

            return new PortalOperationAuditQueryResult(entries, hasMore, true);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从当前 Unity 容器解析审计专用 SQL 连接；缺少容器或外置连接串时返回 null 以触发兼容降级。</zh-CN>
        ///   <en>Resolves the audit SQL connection from the current Unity container; returns null when the container or external connection string is absent so compatibility degradation can occur.</en>
        /// </lang>
        /// </summary>
        /// <returns><lang><zh-CN>仅供当前 using 块拥有的未打开 SQL 连接，或表示不可用的 null。</zh-CN><en>An unopened SQL connection owned only by the current using block, or null to represent unavailability.</en></lang></returns>
        private static SqlConnection CreateConnection()
        {
            // <lang>
            //   <zh-CN>容器尚未建立通常表示启动早期或受限测试宿主；此处不自行构建容器，避免越过应用组合根。</zh-CN>
            //   <en>An absent container normally means early startup or a constrained test host; do not construct one here, avoiding bypass of the application composition root.</en>
            // </lang>
            if (Global.Container == null)
            {
                return null;
            }

            // <lang>
            //   <zh-CN>连接串只短暂保存在局部变量中以创建 SqlConnection；不得记录、格式化或向调用方暴露其内容。</zh-CN>
            //   <en>The connection string remains in a local variable only long enough to create SqlConnection; do not log, format, or expose its content to callers.</en>
            // </lang>
            string connectionString = Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);

            // <lang>
            //   <zh-CN>空白连接串与未配置等价，返回 null 让上层稳定降级，而不是让 provider 产生环境细节异常。</zh-CN>
            //   <en>A blank connection string is equivalent to missing configuration; return null for stable upper-layer degradation rather than letting the provider emit an environment-detail exception.</en>
            // </lang>
            return string.IsNullOrWhiteSpace(connectionString) ? null : new SqlConnection(connectionString);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在已打开的连接中检查运营审计表是否已部署。</zh-CN>
        ///   <en>Checks whether the operations-audit table is deployed on an already open connection.</en>
        /// </lang>
        /// </summary>
        /// <param name="connection"><l><zh-CN>由调用方拥有且已打开的 SQL 连接。</zh-CN><en>SQL connection owned and already opened by the caller.</en></l></param>
        /// <returns><lang><zh-CN>表存在时为 true；该结果只表示 schema 可用，不代表当前用户权限。</zh-CN><en>True when the table exists; this result represents schema availability only, not current-user authorization.</en></lang></returns>
        private static bool IsAuditTableAvailable(SqlConnection connection)
        {
            // <lang>
            //   <zh-CN>命令使用受控常量表名进行 OBJECT_ID 检查；不接受动态 schema 或表名，避免该兼容检查变成 SQL 拼接入口。</zh-CN>
            //   <en>The command uses a controlled constant table name for the OBJECT_ID check; it accepts no dynamic schema or table name, preventing this compatibility check from becoming a SQL-concatenation entry point.</en>
            // </lang>
            using (SqlCommand command = connection.CreateCommand())
            {
                // <lang>
                //   <zh-CN>只读取一个标量存在性值，不查询审计正文；表不存在是旧 schema 的预期降级分支。</zh-CN>
                //   <en>Read one scalar existence value only and never query audit body text; a missing table is the expected legacy-schema degradation branch.</en>
                // </lang>
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[" + AuditTableName + "]', N'U') IS NULL THEN 0 ELSE 1 END";

                // <lang>
                //   <zh-CN>标量值来自受控 CASE 表达式；转换前仍检查 null，避免 provider 在异常 schema 情况下把 null 转换为误导性 true。</zh-CN>
                //   <en>The scalar value comes from a controlled CASE expression; still check null before conversion so an exceptional schema state cannot turn null into a misleading true.</en>
                // </lang>
                object value = command.ExecuteScalar();
                return value != null && Convert.ToInt32(value) == 1;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为运营审计 INSERT 添加一个受长度约束、已净化的 NVARCHAR 参数。</zh-CN>
        ///   <en>Adds one length-bounded, sanitized NVARCHAR parameter for the operations-audit INSERT.</en>
        /// </lang>
        /// </summary>
        /// <param name="command"><l><zh-CN>当前 INSERT 命令；参数只附加到此命令生命周期。</zh-CN><en>Current INSERT command; the parameter is attached only for this command lifetime.</en></l></param>
        /// <param name="parameterName"><l><zh-CN>固定 SQL 参数名，不由外部请求直接控制。</zh-CN><en>Fixed SQL parameter name that external requests do not directly control.</en></l></param>
        /// <param name="size"><l><zh-CN>与审计表列契约对应的最大字符长度。</zh-CN><en>Maximum character length corresponding to the audit-table column contract.</en></l></param>
        /// <param name="value"><l><zh-CN>待净化的候选文本，可能来自 HTTP 上下文或业务调用方。</zh-CN><en>Candidate text to sanitize, which can originate from HTTP context or the business caller.</en></l></param>
        /// <param name="fallback"><l><zh-CN>净化结果为空时使用的稳定非敏感回退值。</zh-CN><en>Stable non-sensitive fallback used when sanitization yields an empty value.</en></l></param>
        private static void AddTextParameter(
            SqlCommand command,
            string parameterName,
            int size,
            string value,
            string fallback)
        {
            // <lang>
            //   <zh-CN>净化器删除或截断不适合进入诊断/审计存储的字符与过长文本；长度由目标列契约而非调用方决定。</zh-CN>
            //   <en>The sanitizer removes or truncates characters and overlong text unsuitable for diagnostic/audit storage; the target-column contract, not the caller, decides the length.</en>
            // </lang>
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);

            // <lang>
            //   <zh-CN>空白净化结果统一落到调用点定义的稳定回退，保证 NOT NULL 审计列不需要把原始值重新带回数据库。</zh-CN>
            //   <en>A blank sanitized result consistently falls back to the stable value defined at the call site, keeping NOT NULL audit columns satisfied without bringing raw text back to the database.</en>
            // </lang>
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将固定 SELECT 列顺序的审计数据行映射为受限传输模型。</zh-CN>
        ///   <en>Maps an audit data row with the fixed SELECT column order into the restricted transport model.</en>
        /// </lang>
        /// </summary>
        /// <param name="reader"><l><zh-CN>已定位在当前行、且由 Query 的固定 SELECT 创建的数据读取器。</zh-CN><en>Data reader positioned on the current row and created by Query's fixed SELECT.</en></l></param>
        /// <returns><lang><zh-CN>不会暴露连接、SQL 或未选择列的审计记录模型。</zh-CN><en>Audit record model that exposes neither the connection, SQL, nor unselected columns.</en></lang></returns>
        private static PortalOperationAuditEntry ReadEntry(SqlDataReader reader)
        {
            // <lang>
            //   <zh-CN>各 ordinal 与 Query 的固定 SELECT 顺序一一对应；可空上下文字段经专用 helper 转为空字符串，供旧 Web Forms 绑定稳定显示。</zh-CN>
            //   <en>Each ordinal corresponds one-to-one with Query's fixed SELECT order; nullable context fields pass through a dedicated helper to become empty strings for stable legacy Web Forms binding.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可空文本列，并以空字符串统一数据库 NULL 与页面显示缺省值。</zh-CN>
        ///   <en>Reads a nullable text column and normalizes database NULL to the empty string used by page display defaults.</en>
        /// </lang>
        /// </summary>
        /// <param name="reader"><l><zh-CN>已定位在当前记录的数据读取器。</zh-CN><en>Data reader positioned on the current record.</en></l></param>
        /// <param name="ordinal"><l><zh-CN>固定 SELECT 列顺序中的零基 ordinal。</zh-CN><en>Zero-based ordinal in the fixed SELECT column order.</en></l></param>
        /// <returns><lang><zh-CN>非 null 文本或空字符串；不会替换为任何敏感默认值。</zh-CN><en>Non-null text or an empty string; no sensitive default is substituted.</en></lang></returns>
        private static string ReadNullableString(SqlDataReader reader, int ordinal)
        {
            // <lang>
            //   <zh-CN>对可空审计上下文字段显式检查 DBNull，避免 GetString 在合法旧记录上抛出异常并使整页查询失败。</zh-CN>
            //   <en>Explicitly check DBNull for nullable audit-context fields, preventing GetString from throwing on valid legacy records and failing the whole page query.</en>
            // </lang>
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化运营审计查询，使日期、筛选值和分页满足受限后台查询的时间与成本边界。</zh-CN>
        ///   <en>Normalizes an operations-audit query so dates, filters, and paging meet the time and cost boundaries of the restricted administration query.</en>
        /// </lang>
        /// </summary>
        /// <param name="query"><l><zh-CN>页面或调用方提供的可空原始查询输入。</zh-CN><en>Nullable raw query input supplied by the page or caller.</en></l></param>
        /// <returns><lang><zh-CN>只含 UTC、长度受限的精确筛选和夹紧分页的新查询对象。</zh-CN><en>New query object containing only UTC, length-bounded exact filters and clamped paging.</en></lang></returns>
        private static PortalOperationAuditQuery NormalizeQuery(PortalOperationAuditQuery query)
        {
            // <lang>
            //   <zh-CN>今天的 UTC 日期是缺省时间窗的锚点；使用 Date 去除当前时分秒，保证默认窗口在一个请求内稳定。</zh-CN>
            //   <en>Today's UTC date anchors the default time window; Date removes current clock time so the default window remains stable within a request.</en>
            // </lang>
            DateTime nowUtc = DateTime.UtcNow.Date;

            // <lang>
            //   <zh-CN>起点缺失时默认回看六天，使含今天的窗口为七个 UTC 日期；提供值仅保留日期部分。</zh-CN>
            //   <en>When the start is missing, default to six days back so the window including today contains seven UTC dates; a supplied value retains its date portion only.</en>
            // </lang>
            DateTime startUtc = query == null || query.StartUtc == DateTime.MinValue
                ? nowUtc.AddDays(-6)
                : query.StartUtc.Date;

            // <lang>
            //   <zh-CN>终点是排除边界，缺失时取明天 UTC 零点；这样今天整日仍被包含且相邻请求不会重叠。</zh-CN>
            //   <en>The end is an exclusive boundary and defaults to tomorrow's UTC midnight; this includes the whole current day without overlapping adjacent requests.</en>
            // </lang>
            DateTime endUtcExclusive = query == null || query.EndUtcExclusive == DateTime.MinValue
                ? nowUtc.AddDays(1)
                : query.EndUtcExclusive.Date;

            // <lang>
            //   <zh-CN>结束早于或等于开始会产生空窗或反向窗，因此收敛为最小的一日有效窗口而不是交给 SQL 解释。</zh-CN>
            //   <en>An end at or before the start would create an empty or reversed window, so reduce it to the minimum valid one-day window rather than delegating interpretation to SQL.</en>
            // </lang>
            if (endUtcExclusive <= startUtc)
            {
                endUtcExclusive = startUtc.AddDays(1);
            }

            // <lang>
            //   <zh-CN>过长时间窗会让高价值审计后台变成无界扫描，故截到固定最大天数；调用方必须以分页或更窄日期继续查询。</zh-CN>
            //   <en>An overlong window would turn the high-value audit page into an unbounded scan, so cap it at the fixed maximum days; callers must continue with paging or narrower dates.</en>
            // </lang>
            if ((endUtcExclusive - startUtc).TotalDays > MaximumQueryDays)
            {
                endUtcExclusive = startUtc.AddDays(MaximumQueryDays);
            }

            // <lang>
            //   <zh-CN>新对象不复用调用方实例，避免页面在规范化后观察到隐式修改；每个筛选和分页值在此冻结为 SQL 可安全消费的范围。</zh-CN>
            //   <en>The new object does not reuse the caller instance, preventing the page from observing an implicit mutation; each filter and paging value is frozen here within the range safe for SQL consumption.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>将精确审计筛选值去空白并截断到对应列长度；该 helper 不执行模糊匹配、转义扩展或字段推断。</zh-CN>
        ///   <en>Trims an exact audit-filter value and bounds it to the corresponding column length; this helper performs no fuzzy matching, escaping expansion, or field inference.</en>
        /// </lang>
        /// </summary>
        /// <param name="value"><l><zh-CN>调用方提供的可空筛选文本。</zh-CN><en>Nullable filter text supplied by the caller.</en></l></param>
        /// <param name="maximumLength"><l><zh-CN>目标 SQL 参数及列允许的最大字符长度。</zh-CN><en>Maximum character length accepted by the target SQL parameter and column.</en></l></param>
        /// <returns><lang><zh-CN>空字符串或已截断的精确筛选值。</zh-CN><en>Either the empty string or a trimmed, bounded exact filter value.</en></lang></returns>
        private static string NormalizeFilter(string value, int maximumLength)
        {
            // <lang>
            //   <zh-CN>空白输入代表“未筛选”，统一为空字符串以配合 SQL 中受参数控制的精确可选条件。</zh-CN>
            //   <en>Blank input represents “not filtered” and is normalized to the empty string to match the parameter-controlled exact optional SQL conditions.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // <lang>
            //   <zh-CN>局部文本只保存去除首尾空白后的值；随后按最大长度截断而不是抛出异常，避免管理筛选输入成为拒绝服务入口。</zh-CN>
            //   <en>The local text holds only the trimmed value; it is then bounded rather than rejected, preventing administration filter input from becoming a denial-of-service entry point.</en>
            // </lang>
            string normalized = value.Trim();
            return normalized.Substring(0, Math.Min(normalized.Length, maximumLength));
        }

        /// <summary><lang><zh-CN>从可选 HTTP 上下文取得动作人用户名；缺少完整身份链时返回空字符串，由写入参数使用匿名回退。</zh-CN><en>Gets the actor user name from optional HTTP context; returns the empty string when the identity chain is incomplete so the write parameter can use its anonymous fallback.</en></lang></summary>
        private static string GetActorUserName(HttpContext context)
        {
            // <lang>
            //   <zh-CN>逐层检查 Context、User 与 Identity，避免后台任务、错误处理或匿名请求访问缺失对象；此 helper 不进行认证或授权判定。</zh-CN>
            //   <en>Check Context, User, and Identity layer by layer to support background tasks, error handling, or anonymous requests with missing objects; this helper performs no authentication or authorization decision.</en>
            // </lang>
            return context == null || context.User == null || context.User.Identity == null
                ? string.Empty
                : context.User.Identity.Name;
        }

        /// <summary><lang><zh-CN>从可选 HTTP 请求取得客户端地址；请求不存在时返回空字符串并由写入参数安全处理。</zh-CN><en>Gets the client address from an optional HTTP request; returns the empty string when no request exists and lets the write parameter handle it safely.</en></lang></summary>
        private static string GetClientIp(HttpContext context)
        {
            // <lang>
            //   <zh-CN>地址仅为审计上下文，不用于信任代理头、访问控制或身份验证；缺少 Request 时不得访问其属性。</zh-CN>
            //   <en>The address is audit context only and is not used for trusted proxy headers, access control, or authentication; do not access request properties when Request is absent.</en>
            // </lang>
            return context == null || context.Request == null ? string.Empty : context.Request.UserHostAddress;
        }

        /// <summary><lang><zh-CN>从可选 HTTP 请求取得 User-Agent；请求不存在时返回空字符串，写入前仍会净化与截断。</zh-CN><en>Gets the User-Agent from an optional HTTP request; returns the empty string when no request exists, and it is still sanitized and bounded before writing.</en></lang></summary>
        private static string GetUserAgent(HttpContext context)
        {
            // <lang>
            //   <zh-CN>User-Agent 是不可信请求元数据，只供低敏审计诊断；缺少 Request 时以空值降级。</zh-CN>
            //   <en>User-Agent is untrusted request metadata used only for low-sensitivity audit diagnostics; degrade to empty when Request is absent.</en>
            // </lang>
            return context == null || context.Request == null ? string.Empty : context.Request.UserAgent;
        }
    }
}
