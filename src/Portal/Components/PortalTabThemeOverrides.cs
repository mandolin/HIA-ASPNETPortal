using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using ASPNET.StarterKit.Portal.Util;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// Tab 主题覆盖读取结果。
    /// Tab theme-override read result.
    /// </summary>
    public sealed class PortalTabThemeOverrideReadResult
    {
        internal PortalTabThemeOverrideReadResult(bool isAvailable, bool isFound, string themeName)
        {
            IsAvailable = isAvailable;
            IsFound = isFound;
            ThemeName = themeName ?? string.Empty;
        }

        /// <summary>
        /// 覆盖表是否已部署并可读取。
        /// Whether the override table is deployed and readable.
        /// </summary>
        public bool IsAvailable { get; private set; }

        /// <summary>
        /// 当前 Tab 是否有覆盖值。
        /// Whether the current tab has an override.
        /// </summary>
        public bool IsFound { get; private set; }

        /// <summary>
        /// 覆盖主题名。
        /// Overridden theme name.
        /// </summary>
        public string ThemeName { get; private set; }
    }

    /// <summary>
    /// Tab 主题覆盖写入结果。
    /// Tab theme-override write result.
    /// </summary>
    public sealed class PortalTabThemeOverrideWriteResult
    {
        internal PortalTabThemeOverrideWriteResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 操作是否成功。
        /// Whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// 可安全展示给管理员的操作说明。
        /// Operation message safe to show to an administrator.
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Tab 主题覆盖的受限存储。
    /// Restricted storage for tab theme overrides.
    /// </summary>
    /// <remarks>
        /// 覆盖值只保存已验证的部署主题名。表缺失或读取失败时解析器回退全局主题，避免旧数据库阻断门户页面。
        /// 此存储不负责调用方授权或运营审计；`ThemeSettings` 在调用前要求管理员并在成功后记录审计，任何新增调用点
        /// 必须采用同等保护。
        /// Override values store validated deployed theme names only. When the table is missing or unreadable, the
        /// resolver falls back to the global theme so a legacy database never blocks portal pages. This store does not
        /// enforce caller authorization or operations audit; `ThemeSettings` requires an administrator before calling
        /// and records an audit after success, and every new call site must use equivalent protection.
    /// </remarks>
    public static class PortalTabThemeOverrides
    {
        private const string TableName = "PortalCfg_TabThemeOverrides";

        /// <summary>
        /// 读取一个 Tab 的主题覆盖值。
        /// Reads the theme override for one tab.
        /// </summary>
        /// <param name="tabId">门户 Tab 标识。Portal tab identifier.</param>
        /// <param name="context">用于受限诊断的当前 HTTP 上下文。Current HTTP context for restricted diagnostics.</param>
        /// <returns>表可用状态、命中状态和主题名。Table availability, match state, and theme name.</returns>
        public static PortalTabThemeOverrideReadResult Read(int tabId, HttpContext context = null)
        {
            if (tabId <= 0)
            {
                return new PortalTabThemeOverrideReadResult(true, false, string.Empty);
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalTabThemeOverrideReadResult(false, false, string.Empty);
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection))
                    {
                        return new PortalTabThemeOverrideReadResult(false, false, string.Empty);
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT [ThemeName]
FROM [dbo].[PortalCfg_TabThemeOverrides]
WHERE [TabId] = @TabId;";
                        command.Parameters.Add("@TabId", SqlDbType.Int).Value = tabId;
                        object value = command.ExecuteScalar();
                        return value == null || value == DBNull.Value
                            ? new PortalTabThemeOverrideReadResult(true, false, string.Empty)
                            : new PortalTabThemeOverrideReadResult(true, true, Convert.ToString(value));
                    }
                }
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error("Theme.TabOverride.Read", "Reading a tab theme override failed.", exception, context);
                return new PortalTabThemeOverrideReadResult(false, false, string.Empty);
            }
        }

        /// <summary>
        /// 保存一个 Tab 的主题覆盖值。
        /// Saves the theme override for one tab.
        /// </summary>
        /// <param name="tabId">门户 Tab 标识。Portal tab identifier.</param>
        /// <param name="themeName">已部署主题名。Deployed theme name.</param>
        /// <param name="context">当前 HTTP 上下文，用于操作人和诊断。Current HTTP context for actor and diagnostics.</param>
        /// <returns>写入结果。Write result.</returns>
        /// <remarks>
        /// 仅在写入前验证当前部署的主题包，不会创建主题目录或变更 manifest。成功结果不代表调用方已经做过授权或审计。
        /// Validates the currently deployed theme package before writing only; it does not create a theme directory or
        /// change a manifest. A successful result does not mean the caller has performed authorization or audit.
        /// </remarks>
        public static PortalTabThemeOverrideWriteResult Save(int tabId, string themeName, HttpContext context = null)
        {
            PortalThemePackage package;
            string validationReason;
            if (tabId <= 0 || !PortalThemeCatalog.TryGetTrustedPackage(themeName, out package, out validationReason))
            {
                return new PortalTabThemeOverrideWriteResult(false, "Select a validated deployed theme.");
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalTabThemeOverrideWriteResult(false, "The runtime settings database is unavailable.");
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection))
                    {
                        return new PortalTabThemeOverrideWriteResult(false, "Run the tab-theme migration before saving an override.");
                    }

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        bool exists = ExistsForUpdate(connection, transaction, tabId);
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = exists
                                ? @"
UPDATE [dbo].[PortalCfg_TabThemeOverrides]
SET [ThemeName] = @ThemeName,
    [UpdatedBy] = @UpdatedBy,
    [UpdatedUtc] = @UpdatedUtc
WHERE [TabId] = @TabId;"
                                : @"
INSERT INTO [dbo].[PortalCfg_TabThemeOverrides]
    ([TabId], [ThemeName], [UpdatedBy], [UpdatedUtc])
VALUES
    (@TabId, @ThemeName, @UpdatedBy, @UpdatedUtc);";
                            command.Parameters.Add("@TabId", SqlDbType.Int).Value = tabId;
                            AddTextParameter(command, "@ThemeName", 64, package.Name, string.Empty);
                            AddTextParameter(command, "@UpdatedBy", 100, GetActorUserName(context), "(anonymous)");
                            command.Parameters.Add("@UpdatedUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                return new PortalTabThemeOverrideWriteResult(true, "The tab theme override was saved.");
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error("Theme.TabOverride.Save", "Saving a tab theme override failed.", exception, context);
                return new PortalTabThemeOverrideWriteResult(false, "The tab theme override could not be saved. Check diagnostics for the event id.");
            }
        }

        /// <summary>
        /// 删除一个 Tab 覆盖值，使其回退到全局主题。
        /// Deletes one tab override so it falls back to the global theme.
        /// </summary>
        /// <param name="tabId">门户 Tab 标识。Portal tab identifier.</param>
        /// <param name="context">当前 HTTP 上下文，用于诊断。Current HTTP context for diagnostics.</param>
        /// <returns>删除结果。Deletion result.</returns>
        /// <remarks>
        /// 删除只移除数据库覆盖值；后续请求按主题解析器的全局设置、appSettings 或 Default 回退，且本方法不自行授权或审计。
        /// Deletion removes only the database override. Later requests fall back through the resolver's global setting,
        /// appSettings, or Default, and this method performs neither authorization nor audit itself.
        /// </remarks>
        public static PortalTabThemeOverrideWriteResult Delete(int tabId, HttpContext context = null)
        {
            if (tabId <= 0)
            {
                return new PortalTabThemeOverrideWriteResult(false, "Select a portal tab before clearing its override.");
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalTabThemeOverrideWriteResult(false, "The runtime settings database is unavailable.");
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection))
                    {
                        return new PortalTabThemeOverrideWriteResult(false, "Run the tab-theme migration before clearing an override.");
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
DELETE FROM [dbo].[PortalCfg_TabThemeOverrides]
WHERE [TabId] = @TabId;";
                        command.Parameters.Add("@TabId", SqlDbType.Int).Value = tabId;
                        command.ExecuteNonQuery();
                    }
                }

                return new PortalTabThemeOverrideWriteResult(true, "The tab theme override was removed.");
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error("Theme.TabOverride.Delete", "Deleting a tab theme override failed.", exception, context);
                return new PortalTabThemeOverrideWriteResult(false, "The tab theme override could not be removed. Check diagnostics for the event id.");
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

        private static bool ExistsForUpdate(SqlConnection connection, SqlTransaction transaction, int tabId)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM [dbo].[PortalCfg_TabThemeOverrides] WITH (UPDLOCK, HOLDLOCK)
    WHERE [TabId] = @TabId
) THEN 1 ELSE 0 END;";
                command.Parameters.Add("@TabId", SqlDbType.Int).Value = tabId;
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
    }
}
