using ASPNET.StarterKit.Portal.Sys;
using ASPNET.StarterKit.Portal.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Hosting;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 只读系统健康检查服务。
    /// Read-only system health checker.
    /// </summary>
    /// <remarks>
    /// P2.2 仅收集和展示状态，不执行修复动作，不提供任意 SQL、脚本、命令或文件浏览入口。
    /// P2.2 collects and displays status only; it does not repair, execute SQL/scripts/commands, or browse files.
    /// </remarks>
    public static class PortalHealthChecker
    {
        /// <summary>
        /// 执行一次系统健康检查。
        /// Runs one system health check.
        /// </summary>
        public static PortalHealthSnapshot Check(HttpContext context)
        {
            var checks = new List<PortalHealthCheckResult>();
            var settings = BuildSettingRows();

            AddApplicationChecks(checks, context);
            AddRuntimeChecks(checks);
            AddConfigurationChecks(checks);
            AddDatabaseCheck(checks, context);
            AddRegistryCheck(checks, settings);
            AddDirectoryChecks(checks);
            AddThemeChecks(checks, context);

            return new PortalHealthSnapshot(DateTime.UtcNow, checks, settings);
        }

        private static void AddApplicationChecks(IList<PortalHealthCheckResult> checks, HttpContext context)
        {
            checks.Add(new PortalHealthCheckResult(
                "Application",
                "应用路径",
                PortalHealthStatus.Healthy,
                "应用域路径已解析。",
                "BaseDirectory=" + AppDomain.CurrentDomain.BaseDirectory +
                "; VirtualPath=" + HttpRuntime.AppDomainAppVirtualPath));

            if (context != null && context.Request != null)
            {
                checks.Add(new PortalHealthCheckResult(
                    "Application",
                    "当前请求",
                    PortalHealthStatus.Healthy,
                    "当前请求上下文可用。",
                    "Url=" + context.Request.Url +
                    "; AppRelativePath=" + context.Request.AppRelativeCurrentExecutionFilePath));
            }
        }

        private static void AddRuntimeChecks(IList<PortalHealthCheckResult> checks)
        {
            string identityName = "(unknown)";
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (identity != null)
                {
                    identityName = identity.Name;
                }
            }
            catch (Exception exception)
            {
                identityName = "无法读取进程身份: " + exception.Message;
            }

            checks.Add(new PortalHealthCheckResult(
                "Runtime",
                "运行时环境",
                PortalHealthStatus.Healthy,
                "运行时基本信息可用。",
                "MachineName=" + Environment.MachineName +
                "; ProcessId=" + Process.GetCurrentProcess().Id +
                "; Identity=" + identityName +
                "; OSVersion=" + Environment.OSVersion +
                "; CLR=" + Environment.Version));
        }

        private static void AddConfigurationChecks(IList<PortalHealthCheckResult> checks)
        {
            string env = string.IsNullOrWhiteSpace(GlobalInfo.Environment) ? "dev" : GlobalInfo.Environment;
            checks.Add(new PortalHealthCheckResult(
                "Configuration",
                "环境标识",
                PortalHealthStatus.Healthy,
                "当前环境标识已解析。",
                "env=" + env));

            try
            {
                string configRoot = ExternalConnectionStringLoader.ResolveExternalConfigRoot();
                string configFile = Path.Combine(
                    configRoot,
                    env,
                    ExternalConnectionStringLoader.ConnectionStringsFileName);
                bool exists = File.Exists(configFile);

                checks.Add(new PortalHealthCheckResult(
                    "Configuration",
                    "外置连接串文件",
                    exists ? PortalHealthStatus.Healthy : PortalHealthStatus.Warning,
                    exists ? "外置连接串文件存在。" : "外置连接串文件不存在。",
                    "ConfigRoot=" + configRoot + "; ConfigFile=" + configFile));
            }
            catch (Exception exception)
            {
                checks.Add(new PortalHealthCheckResult(
                    "Configuration",
                    "外置配置根目录",
                    PortalHealthStatus.Error,
                    "外置配置根目录解析失败。",
                    exception.Message));
            }
        }

        private static void AddDatabaseCheck(IList<PortalHealthCheckResult> checks, HttpContext context)
        {
            string connectionString;
            try
            {
                connectionString = Global.Container == null
                    ? null
                    : Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "SystemHealth.Database",
                    "Database connection string resolve failed.",
                    exception,
                    context);

                checks.Add(new PortalHealthCheckResult(
                    "Database",
                    "数据库连接",
                    PortalHealthStatus.Error,
                    "无法从 Unity 容器解析数据库连接串。",
                    exception.Message,
                    eventId));
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                checks.Add(new PortalHealthCheckResult(
                    "Database",
                    "数据库连接",
                    PortalHealthStatus.Warning,
                    "数据库连接串未配置。",
                    "Unity named instance '" + ExternalConnectionStringLoader.UnityConnectionStringName + "' is empty."));
                return;
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

                checks.Add(new PortalHealthCheckResult(
                    "Database",
                    "数据库连接",
                    PortalHealthStatus.Healthy,
                    "数据库轻量连接测试通过。",
                    "Executed SELECT 1 without exposing the connection string."));
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "SystemHealth.Database",
                    "Database health check failed.",
                    exception,
                    context);

                checks.Add(new PortalHealthCheckResult(
                    "Database",
                    "数据库连接",
                    PortalHealthStatus.Error,
                    "数据库轻量连接测试失败。",
                    exception.Message,
                    eventId));
            }
        }

        private static void AddRegistryCheck(
            IList<PortalHealthCheckResult> checks,
            IList<PortalSettingHealthInfo> settings)
        {
            checks.Add(new PortalHealthCheckResult(
                "Settings",
                "设置 Registry",
                settings.Count > 0 ? PortalHealthStatus.Healthy : PortalHealthStatus.Warning,
                settings.Count > 0 ? "设置 registry 已加载。" : "设置 registry 为空。",
                "RegisteredSettings=" + settings.Count));
        }

        private static void AddDirectoryChecks(IList<PortalHealthCheckResult> checks)
        {
            AddWritableDirectoryCheck(checks, "Storage", "诊断日志目录", PortalDiagnostics.ResolveLogDirectory());
            AddWritableDirectoryCheck(checks, "Storage", "上传目录", HostingEnvironment.MapPath("~/uploads"));
        }

        private static void AddThemeChecks(IList<PortalHealthCheckResult> checks, HttpContext context)
        {
            string configuredTheme = PortalRuntimeSettings.GetString(PortalSettingsRegistry.ThemeName);
            string resolvedTheme = PortalThemeResolver.ResolveThemeName(context);
            string resolvedPath = HostingEnvironment.MapPath("~/App_Themes/" + resolvedTheme);
            string defaultPath = HostingEnvironment.MapPath("~/App_Themes/" + PortalThemeResolver.DefaultThemeName);

            bool resolvedExists = !string.IsNullOrEmpty(resolvedPath) && Directory.Exists(resolvedPath);
            bool defaultExists = !string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath);
            PortalHealthStatus status = resolvedExists && defaultExists
                ? PortalHealthStatus.Healthy
                : PortalHealthStatus.Error;

            if (status == PortalHealthStatus.Healthy &&
                !string.Equals(configuredTheme, resolvedTheme, StringComparison.Ordinal))
            {
                status = PortalHealthStatus.Warning;
            }

            checks.Add(new PortalHealthCheckResult(
                "Theme",
                "主题目录",
                status,
                status == PortalHealthStatus.Healthy ? "主题目录检查通过。" : "主题目录存在异常。",
                "Configured=" + configuredTheme +
                "; Resolved=" + resolvedTheme +
                "; ResolvedPath=" + resolvedPath +
                "; DefaultPath=" + defaultPath));
        }

        private static void AddWritableDirectoryCheck(
            IList<PortalHealthCheckResult> checks,
            string category,
            string name,
            string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                checks.Add(new PortalHealthCheckResult(
                    category,
                    name,
                    PortalHealthStatus.Error,
                    "目录路径为空。",
                    string.Empty));
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                checks.Add(new PortalHealthCheckResult(
                    category,
                    name,
                    PortalHealthStatus.Error,
                    "目录不存在。",
                    directoryPath));
                return;
            }

            string testFile = Path.Combine(directoryPath, ".hia-health-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(testFile, "health", new UTF8Encoding(false));
                File.Delete(testFile);
                checks.Add(new PortalHealthCheckResult(
                    category,
                    name,
                    PortalHealthStatus.Healthy,
                    "目录存在且可写。",
                    directoryPath));
            }
            catch (Exception exception)
            {
                checks.Add(new PortalHealthCheckResult(
                    category,
                    name,
                    PortalHealthStatus.Error,
                    "目录写入测试失败。",
                    directoryPath + "; Error=" + exception.Message));
            }
            finally
            {
                TryDelete(testFile);
            }
        }

        private static IList<PortalSettingHealthInfo> BuildSettingRows()
        {
            var settings = new List<PortalSettingHealthInfo>();
            foreach (PortalSettingDefinition definition in PortalSettingsRegistry.GetAll())
            {
                string currentValue;
                string source;
                GetEffectiveSettingValue(definition, out currentValue, out source);

                settings.Add(new PortalSettingHealthInfo(
                    definition.Key,
                    definition.DisplayName,
                    definition.ValueType.ToString(),
                    definition.IsSensitive ? "(sensitive)" : currentValue,
                    source,
                    definition.IsSensitive,
                    definition.CanEditOnline,
                    definition.RequiresRestart));
            }

            return settings;
        }

        private static void GetEffectiveSettingValue(
            PortalSettingDefinition definition,
            out string currentValue,
            out string source)
        {
            string configuredValue = ConfigurationManager.AppSettings[definition.Key];
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                currentValue = definition.DefaultValue;
                source = "Default";
                return;
            }

            if (TryNormalizeSettingValue(definition, configuredValue, out currentValue))
            {
                source = "AppSettings";
                return;
            }

            currentValue = definition.DefaultValue;
            source = "Default (invalid appSettings)";
        }

        private static bool TryNormalizeSettingValue(
            PortalSettingDefinition definition,
            string configuredValue,
            out string normalizedValue)
        {
            normalizedValue = configuredValue.Trim();

            switch (definition.ValueType)
            {
                case PortalSettingValueType.Boolean:
                    bool boolValue;
                    if (bool.TryParse(configuredValue, out boolValue))
                    {
                        normalizedValue = boolValue.ToString().ToLowerInvariant();
                        return true;
                    }

                    return false;

                case PortalSettingValueType.Integer:
                    int intValue;
                    if (int.TryParse(configuredValue, out intValue) &&
                        (!definition.MinIntegerValue.HasValue || intValue >= definition.MinIntegerValue.Value) &&
                        (!definition.MaxIntegerValue.HasValue || intValue <= definition.MaxIntegerValue.Value))
                    {
                        normalizedValue = intValue.ToString();
                        return true;
                    }

                    return false;

                default:
                    return true;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // 临时文件清理失败不应中断健康页；目录检查结果已经记录。
                // Temporary cleanup failures must not break the health page; the check result is already recorded.
            }
        }
    }
}
