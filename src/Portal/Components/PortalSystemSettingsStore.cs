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
        // <lang>
        //   <zh-CN>当前数据库覆盖值表的固定受控名称；仅由本类的私有 SQL helper 使用，不接受请求、配置或设置键作为表名。</zh-CN>
        //   <en>Fixed controlled name of the current database-override table; used only by this class's private SQL helpers and never accepts a request, configuration, or setting key as a table name.</en>
        // </lang>
        private const string SettingsTableName = "PortalCfg_SystemSettings";

        // <lang>
        //   <zh-CN>设置变更审计表的固定受控名称；写入和删除要求该表同时可用，避免在线变化失去可追溯性。</zh-CN>
        //   <en>Fixed controlled name of the setting-change audit table; writes and deletes require it to be available with the settings table so online changes do not lose traceability.</en>
        // </lang>
        private const string AuditsTableName = "PortalCfg_SystemSettingAudits";

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取一个数据库运行级设置覆盖值，并将不可用状态与可用但未命中状态分开返回。</zh-CN>
        ///   <en>Reads one database runtime-setting override and returns unavailable separately from available-but-not-found.</en>
        /// </lang>
        /// </summary>
        /// <param name="settingKey">
        /// <l>
        ///   <zh-CN>调用方提供的稳定设置键；本方法只拒绝空白键，不验证 registry 或访问授权。</zh-CN>
        ///   <en>Stable setting key supplied by the caller; this method rejects only blank keys and does not validate the registry or access authorization.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>仅在异常诊断时使用的当前 HTTP 上下文，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP context used only for exception diagnostics; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表/读取可用状态、命中状态、原始值和数据库值类型；不可用时调用方必须回退，命中值仍须按定义校验。</zh-CN>
        ///   <en>Table/read availability, match state, raw value, and database value type; callers must fall back when unavailable and still validate a found value against its definition.</en>
        /// </l>
        /// </returns>
        public static PortalSystemSettingReadResult Read(string settingKey, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>空白键既不映射为“未命中”也不进入数据库；返回不可用状态，避免调用方把无效请求当作某个设置没有覆盖值。本方法不实施 registry 校验或页面/用户授权。</zh-CN>
            //   <en>A blank key is neither treated as not found nor sent to the database; return unavailable so callers do not mistake an invalid request for a setting without an override. This method does not implement registry validation or page/user authorization.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(settingKey))
            {
                return new PortalSystemSettingReadResult(false, false, string.Empty, string.Empty);
            }

            try
            {
                // <lang>
                //   <zh-CN>连接只由受控容器路径创建并在 using 结束时释放；缺少容器或连接串时保持不可用结果，不把配置细节公开给读取调用方。</zh-CN>
                //   <en>Create the connection only through the controlled container path and release it at the end of the using block; when the container or connection string is unavailable, retain the unavailable result without exposing configuration detail to the read caller.</en>
                // </lang>
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalSystemSettingReadResult(false, false, string.Empty, string.Empty);
                    }

                    // <lang>
                    //   <zh-CN>先确认固定设置表存在再执行查询；迁移未完成或表不存在时不能把基础设施状态误报为某个键未命中。</zh-CN>
                    //   <en>Confirm that the fixed settings table exists before querying; an incomplete migration or missing table must not be reported as a key that simply was not found.</en>
                    // </lang>
                    connection.Open();
                    if (!IsTableAvailable(connection, SettingsTableName))
                    {
                        return new PortalSystemSettingReadResult(false, false, string.Empty, string.Empty);
                    }

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        // <lang>
                        //   <zh-CN>查询只选择固定表中的当前值和类型；设置键作为参数传入，既不允许动态 SQL，也不在此按注册定义转换或接受该值。</zh-CN>
                        //   <en>The query selects only current value and type from the fixed table; pass the setting key as a parameter, allow no dynamic SQL, and neither convert nor accept the value against a registered definition here.</en>
                        // </lang>
                        command.CommandText = @"
SELECT [SettingValue], [ValueType]
FROM [dbo].[PortalCfg_SystemSettings]
WHERE [SettingKey] = @SettingKey;";
                        AddTextParameter(command, "@SettingKey", 200, settingKey, string.Empty);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // <lang>
                            //   <zh-CN>表已确认可用但没有对应行时保留 IsAvailable=true、IsFound=false；运行时解析器可据此继续其它受控来源，而不是按数据库失败处理。</zh-CN>
                            //   <en>When the table is available but no row matches, retain IsAvailable=true and IsFound=false; the runtime resolver can continue to other controlled sources rather than treating this as a database failure.</en>
                            // </lang>
                            if (!reader.Read())
                            {
                                return new PortalSystemSettingReadResult(true, false, string.Empty, string.Empty);
                            }

                            // <lang>
                            //   <zh-CN>行存在性由 IsFound 表示；数据库 NULL 文本归一为空字符串而不改变命中事实，值和类型仍由上层定义校验后才能采用。</zh-CN>
                            //   <en>IsFound carries row existence; normalize database-null text to empty strings without changing that fact, and upper definitions must still validate value and type before adoption.</en>
                            // </lang>
                            string value = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                            string valueType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                            return new PortalSystemSettingReadResult(true, true, value, valueType);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>诊断层接收异常和可选上下文以保留受限运营证据；调用方只得到不可用状态，不接收连接串、SQL 或异常详情，并应触发既有回退。</zh-CN>
                //   <en>The diagnostic layer receives the exception and optional context for restricted operational evidence; the caller receives only unavailable status, never connection-string, SQL, or exception detail, and should trigger its established fallback.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>先准备稳定失败输出并通过同一运行时规范化门禁验证候选值；该变量只在通过定义、安全性和类型/范围检查后参与数据库参数。</zh-CN>
            //   <en>Prepare a stable failure output and validate the candidate through the same runtime-normalization gate; this variable enters database parameters only after definition, sensitivity, type, and range checks pass.</en>
            // </lang>
            string normalizedValue;
            if (!CanWrite(definition, settingValue, out normalizedValue))
            {
                // <lang>
                //   <zh-CN>存储层拒绝未登记、不可在线编辑、敏感或无效的值，但不自行执行页面/用户授权；返回固定消息而不回显候选设置文本。</zh-CN>
                //   <en>The store rejects unregistered, non-online-editable, sensitive, or invalid values but does not perform page or user authorization itself; return a fixed message without echoing candidate setting text.</en>
                // </lang>
                return new PortalSystemSettingWriteResult(false, "This setting cannot be saved online.");
            }

            try
            {
                // <lang>
                //   <zh-CN>连接由受控 Unity/外置连接串路径创建并在 using 结束时释放；本方法不记录连接串，也不把缺失连接串转换为异常详情。</zh-CN>
                //   <en>Create the connection through the controlled Unity/external-connection-string path and release it at the end of the using block; this method does not record the connection string or turn a missing connection string into exception detail.</en>
                // </lang>
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalSystemSettingWriteResult(false, "The runtime settings database is unavailable.");
                    }

                    // <lang>
                    //   <zh-CN>只有当前值表和审计表均存在才允许变更，防止在部分迁移状态写出无法审计的覆盖值。</zh-CN>
                    //   <en>Allow a change only when both the current-value and audit tables exist, preventing an override from being written in a partial migration state without auditability.</en>
                    // </lang>
                    connection.Open();
                    if (!IsTableAvailable(connection, SettingsTableName) ||
                        !IsTableAvailable(connection, AuditsTableName))
                    {
                        return new PortalSystemSettingWriteResult(false, "Run the system-settings migration before changing this setting.");
                    }

                    // <lang>
                    //   <zh-CN>当前值读取、更新/插入与审计共用一个事务；任一步失败由 using/异常路径阻止成功结果，提交只发生在所有写入完成之后。</zh-CN>
                    //   <en>Current-value read, update/insert, and audit share one transaction; any failure prevents a success result through the using/exception path, and commit occurs only after every write completes.</en>
                    // </lang>
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        // <lang>
                        //   <zh-CN>保存锁定读取到的旧值以形成审计前值；它不离开事务，也不进入成功/失败消息。</zh-CN>
                        //   <en>Keep the locked old value for the audit before-image; it does not leave the transaction or enter success/failure messages.</en>
                        // </lang>
                        string oldValue;

                        // <lang>
                        //   <zh-CN>存在标志决定使用更新还是插入 SQL，避免以竞态下的预先普通读取推断分支。</zh-CN>
                        //   <en>The existence flag selects update versus insert SQL, avoiding a branch inferred from a prior unlocked read under concurrency.</en>
                        // </lang>
                        bool exists;

                        // <lang>
                        //   <zh-CN>保存路径保留锁定行的删除保护输出以维持 helper 契约，但不据此改变已有覆盖值的更新资格。</zh-CN>
                        //   <en>The save path retains the locked row's deletion-protection output to preserve the helper contract but does not use it to change update eligibility for an existing override.</en>
                        // </lang>
                        bool ignoredCanDelete;
                        ReadCurrentValueForUpdate(
                            connection,
                            transaction,
                            definition.Key,
                            out exists,
                            out oldValue,
                            out ignoredCanDelete);

                        // <lang>
                        //   <zh-CN>同一事务中的参数化命令只在已存在行更新规范化值和元数据，否则以固定 SourceLevel/CanDelete 默认值插入；设置键和所有可变值均为参数。</zh-CN>
                        //   <en>The parameterized command in the same transaction updates normalized value and metadata only for an existing row; otherwise it inserts with fixed SourceLevel/CanDelete defaults, while the setting key and every variable value remain parameters.</en>
                        // </lang>
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

                        // <lang>
                        //   <zh-CN>审计记录旧/新值、固定变更类型和受限请求事实，随后才提交；审计插入失败不会留下已提交而无审计的设置变化。</zh-CN>
                        //   <en>Record old/new values, controlled change type, and restricted request facts before committing; an audit-insert failure cannot leave a committed setting change without audit.</en>
                        // </lang>
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

                // <lang>
                //   <zh-CN>成功消息只说明结果，不包含设置值、连接串、SQL 或审计数据。</zh-CN>
                //   <en>The success message states only the outcome and contains no setting value, connection string, SQL, or audit data.</en>
                // </lang>
                return new PortalSystemSettingWriteResult(true, "The runtime setting was saved.");
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>异常交给既有净化诊断链，调用方仅收到固定可展示失败消息；不把数据库或候选值细节泄露到管理界面。</zh-CN>
                //   <en>Send exceptions to the existing sanitized diagnostics chain while the caller receives only a fixed display-safe failure message; do not leak database or candidate-value detail to the administration UI.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>删除同样只允许已登记、可在线编辑且非敏感的定义；该门禁不替代页面授权，也不会从空定义推断任何默认键。</zh-CN>
            //   <en>Deletion likewise permits only a registered, online-editable, non-sensitive definition; this gate does not replace page authorization and never infers a default key from a null definition.</en>
            // </lang>
            if (definition == null || !definition.CanEditOnline || definition.IsSensitive)
            {
                return new PortalSystemSettingWriteResult(false, "This setting cannot be reset online.");
            }

            try
            {
                // <lang>
                //   <zh-CN>连接生命周期与保存路径一致：缺失连接串以受控可展示结果回退，连接对象始终由 using 释放。</zh-CN>
                //   <en>The connection lifecycle matches the save path: a missing connection string falls back to a controlled display-safe result and the connection object is always released by using.</en>
                // </lang>
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalSystemSettingWriteResult(false, "The runtime settings database is unavailable.");
                    }

                    // <lang>
                    //   <zh-CN>删除同样要求当前值和审计表同时存在；不在迁移不完整的环境删除覆盖值，以保持审计与回退事实一致。</zh-CN>
                    //   <en>Deletion also requires both current-value and audit tables; do not remove an override in an incomplete migration environment so audit and fallback facts remain consistent.</en>
                    // </lang>
                    connection.Open();
                    if (!IsTableAvailable(connection, SettingsTableName) ||
                        !IsTableAvailable(connection, AuditsTableName))
                    {
                        return new PortalSystemSettingWriteResult(false, "Run the system-settings migration before resetting this setting.");
                    }

                    // <lang>
                    //   <zh-CN>锁定读取、保护检查、删除和审计被包裹在一个事务中，防止并发修改使审计前值或可删除性与实际删除脱节。</zh-CN>
                    //   <en>Wrap locked read, protection check, delete, and audit in one transaction so concurrent changes cannot detach the audit before-image or deletability from the actual deletion.</en>
                    // </lang>
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        // <lang>
                        //   <zh-CN>旧值仅用于该事务的 Delete 审计；不会出现在返回消息或诊断摘要中。</zh-CN>
                        //   <en>The old value is used only for the transaction's Delete audit and does not appear in a return message or diagnostics summary.</en>
                        // </lang>
                        string oldValue;

                        // <lang>
                        //   <zh-CN>存在标志区分“无需删除”的幂等成功与真实删除；它来自同一锁定读取，而不是独立查询。</zh-CN>
                        //   <en>The existence flag distinguishes idempotent success with nothing to delete from a real deletion; it comes from the same locked read rather than a separate query.</en>
                        // </lang>
                        bool exists;

                        // <lang>
                        //   <zh-CN>删除保护标志由数据库行事实决定，不能由调用方请求、页面参数或定义覆盖。</zh-CN>
                        //   <en>The deletion-protection flag is determined by the database-row fact and cannot be overridden by a caller request, page parameter, or definition.</en>
                        // </lang>
                        bool canDelete;
                        ReadCurrentValueForUpdate(connection, transaction, definition.Key, out exists, out oldValue, out canDelete);
                        if (!exists)
                        {
                            // <lang>
                            //   <zh-CN>不存在覆盖值时仍提交只读事务并返回既有幂等成功；不虚构 Delete 审计，因为没有发生数据变化。</zh-CN>
                            //   <en>When no override exists, commit the read-only transaction and return established idempotent success; do not fabricate a Delete audit because no data change occurred.</en>
                            // </lang>
                            transaction.Commit();
                            return new PortalSystemSettingWriteResult(true, "No database override was present.");
                        }

                        if (!canDelete)
                        {
                            // <lang>
                            //   <zh-CN>受保护行明确回滚并返回固定消息；不尝试删除、绕过标记或暴露行内容。</zh-CN>
                            //   <en>Explicitly roll back a protected row and return a fixed message; do not attempt deletion, bypass the flag, or expose row content.</en>
                            // </lang>
                            transaction.Rollback();
                            return new PortalSystemSettingWriteResult(false, "This database override is protected and cannot be deleted.");
                        }

                        // <lang>
                        //   <zh-CN>删除命令只以参数化稳定键定位锁定行，并绑定同一事务；不会接受动态表名或 SQL 片段。</zh-CN>
                        //   <en>The delete command locates the locked row only by parameterized stable key and binds the same transaction; it accepts no dynamic table name or SQL fragment.</en>
                        // </lang>
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
DELETE FROM [dbo].[PortalCfg_SystemSettings]
WHERE [SettingKey] = @SettingKey;";
                            AddTextParameter(command, "@SettingKey", 200, definition.Key, string.Empty);
                            command.ExecuteNonQuery();
                        }

                        // <lang>
                        //   <zh-CN>删除审计以 null 新值表达覆盖移除；审计成功后才提交，使数据库回退事实与审计记录原子一致。</zh-CN>
                        //   <en>The delete audit uses a null new value to represent override removal; commit only after audit success so database fallback fact and audit record remain atomic.</en>
                        // </lang>
                        WriteAudit(connection, transaction, definition.Key, "Delete", oldValue, null, context);
                        transaction.Commit();
                    }
                }

                // <lang>
                //   <zh-CN>结果消息只确认覆盖已移除，不公开旧值、连接信息或审计请求字段。</zh-CN>
                //   <en>The result message confirms only that the override was removed and does not disclose old value, connection information, or audit request fields.</en>
                // </lang>
                return new PortalSystemSettingWriteResult(true, "The database override was removed.");
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>删除异常进入既有净化诊断链；调用方只收到固定失败消息和既有事件号提示。</zh-CN>
                //   <en>Deletion exceptions enter the existing sanitized diagnostics chain; the caller receives only the fixed failure message and established event-id guidance.</en>
                // </lang>
                PortalDiagnostics.Error(
                    "SystemSettings.Delete",
                    "Deleting a database runtime setting failed.",
                    exception,
                    context);
                return new PortalSystemSettingWriteResult(false, "The runtime setting could not be reset. Check diagnostics for the event id.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证定义和值是否可作为在线数据库覆盖写入。</zh-CN>
        ///   <en>Validates whether a definition and value may be written as an online database override.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        ///   <l>
        ///     <zh-CN>可能为空的设置定义。</zh-CN>
        ///     <en>Setting definition, which may be null.</en>
        ///   </l>
        /// </param>
        /// <param name="settingValue">
        ///   <l>
        ///     <zh-CN>待规范化的候选文本值。</zh-CN>
        ///     <en>Candidate text value to normalize.</en>
        ///   </l>
        /// </param>
        /// <param name="normalizedValue">
        ///   <l>
        ///     <zh-CN>成功时返回受定义约束的规范化值；失败时为空字符串。</zh-CN>
        ///     <en>Definition-constrained normalized value on success; empty string on failure.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>定义允许在线编辑、非敏感且值通过规范化时为 true。</zh-CN>
        ///     <en>True when the definition permits online editing, is non-sensitive, and the value normalizes.</en>
        ///   </l>
        /// </returns>
        private static bool CanWrite(
            PortalSettingDefinition definition,
            string settingValue,
            out string normalizedValue)
        {
            // <lang>
            //   <zh-CN>先建立稳定失败输出，避免失败路径将原始候选值带到调用方或后续 SQL 参数。</zh-CN>
            //   <en>Establish a stable failure output first so a failure path cannot carry raw candidate text to a caller or later SQL parameter.</en>
            // </lang>
            normalizedValue = string.Empty;

            // <lang>
            //   <zh-CN>门禁同时要求定义存在、声明允许在线编辑且非敏感，并复用运行时类型/范围规范化；它不检查当前用户或页面权限。</zh-CN>
            //   <en>The gate requires a definition, declared online editability, non-sensitivity, and shared runtime type/range normalization; it does not check current-user or page authorization.</en>
            // </lang>
            return definition != null &&
                   definition.CanEditOnline &&
                   !definition.IsSensitive &&
                   PortalRuntimeSettings.TryNormalizeValue(definition, settingValue, out normalizedValue);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从受控依赖容器创建系统设置数据库连接对象。</zh-CN>
        ///   <en>Creates a system-settings database connection object from the controlled dependency container.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        ///   <l>
        ///     <zh-CN>未打开的连接对象；容器或连接串不可用时为 null。</zh-CN>
        ///     <en>Unopened connection object, or null when the container or connection string is unavailable.</en>
        ///   </l>
        /// </returns>
        private static SqlConnection CreateConnection()
        {
            // <lang>
            //   <zh-CN>容器尚未初始化时安全返回 null，让上层公开操作映射为受控可展示结果，而不是从这里访问全局配置细节。</zh-CN>
            //   <en>When the container is not initialized, return null safely so upper public operations map it to a controlled display-safe result rather than accessing global configuration detail here.</en>
            // </lang>
            if (Global.Container == null)
            {
                return null;
            }

            // <lang>
            //   <zh-CN>连接串仅从既有外置连接串注册名解析；空白时不创建连接对象、不记录值，非空时也由调用方负责打开和释放。</zh-CN>
            //   <en>Resolve the connection string only through the established external-connection-string registration name; do not create or log a connection object when blank, and callers remain responsible for opening and releasing a nonblank result.</en>
            // </lang>
            string connectionString = Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);
            return string.IsNullOrWhiteSpace(connectionString) ? null : new SqlConnection(connectionString);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查一个受控系统设置表是否存在。</zh-CN>
        ///   <en>Checks whether one controlled system-settings table exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="connection">
        ///   <l>
        ///     <zh-CN>已由调用方打开的数据库连接。</zh-CN>
        ///     <en>Database connection already opened by the caller.</en>
        ///   </l>
        /// </param>
        /// <param name="tableName">
        ///   <l>
        ///     <zh-CN>仅由本类固定常量传入的表名。</zh-CN>
        ///     <en>Table name supplied only by this class's fixed constants.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>目标用户表存在时为 true。</zh-CN>
        ///     <en>True when the target user table exists.</en>
        ///   </l>
        /// </returns>
        private static bool IsTableAvailable(SqlConnection connection, string tableName)
        {
            // <lang>
            //   <zh-CN>命令对象只在本 helper 生命周期内使用；tableName 来自私有常量，拼入 OBJECT_ID 前不接受外部输入。</zh-CN>
            //   <en>The command object is used only for this helper's lifetime; tableName comes from private constants and accepts no external input before being placed in OBJECT_ID.</en>
            // </lang>
            using (SqlCommand command = connection.CreateCommand())
            {
                // <lang>
                //   <zh-CN>固定元数据查询只返回 0/1 表存在事实，不读取设置值、审计值或连接信息。</zh-CN>
                //   <en>The fixed metadata query returns only the 0/1 table-existence fact and reads no setting value, audit value, or connection information.</en>
                // </lang>
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[" + tableName + "]', N'U') IS NULL THEN 0 ELSE 1 END;";

                // <lang>
                //   <zh-CN>标量结果空值或非 1 均按不可用处理，避免部分迁移状态被误判为可安全写入。</zh-CN>
                //   <en>Treat a null scalar result or anything other than 1 as unavailable, avoiding a partial migration state being misclassified as safely writable.</en>
                // </lang>
                object value = command.ExecuteScalar();
                return value != null && Convert.ToInt32(value) == 1;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在写入事务内锁定并读取当前覆盖值及删除保护标志。</zh-CN>
        ///   <en>Locks and reads the current override value and deletion-protection flag inside a write transaction.</en>
        /// </lang>
        /// </summary>
        /// <param name="connection">
        ///   <l>
        ///     <zh-CN>与事务关联的已打开数据库连接。</zh-CN>
        ///     <en>Opened database connection associated with the transaction.</en>
        ///   </l>
        /// </param>
        /// <param name="transaction">
        ///   <l>
        ///     <zh-CN>保护读取、变更和审计原子性的当前事务。</zh-CN>
        ///     <en>Current transaction protecting atomic read, change, and audit.</en>
        ///   </l>
        /// </param>
        /// <param name="settingKey">
        ///   <l>
        ///     <zh-CN>通过定义门禁的稳定设置键。</zh-CN>
        ///     <en>Stable setting key that passed the definition gate.</en>
        ///   </l>
        /// </param>
        /// <param name="exists">
        ///   <l>
        ///     <zh-CN>找到锁定行时为 true。</zh-CN>
        ///     <en>True when a locked row is found.</en>
        ///   </l>
        /// </param>
        /// <param name="currentValue">
        ///   <l>
        ///     <zh-CN>找到时的当前值；数据库 NULL 时为 null。</zh-CN>
        ///     <en>Current value when found, or null for database NULL.</en>
        ///   </l>
        /// </param>
        /// <param name="canDelete">
        ///   <l>
        ///     <zh-CN>找到行且数据库删除保护允许时为 true。</zh-CN>
        ///     <en>True when a row is found and database deletion protection permits removal.</en>
        ///   </l>
        /// </param>
        private static void ReadCurrentValueForUpdate(
            SqlConnection connection,
            SqlTransaction transaction,
            string settingKey,
            out bool exists,
            out string currentValue,
            out bool canDelete)
        {
            // <lang>
            //   <zh-CN>先设置稳定“未找到/不可删除”输出，保证 reader 无行或异常前的调用方不会使用未初始化状态。</zh-CN>
            //   <en>Set stable not-found/not-deletable outputs first so a caller cannot use uninitialized state before a reader has rows or an exception occurs.</en>
            // </lang>
            exists = false;
            currentValue = null;
            canDelete = false;

            // <lang>
            //   <zh-CN>命令绑定当前事务并使用 UPDLOCK、HOLDLOCK 读取单键行，将存在性、旧值和删除保护与随后更新/删除串行化。</zh-CN>
            //   <en>Bind the command to the current transaction and read the single key with UPDLOCK and HOLDLOCK, serializing existence, old value, and deletion protection with the later update or delete.</en>
            // </lang>
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
                    // <lang>
                    //   <zh-CN>未找到行保留初始输出并返回；保存路径据此插入，删除路径据此幂等成功。</zh-CN>
                    //   <en>When no row is found, retain initial outputs and return; the save path then inserts while the delete path treats it as idempotent success.</en>
                    // </lang>
                    if (!reader.Read())
                    {
                        return;
                    }

                    // <lang>
                    //   <zh-CN>读取结果只保存在事务局部变量：当前值供审计前值使用，CanDelete 仅控制删除路径，均不进入用户消息。</zh-CN>
                    //   <en>Keep read results only in transaction-local outputs: current value supplies the audit before-image and CanDelete controls only deletion, and neither enters a user message.</en>
                    // </lang>
                    exists = true;
                    currentValue = reader.IsDBNull(0) ? null : reader.GetString(0);
                    canDelete = !reader.IsDBNull(1) && reader.GetBoolean(1);
                }
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在当前设置变更事务内写入可追溯审计记录。</zh-CN>
        ///   <en>Writes a traceable audit record inside the current setting-change transaction.</en>
        /// </lang>
        /// </summary>
        /// <param name="connection">
        ///   <l>
        ///     <zh-CN>与事务关联的已打开数据库连接。</zh-CN>
        ///     <en>Opened database connection associated with the transaction.</en>
        ///   </l>
        /// </param>
        /// <param name="transaction">
        ///   <l>
        ///     <zh-CN>尚未提交的当前设置变更事务。</zh-CN>
        ///     <en>Current uncommitted setting-change transaction.</en>
        ///   </l>
        /// </param>
        /// <param name="settingKey">
        ///   <l>
        ///     <zh-CN>受控稳定设置键。</zh-CN>
        ///     <en>Controlled stable setting key.</en>
        ///   </l>
        /// </param>
        /// <param name="changeType">
        ///   <l>
        ///     <zh-CN>调用方提供的受控 Insert、Update 或 Delete 变更类型。</zh-CN>
        ///     <en>Caller-supplied controlled Insert, Update, or Delete change type.</en>
        ///   </l>
        /// </param>
        /// <param name="oldValue">
        ///   <l>
        ///     <zh-CN>变更前的数据库值，可为 null。</zh-CN>
        ///     <en>Database value before the change, which may be null.</en>
        ///   </l>
        /// </param>
        /// <param name="newValue">
        ///   <l>
        ///     <zh-CN>变更后的数据库值；删除时为 null。</zh-CN>
        ///     <en>Database value after the change; null for deletion.</en>
        ///   </l>
        /// </param>
        /// <param name="context">
        ///   <l>
        ///     <zh-CN>用于受限操作者和客户端审计字段的可选 HTTP 上下文。</zh-CN>
        ///     <en>Optional HTTP context for restricted actor and client audit fields.</en>
        ///   </l>
        /// </param>
        private static void WriteAudit(
            SqlConnection connection,
            SqlTransaction transaction,
            string settingKey,
            string changeType,
            string oldValue,
            string newValue,
            HttpContext context)
        {
            // <lang>
            //   <zh-CN>审计命令绑定与业务变更相同的事务；任何审计写入失败都会阻止外层提交，避免形成不可追溯的在线变化。</zh-CN>
            //   <en>Bind the audit command to the same transaction as the business change; any audit-write failure prevents outer commit, avoiding an untraceable online change.</en>
            // </lang>
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

                // <lang>
                //   <zh-CN>设置键、固定变更类型、旧/新值和受限请求事实均使用参数；ChangeReason 与 CorrelationId 保持数据库 NULL，不从任意请求内容拼接 SQL。</zh-CN>
                //   <en>Use parameters for setting key, controlled change type, old/new values, and restricted request facts; ChangeReason and CorrelationId remain database NULL and no arbitrary request content is concatenated into SQL.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取用于设置审计的已认证操作者名或固定匿名回退。</zh-CN>
        ///   <en>Gets the authenticated actor name for setting audit or a fixed anonymous fallback.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        ///   <l>
        ///     <zh-CN>调用方提供的可选 HTTP 上下文。</zh-CN>
        ///     <en>Optional HTTP context supplied by the caller.</en>
        ///   </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已认证身份名，或 <c>(anonymous)</c>。</zh-CN>
        ///   <en>Authenticated identity name, or <c>(anonymous)</c>.</en>
        /// </l>
        /// </returns>
        private static string GetActorUserName(HttpContext context)
        {
            // <lang>
            //   <zh-CN>优先使用显式上下文，缺省时才读取当前上下文；审计 helper 不创建身份，也不因缺少请求而抛出。</zh-CN>
            //   <en>Prefer explicit context and read current context only when absent; the audit helper creates no identity and does not throw when a request is missing.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;

            // <lang>
            //   <zh-CN>没有完整的已认证身份时使用固定匿名标记，避免写入 null、空白或未经认证的名称。</zh-CN>
            //   <en>Use the fixed anonymous marker when there is no complete authenticated identity, avoiding a null, blank, or unauthenticated name being written.</en>
            // </lang>
            if (current == null || current.User == null || current.User.Identity == null ||
                !current.User.Identity.IsAuthenticated)
            {
                return "(anonymous)";
            }

            // <lang>
            //   <zh-CN>仅在身份已认证时返回既有身份名；该 helper 不验证其角色、权限或是否有权执行当前页面操作。</zh-CN>
            //   <en>Return the established identity name only when authenticated; this helper does not verify its roles, permissions, or authority for the current page operation.</en>
            // </lang>
            return current.User.Identity.Name;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取用于设置审计的客户端 IP 文本。</zh-CN>
        ///   <en>Gets client IP text for setting audit.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        ///   <l>
        ///     <zh-CN>调用方提供的可选 HTTP 上下文。</zh-CN>
        ///     <en>Optional HTTP context supplied by the caller.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>当前请求的 UserHostAddress；上下文或请求缺失时为空字符串。</zh-CN>
        ///     <en>Current request UserHostAddress, or empty string when context or request is absent.</en>
        ///   </l>
        /// </returns>
        private static string GetClientIp(HttpContext context)
        {
            // <lang>
            //   <zh-CN>与操作者审计使用同一显式优先上下文规则；没有请求时返回稳定空值而不尝试解析转发头或网络连接。</zh-CN>
            //   <en>Use the same explicit-context-first rule as actor audit; return a stable empty value without parsing forwarded headers or network connections when no request exists.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;
            return current == null || current.Request == null ? string.Empty : current.Request.UserHostAddress;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取用于设置审计的客户端 User-Agent 文本。</zh-CN>
        ///   <en>Gets client User-Agent text for setting audit.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        ///   <l>
        ///     <zh-CN>调用方提供的可选 HTTP 上下文。</zh-CN>
        ///     <en>Optional HTTP context supplied by the caller.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>当前请求的 UserAgent；上下文或请求缺失时为空字符串。</zh-CN>
        ///     <en>Current request UserAgent, or empty string when context or request is absent.</en>
        ///   </l>
        /// </returns>
        private static string GetUserAgent(HttpContext context)
        {
            // <lang>
            //   <zh-CN>只读取既有请求字段并允许缺失；长度限制与净化由随后参数 helper 承担，避免此 helper 改写原始 HTTP 对象。</zh-CN>
            //   <en>Read only the established request field and allow it to be absent; the following parameter helper owns length limiting and sanitization, so this helper does not rewrite the original HTTP object.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;
            return current == null || current.Request == null ? string.Empty : current.Request.UserAgent;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加指定长度的净化文本 SQL 参数。</zh-CN>
        ///   <en>Adds a sanitized text SQL parameter with a specified length.</en>
        /// </lang>
        /// </summary>
        /// <param name="command">
        ///   <l>
        ///     <zh-CN>接收参数的当前 SQL 命令。</zh-CN>
        ///     <en>Current SQL command receiving the parameter.</en>
        ///   </l>
        /// </param>
        /// <param name="parameterName">
        ///   <l>
        ///     <zh-CN>调用方提供的固定参数名。</zh-CN>
        ///     <en>Fixed parameter name supplied by the caller.</en>
        ///   </l>
        /// </param>
        /// <param name="size">
        ///   <l>
        ///     <zh-CN>数据库列与参数的最大字符数。</zh-CN>
        ///     <en>Maximum characters for the database column and parameter.</en>
        ///   </l>
        /// </param>
        /// <param name="value">
        ///   <l>
        ///     <zh-CN>待净化和截断的文本。</zh-CN>
        ///     <en>Text to sanitize and truncate.</en>
        ///   </l>
        /// </param>
        /// <param name="fallback">
        ///   <l>
        ///     <zh-CN>净化后为空白时写入的受控回退文本。</zh-CN>
        ///     <en>Controlled fallback text written when the sanitized result is blank.</en>
        ///   </l>
        /// </param>
        private static void AddTextParameter(
            SqlCommand command,
            string parameterName,
            int size,
            string value,
            string fallback)
        {
            // <lang>
            //   <zh-CN>先按目标参数大小净化并截断文本；这同时限制请求审计字段和稳定键/类型字段进入 SQL 前的长度与控制字符。</zh-CN>
            //   <en>Sanitize and truncate text to the target parameter size first; this limits length and control characters for request-audit fields and stable key/type fields before they enter SQL.</en>
            // </lang>
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);

            // <lang>
            //   <zh-CN>始终添加带显式 NVarChar 长度的参数；空白净化结果改用调用方受控回退，而不以内联文本拼接 SQL。</zh-CN>
            //   <en>Always add a parameter with explicit NVarChar length; replace a blank sanitized result with caller-controlled fallback rather than concatenating inline text into SQL.</en>
            // </lang>
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加允许数据库 NULL 的无限长度文本 SQL 参数。</zh-CN>
        ///   <en>Adds an unlimited-length text SQL parameter that permits database NULL.</en>
        /// </lang>
        /// </summary>
        /// <param name="command">
        ///   <l>
        ///     <zh-CN>接收参数的当前 SQL 命令。</zh-CN>
        ///     <en>Current SQL command receiving the parameter.</en>
        ///   </l>
        /// </param>
        /// <param name="parameterName">
        ///   <l>
        ///     <zh-CN>调用方提供的固定参数名。</zh-CN>
        ///     <en>Fixed parameter name supplied by the caller.</en>
        ///   </l>
        /// </param>
        /// <param name="value">
        ///   <l>
        ///     <zh-CN>设置审计的旧值或新值；空值映射为数据库 NULL。</zh-CN>
        ///     <en>Old or new value for setting audit; null maps to database NULL.</en>
        ///   </l>
        /// </param>
        private static void AddUnlimitedTextParameter(SqlCommand command, string parameterName, string value)
        {
            // <lang>
            //   <zh-CN>该 helper 只由已通过非敏感定义门禁的设置审计调用；使用参数化 NVarChar(max) 保留数据库 NULL 语义，不把值内联到 SQL。</zh-CN>
            //   <en>This helper is called only by setting audit after the non-sensitive definition gate; use parameterized NVarChar(max) to preserve database NULL semantics and never inline the value into SQL.</en>
            // </lang>
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, -1).Value =
                string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;
        }
    }
}
