using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ASPNET.StarterKit.Portal;

namespace ASPNET.StarterKit.Portal.Util
{
    /// <summary>
    /// 中文：外置门户连接串的加载结果。
    ///
    /// English: Result of loading the external Portal connection string.
    /// </summary>
    /// <remarks>
    /// 中文：结果保留最终连接串、配置根目录、实际读取文件、来源和数据库 profile，供启动代码使用。
    /// 连接串属于敏感信息，调用方不得将其输出到页面、诊断日志或普通管理界面。
    ///
    /// English: The result retains the effective connection string, configuration root, source file,
    /// source description, and database profile for startup code. The connection string is sensitive
    /// and must not be written to pages, diagnostic logs, or ordinary administration views.
    /// </remarks>
    internal sealed class ExternalConnectionStringLoadResult
    {
        /// <summary>
        /// 中文：创建外置连接串加载结果，并基于有效连接串构造门户主数据库 profile。
        ///
        /// English: Creates an external connection-string load result and builds the Portal primary-database profile from the effective connection string.
        /// </summary>
        /// <param name="connectionString">中文：敏感有效连接串。English: Sensitive effective connection string.</param>
        /// <param name="providerInvariantName">中文：有效 provider invariant name。English: Effective provider invariant name.</param>
        /// <param name="configRoot">中文：解析后的外置配置根目录。English: Resolved external configuration root.</param>
        /// <param name="configFile">中文：读取的环境配置文件路径。English: Path of the environment configuration file that was read.</param>
        /// <param name="source">中文：最终连接串来源说明。English: Description of the final connection-string source.</param>
        /// <param name="environmentName">中文：当前规范化环境名称。English: Current normalized environment name.</param>
        public ExternalConnectionStringLoadResult(
            string connectionString,
            string providerInvariantName,
            string configRoot,
            string configFile,
            string source,
            string environmentName)
        {
            ConnectionString = connectionString;
            ProviderInvariantName = providerInvariantName;
            ConfigRoot = configRoot;
            ConfigFile = configFile;
            Source = source;
            DatabaseProfile = new PortalDatabaseProfile(
                ExternalConnectionStringLoader.LogicalConnectionStringName,
                providerInvariantName,
                connectionString,
                environmentName,
                PortalDatabasePurpose.PrimaryPortal);
        }

        /// <summary>
        /// 中文：最终注入数据访问层的敏感连接串。
        ///
        /// English: Sensitive effective connection string injected into the data-access layer.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// 中文：当前连接串对应的 ADO.NET provider invariant name。
        ///
        /// English: ADO.NET provider invariant name associated with the effective connection string.
        /// </summary>
        public string ProviderInvariantName { get; private set; }

        /// <summary>
        /// 中文：供新数据访问代码使用的数据库 profile，其中包含敏感连接串。
        ///
        /// English: Database profile for new data-access code; it contains the sensitive connection string.
        /// </summary>
        public PortalDatabaseProfile DatabaseProfile { get; private set; }

        /// <summary>
        /// 中文：实际解析出的外置配置根目录，默认规则为 {当前进程用户目录}\Web\{项目名}。
        ///
        /// English: Resolved external configuration root. The default rule is
        /// {current-process user profile}\Web\{project name}.
        /// </summary>
        public string ConfigRoot { get; private set; }

        /// <summary>
        /// 中文：当前环境对应的 <c>connectionStrings.config</c> 完整路径。
        ///
        /// English: Full path of <c>connectionStrings.config</c> for the current environment.
        /// </summary>
        public string ConfigFile { get; private set; }

        /// <summary>
        /// 中文：最终连接串来源，可以是外置文件或指定的敏感环境变量。
        ///
        /// English: Final connection-string source, either the external file or the designated sensitive environment variable.
        /// </summary>
        public string Source { get; private set; }
    }

    /// <summary>
    /// 中文：从仓库外部加载门户主数据库连接串。
    ///
    /// English: Loads the Portal primary-database connection string from outside the repository.
    /// </summary>
    /// <remarks>
    /// 中文：真实连接串不得进入仓库，Unity 配置只保留依赖名称。启动时先读取外置文件中的逻辑连接串
    /// <c>Portal</c>，再注册为旧数据访问层仍使用的 Unity 命名实例 <c>connectionString</c>。
    ///
    /// English: Real connection strings must not enter the repository; Unity configuration retains only
    /// a dependency name. Startup reads the logical external <c>Portal</c> connection string, then
    /// registers it as the legacy Unity named instance <c>connectionString</c>.
    /// </remarks>
    internal static class ExternalConnectionStringLoader
    {
        /// <summary>
        /// 中文：Web.config 中覆盖外置配置根目录的 appSettings 键名。
        ///
        /// English: appSettings key that overrides the external configuration root.
        /// </summary>
        public const string ExternalCfgPathSettingName = "ExternalCfgPath";

        /// <summary>
        /// 中文：默认路径使用的稳定仓库/站点名，不使用 VSCode Profile 名。
        ///
        /// English: Stable repository/site name used by the default path, not a VSCode profile name.
        /// </summary>
        public const string ProjectName = "HIA-ASPNETPortal";

        /// <summary>
        /// 中文：外置文件中使用的语义化连接串名称。
        ///
        /// English: Semantic connection-string name used in the external file.
        /// </summary>
        public const string LogicalConnectionStringName = "Portal";

        /// <summary>
        /// 中文：旧 Unity 配置和数据访问构造函数仍依赖的命名实例。
        ///
        /// English: Named instance still required by legacy Unity configuration and data-access constructors.
        /// </summary>
        public const string UnityConnectionStringName = "connectionString";

        /// <summary>
        /// 中文：仅覆盖敏感连接串值、不能改变外置配置根目录的环境变量名。
        ///
        /// English: Environment-variable name that overrides only the sensitive connection-string value, not the external root.
        /// </summary>
        public const string EnvironmentVariableName = "HIA_ASPNETPORTAL_CONNSTR_PORTAL";

        /// <summary>
        /// 中文：当前唯一支持的外置 XML 文件名；JSON/YAML 兼容性保留给后续规划。
        ///
        /// English: Only supported external XML file name for now; JSON/YAML compatibility is reserved for later planning.
        /// </summary>
        public const string ConnectionStringsFileName = "connectionStrings.config";

        /// <summary>
        /// 中文：加载当前运行环境的门户主数据库连接串。
        ///
        /// English: Loads the Portal primary-database connection string for the current runtime environment.
        /// </summary>
        /// <param name="environmentName">
        /// 中文：运行环境名称，例如 <c>dev</c>、<c>test</c> 或 <c>prod</c>；空白值回退为 <c>dev</c>。
        /// English: Runtime environment name, such as <c>dev</c>, <c>test</c>, or <c>prod</c>; blank falls back to <c>dev</c>.
        /// </param>
        /// <returns>
        /// 中文：包含有效连接串、provider、文件路径和来源的加载结果。
        /// English: Load result containing the effective connection string, provider, file path, and source.
        /// </returns>
        /// <exception cref="ConfigurationErrorsException">
        /// 中文：外置文件、逻辑连接串、provider 或默认根目录不可用时引发。
        /// English: Thrown when the external file, logical connection string, provider, or default root is unavailable.
        /// </exception>
        public static ExternalConnectionStringLoadResult LoadPortalConnectionString(string environmentName)
        {
            string env = NormalizeEnvironmentName(environmentName);
            string configRoot = ResolveExternalConfigRoot();
            string configFile = Path.Combine(configRoot, env, ConnectionStringsFileName);

            // 外置文件必须存在，即使后续使用环境变量覆盖连接串值，也不能绕过文件结构校验。
            string connectionString = ReadConnectionString(configFile, LogicalConnectionStringName);
            string providerInvariantName = ReadProviderInvariantName(configFile, LogicalConnectionStringName);
            ValidatePrimaryPortalProvider(providerInvariantName, configFile);
            string environmentOverride = Environment.GetEnvironmentVariable(EnvironmentVariableName);

            if (!string.IsNullOrWhiteSpace(environmentOverride))
            {
                // 环境变量只作为敏感值覆盖来源，方便部署平台注入真实连接串。
                return new ExternalConnectionStringLoadResult(
                    environmentOverride,
                    providerInvariantName,
                    configRoot,
                    configFile,
                    "environment variable " + EnvironmentVariableName,
                    env);
            }

            return new ExternalConnectionStringLoadResult(
                connectionString,
                providerInvariantName,
                configRoot,
                configFile,
                configFile,
                env);
        }

        /// <summary>
        /// 中文：解析外置配置根目录。
        ///
        /// English: Resolves the external configuration root.
        /// </summary>
        /// <returns>
        /// 中文：经规范化的根目录；优先使用 <c>ExternalCfgPath</c>，否则使用当前进程用户目录下的默认规则。
        /// English: Normalized root; prefers <c>ExternalCfgPath</c>, otherwise uses the default rule beneath the current process user profile.
        /// </returns>
        /// <exception cref="ConfigurationErrorsException">
        /// 中文：未配置覆盖路径且当前进程用户目录不可解析时引发。
        /// English: Thrown when no override is configured and the current process user profile cannot be resolved.
        /// </exception>
        public static string ResolveExternalConfigRoot()
        {
            string configuredPath = ConfigurationManager.AppSettings[ExternalCfgPathSettingName];

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                // Web.config 显式配置优先，主要用于 IIS 或生产部署不适合用户目录时。
                return NormalizePath(configuredPath);
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrWhiteSpace(userProfile))
            {
                throw new ConfigurationErrorsException(
                    "ExternalCfgPath is not configured, and the current process user profile path cannot be resolved.");
            }

            // 默认路径遵循用户确认的规则：{当前进程用户目录}\Web\HIA-ASPNETPortal\。
            return Path.Combine(userProfile, "Web", ProjectName);
        }

        /// <summary>
        /// 中文：规范运行环境名称；空白值回退为 <c>dev</c>。
        ///
        /// English: Normalizes a runtime environment name; blank falls back to <c>dev</c>.
        /// </summary>
        private static string NormalizeEnvironmentName(string environmentName)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                return "dev";
            }

            return environmentName.Trim();
        }

        /// <summary>
        /// 中文：规范 Web.config 中配置的外置根目录。
        ///
        /// English: Normalizes an external root configured in Web.config.
        /// </summary>
        private static string NormalizePath(string configuredPath)
        {
            // 这里只展开 Web.config 中写入的路径片段，不把环境变量作为根目录选择入口。
            string expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());

            if (!Path.IsPathRooted(expandedPath))
            {
                // 相对路径按站点根目录解释，便于本地临时覆盖。
                expandedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expandedPath);
            }

            return Path.GetFullPath(expandedPath);
        }

        /// <summary>
        /// 中文：从外置 XML 文件读取指定名称的连接串。
        ///
        /// English: Reads a named connection string from the external XML file.
        /// </summary>
        private static string ReadConnectionString(string configFile, string connectionStringName)
        {
            XElement entry = ReadConnectionStringEntry(configFile, connectionStringName);
            string connectionString = (string)entry.Attribute("connectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // 空字符串通常意味着模板尚未正确改成真实配置，也按配置错误处理。
                throw new ConfigurationErrorsException(
                    "Connection string '" + connectionStringName + "' is empty in " + configFile);
            }

            return connectionString;
        }

        /// <summary>
        /// 中文：读取外置连接串条目的 provider invariant name。
        ///
        /// English: Reads the provider invariant name from an external connection-string entry.
        /// </summary>
        /// <param name="configFile">中文：外置配置文件完整路径。English: Full path of the external configuration file.</param>
        /// <param name="connectionStringName">中文：逻辑连接串名称。English: Logical connection-string name.</param>
        /// <returns>中文：非空 provider invariant name。English: A non-empty provider invariant name.</returns>
        private static string ReadProviderInvariantName(string configFile, string connectionStringName)
        {
            XElement entry = ReadConnectionStringEntry(configFile, connectionStringName);
            string providerInvariantName = (string)entry.Attribute("providerName");

            if (string.IsNullOrWhiteSpace(providerInvariantName))
            {
                throw new ConfigurationErrorsException(
                    "Connection string '" + connectionStringName + "' is missing providerName in " + configFile);
            }

            return providerInvariantName.Trim();
        }

        /// <summary>
        /// 中文：读取并校验外置 XML 中的指定连接串条目。
        ///
        /// English: Reads and validates a named connection-string entry from external XML.
        /// </summary>
        /// <param name="configFile">中文：外置配置文件完整路径。English: Full path of the external configuration file.</param>
        /// <param name="connectionStringName">中文：逻辑连接串名称。English: Logical connection-string name.</param>
        /// <returns>中文：匹配的 XML 条目。English: Matching XML entry.</returns>
        private static XElement ReadConnectionStringEntry(string configFile, string connectionStringName)
        {
            if (!File.Exists(configFile))
            {
                // 关键配置缺失时立即失败，避免应用静默连接到错误数据库。
                throw new ConfigurationErrorsException(
                    "External connection string file is missing. Expected path: " + configFile);
            }

            XDocument document = XDocument.Load(configFile);
            if (document.Root == null || !string.Equals(document.Root.Name.LocalName, "connectionStrings", StringComparison.OrdinalIgnoreCase))
            {
                // 保持和 .NET connectionStrings 结构接近，后续迁移或扩展时更容易理解。
                throw new ConfigurationErrorsException(
                    "External connection string file must use <connectionStrings> as root. File: " + configFile);
            }

            XElement entry = document.Root
                .Elements()
                .FirstOrDefault(element =>
                    string.Equals(element.Name.LocalName, "add", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string)element.Attribute("name"), connectionStringName, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                // 当前阶段只加载 Portal 这一条，缺失时提示具体名称和文件位置。
                throw new ConfigurationErrorsException(
                    "Connection string '" + connectionStringName + "' is missing in " + configFile);
            }

            return entry;
        }

        /// <summary>
        /// 中文：约束当前门户主业务数据库仍使用 SQL Server provider。
        ///
        /// English: Restricts the current Portal primary database to the SQL Server provider.
        /// </summary>
        /// <param name="providerInvariantName">中文：外置配置声明的 provider invariant。English: Provider invariant declared by external configuration.</param>
        /// <param name="configFile">中文：声明来源文件。English: Source configuration file.</param>
        private static void ValidatePrimaryPortalProvider(string providerInvariantName, string configFile)
        {
            if (!string.Equals(providerInvariantName, PortalDatabaseProviderNames.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                // P3.3 proof 使用独立 profile；正常门户仍不能误切换到未完成迁移的 provider。
                throw new ConfigurationErrorsException(
                    "The primary Portal connection currently supports only '" +
                    PortalDatabaseProviderNames.SqlServer + "'. Configured provider: '" +
                    providerInvariantName + "'. File: " + configFile);
            }
        }
    }
}
