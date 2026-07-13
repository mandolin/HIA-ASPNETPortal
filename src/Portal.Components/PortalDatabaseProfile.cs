using System;
using System.Data.Common;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 门户可识别的数据库提供程序 invariant name 常量。
    /// Stable database-provider invariant names recognized by the portal.
    /// </summary>
    /// <remarks>
    /// 常量只定义配置和目录命名的稳定标识，不表示所有提供程序已被门户完整支持。
    /// These constants define stable configuration and directory identifiers; they do not imply full portal support.
    /// </remarks>
    public static class PortalDatabaseProviderNames
    {
        /// <summary>
        /// SQL Server 的 .NET Framework ADO.NET 提供程序标识。
        /// .NET Framework ADO.NET provider invariant for SQL Server.
        /// </summary>
        public const string SqlServer = "System.Data.SqlClient";

        /// <summary>
        /// SQLite ADO.NET 提供程序标识。
        /// SQLite ADO.NET provider invariant.
        /// </summary>
        public const string Sqlite = "System.Data.SQLite";

        /// <summary>
        /// MySQL 的惯用提供程序标识预留值。
        /// Reserved conventional provider invariant for MySQL.
        /// </summary>
        public const string MySql = "MySql.Data.MySqlClient";

        /// <summary>
        /// PostgreSQL 的惯用提供程序标识预留值。
        /// Reserved conventional provider invariant for PostgreSQL.
        /// </summary>
        public const string PostgreSql = "Npgsql";
    }

    /// <summary>
    /// 描述数据库 profile 的受限用途。
    /// Describes the restricted purpose of a database profile.
    /// </summary>
    public enum PortalDatabasePurpose
    {
        /// <summary>
        /// 门户正常运行所使用的主业务数据库。
        /// Primary business database used by the normal portal runtime.
        /// </summary>
        PrimaryPortal = 0,

        /// <summary>
        /// 仅限开发或测试的提供程序能力验证数据库。
        /// Provider capability database limited to development or test verification.
        /// </summary>
        ProviderProof = 1
    }

    /// <summary>
    /// 表示已解析但不得写入日志的门户数据库配置。
    /// Represents resolved portal database configuration that must not be written to logs.
    /// </summary>
    /// <remarks>
    /// profile 同时携带 provider invariant 和连接串，使新增代码不必把连接串文本误当作数据库类型。
    /// The profile carries both the provider invariant and connection string so new code does not mistake a connection string for a database type.
    /// </remarks>
    public sealed class PortalDatabaseProfile
    {
        /// <summary>
        /// 初始化数据库 profile。
        /// Initializes a database profile.
        /// </summary>
        /// <param name="logicalName">稳定逻辑名称，例如 <c>Portal</c>。Stable logical name, such as <c>Portal</c>.</param>
        /// <param name="providerInvariantName">ADO.NET provider invariant name. ADO.NET provider invariant name.</param>
        /// <param name="connectionString">敏感连接串。Sensitive connection string.</param>
        /// <param name="environmentName">当前运行环境。Current runtime environment.</param>
        /// <param name="purpose">profile 的受限用途。Restricted profile purpose.</param>
        /// <exception cref="ArgumentException">逻辑名称、provider 或连接串为空时抛出。Thrown when the logical name, provider, or connection string is empty.</exception>
        public PortalDatabaseProfile(
            string logicalName,
            string providerInvariantName,
            string connectionString,
            string environmentName,
            PortalDatabasePurpose purpose)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                throw new ArgumentException("A database logical name is required.", nameof(logicalName));
            }

            if (string.IsNullOrWhiteSpace(providerInvariantName))
            {
                throw new ArgumentException("A database provider invariant name is required.", nameof(providerInvariantName));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A database connection string is required.", nameof(connectionString));
            }

            LogicalName = logicalName.Trim();
            ProviderInvariantName = providerInvariantName.Trim();
            ConnectionString = connectionString;
            EnvironmentName = string.IsNullOrWhiteSpace(environmentName) ? "dev" : environmentName.Trim();
            Purpose = purpose;
        }

        /// <summary>
        /// 数据库的稳定逻辑名称。
        /// Stable logical name of the database.
        /// </summary>
        public string LogicalName { get; private set; }

        /// <summary>
        /// 用于解析 ADO.NET 工厂的 provider invariant name。
        /// Provider invariant name used to resolve an ADO.NET factory.
        /// </summary>
        public string ProviderInvariantName { get; private set; }

        /// <summary>
        /// 敏感数据库连接串；调用方不得记录、展示或序列化它。
        /// Sensitive database connection string; callers must not log, display, or serialize it.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// 当前环境标识。
        /// Current environment identifier.
        /// </summary>
        public string EnvironmentName { get; private set; }

        /// <summary>
        /// profile 的受限用途。
        /// Restricted profile purpose.
        /// </summary>
        public PortalDatabasePurpose Purpose { get; private set; }

        /// <summary>
        /// 判断 profile 是否使用指定的 provider。
        /// Determines whether the profile uses a specified provider.
        /// </summary>
        /// <param name="providerInvariantName">待比较的 provider invariant。Provider invariant to compare.</param>
        /// <returns>相同则为 <c>true</c>。<c>true</c> when the invariants match.</returns>
        public bool UsesProvider(string providerInvariantName)
        {
            return !string.IsNullOrWhiteSpace(providerInvariantName) &&
                   string.Equals(ProviderInvariantName, providerInvariantName.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 按 profile 创建 ADO.NET 数据库连接。
    /// Creates ADO.NET database connections from a profile.
    /// </summary>
    public interface IPortalDbConnectionFactory
    {
        /// <summary>
        /// 创建尚未打开的数据库连接。
        /// Creates a database connection that has not yet been opened.
        /// </summary>
        /// <param name="profile">已验证的数据库 profile。Validated database profile.</param>
        /// <returns>由指定 provider 创建的关闭连接。A closed connection created by the selected provider.</returns>
        /// <exception cref="InvalidOperationException">provider 未注册或不能创建连接时抛出。Thrown when the provider is unregistered or cannot create a connection.</exception>
        DbConnection CreateConnection(PortalDatabaseProfile profile);
    }

    /// <summary>
    /// 使用 <see cref="DbProviderFactories"/> 创建连接的默认工厂。
    /// Default factory that creates connections through <see cref="DbProviderFactories"/>.
    /// </summary>
    public sealed class PortalDbConnectionFactory : IPortalDbConnectionFactory
    {
        /// <summary>
        /// 创建尚未打开的数据库连接。
        /// Creates a database connection that has not yet been opened.
        /// </summary>
        /// <param name="profile">已验证的数据库 profile。Validated database profile.</param>
        /// <returns>已配置连接串但尚未打开的连接。Connection configured with its connection string but not opened.</returns>
        /// <exception cref="ArgumentNullException">profile 为空时抛出。Thrown when profile is null.</exception>
        /// <exception cref="InvalidOperationException">provider 未注册或连接对象不可用时抛出。Thrown when the provider is unregistered or returns no usable connection.</exception>
        public DbConnection CreateConnection(PortalDatabaseProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            DbProviderFactory providerFactory = DbProviderFactories.GetFactory(profile.ProviderInvariantName);
            if (providerFactory == null)
            {
                throw new InvalidOperationException(
                    "The database provider factory is unavailable: " + profile.ProviderInvariantName);
            }

            DbConnection connection = providerFactory.CreateConnection();
            if (connection == null)
            {
                throw new InvalidOperationException(
                    "The database provider factory did not create a connection: " + profile.ProviderInvariantName);
            }

            connection.ConnectionString = profile.ConnectionString;
            return connection;
        }
    }
}
