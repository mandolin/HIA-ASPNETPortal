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
        private static readonly object WarningLock = new object();

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
            EnsureDefinition(definition);

            PortalSystemSettingReadResult databaseResult = null;
            if (definition.CanEditOnline && !definition.IsSensitive)
            {
                databaseResult = PortalSystemSettingsStore.Read(definition.Key, context);
                if (databaseResult.IsAvailable && databaseResult.IsFound)
                {
                    string databaseValue;
                    if (string.Equals(databaseResult.ValueType, definition.ValueType.ToString(), StringComparison.Ordinal) &&
                        TryNormalizeValue(definition, databaseResult.Value, out databaseValue))
                    {
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
                return new PortalRuntimeSettingValue(
                    configuredValue,
                    PortalRuntimeSettingSource.AppSettings);
            }

            string defaultValue;
            if (TryNormalizeValue(definition, definition.DefaultValue, out defaultValue))
            {
                return new PortalRuntimeSettingValue(defaultValue, PortalRuntimeSettingSource.Default);
            }

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
        public static string GetString(PortalSettingDefinition definition)
        {
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
            normalizedValue = string.Empty;
            if (definition == null || string.IsNullOrWhiteSpace(candidateValue))
            {
                return false;
            }

            string trimmedValue = candidateValue.Trim();
            switch (definition.ValueType)
            {
                case PortalSettingValueType.Boolean:
                    bool booleanValue;
                    if (bool.TryParse(trimmedValue, out booleanValue))
                    {
                        normalizedValue = booleanValue.ToString().ToLowerInvariant();
                        return true;
                    }

                    return false;

                case PortalSettingValueType.Integer:
                    int integerValue;
                    if (int.TryParse(trimmedValue, out integerValue) && IsIntegerInRange(definition, integerValue))
                    {
                        normalizedValue = integerValue.ToString();
                        return true;
                    }

                    return false;

                default:
                    normalizedValue = trimmedValue;
                    return true;
            }
        }

        private static void EnsureDefinition(PortalSettingDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
        }

        private static void EnsureValueType(PortalSettingDefinition definition, PortalSettingValueType expectedType)
        {
            if (definition.ValueType != expectedType)
            {
                throw new InvalidOperationException(
                    string.Format("Setting '{0}' is '{1}', not '{2}'.", definition.Key, definition.ValueType, expectedType));
            }
        }

        private static bool IsIntegerInRange(PortalSettingDefinition definition, int value)
        {
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
            string warningKey = key + "|" + reason;
            lock (WarningLock)
            {
                if (WarnedDatabaseFallbacks.Contains(warningKey))
                {
                    return;
                }

                WarnedDatabaseFallbacks.Add(warningKey);
            }

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
