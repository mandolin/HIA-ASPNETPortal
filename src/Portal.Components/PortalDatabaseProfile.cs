using System;
using System.Data.Common;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    /// <zh-CN>定义门户可识别的稳定数据库提供程序 invariant 名称。</zh-CN>
    /// <en>Defines stable database-provider invariant names recognized by the portal.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    /// <zh-CN>常量只提供配置和目录中的稳定标识；是否注册、可创建连接或已获运行时支持仍由配置与工厂解析决定。</zh-CN>
    /// <en>Constants provide stable identifiers only for configuration and catalogues; registration, connection creation, and runtime support remain determined by configuration and factory resolution.</en>
    /// </lang>
    /// </remarks>
    public static class PortalDatabaseProviderNames
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>SQL Server 的 .NET Framework ADO.NET provider invariant 名称。</zh-CN>
        /// <en>The .NET Framework ADO.NET provider invariant name for SQL Server.</en>
        /// </lang>
        /// </summary>
        public const string SqlServer = "System.Data.SqlClient";

        /// <summary>
        /// <lang>
        /// <zh-CN>SQLite ADO.NET provider invariant 名称。</zh-CN>
        /// <en>The SQLite ADO.NET provider invariant name.</en>
        /// </lang>
        /// </summary>
        public const string Sqlite = "System.Data.SQLite";

        /// <summary>
        /// <lang>
        /// <zh-CN>为 MySQL 保留的惯用 provider invariant 名称，不保证当前部署已注册对应工厂。</zh-CN>
        /// <en>The conventional provider invariant name reserved for MySQL; it does not guarantee that the current deployment registered the corresponding factory.</en>
        /// </lang>
        /// </summary>
        public const string MySql = "MySql.Data.MySqlClient";

        /// <summary>
        /// <lang>
        /// <zh-CN>为 PostgreSQL 保留的惯用 provider invariant 名称，不保证当前部署已注册对应工厂。</zh-CN>
        /// <en>The conventional provider invariant name reserved for PostgreSQL; it does not guarantee that the current deployment registered the corresponding factory.</en>
        /// </lang>
        /// </summary>
        public const string PostgreSql = "Npgsql";
    }

    /// <summary>
    /// <lang>
    /// <zh-CN>描述数据库 profile 的受限用途。</zh-CN>
    /// <en>Describes the restricted purpose of a database profile.</en>
    /// </lang>
    /// </summary>
    public enum PortalDatabasePurpose
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>门户正常运行使用的主业务数据库。</zh-CN>
        /// <en>The primary business database used by normal portal runtime.</en>
        /// </lang>
        /// </summary>
        PrimaryPortal = 0,

        /// <summary>
        /// <lang>
        /// <zh-CN>仅用于开发或测试中 provider 能力验证的数据库；枚举值本身不授予连接或发布权限。</zh-CN>
        /// <en>A database used only for provider-capability verification in development or testing; the enum value itself grants no connection or publishing permission.</en>
        /// </lang>
        /// </summary>
        ProviderProof = 1
    }

    /// <summary>
    /// <lang>
    /// <zh-CN>表示已解析的门户数据库 profile，其中连接串属于不得记录、展示或序列化的敏感值。</zh-CN>
    /// <en>Represents a resolved portal database profile whose connection string is sensitive and must not be logged, displayed, or serialized.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    /// <zh-CN>profile 同时携带 provider invariant 和连接串，避免调用方把连接串文本误当作数据库类型；本类型验证存在性和规范化，不验证 provider 是否已注册或连接是否可用。</zh-CN>
    /// <en>The profile carries both provider invariant and connection string so callers do not mistake connection-string text for a database type; this type validates presence and normalization, not provider registration or connection availability.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalDatabaseProfile
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>初始化数据库 profile，并规范化逻辑名称、provider 和环境名称。</zh-CN>
        /// <en>Initializes a database profile and normalizes its logical name, provider, and environment name.</en>
        /// </lang>
        /// </summary>
        /// <param name="logicalName"><lang><zh-CN>稳定逻辑名称，例如 <c>Portal</c>；空白值会被拒绝。</zh-CN><en>Stable logical name, such as <c>Portal</c>; blank values are rejected.</en></lang></param>
        /// <param name="providerInvariantName"><lang><zh-CN>ADO.NET provider invariant 名称；空白值会被拒绝。</zh-CN><en>ADO.NET provider invariant name; blank values are rejected.</en></lang></param>
        /// <param name="connectionString"><lang><zh-CN>敏感连接串；只验证非空并原样保存，不记录或规范化其内容。</zh-CN><en>Sensitive connection string; only non-emptiness is validated and content is retained unchanged, without logging or normalization.</en></lang></param>
        /// <param name="environmentName"><lang><zh-CN>当前运行环境；空白值回退为 <c>dev</c>。</zh-CN><en>Current runtime environment; blank values fall back to <c>dev</c>.</en></lang></param>
        /// <param name="purpose"><lang><zh-CN>profile 的受限用途；调用方负责按该用途执行环境与访问策略。</zh-CN><en>Restricted profile purpose; callers enforce environment and access policy for that purpose.</en></lang></param>
        /// <exception cref="ArgumentException"><lang><zh-CN>逻辑名称、provider invariant 或连接串为空白时抛出。</zh-CN><en>Thrown when logical name, provider invariant, or connection string is blank.</en></lang></exception>
        public PortalDatabaseProfile(
            string logicalName,
            string providerInvariantName,
            string connectionString,
            string environmentName,
            PortalDatabasePurpose purpose)
        {
            // <lang>
            //   <zh-CN>在保存或使用 profile 前拒绝空白逻辑名称，确保下游配置键和诊断引用不会以未规范化的空标识继续执行。</zh-CN>
            //   <en>Reject a blank logical name before storing or using the profile so downstream configuration keys and diagnostic references cannot proceed with an unnormalized empty identifier.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                throw new ArgumentException("A database logical name is required.", nameof(logicalName));
            }

            // <lang>
            //   <zh-CN>要求非空白 provider invariant，但不在值对象构造阶段解析或注册工厂；部署依赖由连接工厂在实际创建时处理。</zh-CN>
            //   <en>Require a non-blank provider invariant without resolving or registering a factory during value-object construction; the connection factory handles deployment dependencies when a connection is actually created.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(providerInvariantName))
            {
                throw new ArgumentException("A database provider invariant name is required.", nameof(providerInvariantName));
            }

            // <lang>
            //   <zh-CN>连接串是敏感但必需的运行输入；只拒绝空白值，不在异常、日志或注释中回显内容。</zh-CN>
            //   <en>The connection string is sensitive but required runtime input; reject only a blank value and do not echo content in exceptions, logs, or comments.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A database connection string is required.", nameof(connectionString));
            }

            // <lang>
            //   <zh-CN>修剪稳定逻辑名称和 provider invariant 的外围空白以保持配置比较一致；连接串刻意原样保留，避免改变 provider 特定的值语义。</zh-CN>
            //   <en>Trim surrounding whitespace from stable logical name and provider invariant for consistent configuration comparison; deliberately retain the connection string unchanged to avoid altering provider-specific value semantics.</en>
            // </lang>
            LogicalName = logicalName.Trim();
            ProviderInvariantName = providerInvariantName.Trim();
            ConnectionString = connectionString;

            // <lang>
            //   <zh-CN>环境名空白时使用既有 dev 回退，非空白值只修剪外围空白；该字段描述来源环境，不自行选择配置或授权连接。</zh-CN>
            //   <en>Use the established dev fallback for a blank environment name and trim only surrounding whitespace from a supplied value; this field describes source environment and does not itself choose configuration or authorize a connection.</en>
            // </lang>
            EnvironmentName = string.IsNullOrWhiteSpace(environmentName) ? "dev" : environmentName.Trim();

            // <lang>
            //   <zh-CN>保留调用方提供的受限用途枚举，供上层策略解释；本值对象不以未知枚举值隐式更改访问范围或连接行为。</zh-CN>
            //   <en>Retain the caller-supplied restricted-purpose enum for upper-layer policy interpretation; this value object does not implicitly alter access scope or connection behavior for an unknown enum value.</en>
            // </lang>
            Purpose = purpose;
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>已修剪外围空白的数据库稳定逻辑名称。</zh-CN>
        /// <en>Stable database logical name with surrounding whitespace trimmed.</en>
        /// </lang>
        /// </summary>
        public string LogicalName { get; private set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>用于解析 ADO.NET 工厂的已修剪 provider invariant 名称。</zh-CN>
        /// <en>Trimmed provider invariant name used to resolve an ADO.NET factory.</en>
        /// </lang>
        /// </summary>
        public string ProviderInvariantName { get; private set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>敏感数据库连接串；调用方不得记录、展示或序列化它，且构造器不会改写其内容。</zh-CN>
        /// <en>Sensitive database connection string that callers must not log, display, or serialize; the constructor does not rewrite its content.</en>
        /// </lang>
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>当前环境标识；未提供时为 <c>dev</c>，不等同于连接授权。</zh-CN>
        /// <en>Current environment identifier; it is <c>dev</c> when omitted and is not connection authorization.</en>
        /// </lang>
        /// </summary>
        public string EnvironmentName { get; private set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>profile 的受限用途，供调用方执行环境与访问策略。</zh-CN>
        /// <en>Restricted profile purpose for callers to enforce environment and access policy.</en>
        /// </lang>
        /// </summary>
        public PortalDatabasePurpose Purpose { get; private set; }

        /// <summary>
        /// <lang>
        /// <zh-CN>按序号不区分大小写规则判断 profile 是否使用指定 provider invariant。</zh-CN>
        /// <en>Determines whether the profile uses a specified provider invariant with ordinal case-insensitive comparison.</en>
        /// </lang>
        /// </summary>
        /// <param name="providerInvariantName"><lang><zh-CN>待比较的 provider invariant；null、空或纯空白值返回 <c>false</c>。</zh-CN><en>Provider invariant to compare; null, empty, or whitespace returns <c>false</c>.</en></lang></param>
        /// <returns><lang><zh-CN>修剪后的输入与已保存 invariant 按序号不区分大小写相同时为 <c>true</c>。</zh-CN><en><c>true</c> when trimmed input equals the stored invariant using ordinal case-insensitive comparison.</en></lang></returns>
        public bool UsesProvider(string providerInvariantName)
        {
            // <lang>
            //   <zh-CN>空白查询值短路为 false，避免把缺失 provider 误当作任何已配置 profile；非空白输入只修剪外围空白。</zh-CN>
            //   <en>Short-circuit a blank query to false so a missing provider is not mistaken for any configured profile; non-blank input has only surrounding whitespace trimmed.</en>
            // </lang>
            return !string.IsNullOrWhiteSpace(providerInvariantName) &&
                   string.Equals(ProviderInvariantName, providerInvariantName.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// <lang>
    /// <zh-CN>按已验证 profile 创建 ADO.NET 数据库连接。</zh-CN>
    /// <en>Creates ADO.NET database connections from a validated profile.</en>
    /// </lang>
    /// </summary>
    public interface IPortalDbConnectionFactory
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>创建尚未打开的数据库连接；调用方负责在使用后释放连接。</zh-CN>
        /// <en>Creates a database connection that has not yet been opened; callers dispose the connection after use.</en>
        /// </lang>
        /// </summary>
        /// <param name="profile"><lang><zh-CN>已验证的数据库 profile，提供 provider invariant 和敏感连接串。</zh-CN><en>Validated database profile supplying provider invariant and sensitive connection string.</en></lang></param>
        /// <returns><lang><zh-CN>由指定 provider 创建、已配置连接串但仍关闭的连接。</zh-CN><en>Connection created by the selected provider, configured with its connection string, and still closed.</en></lang></returns>
        /// <exception cref="ArgumentNullException"><lang><zh-CN>profile 为 null 时由实现抛出。</zh-CN><en>Thrown by implementations when profile is null.</en></lang></exception>
        /// <exception cref="ArgumentException"><lang><zh-CN>provider invariant 未注册时由 provider 工厂解析抛出。</zh-CN><en>Thrown by provider-factory resolution when the provider invariant is unregistered.</en></lang></exception>
        /// <exception cref="InvalidOperationException"><lang><zh-CN>已解析工厂或其 CreateConnection 未提供可用连接时抛出。</zh-CN><en>Thrown when a resolved factory or its CreateConnection method does not provide a usable connection.</en></lang></exception>
        DbConnection CreateConnection(PortalDatabaseProfile profile);
    }

    /// <summary>
    /// <lang>
    /// <zh-CN>通过 <see cref="DbProviderFactories"/> 创建关闭连接的默认工厂。</zh-CN>
    /// <en>Default factory that creates closed connections through <see cref="DbProviderFactories"/>.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalDbConnectionFactory : IPortalDbConnectionFactory
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>解析 profile 的 provider 工厂，创建并配置尚未打开的连接。</zh-CN>
        /// <en>Resolves the profile provider factory, creates, and configures a connection that remains unopened.</en>
        /// </lang>
        /// </summary>
        /// <param name="profile"><lang><zh-CN>已验证的数据库 profile，不能为空。</zh-CN><en>Validated database profile, which cannot be null.</en></lang></param>
        /// <returns><lang><zh-CN>已配置敏感连接串但仍关闭的连接；调用方负责释放且决定是否打开。</zh-CN><en>Connection configured with its sensitive connection string and still closed; callers dispose it and decide whether to open it.</en></lang></returns>
        /// <exception cref="ArgumentNullException"><lang><zh-CN>profile 为 null 时抛出。</zh-CN><en>Thrown when profile is null.</en></lang></exception>
        /// <exception cref="ArgumentException"><lang><zh-CN>provider invariant 未注册时由 DbProviderFactories.GetFactory 抛出。</zh-CN><en>Thrown by DbProviderFactories.GetFactory when the provider invariant is unregistered.</en></lang></exception>
        /// <exception cref="InvalidOperationException"><lang><zh-CN>工厂不可用，或 CreateConnection 返回 null 时抛出。</zh-CN><en>Thrown when the factory is unavailable or CreateConnection returns null.</en></lang></exception>
        public DbConnection CreateConnection(PortalDatabaseProfile profile)
        {
            // <lang>
            //   <zh-CN>在读取 profile 字段前拒绝 null，避免把调用方缺失配置收敛为不透明的空引用异常。</zh-CN>
            //   <en>Reject null before reading profile fields so missing caller configuration is not collapsed into an opaque null-reference failure.</en>
            // </lang>
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            // <lang>
            //   <zh-CN>按已验证 invariant 解析已注册的 ADO.NET 工厂；解析异常保持可见，不包装或记录敏感连接串。</zh-CN>
            //   <en>Resolve the registered ADO.NET factory by validated invariant; resolution exceptions remain visible and do not wrap or log the sensitive connection string.</en>
            // </lang>
            DbProviderFactory providerFactory = DbProviderFactories.GetFactory(profile.ProviderInvariantName);

            // <lang>
            //   <zh-CN>保留工厂返回 null 的防御性回退，确保调用方得到稳定的配置/运行时失败，而非在后续成员调用时发生空引用。</zh-CN>
            //   <en>Retain the defensive fallback for a null factory so callers receive a stable configuration/runtime failure rather than a later null reference during member access.</en>
            // </lang>
            if (providerFactory == null)
            {
                throw new InvalidOperationException(
                    "The database provider factory is unavailable: " + profile.ProviderInvariantName);
            }

            // <lang>
            //   <zh-CN>仅请求 provider 创建连接对象，不在工厂内调用 Open；网络、凭据验证和事务生命周期仍由调用方控制。</zh-CN>
            //   <en>Ask the provider only to create a connection object and do not call Open in this factory; networking, credential validation, and transaction lifetime remain caller-controlled.</en>
            // </lang>
            DbConnection connection = providerFactory.CreateConnection();

            // <lang>
            //   <zh-CN>provider 未返回连接对象时以稳定异常失败，避免在连接串赋值时泄露实现相关的空引用细节。</zh-CN>
            //   <en>Fail with a stable exception when the provider returns no connection object, avoiding implementation-specific null-reference detail during connection-string assignment.</en>
            // </lang>
            if (connection == null)
            {
                throw new InvalidOperationException(
                    "The database provider factory did not create a connection: " + profile.ProviderInvariantName);
            }

            // <lang>
            //   <zh-CN>仅在可用连接对象上赋入敏感连接串并立即返回关闭连接；不输出连接串，且连接的打开、使用和释放所有权属于调用方。</zh-CN>
            //   <en>Assign the sensitive connection string only to a usable connection object and immediately return the closed connection; do not output the string, and callers own opening, using, and disposing the connection.</en>
            // </lang>
            connection.ConnectionString = profile.ConnectionString;
            return connection;
        }
    }
}
