using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using ASPNET.StarterKit.Portal.Util;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 已部署模块包的运行状态。
    /// Runtime state of a deployed module package.
    /// </summary>
    public sealed class PortalModulePackageState
    {
        internal PortalModulePackageState(
            string packageId,
            bool isEnabled,
            bool isConfigured,
            DateTime updatedUtc,
            string updatedBy,
            string note)
        {
            PackageId = packageId ?? string.Empty;
            IsEnabled = isEnabled;
            IsConfigured = isConfigured;
            UpdatedUtc = updatedUtc;
            UpdatedBy = updatedBy ?? string.Empty;
            Note = note ?? string.Empty;
        }

        /// <summary>
        /// 与部署 manifest 对应的稳定包标识。
        /// Stable package identifier matching the deployment manifest.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// 包是否允许参与当前请求的模块加载。
        /// Whether the package may participate in module loading for the current request.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// 数据库中是否存在显式状态覆盖。
        /// Whether an explicit state override exists in the database.
        /// </summary>
        public bool IsConfigured { get; private set; }

        /// <summary>
        /// 最近状态更新的 UTC 时间。
        /// UTC time of the latest state update.
        /// </summary>
        public DateTime UpdatedUtc { get; private set; }

        /// <summary>
        /// 最近更新的操作人。
        /// Actor that performed the latest update.
        /// </summary>
        public string UpdatedBy { get; private set; }

        /// <summary>
        /// 可选的非敏感状态备注。
        /// Optional non-sensitive state note.
        /// </summary>
        public string Note { get; private set; }
    }

    /// <summary>
    /// 模块包状态读取结果。
    /// Module-package state read result.
    /// </summary>
    public sealed class PortalModulePackageStateReadResult
    {
        internal PortalModulePackageStateReadResult(bool isAvailable, PortalModulePackageState state)
        {
            IsAvailable = isAvailable;
            State = state;
        }

        /// <summary>
        /// 状态表是否已部署并可读取。
        /// Whether the state table is deployed and readable.
        /// </summary>
        public bool IsAvailable { get; private set; }

        /// <summary>
        /// 已读取状态；不可用时为 null。
        /// Read state; null when the table is unavailable.
        /// </summary>
        public PortalModulePackageState State { get; private set; }
    }

    /// <summary>
    /// 模块包状态写入结果。
    /// Module-package state write result.
    /// </summary>
    public sealed class PortalModulePackageStateWriteResult
    {
        internal PortalModulePackageStateWriteResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 写入是否成功。
        /// Whether the write succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// 可安全展示给管理员的结果说明。
        /// Result message safe to show to an administrator.
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// 已部署模块包的受限启用状态存储。
    /// Restricted enabled-state store for deployed module packages.
    /// </summary>
    /// <remarks>
    /// 本存储不写入模块文件，也不注册未知包。状态表不存在或读取失败时，已验证包会保持默认启用，
    /// 以避免未迁移旧库阻断门户；后台写入则会明确失败并提示执行迁移。
    /// This store never writes module files or registers unknown packages. When its table is missing or unreadable,
    /// validated packages remain enabled by default so an unmigrated legacy database cannot block the portal; admin
    /// writes fail explicitly and request the migration.
    /// </remarks>
    public static class PortalModulePackageStates
    {
        private const string TableName = "PortalCfg_ModulePackageStates";

        /// <summary>
        /// 读取一个模块包的当前状态。
        /// Reads the current state of one module package.
        /// </summary>
        /// <param name="packageId">已验证部署包的稳定标识。Stable identifier of a validated deployment package.</param>
        /// <param name="context">用于受限诊断的当前 HTTP 上下文。Current HTTP context for restricted diagnostics.</param>
        /// <returns>状态表可用性与默认或显式状态。Table availability and the default or explicit state.</returns>
        public static PortalModulePackageStateReadResult Read(string packageId, HttpContext context = null)
        {
            if (!PortalModuleCatalog.IsValidPackageId(packageId))
            {
                return new PortalModulePackageStateReadResult(false, null);
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalModulePackageStateReadResult(false, null);
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection))
                    {
                        return new PortalModulePackageStateReadResult(false, null);
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT [IsEnabled], [UpdatedUtc], [UpdatedBy], [Note]
FROM [dbo].[PortalCfg_ModulePackageStates]
WHERE [PackageId] = @PackageId;";
                        AddTextParameter(command, "@PackageId", 100, packageId, string.Empty);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return new PortalModulePackageStateReadResult(
                                    true,
                                    new PortalModulePackageState(packageId, true, false, DateTime.MinValue, string.Empty, string.Empty));
                            }

                            return new PortalModulePackageStateReadResult(
                                true,
                                new PortalModulePackageState(
                                    packageId,
                                    reader.GetBoolean(0),
                                    true,
                                    reader.GetDateTime(1),
                                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error("ModulePackageState.Read", "Reading a module package state failed.", exception, context);
                return new PortalModulePackageStateReadResult(false, null);
            }
        }

        /// <summary>
        /// 设置一个已验证模块包的启用状态。
        /// Sets the enabled state of a validated module package.
        /// </summary>
        /// <param name="packageId">已验证部署包的稳定标识。Stable identifier of a validated deployment package.</param>
        /// <param name="isEnabled">是否允许该包加载模块。Whether the package may load modules.</param>
        /// <param name="note">不含敏感信息的管理员备注。Administrator note without sensitive information.</param>
        /// <param name="context">当前 HTTP 上下文，用于操作人和诊断。Current HTTP context for actor and diagnostics.</param>
        /// <returns>写入结果。Write result.</returns>
        public static PortalModulePackageStateWriteResult Save(
            string packageId,
            bool isEnabled,
            string note,
            HttpContext context = null)
        {
            if (!PortalModuleCatalog.IsValidPackageId(packageId))
            {
                return new PortalModulePackageStateWriteResult(false, "The module package identifier is invalid.");
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalModulePackageStateWriteResult(false, "The module package state database is unavailable.");
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection))
                    {
                        return new PortalModulePackageStateWriteResult(false, "Run the module-package migration before changing package state.");
                    }

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        bool exists = ExistsForUpdate(connection, transaction, packageId);
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = exists
                                ? @"
UPDATE [dbo].[PortalCfg_ModulePackageStates]
SET [IsEnabled] = @IsEnabled,
    [Note] = @Note,
    [UpdatedBy] = @UpdatedBy,
    [UpdatedUtc] = @UpdatedUtc
WHERE [PackageId] = @PackageId;"
                                : @"
INSERT INTO [dbo].[PortalCfg_ModulePackageStates]
    ([PackageId], [IsEnabled], [Note], [UpdatedBy], [UpdatedUtc])
VALUES
    (@PackageId, @IsEnabled, @Note, @UpdatedBy, @UpdatedUtc);";
                            AddTextParameter(command, "@PackageId", 100, packageId, string.Empty);
                            command.Parameters.Add("@IsEnabled", SqlDbType.Bit).Value = isEnabled;
                            AddNullableTextParameter(command, "@Note", 500, note);
                            AddTextParameter(command, "@UpdatedBy", 100, GetActorUserName(context), "(anonymous)");
                            command.Parameters.Add("@UpdatedUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                return new PortalModulePackageStateWriteResult(
                    true,
                    isEnabled ? "The module package was enabled." : "The module package was disabled.");
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error("ModulePackageState.Save", "Saving a module package state failed.", exception, context);
                return new PortalModulePackageStateWriteResult(false, "The module package state could not be saved. Check diagnostics for the event id.");
            }
        }

        private static SqlConnection CreateConnection()
        {
            if (Global.Container == null)
            {
                return null;
            }

            string connectionString = Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);
            return string.IsNullOrWhiteSpace(connectionString) ? null : new SqlConnection(connectionString);
        }

        private static bool IsTableAvailable(SqlConnection connection)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[" + TableName + "]', N'U') IS NULL THEN 0 ELSE 1 END;";
                object value = command.ExecuteScalar();
                return value != null && Convert.ToInt32(value) == 1;
            }
        }

        private static bool ExistsForUpdate(SqlConnection connection, SqlTransaction transaction, string packageId)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM [dbo].[PortalCfg_ModulePackageStates] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PackageId] = @PackageId
) THEN 1 ELSE 0 END;";
                AddTextParameter(command, "@PackageId", 100, packageId, string.Empty);
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        private static string GetActorUserName(HttpContext context)
        {
            HttpContext current = context ?? HttpContext.Current;
            if (current == null || current.User == null || current.User.Identity == null ||
                !current.User.Identity.IsAuthenticated)
            {
                return "(anonymous)";
            }

            return current.User.Identity.Name;
        }

        private static void AddTextParameter(
            SqlCommand command,
            string parameterName,
            int size,
            string value,
            string fallback)
        {
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        private static void AddNullableTextParameter(SqlCommand command, string parameterName, int size, string value)
        {
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? (object)DBNull.Value : sanitized;
        }
    }
}
