using System;
using System.Configuration;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 运行期系统设置读取助手。
    /// Runtime helper for reading system settings.
    /// </summary>
    /// <remarks>
    /// 当前只读取 appSettings，并按 registry 默认值兜底；数据库运行级设置层会在后续 P2.1/P2.2 中接入。
    /// For now this reads appSettings and falls back to registry defaults; database runtime settings come later.
    /// </remarks>
    public static class PortalRuntimeSettings
    {
        /// <summary>
        /// 读取文本设置；空白值按默认值处理。
        /// Reads a string setting; blank values fall back to the default.
        /// </summary>
        public static string GetString(PortalSettingDefinition definition)
        {
            EnsureDefinition(definition);

            string configuredValue = ConfigurationManager.AppSettings[definition.Key];
            return string.IsNullOrWhiteSpace(configuredValue)
                ? definition.DefaultValue
                : configuredValue;
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
            if (bool.TryParse(ConfigurationManager.AppSettings[definition.Key], out parsed))
            {
                return parsed;
            }

            return bool.TryParse(definition.DefaultValue, out parsed) && parsed;
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
            if (int.TryParse(ConfigurationManager.AppSettings[definition.Key], out parsed) &&
                IsIntegerInRange(definition, parsed))
            {
                return parsed;
            }

            if (int.TryParse(definition.DefaultValue, out parsed) &&
                IsIntegerInRange(definition, parsed))
            {
                return parsed;
            }

            return 0;
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
    }
}
