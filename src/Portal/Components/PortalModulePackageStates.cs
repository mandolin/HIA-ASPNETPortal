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
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一个模块包运行状态快照。</zh-CN>
        ///   <en>Creates a runtime state snapshot for a module package.</en>
        /// </lang>
        /// </summary>
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
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建模块包状态读取结果。</zh-CN>
        ///   <en>Creates a module-package state read result.</en>
        /// </lang>
        /// </summary>
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
    /// <lang>
    ///   <zh-CN>模块包状态写入结果。</zh-CN>
    ///   <en>Result of writing module-package state.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalModulePackageStateWriteResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建模块包状态写入结果。</zh-CN>
        ///   <en>Creates a module-package state write result.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded"><l zh-CN="状态表事务是否已成功提交；不证明模块已被当前 Profile 允许或实际加载。" en="Whether the state-table transaction committed; it does not prove the module is allowed by the current profile or actually loaded." /></param>
        /// <param name="message"><l zh-CN="可安全展示给管理员的结果说明；空值归一为稳定空文本。" en="Result text safe to show an administrator; null is normalized to stable empty text." /></param>
        internal PortalModulePackageStateWriteResult(bool succeeded, string message)
        {
            // <lang>
            //   <zh-CN>该布尔值仅陈述本存储的持久化结果；页面授权、受信任部署校验、Profile gate 和模块加载由其他边界负责。</zh-CN>
            //   <en>This boolean states only the persistence result of this store; page authorization, trusted-deployment validation, profile gating, and module loading belong to other boundaries.</en>
            // </lang>
            Succeeded = succeeded;

            // <lang>
            //   <zh-CN>消息是可展示的受控输出，空值不保留为 null，避免消费页面将其误作缺少失败处理或回显异常细节。</zh-CN>
            //   <en>The message is controlled display-safe output; normalize null rather than leaving it as missing failure handling or an opportunity to echo exception detail in a consuming page.</en>
            // </lang>
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>状态表写入事务是否成功完成。</zh-CN>
        ///   <en>Whether the state-table write transaction completed successfully.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可安全展示给管理员的结果说明，不包含连接串、SQL 或诊断异常详情。</zh-CN>
        ///   <en>Result message safe to show an administrator; it contains no connection-string, SQL, or diagnostic exception detail.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>已部署模块包的受限启用状态存储。</zh-CN>
    ///   <en>Restricted enabled-state store for deployed module packages.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本存储不写入模块文件。读取状态表不存在或失败时，已验证包保持默认启用，以避免未迁移旧库阻断门户；后台写入会明确失败并提示执行迁移。<see cref="Save"/> 仅验证包标识格式，调用方必须先通过 <see cref="PortalModuleCatalog.TryGetTrustedPackage"/> 确认受信任部署包存在；现有目录页已遵守此顺序。</zh-CN>
    ///   <en>This store never writes module files. When the state table is missing or unreadable, validated packages remain enabled by default so an unmigrated legacy database cannot block the portal; administration writes fail explicitly and request migration. <see cref="Save"/> validates only package-id format, so callers must first confirm a trusted deployed package through <see cref="PortalModuleCatalog.TryGetTrustedPackage"/>; the existing catalog page follows that order.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModulePackageStates
    {
        // <lang>
        //   <zh-CN>模块包状态表的固定受控名称；私有 SQL helper 只使用该常量，不接受请求、配置或包标识作为表名。</zh-CN>
        //   <en>Fixed controlled name of the module-package state table; private SQL helpers use only this constant and never accept a request, configuration value, or package identifier as a table name.</en>
        // </lang>
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
        /// <lang>
        ///   <zh-CN>为一个已验证部署包保存启用或禁用状态。</zh-CN>
        ///   <en>Saves enabled or disabled state for one validated deployed package.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId"><l zh-CN="调用方已按受信任目录确认的稳定包标识；本方法仍只验证格式。" en="Stable package identifier already confirmed by the caller through the trusted catalog; this method still validates only its format." /></param>
        /// <param name="isEnabled"><l zh-CN="要保存的状态表启用标志；它不越过当前 Profile、部署或授权门禁。" en="Enabled flag to persist in the state table; it does not bypass current profile, deployment, or authorization gates." /></param>
        /// <param name="note"><l zh-CN="可选非敏感管理员备注；净化后为空时写入数据库 NULL。" en="Optional non-sensitive administrator note; it writes database NULL when empty after sanitization." /></param>
        /// <param name="context"><l zh-CN="用于受限操作人字段和异常诊断的可选 HTTP 上下文。" en="Optional HTTP context for restricted actor fields and exception diagnostics." /></param>
        /// <returns><l zh-CN="安全写入结果；不包含连接串、SQL 或异常详情。" en="Safe write result; it contains no connection-string, SQL, or exception detail." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此公共方法不自行确认 manifest 或物理部署目录。直接调用者若跳过目录校验，可能写入不会被运行时解析的孤立状态记录；这不会使未知包可加载，但应避免作为新的调用模式。状态成功也不表示当前请求会加载该包：Profile gate、受信任部署和页面授权仍独立生效。</zh-CN>
        ///   <en>This public method does not independently confirm a manifest or physical deployment directory. A direct caller that skips catalog validation can write an orphan state record that runtime resolution never uses; it does not make an unknown package loadable, but should be avoided as a new calling pattern. Successful state persistence also does not mean the current request loads the package: profile gating, trusted deployment, and page authorization remain independent.</en>
        /// </lang>
        /// </remarks>
        public static PortalModulePackageStateWriteResult Save(
            string packageId,
            bool isEnabled,
            string note,
            HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>仅拒绝格式非法的包标识，避免它进入参数化 SQL；该门禁不替代调用方的受信任 manifest/目录确认，也不进行页面或用户授权。</zh-CN>
            //   <en>Reject only a format-invalid package identifier before it reaches parameterized SQL; this gate does not replace the caller's trusted manifest/directory confirmation and performs no page or user authorization.</en>
            // </lang>
            if (!PortalModuleCatalog.IsValidPackageId(packageId))
            {
                return new PortalModulePackageStateWriteResult(false, "The module package identifier is invalid.");
            }

            try
            {
                // <lang>
                //   <zh-CN>连接从受控容器路径创建并在 using 结束时释放；本方法不记录连接串，也不把缺失来源转换为异常详情。</zh-CN>
                //   <en>Create the connection through the controlled container path and release it at the end of the using block; this method does not log the connection string or turn a missing source into exception detail.</en>
                // </lang>
                using (SqlConnection connection = CreateConnection())
                {
                    if (connection == null)
                    {
                        return new PortalModulePackageStateWriteResult(false, "The module package state database is unavailable.");
                    }

                    // <lang>
                    //   <zh-CN>写入不采用读取路径的默认启用回退：状态表未迁移或不可用时必须显式失败，不能静默改写其他配置来源或模块文件。</zh-CN>
                    //   <en>Writes do not use the read path's default-enabled fallback: when the state table is not migrated or unavailable, fail explicitly and never silently rewrite another configuration source or module files.</en>
                    // </lang>
                    connection.Open();
                    if (!IsTableAvailable(connection))
                    {
                        return new PortalModulePackageStateWriteResult(false, "Run the module-package migration before changing package state.");
                    }

                    // <lang>
                    //   <zh-CN>存在判断、更新/插入和提交共用一个事务；并发调用不会以未锁定普通读取推断写入分支，任何失败都不会得到成功结果。</zh-CN>
                    //   <en>Existence check, update/insert, and commit share one transaction; concurrent calls do not infer the write branch from an unlocked ordinary read, and any failure cannot receive a success result.</en>
                    // </lang>
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        // <lang>
                        //   <zh-CN>该标志来自事务内带锁存在判断，只决定更新既有状态行还是插入新状态行；它不判断包是否受信任、Profile 允许或当前请求可加载。</zh-CN>
                        //   <en>This flag comes from the locked existence check inside the transaction and selects update of an existing state row versus insertion of a new one; it does not decide whether the package is trusted, profile-allowed, or loadable in the current request.</en>
                        // </lang>
                        bool exists = ExistsForUpdate(connection, transaction, packageId);

                        // <lang>
                        //   <zh-CN>命令被绑定到同一事务；固定 SQL 只改变状态事实、非敏感备注、操作人和 UTC 时间，所有可变输入均由参数传入。</zh-CN>
                        //   <en>The command is bound to the same transaction; fixed SQL changes only state fact, non-sensitive note, actor, and UTC time, while every variable input is supplied through parameters.</en>
                        // </lang>
                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;

                            // <lang>
                            //   <zh-CN>锁定存在结果选择 update 或 insert，不动态拼接包标识、备注或请求内容；两条 SQL 保持同一受控表和字段集。</zh-CN>
                            //   <en>The locked existence result selects update or insert without dynamically concatenating package identifier, note, or request content; both SQL paths keep the same controlled table and field set.</en>
                            // </lang>
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

                            // <lang>
                            //   <zh-CN>包标识、启用标志、备注、操作人和 UTC 事实均作为类型化参数写入；备注可为空并映射数据库 NULL，不能以自由文本改变 SQL 结构。</zh-CN>
                            //   <en>Package identifier, enabled flag, note, actor, and UTC fact all write as typed parameters; the note may be empty and map to database NULL, and free text cannot alter SQL structure.</en>
                            // </lang>
                            AddTextParameter(command, "@PackageId", 100, packageId, string.Empty);
                            command.Parameters.Add("@IsEnabled", SqlDbType.Bit).Value = isEnabled;
                            AddNullableTextParameter(command, "@Note", 500, note);
                            AddTextParameter(command, "@UpdatedBy", 100, GetActorUserName(context), "(anonymous)");
                            command.Parameters.Add("@UpdatedUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                            command.ExecuteNonQuery();
                        }

                        // <lang>
                        //   <zh-CN>仅在本事务的状态命令已完成后提交；提交证明持久化状态已完成，不代表物理部署、Profile 或授权结果。</zh-CN>
                        //   <en>Commit only after the state command completed within this transaction; commit proves persisted state completion, not physical deployment, profile, or authorization outcome.</en>
                        // </lang>
                        transaction.Commit();
                    }
                }

                // <lang>
                //   <zh-CN>成功消息只陈述保存的启用状态，供目录页显示；不承诺即时加载、实例变更、文件操作或跨请求缓存失效。</zh-CN>
                //   <en>The success message states only the saved enabled state for catalog display; it does not promise immediate loading, instance changes, file operations, or cross-request cache invalidation.</en>
                // </lang>
                return new PortalModulePackageStateWriteResult(
                    true,
                    isEnabled ? "The module package was enabled." : "The module package was disabled.");
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>诊断层接收异常和可选上下文以保留受限运营证据；调用方只得到固定安全消息，不获得连接串、SQL 或异常文本。</zh-CN>
                //   <en>The diagnostic layer receives the exception and optional context for restricted operational evidence; the caller receives only a fixed safe message, never connection string, SQL, or exception text.</en>
                // </lang>
                PortalDiagnostics.Error("ModulePackageState.Save", "Saving a module package state failed.", exception, context);
                return new PortalModulePackageStateWriteResult(false, "The module package state could not be saved. Check diagnostics for the event id.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从受控 Unity 依赖解析运行期连接串并创建未打开的 SQL Server 连接。</zh-CN>
        ///   <en>Resolves the runtime connection string from the controlled Unity dependency and creates an unopened SQL Server connection.</en>
        /// </lang>
        /// </summary>
        /// <returns><l zh-CN="未打开的连接对象；容器或连接串不可用时为 null。" en="Unopened connection object, or null when the container or connection string is unavailable." /></returns>
        private static SqlConnection CreateConnection()
        {
            // <lang>
            //   <zh-CN>容器尚未初始化时安全返回 null，让公开写入路径给出受控可展示结果，而不是在 helper 中读取或暴露全局配置细节。</zh-CN>
            //   <en>When the container is not initialized, return null safely so the public write path gives a controlled display-safe result rather than this helper reading or exposing global configuration detail.</en>
            // </lang>
            if (Global.Container == null)
            {
                return null;
            }

            // <lang>
            //   <zh-CN>连接串只在创建连接对象的短生命周期内使用；不记录、不返回文本，空白来源映射为 null 以由调用方统一失败处理。</zh-CN>
            //   <en>Use the connection string only during the short lifetime of creating the connection object; do not log or return the text, and map a blank source to null for uniform caller failure handling.</en>
            // </lang>
            string connectionString = Global.Container.Resolve<string>(ExternalConnectionStringLoader.UnityConnectionStringName);
            return string.IsNullOrWhiteSpace(connectionString) ? null : new SqlConnection(connectionString);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查固定模块包状态表是否已部署为用户表。</zh-CN>
        ///   <en>Checks whether the fixed module-package state table has been deployed as a user table.</en>
        /// </lang>
        /// </summary>
        /// <param name="connection"><l zh-CN="已由调用方打开的数据库连接。" en="Database connection already opened by the caller." /></param>
        /// <returns><l zh-CN="固定状态表存在时为 true。" en="True when the fixed state table exists." /></returns>
        private static bool IsTableAvailable(SqlConnection connection)
        {
            // <lang>
            //   <zh-CN>命令只在 helper 生命周期内使用；表名来自私有常量，拼入 OBJECT_ID 前不接受外部输入或动态 SQL 片段。</zh-CN>
            //   <en>The command is used only for this helper's lifetime; the table name comes from a private constant and accepts no external input or dynamic SQL fragment before placement in OBJECT_ID.</en>
            // </lang>
            using (SqlCommand command = connection.CreateCommand())
            {
                // <lang>
                //   <zh-CN>存在性查询只回答迁移前置是否满足；不会创建表、修复 schema 或把缺失表视为可写状态。</zh-CN>
                //   <en>The existence query answers only whether the migration prerequisite is met; it does not create a table, repair schema, or treat a missing table as writable state.</en>
                // </lang>
                command.CommandText =
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[" + TableName + "]', N'U') IS NULL THEN 0 ELSE 1 END;";

                // <lang>
                //   <zh-CN>数据库标量结果只映射为布尔部署事实；null 或非 1 均安全视为不可用，随后由公开写入路径返回迁移提示。</zh-CN>
                //   <en>Map the database scalar result only to a boolean deployment fact; null or a value other than 1 safely means unavailable, after which the public write path returns migration guidance.</en>
                // </lang>
                object value = command.ExecuteScalar();
                return value != null && Convert.ToInt32(value) == 1;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在事务中锁定并判断目标包状态行是否存在，以选择更新或插入。</zh-CN>
        ///   <en>Locks and determines whether the target package-state row exists within a transaction to select update or insert.</en>
        /// </lang>
        /// </summary>
        /// <param name="connection"><l zh-CN="当前已打开的数据库连接。" en="Current opened database connection." /></param>
        /// <param name="transaction"><l zh-CN="包状态写入所使用的当前事务。" en="Current transaction used for package-state write." /></param>
        /// <param name="packageId"><l zh-CN="已通过格式门禁的稳定包标识。" en="Stable package identifier that passed the format gate." /></param>
        /// <returns><l zh-CN="锁定状态行存在时为 true。" en="True when the locked state row exists." /></returns>
        private static bool ExistsForUpdate(SqlConnection connection, SqlTransaction transaction, string packageId)
        {
            // <lang>
            //   <zh-CN>存在性命令显式绑定写入事务，确保锁定读取与随后 update/insert 共享同一隔离边界。</zh-CN>
            //   <en>Bind the existence command explicitly to the write transaction so the locked read and following update/insert share one isolation boundary.</en>
            // </lang>
            using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                // <lang>
                //   <zh-CN>UPDLOCK/HOLDLOCK 保护同一包标识的判断到提交；包标识仍以参数传入，不以内联文本改变锁定查询。</zh-CN>
                //   <en>UPDLOCK/HOLDLOCK protects the decision for the same package identifier through commit; the package identifier remains a parameter and never changes the locked query through inline text.</en>
                // </lang>
                command.CommandText = @"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM [dbo].[PortalCfg_ModulePackageStates] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PackageId] = @PackageId
) THEN 1 ELSE 0 END;";
                AddTextParameter(command, "@PackageId", 100, packageId, string.Empty);

                // <lang>
                //   <zh-CN>只把受控 CASE 标量转换为分支事实；它不读取包 manifest、Profile 或模块文件，也不泄露数据库行内容。</zh-CN>
                //   <en>Convert only the controlled CASE scalar to the branch fact; it reads no package manifest, profile, or module file and exposes no database row content.</en>
                // </lang>
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前认证操作人用户名，未认证或缺少上下文时使用匿名占位值。</zh-CN>
        ///   <en>Reads current authenticated actor user name, using an anonymous placeholder when unauthenticated or context is absent.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><l zh-CN="调用方提供的可选 HTTP 上下文。" en="Optional HTTP context supplied by the caller." /></param>
        /// <returns><l zh-CN="认证身份名，或固定的匿名占位值。" en="Authenticated identity name, or the fixed anonymous placeholder." /></returns>
        private static string GetActorUserName(HttpContext context)
        {
            // <lang>
            //   <zh-CN>优先使用显式上下文，缺省时才读取当前上下文；该 helper 不创建身份、不检查权限，也不因后台任务缺少请求而抛出。</zh-CN>
            //   <en>Prefer explicit context and read current context only when absent; this helper creates no identity, checks no permission, and does not throw when a background task lacks a request.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;

            // <lang>
            //   <zh-CN>只有已认证身份可进入审计字段；所有缺失或未认证组合归一为固定匿名值，避免写入 null 或猜测用户来源。</zh-CN>
            //   <en>Only an authenticated identity enters the audit field; every missing or unauthenticated combination normalizes to the fixed anonymous value rather than writing null or guessing a user source.</en>
            // </lang>
            if (current == null || current.User == null || current.User.Identity == null ||
                !current.User.Identity.IsAuthenticated)
            {
                return "(anonymous)";
            }

            return current.User.Identity.Name;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加必填文本参数，并使用诊断清理器限制长度和内容。</zh-CN>
        ///   <en>Adds a required text parameter while using the diagnostic sanitizer to limit length and content.</en>
        /// </lang>
        /// </summary>
        /// <param name="command"><l zh-CN="接收参数的当前 SQL 命令。" en="Current SQL command receiving the parameter." /></param>
        /// <param name="parameterName"><l zh-CN="调用方提供的固定参数名。" en="Fixed parameter name supplied by the caller." /></param>
        /// <param name="size"><l zh-CN="数据库列和参数的最大字符数。" en="Maximum characters for the database column and parameter." /></param>
        /// <param name="value"><l zh-CN="待净化和截断的文本。" en="Text to sanitize and truncate." /></param>
        /// <param name="fallback"><l zh-CN="净化后为空白时写入的受控回退文本。" en="Controlled fallback text written when the sanitized result is blank." /></param>
        private static void AddTextParameter(
            SqlCommand command,
            string parameterName,
            int size,
            string value,
            string fallback)
        {
            // <lang>
            //   <zh-CN>先按目标列长度净化并截断外来文本；此 helper 不记录原值，避免包标识、操作人或备注污染日志或参数结构。</zh-CN>
            //   <en>Sanitize and truncate external text to the target column length first; this helper logs no raw value, preventing package identifier, actor, or note from contaminating logs or parameter structure.</en>
            // </lang>
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);

            // <lang>
            //   <zh-CN>参数名称和类型由调用方固定，净化空白使用受控回退而不是内联文本；所有结果仍作为 NVarChar 参数绑定。</zh-CN>
            //   <en>Parameter name and type are fixed by the caller; sanitized blank text uses a controlled fallback rather than inline text, and every result remains bound as an NVarChar parameter.</en>
            // </lang>
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加可空文本参数，空值写入数据库 NULL。</zh-CN>
        ///   <en>Adds a nullable text parameter and writes database NULL for empty values.</en>
        /// </lang>
        /// </summary>
        /// <param name="command"><l zh-CN="接收参数的当前 SQL 命令。" en="Current SQL command receiving the parameter." /></param>
        /// <param name="parameterName"><l zh-CN="调用方提供的固定参数名。" en="Fixed parameter name supplied by the caller." /></param>
        /// <param name="size"><l zh-CN="数据库列和参数的最大字符数。" en="Maximum characters for the database column and parameter." /></param>
        /// <param name="value"><l zh-CN="可为空的待净化和截断文本。" en="Nullable text to sanitize and truncate." /></param>
        private static void AddNullableTextParameter(SqlCommand command, string parameterName, int size, string value)
        {
            // <lang>
            //   <zh-CN>管理员备注先按列长度净化；该文本只能作为状态元数据，不参与包验证、Profile 解析或 SQL 拼接。</zh-CN>
            //   <en>Sanitize the administrator note to the column length first; this text is state metadata only and participates in neither package validation, profile resolution, nor SQL concatenation.</en>
            // </lang>
            string sanitized = PortalDiagnosticSanitizer.SanitizeAndTruncate(value, size);

            // <lang>
            //   <zh-CN>净化后空白保留为数据库 NULL，以区分没有备注和包含受控文本的备注；参数化绑定不暴露原始输入。</zh-CN>
            //   <en>Keep post-sanitization blank text as database NULL to distinguish no note from a note containing controlled text; parameterized binding exposes no raw input.</en>
            // </lang>
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(sanitized) ? (object)DBNull.Value : sanitized;
        }
    }
}
