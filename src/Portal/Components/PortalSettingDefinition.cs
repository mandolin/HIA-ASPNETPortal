using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 系统设置值类型。
    /// Supported system setting value types.
    /// </summary>
    public enum PortalSettingValueType
    {
        Boolean,
        Integer,
        String,
        Enum,
        Path,
        Duration
    }

    /// <summary>
    /// 描述一个系统设置项的元数据。
    /// Metadata definition for one system setting.
    /// </summary>
    /// <remarks>
    /// P2.1 只建立 registry 骨架；运行值仍从 appSettings 读取，数据库设置层后续再接入。
    /// P2.1 builds the registry skeleton only; runtime values still come from appSettings for now.
    /// </remarks>
    public sealed class PortalSettingDefinition
    {
        public PortalSettingDefinition(
            string key,
            string displayName,
            string description,
            PortalSettingValueType valueType,
            string defaultValue,
            bool canEditOnline,
            bool requiresRestart,
            string permission,
            string auditCategory,
            bool isSensitive = false,
            string sourceLevel = "AppSettings",
            int? minIntegerValue = null,
            int? maxIntegerValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Setting key is required.", "key");
            }

            Key = key;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            ValueType = valueType;
            DefaultValue = defaultValue ?? string.Empty;
            CanEditOnline = canEditOnline;
            RequiresRestart = requiresRestart;
            Permission = permission ?? string.Empty;
            AuditCategory = auditCategory ?? string.Empty;
            IsSensitive = isSensitive;
            SourceLevel = sourceLevel ?? string.Empty;
            MinIntegerValue = minIntegerValue;
            MaxIntegerValue = maxIntegerValue;
        }

        /// <summary>
        /// 稳定键名。
        /// Stable setting key.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 后台显示名称。
        /// Display name for admin UI.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// 设置说明。
        /// Setting description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// 值类型。
        /// Value type.
        /// </summary>
        public PortalSettingValueType ValueType { get; private set; }

        /// <summary>
        /// 默认值，统一以文本保存。
        /// Default value, stored as text.
        /// </summary>
        public string DefaultValue { get; private set; }

        /// <summary>
        /// 是否允许后台在线修改。
        /// Whether online editing is allowed.
        /// </summary>
        public bool CanEditOnline { get; private set; }

        /// <summary>
        /// 修改后是否需要重启或重新加载。
        /// Whether a restart or reload is required after changes.
        /// </summary>
        public bool RequiresRestart { get; private set; }

        /// <summary>
        /// 当前阶段所需权限，暂以角色或权限字符串表达。
        /// Required permission or role string for this phase.
        /// </summary>
        public string Permission { get; private set; }

        /// <summary>
        /// 审计分类。
        /// Audit category.
        /// </summary>
        public string AuditCategory { get; private set; }

        /// <summary>
        /// 是否敏感。
        /// Whether the setting is sensitive.
        /// </summary>
        public bool IsSensitive { get; private set; }

        /// <summary>
        /// 当前主要来源层级。
        /// Main source level.
        /// </summary>
        public string SourceLevel { get; private set; }

        /// <summary>
        /// 整数设置的最小值。
        /// Minimum integer value.
        /// </summary>
        public int? MinIntegerValue { get; private set; }

        /// <summary>
        /// 整数设置的最大值。
        /// Maximum integer value.
        /// </summary>
        public int? MaxIntegerValue { get; private set; }
    }
}
