using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using ASPNET.StarterKit.Portal.Util;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>Tab 主题覆盖读取结果。</zh-CN>
    ///   <en>Tab theme-override read result.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalTabThemeOverrideReadResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 Tab 主题覆盖读取结果。</zh-CN>
        ///   <en>Creates a tab theme-override read result.</en>
        /// </lang>
        /// </summary>
        internal PortalTabThemeOverrideReadResult(bool isAvailable, bool isFound, string themeName)
        {
            // <lang>
            //   <zh-CN>保存覆盖表可读性；不可用时调用方必须回退全局主题而非阻断页面。</zh-CN>
            //   <en>Store override-table availability; when unavailable, callers must fall back to the global theme instead of blocking the page.</en>
            // </lang>
            IsAvailable = isAvailable;

            // <lang>
            //   <zh-CN>保存当前 Tab 是否存在覆盖行，与表可用状态分开表达。</zh-CN>
            //   <en>Store whether the current tab has an override row, separately from table availability.</en>
            // </lang>
            IsFound = isFound;

            // <lang>
            //   <zh-CN>将读取主题空值归一为空字符串，避免把数据库 null 当作可应用主题。</zh-CN>
            //   <en>Normalize a null theme to empty so a database null is never treated as an applicable theme.</en>
            // </lang>
            ThemeName = themeName ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>覆盖表是否已部署并可读取。</zh-CN>
        ///   <en>Whether the override table is deployed and readable.</en>
        /// </lang>
        /// </summary>
        public bool IsAvailable { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前 Tab 是否有覆盖值。</zh-CN>
        ///   <en>Whether the current tab has an override.</en>
        /// </lang>
        /// </summary>
        public bool IsFound { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>覆盖主题名。</zh-CN>
        ///   <en>Overridden theme name.</en>
        /// </lang>
        /// </summary>
        public string ThemeName { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>Tab 主题覆盖写入结果。</zh-CN>
    ///   <en>Tab theme-override write result.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalTabThemeOverrideWriteResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 Tab 主题覆盖写入或删除结果。</zh-CN>
        ///   <en>Creates a tab theme-override write or delete result.</en>
        /// </lang>
        /// </summary>
        internal PortalTabThemeOverrideWriteResult(bool succeeded, string message)
        {
            // <lang>
            //   <zh-CN>保存写入/删除是否完成，不代表调用方已经完成授权或运营审计。</zh-CN>
            //   <en>Store whether the write or deletion completed; it does not mean the caller completed authorization or operations audit.</en>
            // </lang>
            Succeeded = succeeded;

            // <lang>
            //   <zh-CN>将安全展示消息空值归一为空字符串，不回显数据库异常或连接信息。</zh-CN>
            //   <en>Normalize a null safe-display message to empty without echoing database exceptions or connection information.</en>
            // </lang>
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作是否成功。</zh-CN>
        ///   <en>Whether the operation succeeded.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可安全展示给管理员的操作说明。</zh-CN>
        ///   <en>Operation message safe to show to an administrator.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>Tab 主题覆盖的受限存储。</zh-CN>
    ///   <en>Restricted storage for tab theme overrides.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>覆盖值只保存已验证的部署主题名。表缺失或读取失败时解析器回退全局主题，避免旧数据库阻断门户页面。此存储不负责调用方授权或运营审计；`ThemeSettings` 在调用前要求管理员并在成功后记录审计，任何新增调用点必须采用同等保护。</zh-CN>
    ///   <en>Override values store validated deployed theme names only. When the table is missing or unreadable, the resolver falls back to the global theme so a legacy database never blocks portal pages. This store does not enforce caller authorization or operations audit; `ThemeSettings` requires an administrator before calling and records an audit after success, and every new call site must use equivalent protection.</en>
    /// </lang>
    /// </remarks>
    public static class PortalTabThemeOverrides
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>受控 Tab 主题覆盖表的固定名称，仅用于参数化范围内的部署检查和读写。</zh-CN>
        ///   <en>Fixed name of the controlled tab-theme override table, used only for deployment checks and reads/writes within parameterized scope.</en>
        /// </lang>
        /// </summary>
        private const string TableName = "PortalCfg_TabThemeOverrides";

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取一个 Tab 的主题覆盖值。</zh-CN>
        ///   <en>Reads the theme override for one tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>门户 Tab 标识。</zh-CN>
        ///   <en>Portal tab identifier.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>用于受限诊断的当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context for restricted diagnostics.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表可用状态、命中状态和主题名。</zh-CN>
        ///   <en>Table availability, match state, and theme name.</en>
        /// </l>
        /// </returns>
        public static PortalTabThemeOverrideReadResult Read(int tabId, HttpContext context = null)
        {
            if (tabId <= 0)
            {
                return new PortalTabThemeOverrideReadResult(true, false, string.Empty);
            }

            try
            {
                // <lang>
                //   <zh-CN>创建短生命周期 SQL 连接；连接不可用时不抛出到页面，而是返回可判定的回退状态。</zh-CN>
                //   <en>Create a short-lived SQL connection; when unavailable, do not throw into the page and instead return a determinable fallback state.</en>
                // </lang>
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

                    // <lang>
                    //   <zh-CN>创建仅按 TabId 参数读取的命令，避免把主题选择扩展为动态 SQL。</zh-CN>
                    //   <en>Create a command that reads only by the TabId parameter, preventing theme selection from becoming dynamic SQL.</en>
                    // </lang>
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT [ThemeName]
FROM [dbo].[PortalCfg_TabThemeOverrides]
WHERE [TabId] = @TabId;";
                        command.Parameters.Add("@TabId", SqlDbType.Int).Value = tabId;

                        // <lang>
                        //   <zh-CN>读取单值主题名；null/DBNull 表示表可用但当前 Tab 没有覆盖。</zh-CN>
                        //   <en>Read the single theme value; null or DBNull means the table is available but the current tab has no override.</en>
                        // </lang>
                        object value = command.ExecuteScalar();
                        return value == null || value == DBNull.Value
                            ? new PortalTabThemeOverrideReadResult(true, false, string.Empty)
                            : new PortalTabThemeOverrideReadResult(true, true, Convert.ToString(value));
                    }
                }
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>仅把异常交给受限诊断记录，返回不可用状态以触发全局主题回退。</zh-CN>
                //   <en>Send the exception only to restricted diagnostics and return unavailable status to trigger global-theme fallback.</en>
                // </lang>
                PortalDiagnostics.Error("Theme.TabOverride.Read", "Reading a tab theme override failed.", exception, context);
                return new PortalTabThemeOverrideReadResult(false, false, string.Empty);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存一个 Tab 的主题覆盖值。</zh-CN>
        ///   <en>Saves the theme override for one tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>门户 Tab 标识。</zh-CN>
        ///   <en>Portal tab identifier.</en>
        /// </l>
        /// </param>
        /// <param name="themeName">
        /// <l>
        ///   <zh-CN>已部署主题名。</zh-CN>
        ///   <en>Deployed theme name.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文，用于操作人和诊断。</zh-CN>
        ///   <en>Current HTTP context for actor and diagnostics.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果。</zh-CN>
        ///   <en>Write result.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>仅在写入前验证当前部署的主题包，不会创建主题目录或变更 manifest。成功结果不代表调用方已经做过授权或审计。</zh-CN>
        ///   <en>Validates the currently deployed theme package before writing only; it does not create a theme directory or change a manifest. A successful result does not mean the caller has performed authorization or audit.</en>
        /// </lang>
        /// </remarks>
        public static PortalTabThemeOverrideWriteResult Save(int tabId, string themeName, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>保存受信部署主题包，确保写入值来自通过 manifest 校验的本地主题。</zh-CN>
            //   <en>Hold the trusted deployed theme package so the stored value comes from a locally manifest-validated theme.</en>
            // </lang>
            PortalThemePackage package;

            // <lang>
            //   <zh-CN>接收主题校验失败原因但不回显给管理员；对外只返回固定安全消息。</zh-CN>
            //   <en>Receive the theme-validation failure reason without echoing it to administrators; expose only a fixed safe message.</en>
            // </lang>
            string validationReason;
            if (tabId <= 0 || !PortalThemeCatalog.TryGetTrustedPackage(themeName, out package, out validationReason))
            {
                return new PortalTabThemeOverrideWriteResult(false, "Select a validated deployed theme.");
            }

            try
            {
                // <lang>
                //   <zh-CN>创建短生命周期 SQL 连接；缺失运行期存储时保持管理员可理解的固定失败结果。</zh-CN>
                //   <en>Create a short-lived SQL connection; when runtime storage is absent, retain an administrator-understandable fixed failure result.</en>
                // </lang>
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

                    // <lang>
                    //   <zh-CN>开始写入事务，使存在性检查与 INSERT/UPDATE 选择保持同一并发边界。</zh-CN>
                    //   <en>Begin the write transaction so existence checking and INSERT/UPDATE selection share one concurrency boundary.</en>
                    // </lang>
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        // <lang>
                        //   <zh-CN>在更新锁事务中确认当前 Tab 是否已有行，避免并发保存时错误选择 INSERT 或 UPDATE。</zh-CN>
                        //   <en>Check whether the tab already has a row under an update-lock transaction so concurrent saves do not choose INSERT or UPDATE incorrectly.</en>
                        // </lang>
                        bool exists = ExistsForUpdate(connection, transaction, tabId);
                        // <lang>
                        //   <zh-CN>创建绑定当前事务的参数化写入命令；文本均经长度限制后作为参数提供。</zh-CN>
                        //   <en>Create a parameterized write command bound to the current transaction; all text is length-limited before being provided as parameters.</en>
                        // </lang>
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
                // <lang>
                //   <zh-CN>记录受限诊断并返回固定失败消息，不暴露 SQL、路径、连接串或主题 manifest 细节。</zh-CN>
                //   <en>Record restricted diagnostics and return a fixed failure message without exposing SQL, paths, connection strings, or theme-manifest details.</en>
                // </lang>
                PortalDiagnostics.Error("Theme.TabOverride.Save", "Saving a tab theme override failed.", exception, context);
                return new PortalTabThemeOverrideWriteResult(false, "The tab theme override could not be saved. Check diagnostics for the event id.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除一个 Tab 覆盖值，使其回退到全局主题。</zh-CN>
        ///   <en>Deletes one tab override so it falls back to the global theme.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>门户 Tab 标识。</zh-CN>
        ///   <en>Portal tab identifier.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文，用于诊断。</zh-CN>
        ///   <en>Current HTTP context for diagnostics.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>删除结果。</zh-CN>
        ///   <en>Deletion result.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>删除只移除数据库覆盖值；下一次请求按主题解析器的全局设置、appSettings 或 Default 回退，且本方法不自行授权或审计。</zh-CN>
        ///   <en>Deletion removes only the database override. Later requests fall back through the resolver's global setting, appSettings, or Default, and this method performs neither authorization nor audit itself.</en>
        /// </lang>
        /// </remarks>
        public static PortalTabThemeOverrideWriteResult Delete(int tabId, HttpContext context = null)
        {
            if (tabId <= 0)
            {
                return new PortalTabThemeOverrideWriteResult(false, "Select a portal tab before clearing its override.");
            }

            try
            {
                // <lang>
                //   <zh-CN>创建短生命周期 SQL 连接；存储不可用时不假定覆盖已被删除。</zh-CN>
                //   <en>Create a short-lived SQL connection; when storage is unavailable, do not assume the override was deleted.</en>
                // </lang>
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

                    // <lang>
                    //   <zh-CN>创建仅按 TabId 参数删除的命令，删除范围不受客户端主题文本影响。</zh-CN>
                    //   <en>Create a command that deletes only by the TabId parameter; deletion scope is unaffected by client theme text.</en>
                    // </lang>
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
                // <lang>
                //   <zh-CN>删除失败保持 fail-safe：只记录受限诊断，不伪造已回退全局主题的成功状态。</zh-CN>
                //   <en>Keep deletion failure fail-safe: record restricted diagnostics only and do not claim that global fallback was successfully restored.</en>
                // </lang>
                PortalDiagnostics.Error("Theme.TabOverride.Delete", "Deleting a tab theme override failed.", exception, context);
                return new PortalTabThemeOverrideWriteResult(false, "The tab theme override could not be removed. Check diagnostics for the event id.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从 Unity 中解析运行期连接串并创建 SQL Server 连接。</zh-CN>
        ///   <en>Resolves the runtime connection string from Unity and creates a SQL Server connection.</en>
        /// </lang>
        /// </summary>
        private static SqlConnection CreateConnection()
        {
            if (Global.Container == null)
            {
                return null;
            }

            // <lang>
            //   <zh-CN>从既有 Unity 注册读取运行期连接串；空值不创建连接，避免把缺失配置误作可用存储。</zh-CN>
            //   <en>Read the runtime connection string from the existing Unity registration; an empty value creates no connection so missing configuration is not treated as available storage.</en>
            // </lang>
            string connectionString = Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);
            return string.IsNullOrWhiteSpace(connectionString) ? null : new SqlConnection(connectionString);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查 Tab 主题覆盖表是否已部署。</zh-CN>
        ///   <en>Checks whether the tab theme-override table has been deployed.</en>
        /// </lang>
        /// </summary>
        private static bool IsTableAvailable(SqlConnection connection)
        {
            // <lang>
            //   <zh-CN>创建只读部署探针命令，避免对缺少迁移的旧数据库进行写入尝试。</zh-CN>
            //   <en>Create a read-only deployment-probe command, avoiding write attempts against legacy databases without the migration.</en>
            // </lang>
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[" + TableName + "]', N'U') IS NULL THEN 0 ELSE 1 END;";
                // <lang>
                //   <zh-CN>读取部署探针的标量结果；表不存在时明确返回 false，供上层回退。</zh-CN>
                //   <en>Read the deployment-probe scalar result; return false explicitly when the table is absent so upper layers can fall back.</en>
                // </lang>
                object value = command.ExecuteScalar();
                return value != null && Convert.ToInt32(value) == 1;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在事务中锁定并判断指定 Tab 覆盖行是否存在。</zh-CN>
        ///   <en>Locks and determines whether the specified tab override row exists within a transaction.</en>
        /// </lang>
        /// </summary>
        private static bool ExistsForUpdate(SqlConnection connection, SqlTransaction transaction, int tabId)
        {
            // <lang>
            //   <zh-CN>创建事务内存在性检查命令，UPDLOCK/HOLDLOCK 保护后续的条件写入选择。</zh-CN>
            //   <en>Create the in-transaction existence-check command; UPDLOCK/HOLDLOCK protects subsequent conditional-write selection.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前操作人用户名，未认证时使用匿名占位值。</zh-CN>
        ///   <en>Reads the current actor user name, using an anonymous placeholder when unauthenticated.</en>
        /// </lang>
        /// </summary>
        private static string GetActorUserName(HttpContext context)
        {
            // <lang>
            //   <zh-CN>优先采用显式上下文，再回退当前请求；该引用只在本次写入中存活。</zh-CN>
            //   <en>Prefer the explicit context and then fall back to the current request; this reference lives only for the current write.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;
            if (current == null || current.User == null || current.User.Identity == null ||
                !current.User.Identity.IsAuthenticated)
            {
                return "(anonymous)";
            }

            return current.User.Identity.Name;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加文本参数，并在写入前进行长度限制和诊断级清理。</zh-CN>
        ///   <en>Adds a text parameter with length limiting and diagnostic-grade sanitization before writing.</en>
        /// </lang>
        /// </summary>
        private static void AddTextParameter(
            SqlCommand command,
            string parameterName,
            int size,
            string value,
            string fallback)
        {
            // <lang>
            //   <zh-CN>在参数化写入前净化并截断文本，限制操作人和主题名等管理数据的长度与诊断风险。</zh-CN>
            //   <en>Sanitize and truncate text before the parameterized write to bound the length and diagnostic risk of administrative data such as actor and theme names.</en>
            // </lang>
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }
    }
}
