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
    ///   <zh-CN>数据库运行级设置读取结果。</zh-CN>
    ///   <en>Database runtime-setting read result.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalSystemSettingReadResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建数据库运行级设置读取结果。</zh-CN>
        ///   <en>Creates a database runtime-setting read result.</en>
        /// </lang>
        /// </summary>
        /// <param name="isAvailable">
        /// <l>
        ///   <zh-CN>设置表是否可用。</zh-CN>
        ///   <en>Whether the settings table is available.</en>
        /// </l>
        /// </param>
        /// <param name="isFound">
        /// <l>
        ///   <zh-CN>是否找到对应覆盖值。</zh-CN>
        ///   <en>Whether the matching override was found.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始覆盖值文本。</zh-CN>
        ///   <en>Raw override value text.</en>
        /// </l>
        /// </param>
        /// <param name="valueType">
        /// <l>
        ///   <zh-CN>数据库记录的值类型名称。</zh-CN>
        ///   <en>Value-type name recorded by the database.</en>
        /// </l>
        /// </param>
        internal PortalSystemSettingReadResult(bool isAvailable, bool isFound, string value, string valueType)
        {
            IsAvailable = isAvailable;
            IsFound = isFound;
            Value = value ?? string.Empty;
            ValueType = valueType ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>运行级设置表是否可用。</zh-CN>
        ///   <en>Whether the runtime-settings table is available.</en>
        /// </lang>
        /// </summary>
        public bool IsAvailable { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前键是否存在数据库覆盖值；仅当 <see cref="IsAvailable"/> 为 <c>true</c> 时有意义。</zh-CN>
        ///   <en>Whether a database override exists for the requested key; meaningful only when <see cref="IsAvailable"/> is <c>true</c>.</en>
        /// </lang>
        /// </summary>
        public bool IsFound { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据库覆盖值文本；调用方应按注册定义校验后再使用。</zh-CN>
        ///   <en>Database override value text; callers must validate it against the registered definition before use.</en>
        /// </lang>
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据库中保存的值类型名称，用于防止错误类型覆盖被直接采用。</zh-CN>
        ///   <en>Value-type name stored in the database, used to prevent direct use of an override with the wrong type.</en>
        /// </lang>
        /// </summary>
        public string ValueType { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>数据库运行级设置写入或删除结果。</zh-CN>
    ///   <en>Result of writing or deleting a database runtime setting.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalSystemSettingWriteResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建数据库运行级设置写入或删除结果。</zh-CN>
        ///   <en>Creates a database runtime-setting write or deletion result.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded">
        /// <l>
        ///   <zh-CN>操作是否成功。</zh-CN>
        ///   <en>Whether the operation succeeded.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>可展示给管理员的安全说明。</zh-CN>
        ///   <en>Safe message that may be shown to an administrator.</en>
        /// </l>
        /// </param>
        internal PortalSystemSettingWriteResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作是否成功完成。</zh-CN>
        ///   <en>Whether the operation completed successfully.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可安全展示给管理员的结果说明，不包含连接串或 SQL 细节。</zh-CN>
        ///   <en>Result message safe to show to an administrator; it contains no connection-string or SQL details.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>受限数据库运行级设置存储。</zh-CN>
    ///   <en>Restricted database runtime-settings store.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类只处理 registry 已登记、允许在线编辑且非敏感的设置。读取失败时调用方回退；写入要求 当前值表和设置审计表均可用，并在同一事务中完成，避免出现无法追溯的在线配置变化。</zh-CN>
    ///   <en>This class handles only registered, non-sensitive settings that allow online editing. Callers fall back when reads fail; writes require both current-value and setting-audit tables and complete in one transaction so online configuration changes remain traceable.</en>
    /// </lang>
    /// </remarks>
    public static class PortalSystemSettingsStore
    {
        private const string SettingsTableName = "PortalCfg_SystemSettings";
        private const string AuditsTableName = "PortalCfg_SystemSettingAudits";

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取一个数据库运行级设置覆盖值。</zh-CN>
        ///   <en>Reads one database runtime-setting override.</en>
        /// </lang>
        /// </summary>
        /// <param name="settingKey">
        /// <l>
        ///   <zh-CN>已登记的稳定设置键。</zh-CN>
        ///   <en>Registered stable setting key.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>用于受限诊断的当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context for restricted diagnostics; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表可用状态、命中状态、原始值和数据库值类型。</zh-CN>
        ///   <en>Table availability, match state, raw value, and database value type.</en>
        /// </l>
        /// </returns>
        public static PortalSystemSettingReadResult Read(string settingKey, HttpContext context = null)
        {
            if (string.IsNullOrWhiteSpace(settingKey))
            {
                return new PortalSystemSettingReadResult(false, false, string.Empty, string.Empty);
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalSystemSettingReadResult(false, false, string.Empty, string.Empty);
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection, SettingsTableName))
                    {
                        return new PortalSystemSettingReadResult(false, false, string.Empty, string.Empty);
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT [SettingValue], [ValueType]
FROM [dbo].[PortalCfg_SystemSettings]
WHERE [SettingKey] = @SettingKey;";
                        AddTextParameter(command, "@SettingKey", 200, settingKey, string.Empty);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return new PortalSystemSettingReadResult(true, false, string.Empty, string.Empty);
                            }

                            string value = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                            string valueType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                            return new PortalSystemSettingReadResult(true, true, value, valueType);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error(
                    "SystemSettings.Read",
                    "Reading a database runtime setting failed.",
                    exception,
                    context);
                return new PortalSystemSettingReadResult(false, false, string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>写入一个允许在线管理的非敏感设置覆盖值，并在同一事务记录设置审计。</zh-CN>
        ///   <en>Writes one non-sensitive setting override allowed for online management and records setting audit data in the same transaction.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>已登记且允许在线编辑的非敏感设置定义。</zh-CN>
        ///   <en>Registered non-sensitive setting definition that allows online editing.</en>
        /// </l>
        /// </param>
        /// <param name="settingValue">
        /// <l>
        ///   <zh-CN>候选文本值；存储前会再次按定义校验和规范化。</zh-CN>
        ///   <en>Candidate text value; it is validated and normalized against the definition again before storage.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>用于审计和受限诊断的当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context for auditing and restricted diagnostics; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果，不包含连接串或 SQL 细节。</zh-CN>
        ///   <en>Write result without connection-string or SQL details.</en>
        /// </l>
        /// </returns>
        public static PortalSystemSettingWriteResult SaveOverride(
            PortalSettingDefinition definition,
            string settingValue,
            HttpContext context = null)
        {
            string normalizedValue;
            if (!CanWrite(definition, settingValue, out normalizedValue))
            {
                return new PortalSystemSettingWriteResult(false, "This setting cannot be saved online.");
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalSystemSettingWriteResult(false, "The runtime settings database is unavailable.");
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection, SettingsTableName) ||
                        !IsTableAvailable(connection, AuditsTableName))
                    {
                        return new PortalSystemSettingWriteResult(false, "Run the system-settings migration before changing this setting.");
                    }

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        string oldValue;
                        bool exists;
                        bool ignoredCanDelete;
                        ReadCurrentValueForUpdate(
                            connection,
                            transaction,
                            definition.Key,
                            out exists,
                            out oldValue,
                            out ignoredCanDelete);

                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = exists
                                ? @"
UPDATE [dbo].[PortalCfg_SystemSettings]
SET [SettingValue] = @SettingValue,
    [ValueType] = @ValueType,
    [UpdatedBy] = @UpdatedBy,
    [UpdatedUtc] = @UpdatedUtc
WHERE [SettingKey] = @SettingKey;"
                                : @"
INSERT INTO [dbo].[PortalCfg_SystemSettings]
    ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
VALUES
    (@SettingKey, @SettingValue, @ValueType, N'Database', 1, @UpdatedBy, @UpdatedUtc);";
                            AddTextParameter(command, "@SettingKey", 200, definition.Key, string.Empty);
                            AddUnlimitedTextParameter(command, "@SettingValue", normalizedValue);
                            AddTextParameter(command, "@ValueType", 50, definition.ValueType.ToString(), string.Empty);
                            AddTextParameter(command, "@UpdatedBy", 100, GetActorUserName(context), "(anonymous)");
                            command.Parameters.Add("@UpdatedUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                            command.ExecuteNonQuery();
                        }

                        WriteAudit(
                            connection,
                            transaction,
                            definition.Key,
                            exists ? "Update" : "Insert",
                            oldValue,
                            normalizedValue,
                            context);
                        transaction.Commit();
                    }
                }

                return new PortalSystemSettingWriteResult(true, "The runtime setting was saved.");
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error(
                    "SystemSettings.Save",
                    "Writing a database runtime setting failed.",
                    exception,
                    context);
                return new PortalSystemSettingWriteResult(false, "The runtime setting could not be saved. Check diagnostics for the event id.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除允许删除的数据库运行级覆盖值，使设置回退至 appSettings 或代码默认值，并写入审计。</zh-CN>
        ///   <en>Deletes a deletable database runtime override so the setting falls back to appSettings or code defaults, and writes audit data.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>已登记且允许在线编辑的非敏感设置定义。</zh-CN>
        ///   <en>Registered non-sensitive setting definition that allows online editing.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>用于审计和受限诊断的当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context for auditing and restricted diagnostics; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>删除结果，或不存在覆盖值时的成功结果。</zh-CN>
        ///   <en>Deletion result, or a successful result when no override exists.</en>
        /// </l>
        /// </returns>
        public static PortalSystemSettingWriteResult DeleteOverride(
            PortalSettingDefinition definition,
            HttpContext context = null)
        {
            if (definition == null || !definition.CanEditOnline || definition.IsSensitive)
            {
                return new PortalSystemSettingWriteResult(false, "This setting cannot be reset online.");
            }

            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalSystemSettingWriteResult(false, "The runtime settings database is unavailable.");
                    }

                    connection.Open();
                    if (!IsTableAvailable(connection, SettingsTableName) ||
                        !IsTableAvailable(connection, AuditsTableName))
                    {
                        return new PortalSystemSettingWriteResult(false, "Run the system-settings migration before resetting this setting.");
                    }

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        string oldValue;
                        bool exists;
                        bool canDelete;
                        ReadCurrentValueForUpdate(connection, transaction, definition.Key, out exists, out oldValue, out canDelete);
                        if (!exists)
                        {
                            transaction.Commit();
                            return new PortalSystemSettingWriteResult(true, "No database override was present.");
                        }

                        if (!canDelete)
                        {
                            transaction.Rollback();
                            return new PortalSystemSettingWriteResult(false, "This database override is protected and cannot be deleted.");
                        }

                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
DELETE FROM [dbo].[PortalCfg_SystemSettings]
WHERE [SettingKey] = @SettingKey;";
                            AddTextParameter(command, "@SettingKey", 200, definition.Key, string.Empty);
                            command.ExecuteNonQuery();
                        }

                        WriteAudit(connection, transaction, definition.Key, "Delete", oldValue, null, context);
                        transaction.Commit();
                    }
                }

                return new PortalSystemSettingWriteResult(true, "The database override was removed.");
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error(
                    "SystemSettings.Delete",
                    "Deleting a database runtime setting failed.",
                    exception,
                    context);
                return new PortalSystemSettingWriteResult(false, "The runtime setting could not be reset. Check diagnostics for the event id.");
            }
        }

        private static bool CanWrite(
            PortalSettingDefinition definition,
            string settingValue,
            out string normalizedValue)
        {
            normalizedValue = string.Empty;
            return definition != null &&
                   definition.CanEditOnline &&
                   !definition.IsSensitive &&
                   PortalRuntimeSettings.TryNormalizeValue(definition, settingValue, out normalizedValue);
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

        private static bool IsTableAvailable(SqlConnection connection, string tableName)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[" + tableName + "]', N'U') IS NULL THEN 0 ELSE 1 END;";
                object value = command.ExecuteScalar();
                return value != null && Convert.ToInt32(value) == 1;
            }
        }

        private static void ReadCurrentValueForUpdate(
            SqlConnection connection,
            SqlTransaction transaction,
            string settingKey,
            out bool exists,
            out string currentValue,
            out bool canDelete)
        {
            exists = false;
            currentValue = null;
            canDelete = false;

            using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT [SettingValue], [CanDelete]
FROM [dbo].[PortalCfg_SystemSettings] WITH (UPDLOCK, HOLDLOCK)
WHERE [SettingKey] = @SettingKey;";
                AddTextParameter(command, "@SettingKey", 200, settingKey, string.Empty);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return;
                    }

                    exists = true;
                    currentValue = reader.IsDBNull(0) ? null : reader.GetString(0);
                    canDelete = !reader.IsDBNull(1) && reader.GetBoolean(1);
                }
            }
        }

        private static void WriteAudit(
            SqlConnection connection,
            SqlTransaction transaction,
            string settingKey,
            string changeType,
            string oldValue,
            string newValue,
            HttpContext context)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO [dbo].[PortalCfg_SystemSettingAudits]
    ([SettingKey], [ChangeType], [OldValue], [NewValue], [ChangedBy], [ChangedUtc],
     [ChangeReason], [ClientIp], [UserAgent], [CorrelationId])
VALUES
    (@SettingKey, @ChangeType, @OldValue, @NewValue, @ChangedBy, @ChangedUtc,
     NULL, @ClientIp, @UserAgent, NULL);";
                AddTextParameter(command, "@SettingKey", 200, settingKey, string.Empty);
                AddTextParameter(command, "@ChangeType", 20, changeType, "Update");
                AddUnlimitedTextParameter(command, "@OldValue", oldValue);
                AddUnlimitedTextParameter(command, "@NewValue", newValue);
                AddTextParameter(command, "@ChangedBy", 100, GetActorUserName(context), "(anonymous)");
                command.Parameters.Add("@ChangedUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                AddTextParameter(command, "@ClientIp", 64, GetClientIp(context), string.Empty);
                AddTextParameter(command, "@UserAgent", 400, GetUserAgent(context), string.Empty);
                command.ExecuteNonQuery();
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

        private static string GetClientIp(HttpContext context)
        {
            HttpContext current = context ?? HttpContext.Current;
            return current == null || current.Request == null ? string.Empty : current.Request.UserHostAddress;
        }

        private static string GetUserAgent(HttpContext context)
        {
            HttpContext current = context ?? HttpContext.Current;
            return current == null || current.Request == null ? string.Empty : current.Request.UserAgent;
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

        private static void AddUnlimitedTextParameter(SqlCommand command, string parameterName, string value)
        {
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, -1).Value =
                string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;
        }
    }
}
