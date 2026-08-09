using ASPNET.StarterKit.Portal.Sys;
using ASPNET.StarterKit.Portal.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Hosting;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>只读系统健康检查服务。</zh-CN>
    ///   <en>Read-only system health checker.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P2.2 仅收集和展示状态，不执行修复动作，不提供任意 SQL、脚本、命令或文件浏览入口。</zh-CN>
    ///   <en>P2.2 collects and displays status only; it does not repair, execute SQL/scripts/commands, or browse files.</en>
    /// </lang>
    /// </remarks>
    public static class PortalHealthChecker
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>执行一次只读系统健康检查，并按固定顺序收集受控状态。</zh-CN>
        ///   <en>Runs one read-only system health check and collects controlled status in a fixed order.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文；用于受限诊断和请求/主题检查，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context for restricted diagnostics and request/theme checks; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>包含健康检查时间、结果和设置状态的只读快照。</zh-CN>
        ///   <en>Read-only snapshot containing check time, results, and setting status.</en>
        /// </l>
        /// </returns>
        public static PortalHealthSnapshot Check(HttpContext context)
        {
            // <lang>
            //   <zh-CN>使用本次检查独立的结果集合，避免跨请求共享可变健康状态。</zh-CN>
            //   <en>Use a result collection owned by this check so mutable health state is not shared across requests.</en>
            // </lang>
            var checks = new List<PortalHealthCheckResult>();

            // <lang>
            //   <zh-CN>先读取设置行快照，后续健康检查只消费已构建的受控元数据，不在编排过程中临时推断设置。</zh-CN>
            //   <en>Build the setting rows first so later checks consume controlled metadata rather than inferring settings during orchestration.</en>
            // </lang>
            var settings = BuildSettingRows();

            // <lang>
            //   <zh-CN>按应用、运行时、配置、模块、数据库、Registry、目录和主题的固定顺序执行只读检查，保持快照输出稳定。</zh-CN>
            //   <en>Run read-only checks in the fixed application, runtime, configuration, module, database, registry, directory, and theme order so snapshot output remains stable.</en>
            // </lang>
            AddApplicationChecks(checks, context);
            AddRuntimeChecks(checks);
            AddConfigurationChecks(checks);
            AddModuleProfileChecks(checks, context);
            AddDatabaseCheck(checks, context);
            AddRegistryCheck(checks, settings);
            AddDirectoryChecks(checks);
            AddThemeChecks(checks, context);

            // <lang>
            //   <zh-CN>以当前 UTC 时间封存结果；快照不触发修复、写配置或暴露连接串等秘密。</zh-CN>
            //   <en>Seal the result with the current UTC time; the snapshot performs no repair or configuration write and exposes no secrets such as connection strings.</en>
            // </lang>
            return new PortalHealthSnapshot(DateTime.UtcNow, checks, settings);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>收集应用域路径和可选当前请求的只读状态。</zh-CN>
        ///   <en>Collects read-only application-domain path and optional current-request status.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>可选当前 HTTP 上下文。</zh-CN>
        ///   <en>Optional current HTTP context.</en>
        /// </l>
        /// </param>
        private static void AddApplicationChecks(IList<PortalHealthCheckResult> checks, HttpContext context)
        {
            // <lang>
            //   <zh-CN>把应用域基目录和虚拟路径作为只读部署事实输出，不读取或暴露应用配置内容。</zh-CN>
            //   <en>Project the application-domain base and virtual paths as read-only deployment facts without reading or exposing configuration content.</en>
            // </lang>
            checks.Add(new PortalHealthCheckResult(
                "Application",
                "应用路径",
                PortalHealthStatus.Healthy,
                "应用域路径已解析。",
                "BaseDirectory=" + AppDomain.CurrentDomain.BaseDirectory +
                "; VirtualPath=" + HttpRuntime.AppDomainAppVirtualPath));

            // <lang>
            //   <zh-CN>仅当请求上下文和 Request 同时存在时附加请求路径事实；不构造请求、不访问请求正文或凭据。</zh-CN>
            //   <en>Append request-path facts only when both context and Request exist; do not construct a request or access its body or credentials.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>收集进程身份、机器、进程和 CLR 的只读运行时状态。</zh-CN>
        ///   <en>Collects read-only runtime status for process identity, machine, process, and CLR.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        private static void AddRuntimeChecks(IList<PortalHealthCheckResult> checks)
        {
            // <lang>
            //   <zh-CN>使用固定匿名占位初始化身份文本，保证身份读取失败时结果仍可序列化且不携带异常原文。</zh-CN>
            //   <en>Initialize identity text with a fixed placeholder so the result remains serializable on identity-read failure without carrying raw exception text.</en>
            // </lang>
            string identityName = "(unknown)";
            try
            {
                // <lang>
                //   <zh-CN>读取当前进程身份作为运行环境事实；该调用只获取身份对象，不改变线程或权限。</zh-CN>
                //   <en>Read the current process identity as a runtime fact; this call only obtains the identity object and changes neither thread nor permissions.</en>
                // </lang>
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (identity != null)
                {
                    // <lang>
                    //   <zh-CN>仅保存身份名文本供健康结果使用，不保存身份对象或令牌生命周期。</zh-CN>
                    //   <en>Keep only the identity name text for the health result, not the identity object or token lifetime.</en>
                    // </lang>
                    identityName = identity.Name;
                }
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>身份读取失败时使用受控提示，健康检查继续收集其它运行时事实。</zh-CN>
                //   <en>Use a controlled notice when identity reading fails and continue collecting the other runtime facts.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查环境标识和受控外置连接串文件是否存在，不返回文件内容。</zh-CN>
        ///   <en>Checks the environment identifier and existence of the controlled external connection-string file without returning file contents.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        private static void AddConfigurationChecks(IList<PortalHealthCheckResult> checks)
        {
            // <lang>
            //   <zh-CN>把环境标识归一到稳定的 dev 回退，不把空白配置传播到路径拼接或诊断结果。</zh-CN>
            //   <en>Normalize the environment identifier to a stable dev fallback so blank configuration does not flow into path composition or diagnostics.</en>
            // </lang>
            string env = string.IsNullOrWhiteSpace(GlobalInfo.Environment) ? "dev" : GlobalInfo.Environment;
            checks.Add(new PortalHealthCheckResult(
                "Configuration",
                "环境标识",
                PortalHealthStatus.Healthy,
                "当前环境标识已解析。",
                "env=" + env));

            try
            {
                // <lang>
                //   <zh-CN>解析受控外置配置根目录；该路径只用于存在性检查，不读取连接串内容。</zh-CN>
                //   <en>Resolve the controlled external-configuration root; use the path only for an existence check and never read connection-string content.</en>
                // </lang>
                string configRoot = ExternalConnectionStringLoader.ResolveExternalConfigRoot();

                // <lang>
                //   <zh-CN>按环境和固定文件名构造预期配置文件路径，环境名来自受控回退，文件名不接受请求输入。</zh-CN>
                //   <en>Compose the expected configuration-file path from the controlled environment fallback and fixed filename; neither accepts request input.</en>
                // </lang>
                string configFile = Path.Combine(
                    configRoot,
                    env,
                    ExternalConnectionStringLoader.ConnectionStringsFileName);
                // <lang>
                //   <zh-CN>只记录文件是否存在，不打开、解析或回显文件内容。</zh-CN>
                //   <en>Record only whether the file exists; do not open, parse, or echo its contents.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>路径解析失败映射为健康错误并保留受限异常消息；不让配置检查异常中断其它健康检查。</zh-CN>
                //   <en>Map path-resolution failure to a health error with restricted exception text so it cannot abort the other health checks.</en>
                // </lang>
                checks.Add(new PortalHealthCheckResult(
                    "Configuration",
                    "外置配置根目录",
                    PortalHealthStatus.Error,
                    "外置配置根目录解析失败。",
                    exception.Message));
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行固定的轻量数据库连接检查，并将异常交给受限诊断链。</zh-CN>
        ///   <en>Runs a fixed lightweight database connectivity check and sends exceptions through the restricted diagnostics chain.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>供诊断净化使用的当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context for diagnostics sanitization; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        private static void AddDatabaseCheck(IList<PortalHealthCheckResult> checks, HttpContext context)
        {
            // <lang>
            //   <zh-CN>本检查需要健康状态和关联事件编号，不能复用仅记录结果的 PortalDiagnostics.CheckSqlConnection。</zh-CN>
            //   <en>This check needs a health status and correlated event id, so it cannot reuse the record-only PortalDiagnostics.CheckSqlConnection.</en>
            // </lang>
            string connectionString;
            try
            {
                connectionString = Global.Container == null
                    ? null
                    : Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>连接串解析异常先生成受限事件编号，再加入固定错误结果；调用方永远只看到健康状态和安全摘要。</zh-CN>
                //   <en>Generate a restricted event id for connection-string resolution failures before adding a fixed error result; callers see only health state and a safe summary.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>用未打开的连接和固定 SELECT 1 执行轻量连通性检查；连接对象由 using 管理，不保存连接串。</zh-CN>
                //   <en>Run a lightweight connectivity check with an unopened connection and fixed SELECT 1; using owns the connection and no connection string is retained.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>数据库检查异常转换为受限事件和错误结果，不向健康输出拼接连接串或 SQL 详情。</zh-CN>
                //   <en>Convert database-check exceptions into a restricted event and error result without adding connection-string or SQL detail to health output.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查模块 Profile，并只把受控名称和数量投影到健康结果。</zh-CN>
        ///   <en>Checks the module Profile and projects only controlled names and counts into health results.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>供 Profile 解析使用的当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context for Profile resolution; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        private static void AddModuleProfileChecks(IList<PortalHealthCheckResult> checks, HttpContext context)
        {
            // <lang>
            //   <zh-CN>从当前部署和请求上下文解析一次 Profile 快照，后续只消费其已净化的集合。</zh-CN>
            //   <en>Resolve one Profile snapshot from the deployment and request context, then consume only its sanitized collections.</en>
            // </lang>
            PortalModuleProfileSnapshot profile = PortalModuleProfileResolver.Resolve(context);

            // <lang>
            //   <zh-CN>用无效条目数量决定警告级别；数量是健康判定事实，不把无效配置原文当作控制输入。</zh-CN>
            //   <en>Use the invalid-entry count to decide warning severity; the count is the health fact, not raw invalid configuration as control input.</en>
            // </lang>
            bool hasInvalidEntries = profile.InvalidEntries.Count > 0;

            // <lang>
            //   <zh-CN>Profile 只展示非敏感名称和数量；它是部署能力集的解释信息，不展示连接串或文件内容。</zh-CN>
            //   <en>The Profile check exposes only non-sensitive names and counts. It explains the deployment capability set and never shows connection strings or file contents.</en>
            // </lang>
            checks.Add(new PortalHealthCheckResult(
                "Modules",
                "模块 Profile",
                hasInvalidEntries ? PortalHealthStatus.Warning : PortalHealthStatus.Healthy,
                hasInvalidEntries ? "模块 Profile 已加载，但存在无效配置项。" : "模块 Profile 已加载。",
                "ActiveProfile=" + profile.ActiveProfile +
                "; AllowedPackageCount=" + profile.AllowedPackageIds.Count +
                "; AllowedPackages=" + string.Join(",", profile.AllowedPackageIds.ToArray()) +
                "; InvalidEntries=" + string.Join(",", profile.InvalidEntries.ToArray())));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将已构建的设置 Registry 数量投影为健康结果。</zh-CN>
        ///   <en>Projects the size of the built setting registry into a health result.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        /// <param name="settings">
        /// <l>
        ///   <zh-CN>本次检查已经解析的设置行集合。</zh-CN>
        ///   <en>Setting rows already resolved for this check.</en>
        /// </l>
        /// </param>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查诊断日志和上传目录的存在性与可写性。</zh-CN>
        ///   <en>Checks existence and writability of the diagnostics-log and upload directories.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        private static void AddDirectoryChecks(IList<PortalHealthCheckResult> checks)
        {
            // <lang>
            //   <zh-CN>对两个固定用途目录复用同一可写性检查，保持空路径、缺失目录、写入失败和清理规则一致。</zh-CN>
            //   <en>Reuse the same writability check for the two fixed-purpose directories so blank paths, missing directories, write failures, and cleanup follow one rule.</en>
            // </lang>
            AddWritableDirectoryCheck(checks, "Storage", "诊断日志目录", PortalDiagnostics.ResolveLogDirectory());
            AddWritableDirectoryCheck(checks, "Storage", "上传目录", HostingEnvironment.MapPath("~/uploads"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查当前主题与默认主题的目录和受信 manifest 状态，并标记配置回退。</zh-CN>
        ///   <en>Checks directory and trusted-manifest status for the current and default themes and marks configuration fallback.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>供运行期设置和主题解析使用的当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context for runtime-setting and theme resolution; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        private static void AddThemeChecks(IList<PortalHealthCheckResult> checks, HttpContext context)
        {
            // <lang>
            //   <zh-CN>解析当前主题的有效设置值及来源，供后续比较配置意图与实际主题选择。</zh-CN>
            //   <en>Resolve the effective theme setting and source so later checks can compare configuration intent with actual theme selection.</en>
            // </lang>
            PortalRuntimeSettingValue configuredTheme = PortalRuntimeSettings.GetEffectiveValue(
                PortalSettingsRegistry.ThemeName,
                context);

            // <lang>
            //   <zh-CN>解析主题上下文并保存最终主题名；该值来自受信主题解析器，不直接接受请求主题文本。</zh-CN>
            //   <en>Resolve the theme context and keep the final theme name from the trusted resolver rather than accepting request theme text directly.</en>
            // </lang>
            PortalThemeContext resolvedThemeContext = PortalThemeResolver.ResolveThemeContext(context);

            // <lang>
            //   <zh-CN>保存解析主题名用于目录、manifest 和配置来源三路一致性检查。</zh-CN>
            //   <en>Keep the resolved theme name for consistent directory, manifest, and configuration-source checks.</en>
            // </lang>
            string resolvedTheme = resolvedThemeContext.ThemeName;

            // <lang>
            //   <zh-CN>把解析主题映射为应用主题目录路径；路径仅用于存在性检查，不作为用户可浏览入口。</zh-CN>
            //   <en>Map the resolved theme to an application-theme directory path for existence checks only, not as a user-browsable entry point.</en>
            // </lang>
            string resolvedPath = HostingEnvironment.MapPath("~/App_Themes/" + resolvedTheme);

            // <lang>
            //   <zh-CN>构造固定默认主题目录路径，作为主题回退的独立部署基线。</zh-CN>
            //   <en>Compose the fixed default-theme directory path as an independent deployment baseline for fallback.</en>
            // </lang>
            string defaultPath = HostingEnvironment.MapPath("~/App_Themes/" + PortalThemeResolver.DefaultThemeName);

            // <lang>
            //   <zh-CN>仅把解析主题和默认主题目录是否存在作为布尔健康事实，不读取目录内容。</zh-CN>
            //   <en>Record only whether the resolved and default theme directories exist as Boolean health facts; do not read directory contents here.</en>
            // </lang>
            bool resolvedExists = !string.IsNullOrEmpty(resolvedPath) && Directory.Exists(resolvedPath);

            // <lang>
            //   <zh-CN>单独保存默认主题目录存在事实，与解析主题目录结果保持可审计的独立字段。</zh-CN>
            //   <en>Keep the default-theme directory fact separately so it remains auditable alongside the resolved-theme result.</en>
            // </lang>
            bool defaultExists = !string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath);

            // <lang>
            //   <zh-CN>承接受信主题包检查结果及受控失败原因；包对象仅在当前健康检查中短暂使用。</zh-CN>
            //   <en>Receive trusted-theme-package results and controlled failure reasons; package objects live only for this health check.</en>
            // </lang>
            PortalThemePackage resolvedPackage;

            // <lang>
            //   <zh-CN>保存解析主题受信包失败原因的受控文本，仅用于健康摘要，不作为路径或控制输入。</zh-CN>
            //   <en>Keep the controlled failure text for the resolved trusted package for health summary only, never as a path or control input.</en>
            // </lang>
            string resolvedPackageReason;

            // <lang>
            //   <zh-CN>验证解析主题的 manifest 和资源边界，拒绝以目录存在替代受信包校验。</zh-CN>
            //   <en>Validate the resolved theme manifest and resource boundary rather than treating directory existence as package trust.</en>
            // </lang>
            bool resolvedPackageIsValid = PortalThemeCatalog.TryGetTrustedPackage(
                resolvedTheme,
                out resolvedPackage,
                out resolvedPackageReason);
            // <lang>
            //   <zh-CN>承接默认主题受信包对象；该对象只在当前健康检查中短暂使用。</zh-CN>
            //   <en>Hold the default trusted-package object only for the current health check.</en>
            // </lang>
            PortalThemePackage defaultPackage;

            // <lang>
            //   <zh-CN>保存默认主题受信包失败原因的受控文本，仅用于健康摘要。</zh-CN>
            //   <en>Keep the controlled failure text for the default trusted package for health summary only.</en>
            // </lang>
            string defaultPackageReason;

            // <lang>
            //   <zh-CN>验证固定默认主题包，使回退基线本身也必须通过相同受信校验；布尔结果只表达 manifest/资源边界是否有效。</zh-CN>
            //   <en>Validate the fixed default package with the same trust gate so the fallback baseline is trusted; the Boolean result expresses only manifest/resource-boundary validity.</en>
            // </lang>
            bool defaultPackageIsValid = PortalThemeCatalog.TryGetTrustedPackage(
                PortalThemeResolver.DefaultThemeName,
                out defaultPackage,
                out defaultPackageReason);
            // <lang>
            //   <zh-CN>只有两套目录和两份受信 manifest 都有效时才判定健康；任一部署缺口都 fail-closed 为 Error。</zh-CN>
            //   <en>Mark the check healthy only when both directories and both trusted manifests are valid; any deployment gap fails closed to Error.</en>
            // </lang>
            PortalHealthStatus status = resolvedExists && defaultExists && resolvedPackageIsValid && defaultPackageIsValid
                ? PortalHealthStatus.Healthy
                : PortalHealthStatus.Error;

            // <lang>
            //   <zh-CN>目录和 manifest 均健康但有效配置与解析主题不一致，或解析器发生回退时标记 Warning，不伪装为错误部署。</zh-CN>
            //   <en>When directories and manifests are healthy but effective configuration disagrees with the resolved theme or the resolver fell back, mark Warning rather than mislabeling deployment as Error.</en>
            // </lang>
            if (status == PortalHealthStatus.Healthy &&
                (!string.Equals(configuredTheme.Value, resolvedTheme, StringComparison.Ordinal) ||
                 resolvedThemeContext.Source == PortalThemeSource.Fallback))
            {
                status = PortalHealthStatus.Warning;
            }

            checks.Add(new PortalHealthCheckResult(
                "Theme",
                "主题部署包",
                status,
                status == PortalHealthStatus.Healthy ? "主题部署包检查通过。" : "主题目录或 manifest 存在异常。",
                "Configured=" + configuredTheme.Value +
                "; Source=" + configuredTheme.Source +
                "; Resolved=" + resolvedTheme +
                "; ResolvedPath=" + resolvedPath +
                "; DefaultPath=" + defaultPath +
                "; ResolvedManifest=" + resolvedPackageIsValid +
                "; DefaultManifest=" + defaultPackageIsValid +
                "; ResolvedManifestReason=" + resolvedPackageReason +
                "; DefaultManifestReason=" + defaultPackageReason));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>以临时文件写入/删除探测一个固定用途目录的可写性。</zh-CN>
        ///   <en>Probes writability of a fixed-purpose directory by writing and deleting a temporary file.</en>
        /// </lang>
        /// </summary>
        /// <param name="checks">
        /// <l>
        ///   <zh-CN>接收检查结果的本次快照集合。</zh-CN>
        ///   <en>Snapshot collection receiving check results.</en>
        /// </l>
        /// </param>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>健康结果分类。</zh-CN>
        ///   <en>Health-result category.</en>
        /// </l>
        /// </param>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>健康结果显示名称。</zh-CN>
        ///   <en>Health-result display name.</en>
        /// </l>
        /// </param>
        /// <param name="directoryPath">
        /// <l>
        ///   <zh-CN>已由调用方解析的目录路径；空白路径不触发文件系统写入。</zh-CN>
        ///   <en>Directory path resolved by the caller; a blank path triggers no file-system write.</en>
        /// </l>
        /// </param>
        private static void AddWritableDirectoryCheck(
            IList<PortalHealthCheckResult> checks,
            string category,
            string name,
            string directoryPath)
        {
            // <lang>
            //   <zh-CN>空白目录路径直接失败并返回，避免 Path.Combine 或文件写入隐式使用未定义位置。</zh-CN>
            //   <en>Fail and return immediately for a blank directory path so Path.Combine or file writes cannot use an undefined location.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>目录不存在时不尝试创建目录；健康检查保持只读部署诊断，不承担修复或初始化职责。</zh-CN>
            //   <en>Do not create a missing directory; the health check remains a read-only deployment diagnostic and performs no repair or initialization.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>生成当前检查专用的临时文件路径；随机名避免并发冲突，文件仅用于可写性探测并在 finally 清理。</zh-CN>
            //   <en>Generate a check-specific temporary-file path; the random name avoids concurrency collisions, and the file exists only for writability probing before finally cleanup.</en>
            // </lang>
            string testFile = Path.Combine(directoryPath, ".hia-health-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                // <lang>
                //   <zh-CN>写入固定无敏感内容后立即删除，验证目录写权限而不留下健康检查数据。</zh-CN>
                //   <en>Write fixed non-sensitive content and delete it immediately to verify directory write access without leaving health-check data.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>写入或删除失败映射为目录错误，摘要仅用于诊断展示，不改变检查整体编排。</zh-CN>
                //   <en>Map write or delete failure to a directory error; the summary is diagnostic display only and does not change overall orchestration.</en>
                // </lang>
                checks.Add(new PortalHealthCheckResult(
                    category,
                    name,
                    PortalHealthStatus.Error,
                    "目录写入测试失败。",
                    directoryPath + "; Error=" + exception.Message));
            }
            finally
            {
                // <lang>
                //   <zh-CN>无论探测成功与否都尝试删除临时文件，清理失败由 TryDelete 隔离而不掩盖原始结果。</zh-CN>
                //   <en>Attempt temporary-file deletion regardless of probe outcome; TryDelete isolates cleanup failure without hiding the original result.</en>
                // </lang>
                TryDelete(testFile);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按 Registry 声明顺序构建设置健康行，并对敏感值只输出固定占位。</zh-CN>
        ///   <en>Builds setting-health rows in registry declaration order and projects sensitive values as a fixed placeholder.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>本次健康检查使用的设置行集合。</zh-CN>
        ///   <en>Setting rows used by the current health check.</en>
        /// </l>
        /// </returns>
        private static IList<PortalSettingHealthInfo> BuildSettingRows()
        {
            // <lang>
            //   <zh-CN>使用本次构建独立的可变列表承接每个已登记定义的只读投影。</zh-CN>
            //   <en>Use a list owned by this build to hold the read-only projection for each registered definition.</en>
            // </lang>
            var settings = new List<PortalSettingHealthInfo>();

            // <lang>
            //   <zh-CN>只遍历静态 Registry 定义，不从请求或未知键动态扩展健康行。</zh-CN>
            //   <en>Traverse only static registry definitions and never extend health rows from requests or unknown keys.</en>
            // </lang>
            foreach (PortalSettingDefinition definition in PortalSettingsRegistry.GetAll())
            {
                // <lang>
                //   <zh-CN>承接当前定义的有效文本和来源；解析器负责来源优先级与安全回退。</zh-CN>
                //   <en>Receive the current definition's effective text and source; the resolver owns source precedence and safe fallback.</en>
                // </lang>
                string currentValue;

                // <lang>
                //   <zh-CN>保存当前有效值的来源名称，供设置健康行展示；不把来源名称当作新的解析输入。</zh-CN>
                //   <en>Keep the source name of the effective value for the setting-health row; do not treat the source label as new resolver input.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取一个设置的有效文本和来源名称，保持健康行与运行期解析器使用同一契约。</zh-CN>
        ///   <en>Reads one setting's effective text and source name so health rows share the runtime resolver contract.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>已登记的设置定义。</zh-CN>
        ///   <en>Registered setting definition.</en>
        /// </l>
        /// </param>
        /// <param name="currentValue">
        /// <l>
        ///   <zh-CN>输出有效文本值；敏感性占位由调用方决定。</zh-CN>
        ///   <en>Output effective text; the caller decides sensitive-value projection.</en>
        /// </l>
        /// </param>
        /// <param name="source">
        /// <l>
        ///   <zh-CN>输出来源层级名称。</zh-CN>
        ///   <en>Output source-layer name.</en>
        /// </l>
        /// </param>
        private static void GetEffectiveSettingValue(
            PortalSettingDefinition definition,
            out string currentValue,
            out string source)
        {
            // <lang>
            //   <zh-CN>复用运行期解析器获取已经过类型/范围门禁的结果，不在健康层重新实现来源优先级。</zh-CN>
            //   <en>Reuse the runtime resolver's type/range-gated result instead of reimplementing source precedence in the health layer.</en>
            // </lang>
            PortalRuntimeSettingValue effectiveValue = PortalRuntimeSettings.GetEffectiveValue(definition);
            currentValue = effectiveValue.Value;
            source = effectiveValue.Source.ToString();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尽力删除健康检查临时文件；清理失败被隔离，不覆盖已经记录的目录检查结果。</zh-CN>
        ///   <en>Best-effort deletes a health-check temporary file while isolating cleanup failure from the recorded directory result.</en>
        /// </lang>
        /// </summary>
        /// <param name="path">
        /// <l>
        ///   <zh-CN>待删除的临时文件路径，可为空。</zh-CN>
        ///   <en>Temporary-file path to delete; may be empty.</en>
        /// </l>
        /// </param>
        private static void TryDelete(string path)
        {
            // <lang>
            //   <zh-CN>只对非空且确实存在的路径执行删除；不创建目录、不解析用户路径，也不传播清理异常。</zh-CN>
            //   <en>Delete only a nonblank path that exists; do not create directories, resolve user paths, or propagate cleanup exceptions.</en>
            // </lang>
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // <lang>
                //   <zh-CN>临时文件清理失败不应中断健康页；目录检查结果已经记录。</zh-CN>
                //   <en>Temporary cleanup failures must not break the health page; the check result is already recorded.</en>
                // </lang>
            }
        }
    }
}
