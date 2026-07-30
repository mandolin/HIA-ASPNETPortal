using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户运行期事件的统一诊断门面。</zh-CN>
    ///   <en>Unified diagnostics facade for portal runtime events.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P2.4 保持既有调用语义，并将新事件写入 UTF-8 无 BOM 的 NDJSON 文件；旧版多行 <c>.log</c> 文件仅作为历史记录保留，不再追加写入。</zh-CN>
    ///   <en>P2.4 preserves existing call semantics and writes new events to UTF-8 without BOM NDJSON files; legacy multi-line <c>.log</c> files remain historical records and receive no new writes.</en>
    /// </lang>
    /// </remarks>
    public static class PortalDiagnostics
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用于覆盖诊断日志目录的 appSettings 键名。</zh-CN>
        ///   <en>AppSettings key used to override the diagnostics log directory.</en>
        /// </lang>
        /// </summary>
        public const string LogDirectorySettingKey = PortalSettingKeys.DiagnosticsLogDirectory;

        /// <summary>
        /// <lang>
        ///   <zh-CN>用于允许开发期详细 ASP.NET 错误输出的 appSettings 键名。</zh-CN>
        ///   <en>AppSettings key used to allow detailed ASP.NET error output in development.</en>
        /// </lang>
        /// </summary>
        public const string DetailedErrorsSettingKey = PortalSettingKeys.DiagnosticsDetailedErrors;

        // <lang>
        //   <zh-CN>同一进程内的锁仅序列化诊断文件的创建、选择、追加和清理；它不承担跨进程协调或业务事务职责。</zh-CN>
        //   <en>The in-process lock serializes diagnostics file creation, selection, append, and cleanup only; it is not cross-process coordination or a business-transaction boundary.</en>
        // </lang>
        private static readonly object LogLock = new object();
        private static readonly Regex ManagedLogFileNamePattern = new Regex(
            @"^portal-(?<date>\d{8})-(?<sequence>\d{3})\.jsonl$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static DateTime _lastRetentionCleanupUtcDate = DateTime.MinValue;

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录普通诊断信息。</zh-CN>
        ///   <en>Records an informational diagnostics message.</en>
        /// </lang>
        /// </summary>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>经写入链净化和限长的诊断分类候选。</zh-CN>
        ///   <en>The diagnostics-category candidate sanitized and capped by the write pipeline.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>经写入链净化和限长的诊断消息候选。</zh-CN>
        ///   <en>The diagnostics-message candidate sanitized and capped by the write pipeline.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>可选 HTTP 上下文；缺失时不附加请求字段。</zh-CN>
        ///   <en>Optional HTTP context; no request fields are appended when absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>用于关联诊断输出的事件编号，而非授权或保密值。</zh-CN>
        ///   <en>The event id used to correlate diagnostics output, not an authorization or secret value.</en>
        /// </l>
        /// </returns>
        public static string Info(string category, string message, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>门面固定使用受控 Info 级别，不接受调用方提供任意 Trace 路由值。</zh-CN>
            //   <en>The facade fixes the controlled Info level and does not accept an arbitrary caller-supplied Trace-routing value.</en>
            // </lang>
            return Write("Info", category, message, null, context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录警告诊断信息。</zh-CN>
        ///   <en>Records a warning diagnostics message.</en>
        /// </lang>
        /// </summary>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>经写入链净化和限长的诊断分类候选。</zh-CN>
        ///   <en>The diagnostics-category candidate sanitized and capped by the write pipeline.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>经写入链净化和限长的诊断消息候选。</zh-CN>
        ///   <en>The diagnostics-message candidate sanitized and capped by the write pipeline.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>可选 HTTP 上下文；缺失时不附加请求字段。</zh-CN>
        ///   <en>Optional HTTP context; no request fields are appended when absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>用于关联诊断输出的事件编号，而非授权或保密值。</zh-CN>
        ///   <en>The event id used to correlate diagnostics output, not an authorization or secret value.</en>
        /// </l>
        /// </returns>
        public static string Warn(string category, string message, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>门面固定使用受控 Warning 级别，实际字段净化和接收器故障隔离仍由共享写入链负责。</zh-CN>
            //   <en>The facade fixes the controlled Warning level; shared write pipeline still owns field sanitization and sink-failure isolation.</en>
            // </lang>
            return Write("Warning", category, message, null, context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录已处理异常，并返回诊断事件编号。</zh-CN>
        ///   <en>Records a handled exception and returns the diagnostics event id.</en>
        /// </lang>
        /// </summary>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>经写入链净化和限长的诊断分类候选。</zh-CN>
        ///   <en>The diagnostics-category candidate sanitized and capped by the write pipeline.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>经写入链净化和限长的诊断消息候选。</zh-CN>
        ///   <en>The diagnostics-message candidate sanitized and capped by the write pipeline.</en>
        /// </l>
        /// </param>
        /// <param name="exception">
        /// <l>
        ///   <zh-CN>可选已处理异常；类型和详情由写入链净化、限长后才可进入诊断条目。</zh-CN>
        ///   <en>Optional handled exception; its type and detail may enter a diagnostics entry only after write-pipeline sanitization and capping.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>可选 HTTP 上下文；缺失时不附加请求字段。</zh-CN>
        ///   <en>Optional HTTP context; no request fields are appended when absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>用于关联诊断输出的事件编号，而非授权或保密值。</zh-CN>
        ///   <en>The event id used to correlate diagnostics output, not an authorization or secret value.</en>
        /// </l>
        /// </returns>
        public static string Error(string category, string message, Exception exception, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>门面固定 Error 级别；异常本身不直接返回给调用方，而是进入受控构造和净化链。</zh-CN>
            //   <en>The facade fixes the Error level; the exception is not returned directly to callers and enters the controlled construction and sanitization pipeline instead.</en>
            // </lang>
            return Write("Error", category, message, exception, context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录未处理异常，并返回可展示给用户的诊断事件编号。</zh-CN>
        ///   <en>Records an unhandled exception and returns the diagnostics event id shown to users.</en>
        /// </lang>
        /// </summary>
        /// <param name="exception">
        /// <l>
        ///   <zh-CN>可选未处理异常；详情由写入链净化、限长后才可进入诊断条目。</zh-CN>
        ///   <en>Optional unhandled exception; its detail may enter a diagnostics entry only after write-pipeline sanitization and capping.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>可选 HTTP 上下文；缺失时不附加请求字段。</zh-CN>
        ///   <en>Optional HTTP context; no request fields are appended when absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可用于向用户关联支持请求的诊断事件编号，而非异常详情。</zh-CN>
        ///   <en>The diagnostics event id that may correlate a support request for a user, not exception detail.</en>
        /// </l>
        /// </returns>
        public static string Unhandled(Exception exception, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>未处理路径固定分类和对外摘要，避免把异常文本作为调用方消息直接传播到输出链。</zh-CN>
            //   <en>The unhandled path fixes category and external summary so exception text is not propagated as a caller message directly into the output pipeline.</en>
            // </lang>
            return Write("Error", "UnhandledException", "Unhandled portal exception.", exception, context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行轻量 SQL Server 健康检查，且不记录连接串原文。</zh-CN>
        ///   <en>Executes a lightweight SQL Server health check without logging the connection string.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>仅用于创建短生命周期探针连接的连接串；不会作为诊断消息或返回值回显。</zh-CN>
        ///   <en>Connection string used only to create a short-lived probe connection; it is never echoed as diagnostics text or a return value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已记录探针结果的诊断事件编号，而非连接状态详情或连接串。</zh-CN>
        ///   <en>The diagnostics event id that records the probe result, not connection-state detail or the connection string.</en>
        /// </l>
        /// </returns>
        public static string CheckSqlConnection(string connectionString)
        {
            // <lang>
            //   <zh-CN>空白连接串只写入固定“跳过”事实，不将配置值或其长度带入诊断输出。</zh-CN>
            //   <en>A blank connection string records only the fixed “skipped” fact and does not put the configuration value or its length into diagnostics output.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Warn("DatabaseHealth", "SQL health check skipped because the connection string is empty.");
            }

            try
            {
                // <lang>
                //   <zh-CN>探针固定为短生命周期连接、无参数的 <c>SELECT 1</c> 和五秒命令超时；using 保证连接与命令在成功或异常后释放。</zh-CN>
                //   <en>The probe is fixed to a short-lived connection, parameterless <c>SELECT 1</c>, and a five-second command timeout; using releases connection and command after success or failure.</en>
                // </lang>
                using (var connection = new SqlConnection(connectionString))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1";
                    command.CommandTimeout = 5;
                    connection.Open();
                    command.ExecuteScalar();
                }

                return Info("DatabaseHealth", "SQL health check passed.");
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>失败交给既有 Error 写入链净化、限长和隔离；不把数据库异常或连接串直接返回给健康检查调用方。</zh-CN>
                //   <en>Route failure through the established Error write pipeline for sanitization, capping, and isolation; do not return database exception or connection string directly to the health-check caller.</en>
                // </lang>
                return Error("DatabaseHealth", "SQL health check failed.", exception);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断是否显式启用了详细 ASP.NET 错误输出。</zh-CN>
        ///   <en>Determines whether detailed ASP.NET errors are explicitly enabled.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>受控运行设置的布尔值；该值本身不是用户身份验证或授权结果。</zh-CN>
        ///   <en>The Boolean value from controlled runtime settings; it is not itself a user-authentication or authorization result.</en>
        /// </l>
        /// </returns>
        public static bool AreDetailedErrorsEnabled()
        {
            // <lang>
            //   <zh-CN>只读取既有注册设置并返回其布尔事实；不读取错误正文、改变 ASP.NET 配置或决定当前请求的访问权。</zh-CN>
            //   <en>Read only the established registered setting and return its Boolean fact; do not read error bodies, alter ASP.NET configuration, or decide current-request access.</en>
            // </lang>
            return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.DiagnosticsDetailedErrors);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断管理员是否可查看已净化的诊断详情。</zh-CN>
        ///   <en>Determines whether administrators may view sanitized diagnostic details.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>受控运行设置的功能开关；调用方仍必须独立执行当前用户的管理员授权。</zh-CN>
        ///   <en>The controlled runtime feature flag; callers must still independently authorize the current user as an administrator.</en>
        /// </l>
        /// </returns>
        public static bool AreAdminLogDetailsEnabled()
        {
            // <lang>
            //   <zh-CN>只返回详情功能开关，不验证当前用户、不会加载诊断详情，也不替代页面层的授权门禁。</zh-CN>
            //   <en>Return only the details feature flag; do not validate the current user, load diagnostics detail, or replace page-layer authorization gates.</en>
            // </lang>
            return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.DiagnosticsAllowAdminDetailView);
        }

        private static string Write(string level, string category, string message, Exception exception, HttpContext context)
        {
            // <lang>
            //   <zh-CN>先构造已净化且带关联编号的独立条目；无论后续诊断接收器是否可用，调用方仅取得该编号而不暴露原始异常或上下文。</zh-CN>
            //   <en>Build a detached sanitized entry with a correlation id first; whether later diagnostics sinks are available or not, callers receive only that id and never raw exception or context.</en>
            // </lang>
            PortalDiagnosticEntry entry = BuildEntry(level, category, message, exception, context);

            try
            {
                // <lang>
                //   <zh-CN>Trace 与文件接收器按既有顺序独立执行；两者都只消费已受控字段。</zh-CN>
                //   <en>Run Trace and file sinks independently in their established order; both consume only controlled fields.</en>
                // </lang>
                WriteTrace(entry);
                WriteFile(entry);
            }
            catch (Exception logException)
            {
                // <lang>
                //   <zh-CN>诊断接收器失败不得反过来破坏业务请求；回退 Trace 只包含事件编号和已净化、截断的异常摘要。</zh-CN>
                //   <en>A diagnostics-sink failure must not break the business request; fallback Trace contains only the event id and a sanitized, truncated exception summary.</en>
                // </lang>
                try
                {
                    Trace.TraceError(
                        "Portal diagnostics write failed. EventId={0}; Error={1}",
                        entry.EventId,
                        PortalDiagnosticSanitizer.SanitizeAndTruncate(logException.ToString(), 2000));
                }
                catch
                {
                    // <lang>
                    //   <zh-CN>最后一层隔离故意吞并 Trace 自身故障，保持原请求的成功或失败语义不被诊断路径覆盖。</zh-CN>
                    //   <en>The final isolation layer deliberately swallows a Trace failure so the diagnostics path cannot override the original request outcome.</en>
                    // </lang>
                }
            }

            return entry.EventId;
        }

        private static PortalDiagnosticEntry BuildEntry(
            string level,
            string category,
            string message,
            Exception exception,
            HttpContext context)
        {
            // <lang>
            //   <zh-CN>在进入任何输出接收器前固定 UTC 时间、关联编号和字段长度；异常详情仅在确有异常时纳入并同样净化。</zh-CN>
            //   <en>Fix UTC time, correlation id, and field caps before any output sink; include exception detail only when present and sanitize it as well.</en>
            // </lang>
            var entry = new PortalDiagnosticEntry
            {
                EventId = CreateEventId(),
                UtcTime = DateTime.UtcNow,
                Level = PortalDiagnosticSanitizer.SanitizeAndTruncate(level, 20),
                Category = PortalDiagnosticSanitizer.SanitizeAndTruncate(category, 80),
                Message = PortalDiagnosticSanitizer.SanitizeAndTruncate(message, 2000),
                ExceptionType = exception == null
                    ? string.Empty
                    : PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.GetType().FullName, 300),
                ExceptionDetail = exception == null
                    ? string.Empty
                    : PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.ToString(), 16000)
            };

            // <lang>
            //   <zh-CN>可选请求上下文仅通过受控 helper 附加，不能绕过条目字段的净化和长度限制。</zh-CN>
            //   <en>Append optional request context only through the controlled helper; it cannot bypass entry-field sanitization or length limits.</en>
            // </lang>
            AppendRequestContext(entry, context);
            return entry;
        }

        private static void AppendRequestContext(PortalDiagnosticEntry entry, HttpContext context)
        {
            // <lang>
            //   <zh-CN>后台任务等调用方可没有 HTTP 上下文；此时保留基础条目，不推造请求字段。</zh-CN>
            //   <en>Callers such as background work may have no HTTP context; retain the base entry without fabricating request fields.</en>
            // </lang>
            HttpRequest request = context == null ? null : context.Request;
            if (request == null)
            {
                return;
            }

            // <lang>
            //   <zh-CN>只记录不含查询值的 Request.Path，避免邀请代码、Token 等随 URL 进入日志；其余上下文字段仍逐项净化并限长。</zh-CN>
            //   <en>Record only Request.Path without query values to keep invite codes and tokens out of logs; sanitize and cap every remaining context field individually.</en>
            // </lang>
            entry.RequestPath = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.Path, 1000);
            entry.HttpMethod = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.HttpMethod, 20);
            entry.UserName = PortalDiagnosticSanitizer.SanitizeAndTruncate(context.User == null || context.User.Identity == null ? null : context.User.Identity.Name, 100);
            entry.ClientIp = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.UserHostAddress, 64);
            entry.PhysicalPath = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.PhysicalPath, 2000);
            entry.UserAgent = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.UserAgent, 400);
        }

        private static string CreateEventId()
        {
            // <lang>
            //   <zh-CN>UTC 时间与短 GUID 片段仅用于日志关联和查找，不是认证票据、授权依据或保密值。</zh-CN>
            //   <en>The UTC time and short GUID fragment are for log correlation and lookup only, not an authentication token, authorization basis, or secret.</en>
            // </lang>
            return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static void WriteTrace(PortalDiagnosticEntry entry)
        {
            // <lang>
            //   <zh-CN>Trace 投影只使用已净化的标识、级别、分类和摘要消息；不追加异常详情、物理路径或原始请求内容。</zh-CN>
            //   <en>The Trace projection uses only sanitized id, level, category, and summary message; it adds no exception detail, physical path, or raw request content.</en>
            // </lang>
            string traceMessage = string.Format(
                "Portal diagnostics. EventId={0}; Level={1}; Category={2}; Message={3}",
                entry.EventId,
                entry.Level,
                entry.Category,
                entry.Message);

            switch (entry.Level)
            {
                case "Info":
                    Trace.TraceInformation(traceMessage);
                    break;
                case "Warning":
                    Trace.TraceWarning(traceMessage);
                    break;
                default:
                    // <lang>
                    //   <zh-CN>未知级别沿用既有保守回退为错误 Trace，不扩展为新的日志路由或信任外部级别文本。</zh-CN>
                    //   <en>An unknown level retains the established conservative fallback to error Trace; do not create new routing or trust external level text.</en>
                    // </lang>
                    Trace.TraceError(traceMessage);
                    break;
            }
        }

        private static void WriteFile(PortalDiagnosticEntry entry)
        {
            // <lang>
            //   <zh-CN>日志目录只由受控运行设置解析；序列化前的条目已净化，NDJSON 仍显式 HTML 转义以降低后续展示链路的解释风险。</zh-CN>
            //   <en>The log directory is resolved only from controlled runtime settings; the entry is sanitized before serialization and NDJSON still escapes HTML to reduce interpretation risk in later display paths.</en>
            // </lang>
            string logDirectory = ResolveLogDirectory();
            string serialized = JsonConvert.SerializeObject(
                entry,
                Formatting.None,
                new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
            string payload = serialized + Environment.NewLine;
            int payloadByteCount = new UTF8Encoding(false).GetByteCount(payload);

            // <lang>
            //   <zh-CN>锁覆盖建目录、文件选择、追加与保留清理，使并发写入使用与实际追加相同的 UTF-8 无 BOM 字节计数和受控顺序。</zh-CN>
            //   <en>The lock covers directory creation, file selection, append, and retention cleanup so concurrent writes use the same UTF-8-without-BOM byte count and controlled order as the actual append.</en>
            // </lang>
            lock (LogLock)
            {
                Directory.CreateDirectory(logDirectory);
                string logFile = ResolveCurrentLogFile(logDirectory, entry.UtcTime, payloadByteCount);
                File.AppendAllText(logFile, payload, new UTF8Encoding(false));
                CleanupExpiredLogs(logDirectory, entry.UtcTime.Date);
            }
        }

        private static string ResolveCurrentLogFile(string logDirectory, DateTime utcTime, int incomingByteCount)
        {
            // <lang>
            //   <zh-CN>文件大小上限来自受控运行设置；传入字节数由调用方以实际 UTF-8 无 BOM payload 计算，本 helper 不接收调用方文件名。</zh-CN>
            //   <en>The file-size cap comes from controlled runtime settings; the caller supplies bytes calculated from the actual UTF-8-without-BOM payload and this helper accepts no caller file name.</en>
            // </lang>
            int maximumFileBytes = PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.DiagnosticsMaxFileBytes);
            string datePart = utcTime.ToString("yyyyMMdd");

            // <lang>
            //   <zh-CN>只在已解析目录中探测固定日期和三位序号的受控命名空间；首个不存在候选或仍有容量的候选可继续使用。</zh-CN>
            //   <en>Probe only the controlled fixed-date, three-digit-sequence namespace under the resolved directory; the first missing candidate or one still within capacity may be used.</en>
            // </lang>
            for (int sequence = 1; sequence <= 999; sequence++)
            {
                string candidate = Path.Combine(logDirectory, string.Format("portal-{0}-{1:D3}.jsonl", datePart, sequence));
                if (!File.Exists(candidate))
                {
                    return candidate;
                }

                long currentLength = new FileInfo(candidate).Length;
                if (currentLength + incomingByteCount <= maximumFileBytes)
                {
                    return candidate;
                }
            }

            // <lang>
            //   <zh-CN>每日受控序号耗尽时抛出固定、无路径的 I/O 失败，交由上层诊断隔离逻辑处理。</zh-CN>
            //   <en>When the controlled daily sequence is exhausted, throw a fixed I/O failure without paths for the upper diagnostics-isolation logic to handle.</en>
            // </lang>
            throw new IOException("Portal diagnostics exhausted the daily log-file sequence range.");
        }

        private static void CleanupExpiredLogs(string logDirectory, DateTime currentUtcDate)
        {
            // <lang>
            //   <zh-CN>同一进程每个 UTC 日期最多尝试一次保留清理；即使本次失败也保留既有“当日不重试”语义，避免写入路径反复触发删除扫描。</zh-CN>
            //   <en>Attempt retention cleanup at most once per UTC date in the process; even a failure retains the established no-retry-that-day behavior so the write path does not repeatedly trigger deletion scanning.</en>
            // </lang>
            if (_lastRetentionCleanupUtcDate == currentUtcDate)
            {
                return;
            }

            _lastRetentionCleanupUtcDate = currentUtcDate;
            int retentionDays = PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.DiagnosticsRetentionDays);
            DateTime cutoffUtcDate = currentUtcDate.AddDays(-retentionDays);

            try
            {
                // <lang>
                //   <zh-CN>仅枚举受控目录顶层的 portal JSONL 名称；不能解析为受管 UTC 日期或仍在保留窗口内的候选绝不删除。</zh-CN>
                //   <en>Enumerate only portal JSONL names at the controlled directory top level; candidates that cannot parse as managed UTC dates or remain in the retention window are never deleted.</en>
                // </lang>
                foreach (string filePath in Directory.EnumerateFiles(logDirectory, "portal-*.jsonl", SearchOption.TopDirectoryOnly))
                {
                    DateTime fileDate;
                    if (!TryGetManagedLogDate(Path.GetFileName(filePath), out fileDate) || fileDate >= cutoffUtcDate)
                    {
                        continue;
                    }

                    try
                    {
                        // <lang>
                        //   <zh-CN>删除仅作用于已通过受管文件名和截止日期检查的完整候选路径；本批只说明既有行为，不执行该操作。</zh-CN>
                        //   <en>Deletion applies only to the complete candidate path that passed managed-filename and cutoff-date checks; this batch documents the existing behavior and does not execute it.</en>
                        // </lang>
                        File.Delete(filePath);
                    }
                    catch (Exception exception)
                    {
                        // <lang>
                        //   <zh-CN>单文件删除失败不阻断其它候选；告警仅公开文件名和已净化、截断的异常摘要，不记录完整路径。</zh-CN>
                        //   <en>One file-deletion failure does not block other candidates; the warning exposes only the file name and a sanitized, truncated exception summary, never the full path.</en>
                        // </lang>
                        Trace.TraceWarning(
                            "Portal diagnostics retention cleanup could not delete '{0}'. Error={1}",
                            Path.GetFileName(filePath),
                            PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.Message, 500));
                    }
                }
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>枚举或整体清理失败仅写入已净化、截断的摘要；诊断保留故障不得影响当前业务请求或日志追加的上层回退。</zh-CN>
                //   <en>An enumeration or whole-cleanup failure logs only a sanitized, truncated summary; a diagnostics-retention fault must not affect the current business request or the upper append fallback.</en>
                // </lang>
                Trace.TraceWarning(
                    "Portal diagnostics retention cleanup failed. Error={0}",
                    PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.Message, 500));
            }
        }

        private static bool TryGetManagedLogDate(string fileName, out DateTime fileDate)
        {
            // <lang>
            //   <zh-CN>先写入稳定失败日期，再以文化无关的受控文件名和 yyyyMMdd UTC 解析；该 helper 不访问磁盘也不抛出格式细节。</zh-CN>
            //   <en>Set a stable failure date first, then parse only the culture-invariant controlled filename and yyyyMMdd UTC value; this helper neither accesses disk nor throws format detail.</en>
            // </lang>
            fileDate = DateTime.MinValue;
            Match match = ManagedLogFileNamePattern.Match(fileName ?? string.Empty);
            return match.Success && DateTime.TryParseExact(
                match.Groups["date"].Value,
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out fileDate);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析文件日志使用的诊断日志目录。</zh-CN>
        ///   <en>Resolves the diagnostics log directory used by file logging.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>由受控运行设置解析并规范化的绝对目录；设置为空时返回门户基目录下的默认 <c>App_Data/Logs</c>。</zh-CN>
        ///   <en>An absolute directory resolved and normalized from controlled runtime settings, or the default <c>App_Data/Logs</c> beneath the portal base directory when unset.</en>
        /// </l>
        /// </returns>
        public static string ResolveLogDirectory()
        {
            // <lang>
            //   <zh-CN>目录仅来自受控部署期运行设置，不接收 HTTP 或业务调用方路径；先去空白并展开环境变量以保留既有配置兼容性。</zh-CN>
            //   <en>The directory comes only from controlled deployment-time runtime settings, not HTTP or business-caller paths; trim and expand environment variables first to preserve established configuration compatibility.</en>
            // </lang>
            string configuredDirectory = PortalRuntimeSettings.GetString(PortalSettingsRegistry.DiagnosticsLogDirectory);
            if (!string.IsNullOrWhiteSpace(configuredDirectory))
            {
                string expanded = Environment.ExpandEnvironmentVariables(configuredDirectory.Trim());
                if (!Path.IsPathRooted(expanded))
                {
                    // <lang>
                    //   <zh-CN>相对设置始终锚定门户应用基目录，避免按当前工作目录解析。</zh-CN>
                    //   <en>Anchor a relative setting to the portal application base directory rather than resolving it from the current working directory.</en>
                    // </lang>
                    expanded = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expanded);
                }

                // <lang>
                //   <zh-CN>在返回前规范化完整路径；绝对部署设置仍按既有契约保留，不在此 helper 中重新定义部署授权。</zh-CN>
                //   <en>Normalize the full path before returning; an absolute deployment setting remains allowed by the established contract and this helper does not redefine deployment authorization.</en>
                // </lang>
                return Path.GetFullPath(expanded);
            }

            // <lang>
            //   <zh-CN>未设置时使用门户基目录内的固定默认位置，避免按环境猜测外部临时目录。</zh-CN>
            //   <en>When unset, use the fixed location beneath the portal base directory rather than guessing an external temporary directory from the environment.</en>
            // </lang>
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs");
        }
    }
}
