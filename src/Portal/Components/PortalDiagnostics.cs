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
    /// 门户运行期事件的统一诊断门面。
    /// Unified diagnostics facade for portal runtime events.
    /// </summary>
    /// <remarks>
    /// P2.4 保持既有调用语义，并将新事件写入 UTF-8 无 BOM 的 NDJSON 文件。
    /// 旧版多行 <c>.log</c> 文件仅作为历史记录保留，不再追加写入。
    /// P2.4 preserves existing call semantics and writes new events to UTF-8 without BOM NDJSON files.
    /// Legacy multi-line <c>.log</c> files remain historical records and receive no new writes.
    /// </remarks>
    public static class PortalDiagnostics
    {
        /// <summary>
        /// 用于覆盖诊断日志目录的 appSettings 键名。
        /// AppSettings key used to override the diagnostics log directory.
        /// </summary>
        public const string LogDirectorySettingKey = PortalSettingKeys.DiagnosticsLogDirectory;

        /// <summary>
        /// 用于允许开发期详细 ASP.NET 错误输出的 appSettings 键名。
        /// AppSettings key used to allow detailed ASP.NET error output in development.
        /// </summary>
        public const string DetailedErrorsSettingKey = PortalSettingKeys.DiagnosticsDetailedErrors;

        private static readonly object LogLock = new object();
        private static readonly Regex ManagedLogFileNamePattern = new Regex(
            @"^portal-(?<date>\d{8})-(?<sequence>\d{3})\.jsonl$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static DateTime _lastRetentionCleanupUtcDate = DateTime.MinValue;

        /// <summary>
        /// 记录普通诊断信息。
        /// Records an informational diagnostics message.
        /// </summary>
        public static string Info(string category, string message, HttpContext context = null)
        {
            return Write("Info", category, message, null, context);
        }

        /// <summary>
        /// 记录警告诊断信息。
        /// Records a warning diagnostics message.
        /// </summary>
        public static string Warn(string category, string message, HttpContext context = null)
        {
            return Write("Warning", category, message, null, context);
        }

        /// <summary>
        /// 记录已处理异常，并返回诊断事件编号。
        /// Records a handled exception and returns the diagnostics event id.
        /// </summary>
        public static string Error(string category, string message, Exception exception, HttpContext context = null)
        {
            return Write("Error", category, message, exception, context);
        }

        /// <summary>
        /// 记录未处理异常，并返回可展示给用户的诊断事件编号。
        /// Records an unhandled exception and returns the diagnostics event id shown to users.
        /// </summary>
        public static string Unhandled(Exception exception, HttpContext context = null)
        {
            return Write("Error", "UnhandledException", "Unhandled portal exception.", exception, context);
        }

        /// <summary>
        /// 执行轻量 SQL Server 健康检查，且不记录连接串原文。
        /// Executes a lightweight SQL Server health check without logging the connection string.
        /// </summary>
        public static string CheckSqlConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Warn("DatabaseHealth", "SQL health check skipped because the connection string is empty.");
            }

            try
            {
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
                return Error("DatabaseHealth", "SQL health check failed.", exception);
            }
        }

        /// <summary>
        /// 判断是否显式启用了详细 ASP.NET 错误输出。
        /// Determines whether detailed ASP.NET errors are explicitly enabled.
        /// </summary>
        public static bool AreDetailedErrorsEnabled()
        {
            return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.DiagnosticsDetailedErrors);
        }

        /// <summary>
        /// 判断管理员是否可查看已净化的诊断详情。
        /// Determines whether administrators may view sanitized diagnostic details.
        /// </summary>
        public static bool AreAdminLogDetailsEnabled()
        {
            return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.DiagnosticsAllowAdminDetailView);
        }

        private static string Write(string level, string category, string message, Exception exception, HttpContext context)
        {
            PortalDiagnosticEntry entry = BuildEntry(level, category, message, exception, context);

            try
            {
                WriteTrace(entry);
                WriteFile(entry);
            }
            catch (Exception logException)
            {
                // 诊断系统不能反过来破坏业务请求；文件写入失败时只尝试写入 Trace。
                // Diagnostics must not break the request; file-write failures fall back to Trace only.
                try
                {
                    Trace.TraceError(
                        "Portal diagnostics write failed. EventId={0}; Error={1}",
                        entry.EventId,
                        PortalDiagnosticSanitizer.SanitizeAndTruncate(logException.ToString(), 2000));
                }
                catch
                {
                    // 最后一层保护：忽略诊断自身的失败。
                    // Last-resort guard: ignore diagnostics failures.
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

            AppendRequestContext(entry, context);
            return entry;
        }

        private static void AppendRequestContext(PortalDiagnosticEntry entry, HttpContext context)
        {
            HttpRequest request = context == null ? null : context.Request;
            if (request == null)
            {
                return;
            }

            // Request.Path 不含查询值，避免邀请代码、Token 等随 URL 进入日志。
            // Request.Path excludes query values, avoiding invite codes and tokens in logs.
            entry.RequestPath = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.Path, 1000);
            entry.HttpMethod = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.HttpMethod, 20);
            entry.UserName = PortalDiagnosticSanitizer.SanitizeAndTruncate(context.User == null || context.User.Identity == null ? null : context.User.Identity.Name, 100);
            entry.ClientIp = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.UserHostAddress, 64);
            entry.PhysicalPath = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.PhysicalPath, 2000);
            entry.UserAgent = PortalDiagnosticSanitizer.SanitizeAndTruncate(request.UserAgent, 400);
        }

        private static string CreateEventId()
        {
            return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static void WriteTrace(PortalDiagnosticEntry entry)
        {
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
                    Trace.TraceError(traceMessage);
                    break;
            }
        }

        private static void WriteFile(PortalDiagnosticEntry entry)
        {
            string logDirectory = ResolveLogDirectory();
            string serialized = JsonConvert.SerializeObject(
                entry,
                Formatting.None,
                new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
            string payload = serialized + Environment.NewLine;
            int payloadByteCount = new UTF8Encoding(false).GetByteCount(payload);

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
            int maximumFileBytes = PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.DiagnosticsMaxFileBytes);
            string datePart = utcTime.ToString("yyyyMMdd");

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

            throw new IOException("Portal diagnostics exhausted the daily log-file sequence range.");
        }

        private static void CleanupExpiredLogs(string logDirectory, DateTime currentUtcDate)
        {
            if (_lastRetentionCleanupUtcDate == currentUtcDate)
            {
                return;
            }

            _lastRetentionCleanupUtcDate = currentUtcDate;
            int retentionDays = PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.DiagnosticsRetentionDays);
            DateTime cutoffUtcDate = currentUtcDate.AddDays(-retentionDays);

            try
            {
                foreach (string filePath in Directory.EnumerateFiles(logDirectory, "portal-*.jsonl", SearchOption.TopDirectoryOnly))
                {
                    DateTime fileDate;
                    if (!TryGetManagedLogDate(Path.GetFileName(filePath), out fileDate) || fileDate >= cutoffUtcDate)
                    {
                        continue;
                    }

                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceWarning(
                            "Portal diagnostics retention cleanup could not delete '{0}'. Error={1}",
                            Path.GetFileName(filePath),
                            PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.Message, 500));
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.TraceWarning(
                    "Portal diagnostics retention cleanup failed. Error={0}",
                    PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.Message, 500));
            }
        }

        private static bool TryGetManagedLogDate(string fileName, out DateTime fileDate)
        {
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
        /// 解析文件日志使用的诊断日志目录。
        /// Resolves the diagnostics log directory used by file logging.
        /// </summary>
        public static string ResolveLogDirectory()
        {
            string configuredDirectory = PortalRuntimeSettings.GetString(PortalSettingsRegistry.DiagnosticsLogDirectory);
            if (!string.IsNullOrWhiteSpace(configuredDirectory))
            {
                string expanded = Environment.ExpandEnvironmentVariables(configuredDirectory.Trim());
                if (!Path.IsPathRooted(expanded))
                {
                    expanded = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expanded);
                }

                return Path.GetFullPath(expanded);
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs");
        }
    }
}
