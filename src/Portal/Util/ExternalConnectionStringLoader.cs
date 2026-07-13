using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ASPNET.StarterKit.Portal;

namespace ASPNET.StarterKit.Portal.Util
{
    /// <summary>
    /// 外置连接串加载结果。
    /// </summary>
    /// <remarks>
    /// 结果中同时保留最终连接串、配置根目录、实际读取文件和来源说明，
    /// 方便启动调试时确认当前环境到底使用了哪一路配置。
    /// </remarks>
    internal sealed class ExternalConnectionStringLoadResult
    {
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
        /// 最终用于注入数据访问层的连接串。
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// 当前连接串对应的 ADO.NET provider invariant name。
        /// ADO.NET provider invariant name associated with the current connection string.
        /// </summary>
        public string ProviderInvariantName { get; private set; }

        /// <summary>
        /// 供新数据访问代码使用的数据库 profile；其中包含敏感连接串。
        /// Database profile for new data-access code; it contains the sensitive connection string.
        /// </summary>
        public PortalDatabaseProfile DatabaseProfile { get; private set; }

        /// <summary>
        /// 外置配置根目录，例如 C:\Users\xxx\Web\HIA-ASPNETPortal。
        /// </summary>
        public string ConfigRoot { get; private set; }

        /// <summary>
        /// 当前环境对应的 connectionStrings.config 完整路径。
        /// </summary>
        public string ConfigFile { get; private set; }

        /// <summary>
        /// 连接串最终来源；可能是外置文件，也可能是指定的环境变量。
        /// </summary>
        public string Source { get; private set; }
    }

    /// <summary>
    /// 负责从仓库外部加载门户数据库连接串。
    /// </summary>
    /// <remarks>
    /// G1-P3 的约定是：真实连接串不进入仓库，Unity 配置只保留依赖名。
    /// 启动时先读取外置文件中的逻辑连接串 <c>Portal</c>，
    /// 再注册成旧数据访问层仍在使用的 Unity 命名实例 <c>connectionString</c>。
    /// </remarks>
    internal static class ExternalConnectionStringLoader
    {
        // Web.config 中用于覆盖外置配置根目录的 appSettings 键名。
        public const string ExternalCfgPathSettingName = "ExternalCfgPath";

        // 默认路径中的项目名称固定使用仓库/站点名，不使用 VSCode Profile 名。
        public const string ProjectName = "HIA-ASPNETPortal";

        // 外置文件中的语义化连接串名称。
        public const string LogicalConnectionStringName = "Portal";

        // 旧 Unity 配置和数据访问构造函数当前仍依赖这个命名实例。
        public const string UnityConnectionStringName = "connectionString";

        // 环境变量只覆盖具体敏感值，不负责改变外置配置根目录。
        public const string EnvironmentVariableName = "HIA_ASPNETPORTAL_CONNSTR_PORTAL";

        // 第一阶段只支持 XML 格式，后续再按规划扩展 JSON/YAML。
        public const string ConnectionStringsFileName = "connectionStrings.config";

        /// <summary>
        /// 加载当前运行环境的门户连接串。
        /// </summary>
        /// <param name="environmentName">运行配置环境，例如 dev、test 或 prod。</param>
        /// <returns>包含最终连接串和来源信息的加载结果。</returns>
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
        /// 解析外置配置根目录。
        /// </summary>
        /// <returns>规范化后的外置配置根目录。</returns>
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
        /// 规范运行环境名称；缺省时回落到 dev。
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
        /// 规范 Web.config 中配置的外置根目录。
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
        /// 从外置 XML 文件中读取指定名称的连接串。
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
        /// 读取外置连接串条目的 provider invariant name。
        /// Reads the provider invariant name from an external connection-string entry.
        /// </summary>
        /// <param name="configFile">外置配置文件完整路径。Full path of the external configuration file.</param>
        /// <param name="connectionStringName">逻辑连接串名称。Logical connection-string name.</param>
        /// <returns>非空 provider invariant name。A non-empty provider invariant name.</returns>
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
        /// 读取并校验外置 XML 中的指定连接串条目。
        /// Reads and validates a named connection-string entry from external XML.
        /// </summary>
        /// <param name="configFile">外置配置文件完整路径。Full path of the external configuration file.</param>
        /// <param name="connectionStringName">逻辑连接串名称。Logical connection-string name.</param>
        /// <returns>匹配的 XML 条目。Matching XML entry.</returns>
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
        /// 约束当前门户主业务数据库仍为 SQL Server。
        /// Restricts the current primary portal database to SQL Server.
        /// </summary>
        /// <param name="providerInvariantName">外置配置声明的 provider invariant。Provider invariant declared by external configuration.</param>
        /// <param name="configFile">声明来源文件。Source configuration file.</param>
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
