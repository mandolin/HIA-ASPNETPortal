using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：运行期系统设置读取助手。
    ///
    /// English: Runtime helper for reading system settings.
    /// </summary>
    /// <remarks>
    /// 中文：允许在线管理的非敏感设置优先采用数据库覆盖值，其后为 appSettings 和代码默认值。
    /// 读取失败、缺表或值无效时安全回退，并对每种回退原因仅记录一次诊断警告。
    ///
    /// English: Eligible non-sensitive settings prefer database overrides, followed by appSettings and code
    /// defaults. Read failures, missing tables, and invalid values fall back safely; each fallback reason is
    /// logged as a diagnostic warning only once.
    /// </remarks>
    public static class PortalRuntimeSettings
    {
        private static readonly object WarningLock = new object();

        private static readonly HashSet<string> WarnedDatabaseFallbacks =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 中文：获取一个设置的有效文本值及其来源层级。
        ///
        /// English: Gets a setting's effective text value and source layer.
        /// </summary>
        /// <param name="definition">中文：已登记的设置元数据定义，不能为 <c>null</c>。English: Registered setting metadata definition; cannot be <c>null</c>.</param>
        /// <param name="context">中文：用于受限诊断的当前 HTTP 上下文，可为 <c>null</c>。English: Current HTTP context for restricted diagnostics; may be <c>null</c>.</param>
        /// <returns>中文：已通过基础类型和范围校验的有效值。English: Effective value that passed basic type and range validation.</returns>
        /// <exception cref="ArgumentNullException">中文：<paramref name="definition"/> 为 <c>null</c> 时引发。English: Thrown when <paramref name="definition"/> is <c>null</c>.</exception>
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
        /// 中文：读取定义的最终文本表示；空白或不可用值回退至更低优先级来源。
        ///
        /// English: Reads the definition's effective text representation; blank or unavailable values fall back to lower-priority sources.
        /// </summary>
        /// <param name="definition">中文：已登记的设置定义。English: Registered setting definition.</param>
        /// <returns>中文：最终规范化文本值。English: Final normalized text value.</returns>
        public static string GetString(PortalSettingDefinition definition)
        {
            EnsureDefinition(definition);

            return GetEffectiveValue(definition).Value;
        }

        /// <summary>
        /// 中文：读取布尔设置；非法值回退至更低优先级来源。
        ///
        /// English: Reads a Boolean setting; invalid values fall back to lower-priority sources.
        /// </summary>
        /// <param name="definition">中文：值类型必须为 <see cref="PortalSettingValueType.Boolean"/> 的已登记定义。English: Registered definition whose value type must be <see cref="PortalSettingValueType.Boolean"/>.</param>
        /// <returns>中文：最终布尔值。English: Final Boolean value.</returns>
        /// <exception cref="ArgumentNullException">中文：<paramref name="definition"/> 为 <c>null</c> 时引发。English: Thrown when <paramref name="definition"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">中文：定义值类型不是布尔值时引发。English: Thrown when the definition is not Boolean.</exception>
        public static bool GetBoolean(PortalSettingDefinition definition)
        {
            EnsureDefinition(definition);
            EnsureValueType(definition, PortalSettingValueType.Boolean);

            bool parsed;
            return bool.TryParse(GetEffectiveValue(definition).Value, out parsed) && parsed;
        }

        /// <summary>
        /// 中文：读取整数设置；非法或超出范围的值回退至更低优先级来源。
        ///
        /// English: Reads an integer setting; invalid or out-of-range values fall back to lower-priority sources.
        /// </summary>
        /// <param name="definition">中文：值类型必须为 <see cref="PortalSettingValueType.Integer"/> 的已登记定义。English: Registered definition whose value type must be <see cref="PortalSettingValueType.Integer"/>.</param>
        /// <returns>中文：最终整数值；所有来源均不可用时为 <c>0</c>。English: Final integer value, or <c>0</c> when no source can provide a valid value.</returns>
        /// <exception cref="ArgumentNullException">中文：<paramref name="definition"/> 为 <c>null</c> 时引发。English: Thrown when <paramref name="definition"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">中文：定义值类型不是整数时引发。English: Thrown when the definition is not an integer.</exception>
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
        /// 中文：按设置定义校验并规范化候选文本值。
        ///
        /// English: Validates and normalizes a candidate text value against its setting definition.
        /// </summary>
        /// <param name="definition">中文：用于类型和范围校验的设置定义。English: Setting definition used for type and range validation.</param>
        /// <param name="candidateValue">中文：待校验的候选文本值。English: Candidate text value to validate.</param>
        /// <param name="normalizedValue">中文：成功时返回规范化值；失败时为空字符串。English: Normalized value when successful; otherwise an empty string.</param>
        /// <returns>中文：候选值满足基础类型和范围规则时为 <c>true</c>。English: <c>true</c> when the candidate meets the basic type and range rules.</returns>
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
    /// 中文：有效运行期设置值的来源层级。
    ///
    /// English: Source layer of an effective runtime setting value.
    /// </summary>
    public enum PortalRuntimeSettingSource
    {
        /// <summary>
        /// 中文：代码定义的默认值。
        /// English: Code-defined default value.
        /// </summary>
        Default,

        /// <summary>
        /// 中文：Web.config 的 appSettings 值。
        /// English: Web.config appSettings value.
        /// </summary>
        AppSettings,

        /// <summary>
        /// 中文：允许在线管理的数据库覆盖值。
        /// English: Database override allowed for online management.
        /// </summary>
        Database
    }

    /// <summary>
    /// 中文：已解析的运行期设置值及其来源。
    ///
    /// English: Resolved runtime setting value and its source.
    /// </summary>
    public sealed class PortalRuntimeSettingValue
    {
        /// <summary>
        /// 中文：创建已解析设置值。
        ///
        /// English: Creates a resolved setting value.
        /// </summary>
        /// <param name="value">中文：规范化文本值；<c>null</c> 会转换为空字符串。English: Normalized text value; <c>null</c> becomes an empty string.</param>
        /// <param name="source">中文：值来源层级。English: Source layer of the value.</param>
        public PortalRuntimeSettingValue(string value, PortalRuntimeSettingSource source)
        {
            Value = value ?? string.Empty;
            Source = source;
        }

        /// <summary>
        /// 中文：已规范化的文本值。
        ///
        /// English: Normalized text value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// 中文：该值的来源层级。
        ///
        /// English: Source layer of this value.
        /// </summary>
        public PortalRuntimeSettingSource Source { get; private set; }
    }
}
