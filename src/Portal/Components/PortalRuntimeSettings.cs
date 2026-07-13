using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 运行期系统设置读取助手。
    /// Runtime helper for reading system settings.
    /// </summary>
    /// <remarks>
    /// 对允许在线管理的非敏感设置，数据库覆盖值优先于 appSettings；读取失败、缺表或无效值都会安全回退。
    /// For eligible non-sensitive settings, database overrides take precedence over appSettings; read failures,
    /// missing tables, and invalid values all fall back safely.
    /// </remarks>
    public static class PortalRuntimeSettings
    {
        private static readonly object WarningLock = new object();

        private static readonly HashSet<string> WarnedDatabaseFallbacks =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 获取一个设置的最终文本值及其来源。
        /// Gets the effective text value of a setting together with its source.
        /// </summary>
        /// <param name="definition">设置元数据定义。Setting metadata definition.</param>
        /// <param name="context">用于受限诊断的当前 HTTP 上下文。Current HTTP context for restricted diagnostics.</param>
        /// <returns>经过类型和范围校验的最终值。Effective value validated for type and range.</returns>
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
        /// 读取文本设置；空白值按默认值处理。
        /// Reads a string setting; blank values fall back to the default.
        /// </summary>
        public static string GetString(PortalSettingDefinition definition)
        {
            EnsureDefinition(definition);

            return GetEffectiveValue(definition).Value;
        }

        /// <summary>
        /// 读取布尔设置；非法值按默认值处理。
        /// Reads a boolean setting; invalid values fall back to the default.
        /// </summary>
        public static bool GetBoolean(PortalSettingDefinition definition)
        {
            EnsureDefinition(definition);
            EnsureValueType(definition, PortalSettingValueType.Boolean);

            bool parsed;
            return bool.TryParse(GetEffectiveValue(definition).Value, out parsed) && parsed;
        }

        /// <summary>
        /// 读取整数设置；非法或超出范围时按默认值处理。
        /// Reads an integer setting; invalid or out-of-range values fall back to the default.
        /// </summary>
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
        /// 按设置定义校验并规范化候选文本值。
        /// Validates and normalizes a candidate text value against its setting definition.
        /// </summary>
        /// <param name="definition">设置元数据定义。Setting metadata definition.</param>
        /// <param name="candidateValue">候选文本值。Candidate text value.</param>
        /// <param name="normalizedValue">成功时返回规范化值。Normalized value when successful.</param>
        /// <returns>候选值是否满足基础类型和范围规则。Whether the candidate meets basic type and range rules.</returns>
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
    /// 运行期设置值的来源层级。
    /// Source layer of an effective runtime setting value.
    /// </summary>
    public enum PortalRuntimeSettingSource
    {
        Default,
        AppSettings,
        Database
    }

    /// <summary>
    /// 已解析的运行期设置值。
    /// Resolved runtime setting value.
    /// </summary>
    public sealed class PortalRuntimeSettingValue
    {
        /// <summary>
        /// 创建已解析设置值。
        /// Creates a resolved setting value.
        /// </summary>
        /// <param name="value">规范化文本值。Normalized text value.</param>
        /// <param name="source">值来源层级。Source layer.</param>
        public PortalRuntimeSettingValue(string value, PortalRuntimeSettingSource source)
        {
            Value = value ?? string.Empty;
            Source = source;
        }

        /// <summary>
        /// 规范化文本值。
        /// Normalized text value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// 值来源层级。
        /// Source layer.
        /// </summary>
        public PortalRuntimeSettingSource Source { get; private set; }
    }
}
