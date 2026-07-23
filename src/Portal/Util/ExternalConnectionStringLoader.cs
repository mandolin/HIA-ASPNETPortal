using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ASPNET.StarterKit.Portal;

namespace ASPNET.StarterKit.Portal.Util
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>外置门户连接串的加载结果。</zh-CN>
    ///   <en>Result of loading the external Portal connection string.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>结果保留最终连接串、配置根目录、实际读取文件、来源和数据库 profile，供启动代码使用。 连接串属于敏感信息，调用方不得将其输出到页面、诊断日志或普通管理界面。</zh-CN>
    ///   <en>The result retains the effective connection string, configuration root, source file, source description, and database profile for startup code. The connection string is sensitive and must not be written to pages, diagnostic logs, or ordinary administration views.</en>
    /// </lang>
    /// </remarks>
    internal sealed class ExternalConnectionStringLoadResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建外置连接串加载结果，并基于有效连接串构造门户主数据库 profile。</zh-CN>
        ///   <en>Creates an external connection-string load result and builds the Portal primary-database profile from the effective connection string.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>敏感有效连接串。</zh-CN>
        ///   <en>Sensitive effective connection string.</en>
        /// </l>
        /// </param>
        /// <param name="providerInvariantName">
        /// <l>
        ///   <zh-CN>有效 provider invariant name。</zh-CN>
        ///   <en>Effective provider invariant name.</en>
        /// </l>
        /// </param>
        /// <param name="configRoot">
        /// <l>
        ///   <zh-CN>解析后的外置配置根目录。</zh-CN>
        ///   <en>Resolved external configuration root.</en>
        /// </l>
        /// </param>
        /// <param name="configFile">
        /// <l>
        ///   <zh-CN>读取的环境配置文件路径。</zh-CN>
        ///   <en>Path of the environment configuration file that was read.</en>
        /// </l>
        /// </param>
        /// <param name="source">
        /// <l>
        ///   <zh-CN>最终连接串来源说明。</zh-CN>
        ///   <en>Description of the final connection-string source.</en>
        /// </l>
        /// </param>
        /// <param name="environmentName">
        /// <l>
        ///   <zh-CN>当前规范化环境名称。</zh-CN>
        ///   <en>Current normalized environment name.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>最终注入数据访问层的敏感连接串。</zh-CN>
        ///   <en>Sensitive effective connection string injected into the data-access layer.</en>
        /// </lang>
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前连接串对应的 ADO.NET provider invariant name。</zh-CN>
        ///   <en>ADO.NET provider invariant name associated with the effective connection string.</en>
        /// </lang>
        /// </summary>
        public string ProviderInvariantName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>供新数据访问代码使用的数据库 profile，其中包含敏感连接串。</zh-CN>
        ///   <en>Database profile for new data-access code; it contains the sensitive connection string.</en>
        /// </lang>
        /// </summary>
        public PortalDatabaseProfile DatabaseProfile { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>实际解析出的外置配置根目录，默认规则为 {当前进程用户目录}\Web\{项目名}。</zh-CN>
        ///   <en>Resolved external configuration root. The default rule is {current-process user profile}\Web\{project name}.</en>
        /// </lang>
        /// </summary>
        public string ConfigRoot { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前环境对应的 <c>connectionStrings.config</c> 完整路径。</zh-CN>
        ///   <en>Full path of <c>connectionStrings.config</c> for the current environment.</en>
        /// </lang>
        /// </summary>
        public string ConfigFile { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最终连接串来源，可以是外置文件或指定的敏感环境变量。</zh-CN>
        ///   <en>Final connection-string source, either the external file or the designated sensitive environment variable.</en>
        /// </lang>
        /// </summary>
        public string Source { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>从仓库外部加载门户主数据库连接串。</zh-CN>
    ///   <en>Loads the Portal primary-database connection string from outside the repository.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>真实连接串不得进入仓库，Unity 配置只保留依赖名称。启动时先读取外置文件中的逻辑连接串 <c>Portal</c>，再注册为旧数据访问层仍使用的 Unity 命名实例 <c>connectionString</c>。</zh-CN>
    ///   <en>Real connection strings must not enter the repository; Unity configuration retains only a dependency name. Startup reads the logical external <c>Portal</c> connection string, then registers it as the legacy Unity named instance <c>connectionString</c>.</en>
    /// </lang>
    /// </remarks>
    internal static class ExternalConnectionStringLoader
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>Web.config 中覆盖外置配置根目录的 appSettings 键名。</zh-CN>
        ///   <en>appSettings key that overrides the external configuration root.</en>
        /// </lang>
        /// </summary>
        public const string ExternalCfgPathSettingName = "ExternalCfgPath";

        /// <summary>
        /// <lang>
        ///   <zh-CN>默认路径使用的稳定仓库/站点名，不使用 VSCode Profile 名。</zh-CN>
        ///   <en>Stable repository/site name used by the default path, not a VSCode profile name.</en>
        /// </lang>
        /// </summary>
        public const string ProjectName = "HIA-ASPNETPortal";

        /// <summary>
        /// <lang>
        ///   <zh-CN>外置文件中使用的语义化连接串名称。</zh-CN>
        ///   <en>Semantic connection-string name used in the external file.</en>
        /// </lang>
        /// </summary>
        public const string LogicalConnectionStringName = "Portal";

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧 Unity 配置和数据访问构造函数仍依赖的命名实例。</zh-CN>
        ///   <en>Named instance still required by legacy Unity configuration and data-access constructors.</en>
        /// </lang>
        /// </summary>
        public const string UnityConnectionStringName = "connectionString";

        /// <summary>
        /// <lang>
        ///   <zh-CN>仅覆盖敏感连接串值、不能改变外置配置根目录的环境变量名。</zh-CN>
        ///   <en>Environment-variable name that overrides only the sensitive connection-string value, not the external root.</en>
        /// </lang>
        /// </summary>
        public const string EnvironmentVariableName = "HIA_ASPNETPORTAL_CONNSTR_PORTAL";

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前唯一支持的外置 XML 文件名；JSON/YAML 兼容性保留给后续规划。</zh-CN>
        ///   <en>Only supported external XML file name for now; JSON/YAML compatibility is reserved for later planning.</en>
        /// </lang>
        /// </summary>
        public const string ConnectionStringsFileName = "connectionStrings.config";

        /// <summary>
        /// <lang>
        ///   <zh-CN>加载当前运行环境的门户主数据库连接串。</zh-CN>
        ///   <en>Loads the Portal primary-database connection string for the current runtime environment.</en>
        /// </lang>
        /// </summary>
        /// <param name="environmentName">
        /// <l>
        ///   <zh-CN>运行环境名称，例如 <c>dev</c>、<c>test</c> 或 <c>prod</c>；空白值回退为 <c>dev</c>。</zh-CN>
        ///   <en>Runtime environment name, such as <c>dev</c>, <c>test</c>, or <c>prod</c>; blank falls back to <c>dev</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>包含有效连接串、provider、文件路径和来源的加载结果。</zh-CN>
        ///   <en>Load result containing the effective connection string, provider, file path, and source.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ConfigurationErrorsException">
        /// <l>
        ///   <zh-CN>外置文件、逻辑连接串、provider 或默认根目录不可用时引发。</zh-CN>
        ///   <en>Thrown when the external file, logical connection string, provider, or default root is unavailable.</en>
        /// </l>
        /// </exception>
        public static ExternalConnectionStringLoadResult LoadPortalConnectionString(string environmentName)
        {
            string env = NormalizeEnvironmentName(environmentName);
            string configRoot = ResolveExternalConfigRoot();
            string configFile = Path.Combine(configRoot, env, ConnectionStringsFileName);

            // <lang>
            //   <zh-CN>外置文件必须存在，即使后续使用环境变量覆盖连接串值，也不能绕过文件结构校验。</zh-CN>
            //   <en>The external file must exist even when an environment variable later overrides the sensitive connection-string value; the file contract still has to be validated.</en>
            // </lang>
            string connectionString = ReadConnectionString(configFile, LogicalConnectionStringName);
            string providerInvariantName = ReadProviderInvariantName(configFile, LogicalConnectionStringName);
            ValidatePrimaryPortalProvider(providerInvariantName, configFile);
            string environmentOverride = Environment.GetEnvironmentVariable(EnvironmentVariableName);

            if (!string.IsNullOrWhiteSpace(environmentOverride))
            {
                // <lang>
                //   <zh-CN>环境变量只作为敏感值覆盖来源，方便部署平台注入真实连接串。</zh-CN>
                //   <en>The environment variable overrides only the sensitive value, allowing deployment platforms to inject the real connection string.</en>
                // </lang>
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
        /// <lang>
        ///   <zh-CN>解析外置配置根目录。</zh-CN>
        ///   <en>Resolves the external configuration root.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>经规范化的根目录；优先使用 <c>ExternalCfgPath</c>，否则使用当前进程用户目录下的默认规则。</zh-CN>
        ///   <en>Normalized root; prefers <c>ExternalCfgPath</c>, otherwise uses the default rule beneath the current process user profile.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ConfigurationErrorsException">
        /// <l>
        ///   <zh-CN>未配置覆盖路径且当前进程用户目录不可解析时引发。</zh-CN>
        ///   <en>Thrown when no override is configured and the current process user profile cannot be resolved.</en>
        /// </l>
        /// </exception>
        public static string ResolveExternalConfigRoot()
        {
            string configuredPath = ConfigurationManager.AppSettings[ExternalCfgPathSettingName];

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                // <lang>
                //   <zh-CN>Web.config 显式配置优先，主要用于 IIS 或生产部署不适合用户目录时。</zh-CN>
                //   <en>An explicit Web.config value takes priority, mainly for IIS or production deployments where the user-profile default is unsuitable.</en>
                // </lang>
                return NormalizePath(configuredPath);
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrWhiteSpace(userProfile))
            {
                throw new ConfigurationErrorsException(
                    "ExternalCfgPath is not configured, and the current process user profile path cannot be resolved.");
            }

            // <lang>
            //   <zh-CN>默认路径遵循用户确认的规则：{当前进程用户目录}\Web\HIA-ASPNETPortal\。</zh-CN>
            //   <en>The default path follows the user-confirmed rule: {current-process user profile}\Web\HIA-ASPNETPortal\.</en>
            // </lang>
            return Path.Combine(userProfile, "Web", ProjectName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范运行环境名称；空白值回退为 <c>dev</c>。</zh-CN>
        ///   <en>Normalizes a runtime environment name; blank falls back to <c>dev</c>.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>规范 Web.config 中配置的外置根目录。</zh-CN>
        ///   <en>Normalizes an external root configured in Web.config.</en>
        /// </lang>
        /// </summary>
        private static string NormalizePath(string configuredPath)
        {
            // <lang>
            //   <zh-CN>这里只展开 Web.config 中写入的路径片段，不把环境变量作为根目录选择入口。</zh-CN>
            //   <en>Only the path fragment written in Web.config is expanded here; environment variables are not a separate root-selection mechanism.</en>
            // </lang>
            string expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());

            if (!Path.IsPathRooted(expandedPath))
            {
                // <lang>
                //   <zh-CN>相对路径按站点根目录解释，便于本地临时覆盖。</zh-CN>
                //   <en>Relative paths are resolved beneath the site root so local temporary overrides remain easy to use.</en>
                // </lang>
                expandedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expandedPath);
            }

            return Path.GetFullPath(expandedPath);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从外置 XML 文件读取指定名称的连接串。</zh-CN>
        ///   <en>Reads a named connection string from the external XML file.</en>
        /// </lang>
        /// </summary>
        private static string ReadConnectionString(string configFile, string connectionStringName)
        {
            XElement entry = ReadConnectionStringEntry(configFile, connectionStringName);
            string connectionString = (string)entry.Attribute("connectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // <lang>
                //   <zh-CN>空字符串通常意味着模板尚未正确改成真实配置，也按配置错误处理。</zh-CN>
                //   <en>An empty value usually means the template was not replaced with a real configuration, so treat it as a configuration error.</en>
                // </lang>
                throw new ConfigurationErrorsException(
                    "Connection string '" + connectionStringName + "' is empty in " + configFile);
            }

            return connectionString;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取外置连接串条目的 provider invariant name。</zh-CN>
        ///   <en>Reads the provider invariant name from an external connection-string entry.</en>
        /// </lang>
        /// </summary>
        /// <param name="configFile">
        /// <l>
        ///   <zh-CN>外置配置文件完整路径。</zh-CN>
        ///   <en>Full path of the external configuration file.</en>
        /// </l>
        /// </param>
        /// <param name="connectionStringName">
        /// <l>
        ///   <zh-CN>逻辑连接串名称。</zh-CN>
        ///   <en>Logical connection-string name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>非空 provider invariant name。</zh-CN>
        ///   <en>A non-empty provider invariant name.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>读取并校验外置 XML 中的指定连接串条目。</zh-CN>
        ///   <en>Reads and validates a named connection-string entry from external XML.</en>
        /// </lang>
        /// </summary>
        /// <param name="configFile">
        /// <l>
        ///   <zh-CN>外置配置文件完整路径。</zh-CN>
        ///   <en>Full path of the external configuration file.</en>
        /// </l>
        /// </param>
        /// <param name="connectionStringName">
        /// <l>
        ///   <zh-CN>逻辑连接串名称。</zh-CN>
        ///   <en>Logical connection-string name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配的 XML 条目。</zh-CN>
        ///   <en>Matching XML entry.</en>
        /// </l>
        /// </returns>
        private static XElement ReadConnectionStringEntry(string configFile, string connectionStringName)
        {
            if (!File.Exists(configFile))
            {
                // <lang>
                //   <zh-CN>关键配置缺失时立即失败，避免应用静默连接到错误数据库。</zh-CN>
                //   <en>Fail immediately when critical configuration is missing so the application cannot silently connect to the wrong database.</en>
                // </lang>
                throw new ConfigurationErrorsException(
                    "External connection string file is missing. Expected path: " + configFile);
            }

            XDocument document = XDocument.Load(configFile);
            if (document.Root == null || !string.Equals(document.Root.Name.LocalName, "connectionStrings", StringComparison.OrdinalIgnoreCase))
            {
                // <lang>
                //   <zh-CN>保持和 .NET connectionStrings 结构接近，后续迁移或扩展时更容易理解。</zh-CN>
                //   <en>Keep the shape close to .NET connectionStrings so later migration or extension remains easy to understand.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>当前阶段只加载 Portal 这一条，缺失时提示具体名称和文件位置。</zh-CN>
                //   <en>The current stage loads only the Portal entry, so missing configuration should name both the logical key and the file path.</en>
                // </lang>
                throw new ConfigurationErrorsException(
                    "Connection string '" + connectionStringName + "' is missing in " + configFile);
            }

            return entry;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>约束当前门户主业务数据库仍使用 SQL Server provider。</zh-CN>
        ///   <en>Restricts the current Portal primary database to the SQL Server provider.</en>
        /// </lang>
        /// </summary>
        /// <param name="providerInvariantName">
        /// <l>
        ///   <zh-CN>外置配置声明的 provider invariant。</zh-CN>
        ///   <en>Provider invariant declared by external configuration.</en>
        /// </l>
        /// </param>
        /// <param name="configFile">
        /// <l>
        ///   <zh-CN>声明来源文件。</zh-CN>
        ///   <en>Source configuration file.</en>
        /// </l>
        /// </param>
        private static void ValidatePrimaryPortalProvider(string providerInvariantName, string configFile)
        {
            if (!string.Equals(providerInvariantName, PortalDatabaseProviderNames.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                // <lang>
                //   <zh-CN>P3.3 proof 使用独立 profile；正常门户仍不能误切换到未完成迁移的 provider。</zh-CN>
                //   <en>The P3.3 proof uses a separate profile; the normal Portal path must not accidentally switch to a provider whose migration is not complete.</en>
                // </lang>
                throw new ConfigurationErrorsException(
                    "The primary Portal connection currently supports only '" +
                    PortalDatabaseProviderNames.SqlServer + "'. Configured provider: '" +
                    providerInvariantName + "'. File: " + configFile);
            }
        }
    }
}
