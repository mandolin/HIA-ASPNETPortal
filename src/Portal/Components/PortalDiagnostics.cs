using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// Provides the minimal diagnostics facade for portal runtime events.
    /// 提供门户运行期事件的最小诊断门面。
    /// </summary>
    /// <remarks>
    /// P1.3 keeps the implementation small: write a daily text log and forward to <see cref="Trace"/>.
    /// P1.3 只保留小型实现：写入每日文本日志，并转发到 <see cref="Trace"/>。
    /// </remarks>
    public static class PortalDiagnostics
    {
        /// <summary>
        /// AppSettings key used to override the diagnostics log directory.
        /// 用于覆盖诊断日志目录的 appSettings 键名。
        /// </summary>
        public const string LogDirectorySettingKey = "Portal.Diagnostics.LogDirectory";

        /// <summary>
        /// AppSettings key used to allow detailed ASP.NET error output in development.
        /// 用于允许开发期详细 ASP.NET 错误输出的 appSettings 键名。
        /// </summary>
        public const string DetailedErrorsSettingKey = "Portal.Diagnostics.EnableDetailedErrors";

        private static readonly object LogLock = new object();

        /// <summary>
        /// Records an informational diagnostics message.
        /// 记录普通诊断信息。
        /// </summary>
        public static string Info(string category, string message, HttpContext context = null)
        {
            return Write("INFO", category, message, null, context);
        }

        /// <summary>
        /// Records a warning diagnostics message.
        /// 记录警告诊断信息。
        /// </summary>
        public static string Warn(string category, string message, HttpContext context = null)
        {
            return Write("WARN", category, message, null, context);
        }

        /// <summary>
        /// Records a handled exception and returns the diagnostics event id.
        /// 记录已处理异常，并返回诊断事件编号。
        /// </summary>
        public static string Error(string category, string message, Exception exception, HttpContext context = null)
        {
            return Write("ERROR", category, message, exception, context);
        }

        /// <summary>
        /// Records an unhandled exception and returns the diagnostics event id shown to users.
        /// 记录未处理异常，并返回展示给用户的诊断事件编号。
        /// </summary>
        public static string Unhandled(Exception exception, HttpContext context = null)
        {
            return Write("ERROR", "UnhandledException", "Unhandled portal exception.", exception, context);
        }

        /// <summary>
        /// Executes a lightweight SQL Server health check without logging the connection string.
        /// 执行轻量 SQL Server 健康检查，且不记录连接串原文。
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
        /// Determines whether detailed ASP.NET errors are explicitly enabled.
        /// 判断是否显式启用了详细 ASP.NET 错误输出。
        /// </summary>
        public static bool AreDetailedErrorsEnabled()
        {
            bool enabled;
            return bool.TryParse(ConfigurationManager.AppSettings[DetailedErrorsSettingKey], out enabled) && enabled;
        }

        private static string Write(string level, string category, string message, Exception exception, HttpContext context)
        {
            string eventId = CreateEventId();
            string entry = BuildEntry(eventId, level, category, message, exception, context);

            try
            {
                WriteTrace(level, entry);
                WriteFile(entry);
            }
            catch (Exception logException)
            {
                // 诊断系统不能反过来破坏业务请求；文件写入失败时只尝试写入 Trace。
                // Diagnostics must not break the request; fall back to Trace when file logging fails.
                try
                {
                    Trace.TraceError("Portal diagnostics write failed. EventId={0}; Error={1}", eventId, logException);
                }
                catch
                {
                    // 最后一层保护：忽略诊断自身的失败。
                    // Last-resort guard: ignore diagnostics failures.
                }
            }

            return eventId;
        }

        private static string CreateEventId()
        {
            return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static string BuildEntry(
            string eventId,
            string level,
            string category,
            string message,
            Exception exception,
            HttpContext context)
        {
            var builder = new StringBuilder();
            builder.AppendLine("------------------------------------------------------------------------");
            builder.AppendLine("EventId: " + eventId);
            builder.AppendLine("UtcTime: " + DateTime.UtcNow.ToString("o"));
            builder.AppendLine("Level: " + level);
            builder.AppendLine("Category: " + SafeText(category));
            builder.AppendLine("Message: " + SafeText(message));

            if (context != null)
            {
                AppendRequestContext(builder, context);
            }

            if (exception != null)
            {
                builder.AppendLine("Exception:");
                builder.AppendLine(exception.ToString());
            }

            return builder.ToString();
        }

        private static void AppendRequestContext(StringBuilder builder, HttpContext context)
        {
            HttpRequest request = context.Request;
            if (request == null)
            {
                return;
            }

            builder.AppendLine("RequestUrl: " + SafeText(request.Url?.ToString()));
            builder.AppendLine("HttpMethod: " + SafeText(request.HttpMethod));
            builder.AppendLine("UserHostAddress: " + SafeText(request.UserHostAddress));
            builder.AppendLine("UserName: " + SafeText(context.User?.Identity?.Name));
            builder.AppendLine("IsAuthenticated: " + (context.User?.Identity?.IsAuthenticated ?? false));
            builder.AppendLine("ApplicationPath: " + SafeText(request.ApplicationPath));
            builder.AppendLine("AppRelativePath: " + SafeText(request.AppRelativeCurrentExecutionFilePath));
            builder.AppendLine("PhysicalPath: " + SafeText(request.PhysicalPath));
            builder.AppendLine("UserAgent: " + SafeText(request.UserAgent));
        }

        private static string SafeText(string value)
        {
            return string.IsNullOrEmpty(value) ? "(empty)" : value.Replace("\r", " ").Replace("\n", " ");
        }

        private static void WriteTrace(string level, string entry)
        {
            switch (level)
            {
                case "INFO":
                    Trace.TraceInformation(entry);
                    break;
                case "WARN":
                    Trace.TraceWarning(entry);
                    break;
                default:
                    Trace.TraceError(entry);
                    break;
            }
        }

        private static void WriteFile(string entry)
        {
            string logDirectory = ResolveLogDirectory();
            Directory.CreateDirectory(logDirectory);

            string logFile = Path.Combine(logDirectory, "portal-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
            lock (LogLock)
            {
                File.AppendAllText(logFile, entry + Environment.NewLine, new UTF8Encoding(false));
            }
        }

        private static string ResolveLogDirectory()
        {
            string configuredDirectory = ConfigurationManager.AppSettings[LogDirectorySettingKey];
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
