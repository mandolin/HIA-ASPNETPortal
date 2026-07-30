using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>运行期系统设置读取助手。</zh-CN>
    ///   <en>Runtime helper for reading system settings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>允许在线管理的非敏感设置优先采用数据库覆盖值，其后为 appSettings 和代码默认值。 读取失败、缺表或值无效时安全回退，并对每种回退原因仅记录一次诊断警告。</zh-CN>
    ///   <en>Eligible non-sensitive settings prefer database overrides, followed by appSettings and code defaults. Read failures, missing tables, and invalid values fall back safely; each fallback reason is logged as a diagnostic warning only once.</en>
    /// </lang>
    /// </remarks>
    public static class PortalRuntimeSettings
    {
        // <lang>
        //   <zh-CN>进程内锁只保护“某键/原因是否已告警”的集合，避免并发回退重复写入诊断；它不锁设置读取或数据库访问。</zh-CN>
        //   <en>The in-process lock protects only the “key/reason was warned” set to avoid duplicate diagnostics under concurrent fallbacks; it does not lock settings reads or database access.</en>
        // </lang>
        private static readonly object WarningLock = new object();

        // <lang>
        //   <zh-CN>集合只保存稳定设置键和受控回退原因，不保存原始配置值、连接串或 HTTP 数据。</zh-CN>
        //   <en>The set stores only stable setting keys and controlled fallback reasons, never raw configuration values, connection strings, or HTTP data.</en>
        // </lang>
        private static readonly HashSet<string> WarnedDatabaseFallbacks =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取一个设置的有效文本值及其来源层级。</zh-CN>
        ///   <en>Gets a setting's effective text value and source layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>已登记的设置元数据定义，不能为 <c>null</c>。</zh-CN>
        ///   <en>Registered setting metadata definition; cannot be <c>null</c>.</en>
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
        ///   <zh-CN>已通过基础类型和范围校验的有效值。</zh-CN>
        ///   <en>Effective value that passed basic type and range validation.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN><paramref name="definition"/> 为 <c>null</c> 时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="definition"/> is <c>null</c>.</en>
        /// </l>
        /// </exception>
        public static PortalRuntimeSettingValue GetEffectiveValue(
            PortalSettingDefinition definition,
            HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>在访问任何来源前拒绝空定义，确保后续键、类型、默认值和权限元数据都来自已登记契约。</zh-CN>
            //   <en>Reject a null definition before accessing any source so later key, type, default, and permission metadata all come from the registered contract.</en>
            // </lang>
            EnsureDefinition(definition);

            PortalSystemSettingReadResult databaseResult = null;
            if (definition.CanEditOnline && !definition.IsSensitive)
            {
                // <lang>
                //   <zh-CN>只有可在线编辑且非敏感的定义可尝试数据库覆盖；敏感或部署级设置直接跳过数据库，避免在线层成为秘密读取通道。</zh-CN>
                //   <en>Only online-editable, non-sensitive definitions may attempt a database override; sensitive or deployment-level settings bypass the database to prevent the online layer from becoming a secret-read channel.</en>
                // </lang>
                databaseResult = PortalSystemSettingsStore.Read(definition.Key, context);
                if (databaseResult.IsAvailable && databaseResult.IsFound)
                {
                    string databaseValue;
                    if (string.Equals(databaseResult.ValueType, definition.ValueType.ToString(), StringComparison.Ordinal) &&
                        TryNormalizeValue(definition, databaseResult.Value, out databaseValue))
                    {
                        // <lang>
                        //   <zh-CN>数据库值必须同时匹配已登记值类型并通过同一规范化/范围门禁，才可成为有效值；不信任存储层的文本本身。</zh-CN>
                        //   <en>A database value becomes effective only when it matches the registered value type and passes the same normalization/range gate; do not trust storage-layer text by itself.</en>
                        // </lang>
                        return new PortalRuntimeSettingValue(
                            databaseValue,
                            PortalRuntimeSettingSource.Database);
                    }

                    WarnDatabaseFallback(
                        definition.Key,
                        "数据库设置值的类型或内容无效。 Database setting value is invalid.",
                        context);
                }
                else if (!databaseResult.IsAvailable)
                {
                    // <lang>
                    //   <zh-CN>覆盖表不可用时仅记录一次受控回退事实，继续较低优先级来源而不把数据库错误详情或连接信息带入返回值。</zh-CN>
                    //   <en>When the override table is unavailable, record only one controlled fallback fact and continue to lower-priority sources without putting database-error detail or connection information into the return value.</en>
                    // </lang>
                    WarnDatabaseFallback(
                        definition.Key,
                        "数据库设置表不可用。 Database setting table is unavailable.",
                        context);
                }
            }

            string configuredValue;
            if (TryNormalizeValue(
                definition,
                ConfigurationManager.AppSettings[definition.Key],
                out configuredValue))
            {
                // <lang>
                //   <zh-CN>appSettings 仅在通过已登记类型/范围规范化后生效；空白或无效文本自然继续回退。</zh-CN>
                //   <en>AppSettings becomes effective only after registered type/range normalization; blank or invalid text naturally continues fallback.</en>
                // </lang>
                return new PortalRuntimeSettingValue(
                    configuredValue,
                    PortalRuntimeSettingSource.AppSettings);
            }

            string defaultValue;
            if (TryNormalizeValue(definition, definition.DefaultValue, out defaultValue))
            {
                // <lang>
                //   <zh-CN>代码默认值经过同一门禁后才作为最终常规回退，保持各来源的解析规则一致。</zh-CN>
                //   <en>The code default becomes the final normal fallback only after the same gate, keeping parsing rules consistent across sources.</en>
                // </lang>
                return new PortalRuntimeSettingValue(defaultValue, PortalRuntimeSettingSource.Default);
            }

            // <lang>
            //   <zh-CN>仅当定义自身默认值也无法规范化时返回稳定空字符串和 Default 来源；不臆造来源值或抛出原始设置文本。</zh-CN>
            //   <en>Return stable empty text with Default source only when even the definition default cannot normalize; do not invent a source value or throw raw settings text.</en>
            // </lang>
            return new PortalRuntimeSettingValue(string.Empty, PortalRuntimeSettingSource.Default);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取定义的最终文本表示；空白或不可用值回退至更低优先级来源。</zh-CN>
        ///   <en>Reads the definition's effective text representation; blank or unavailable values fall back to lower-priority sources.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>已登记的设置定义。</zh-CN>
        ///   <en>Registered setting definition.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最终规范化文本值。</zh-CN>
        ///   <en>Final normalized text value.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN><paramref name="definition"/> 为 <c>null</c> 时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="definition"/> is <c>null</c>.</en>
        /// </l>
        /// </exception>
        public static string GetString(PortalSettingDefinition definition)
        {
            // <lang>
            //   <zh-CN>先保持与其它读取入口一致的空定义门禁，再仅返回已解析值文本；不暴露来源内部状态或绕过规范化。</zh-CN>
            //   <en>Keep the same null-definition gate as other read entry points, then return only resolved value text without exposing source internals or bypassing normalization.</en>
            // </lang>
            EnsureDefinition(definition);

            return GetEffectiveValue(definition).Value;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取布尔设置；非法值回退至更低优先级来源。</zh-CN>
        ///   <en>Reads a Boolean setting; invalid values fall back to lower-priority sources.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>值类型必须为 <see cref="PortalSettingValueType.Boolean"/> 的已登记定义。</zh-CN>
        ///   <en>Registered definition whose value type must be <see cref="PortalSettingValueType.Boolean"/>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最终布尔值。</zh-CN>
        ///   <en>Final Boolean value.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN><paramref name="definition"/> 为 <c>null</c> 时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="definition"/> is <c>null</c>.</en>
        /// </l>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>定义值类型不是布尔值时引发。</zh-CN>
        ///   <en>Thrown when the definition is not Boolean.</en>
        /// </l>
        /// </exception>
        public static bool GetBoolean(PortalSettingDefinition definition)
        {
            // <lang>
            //   <zh-CN>先验证定义与布尔契约；最终文本再次以 TryParse 转换，防御性地将无法表示的结果收敛为 <c>false</c>。</zh-CN>
            //   <en>Validate definition and Boolean contract first; parse final text again with TryParse so an unrepresentable result defensively converges to <c>false</c>.</en>
            // </lang>
            EnsureDefinition(definition);
            EnsureValueType(definition, PortalSettingValueType.Boolean);

            bool parsed;
            return bool.TryParse(GetEffectiveValue(definition).Value, out parsed) && parsed;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取整数设置；非法或超出范围的值回退至更低优先级来源。</zh-CN>
        ///   <en>Reads an integer setting; invalid or out-of-range values fall back to lower-priority sources.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>值类型必须为 <see cref="PortalSettingValueType.Integer"/> 的已登记定义。</zh-CN>
        ///   <en>Registered definition whose value type must be <see cref="PortalSettingValueType.Integer"/>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最终整数值；所有来源均不可用时为 <c>0</c>。</zh-CN>
        ///   <en>Final integer value, or <c>0</c> when no source can provide a valid value.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN><paramref name="definition"/> 为 <c>null</c> 时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="definition"/> is <c>null</c>.</en>
        /// </l>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>定义值类型不是整数时引发。</zh-CN>
        ///   <en>Thrown when the definition is not an integer.</en>
        /// </l>
        /// </exception>
        public static int GetInt32(PortalSettingDefinition definition)
        {
            // <lang>
            //   <zh-CN>先验证定义与整数契约；最终解析再次应用范围门禁，所有来源均不可用时按既有契约返回 <c>0</c>。</zh-CN>
            //   <en>Validate definition and integer contract first; apply the range gate again to final parsing and return <c>0</c> by the established contract when no source is usable.</en>
            // </lang>
            EnsureDefinition(definition);
            EnsureValueType(definition, PortalSettingValueType.Integer);

            int parsed;
            return int.TryParse(GetEffectiveValue(definition).Value, out parsed) &&
                   IsIntegerInRange(definition, parsed)
                ? parsed
                : 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按设置定义校验并规范化候选文本值。</zh-CN>
        ///   <en>Validates and normalizes a candidate text value against its setting definition.</en>
        /// </lang>
        /// </summary>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>用于类型和范围校验的设置定义。</zh-CN>
        ///   <en>Setting definition used for type and range validation.</en>
        /// </l>
        /// </param>
        /// <param name="candidateValue">
        /// <l>
        ///   <zh-CN>待校验的候选文本值。</zh-CN>
        ///   <en>Candidate text value to validate.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedValue">
        /// <l>
        ///   <zh-CN>成功时返回规范化值；失败时为空字符串。</zh-CN>
        ///   <en>Normalized value when successful; otherwise an empty string.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选值满足基础类型和范围规则时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the candidate meets the basic type and range rules.</en>
        /// </l>
        /// </returns>
        public static bool TryNormalizeValue(
            PortalSettingDefinition definition,
            string candidateValue,
            out string normalizedValue)
        {
            // <lang>
            //   <zh-CN>先写入稳定失败输出；缺少定义或候选为空白时不尝试推断默认值，也不把原始输入作为成功值返回。</zh-CN>
            //   <en>Set a stable failure output first; when definition is missing or candidate is blank, do not infer a default or return raw input as a successful value.</en>
            // </lang>
            normalizedValue = string.Empty;
            if (definition == null || string.IsNullOrWhiteSpace(candidateValue))
            {
                return false;
            }

            string trimmedValue = candidateValue.Trim();
            switch (definition.ValueType)
            {
                case PortalSettingValueType.Boolean:
                    // <lang>
                    //   <zh-CN>布尔值只接受 .NET 固定解析形式，并规范为小写稳定文本，避免来源间大小写差异影响后续比较。</zh-CN>
                    //   <en>Accept only fixed .NET Boolean parse forms and normalize to stable lowercase text so source casing differences do not affect later comparisons.</en>
                    // </lang>
                    bool booleanValue;
                    if (bool.TryParse(trimmedValue, out booleanValue))
                    {
                        normalizedValue = booleanValue.ToString().ToLowerInvariant();
                        return true;
                    }

                    return false;

                case PortalSettingValueType.Integer:
                    // <lang>
                    //   <zh-CN>整数必须可解析且同时满足定义的可选上下限；超界值与格式错误同样触发来源回退。</zh-CN>
                    //   <en>An integer must parse and satisfy the definition's optional lower and upper bounds; out-of-range values fall back just like malformed values.</en>
                    // </lang>
                    int integerValue;
                    if (int.TryParse(trimmedValue, out integerValue) && IsIntegerInRange(definition, integerValue))
                    {
                        normalizedValue = integerValue.ToString();
                        return true;
                    }

                    return false;

                default:
                    // <lang>
                    //   <zh-CN>其它已登记类型保留去空白后的文本；其语义约束由对应定义和消费方承担，本 helper 不把文本解释为路径、权限或秘密。</zh-CN>
                    //   <en>Other registered types retain trimmed text; their semantic constraints belong to the definition and consumer, and this helper does not interpret text as a path, permission, or secret.</en>
                    // </lang>
                    normalizedValue = trimmedValue;
                    return true;
            }
        }

        private static void EnsureDefinition(PortalSettingDefinition definition)
        {
            // <lang>
            //   <zh-CN>用固定参数名抛出受控契约异常，避免后续空引用并且不包含候选设置值。</zh-CN>
            //   <en>Throw a controlled contract exception with the fixed parameter name to avoid later null dereference and include no candidate setting value.</en>
            // </lang>
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
        }

        private static void EnsureValueType(PortalSettingDefinition definition, PortalSettingValueType expectedType)
        {
            // <lang>
            //   <zh-CN>读取入口不得把错误类型定义静默转换；异常只描述稳定键和元数据类型，不包含实际设置内容。</zh-CN>
            //   <en>A read entry point must not silently convert a definition of the wrong type; the exception describes only stable key and metadata types, never actual setting content.</en>
            // </lang>
            if (definition.ValueType != expectedType)
            {
                throw new InvalidOperationException(
                    string.Format("Setting '{0}' is '{1}', not '{2}'.", definition.Key, definition.ValueType, expectedType));
            }
        }

        private static bool IsIntegerInRange(PortalSettingDefinition definition, int value)
        {
            // <lang>
            //   <zh-CN>下限和上限均为可选约束；只有存在的边界参与拒绝，未设置的边界不隐含额外限制。</zh-CN>
            //   <en>Lower and upper bounds are optional constraints; only present bounds reject a value and an absent bound implies no additional restriction.</en>
            // </lang>
            if (definition.MinIntegerValue.HasValue && value < definition.MinIntegerValue.Value)
            {
                return false;
            }

            if (definition.MaxIntegerValue.HasValue && value > definition.MaxIntegerValue.Value)
            {
                return false;
            }

            return true;
        }

        private static void WarnDatabaseFallback(string key, string reason, HttpContext context)
        {
            // <lang>
            //   <zh-CN>去重键由稳定设置键和调用方提供的受控原因组成，不附加数据库值、连接串或 HTTP 内容。</zh-CN>
            //   <en>The deduplication key consists of the stable setting key and caller-supplied controlled reason, with no database value, connection string, or HTTP content appended.</en>
            // </lang>
            string warningKey = key + "|" + reason;
            lock (WarningLock)
            {
                if (WarnedDatabaseFallbacks.Contains(warningKey))
                {
                    // <lang>
                    //   <zh-CN>同一键/原因已记录时静默返回，避免持续故障在高频读取路径放大为日志噪声。</zh-CN>
                    //   <en>Return silently when the same key/reason was recorded, preventing a persistent fault from amplifying into log noise on a high-frequency read path.</en>
                    // </lang>
                    return;
                }

                // <lang>
                //   <zh-CN>在写入诊断前先标记为已告警，保留既有“诊断接收器失败也不无限重试”语义。</zh-CN>
                //   <en>Mark as warned before writing diagnostics, retaining the established behavior that a diagnostics-sink failure is not retried without bound.</en>
                // </lang>
                WarnedDatabaseFallbacks.Add(warningKey);
            }

            // <lang>
            //   <zh-CN>告警消息只包含稳定设置键和受控原因；PortalDiagnostics 继续负责请求上下文净化、长度限制和接收器隔离。</zh-CN>
            //   <en>The warning message contains only stable setting key and controlled reason; PortalDiagnostics continues to own request-context sanitization, length caps, and sink isolation.</en>
            // </lang>
            PortalDiagnostics.Warn(
                "RuntimeSettings",
                "Setting '" + key + "' fell back from database override. " + reason,
                context);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>有效运行期设置值的来源层级。</zh-CN>
    ///   <en>Source layer of an effective runtime setting value.</en>
    /// </lang>
    /// </summary>
    public enum PortalRuntimeSettingSource
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>代码定义的默认值。</zh-CN>
        ///   <en>Code-defined default value.</en>
        /// </lang>
        /// </summary>
        Default,

        /// <summary>
        /// <lang>
        ///   <zh-CN>Web.config 的 appSettings 值。</zh-CN>
        ///   <en>Web.config appSettings value.</en>
        /// </lang>
        /// </summary>
        AppSettings,

        /// <summary>
        /// <lang>
        ///   <zh-CN>允许在线管理的数据库覆盖值。</zh-CN>
        ///   <en>Database override allowed for online management.</en>
        /// </lang>
        /// </summary>
        Database
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>已解析的运行期设置值及其来源。</zh-CN>
    ///   <en>Resolved runtime setting value and its source.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalRuntimeSettingValue
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建已解析设置值。</zh-CN>
        ///   <en>Creates a resolved setting value.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>规范化文本值；<c>null</c> 会转换为空字符串。</zh-CN>
        ///   <en>Normalized text value; <c>null</c> becomes an empty string.</en>
        /// </l>
        /// </param>
        /// <param name="source">
        /// <l>
        ///   <zh-CN>值来源层级。</zh-CN>
        ///   <en>Source layer of the value.</en>
        /// </l>
        /// </param>
        public PortalRuntimeSettingValue(string value, PortalRuntimeSettingSource source)
        {
            Value = value ?? string.Empty;
            Source = source;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>已规范化的文本值。</zh-CN>
        ///   <en>Normalized text value.</en>
        /// </lang>
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>该值的来源层级。</zh-CN>
        ///   <en>Source layer of this value.</en>
        /// </lang>
        /// </summary>
        public PortalRuntimeSettingSource Source { get; private set; }
    }
}
