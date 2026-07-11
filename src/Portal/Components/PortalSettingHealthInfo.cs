namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 系统设置 registry 的只读展示信息。
    /// Read-only display row for one registered system setting.
    /// </summary>
    public sealed class PortalSettingHealthInfo
    {
        public PortalSettingHealthInfo(
            string key,
            string displayName,
            string valueType,
            string currentValue,
            string source,
            bool isSensitive,
            bool canEditOnline,
            bool requiresRestart)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ValueType = valueType ?? string.Empty;
            CurrentValue = currentValue ?? string.Empty;
            Source = source ?? string.Empty;
            IsSensitive = isSensitive;
            CanEditOnline = canEditOnline;
            RequiresRestart = requiresRestart;
        }

        /// <summary>
        /// 设置键名。
        /// Setting key.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 展示名称。
        /// Display name.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// 值类型。
        /// Value type.
        /// </summary>
        public string ValueType { get; private set; }

        /// <summary>
        /// 当前有效值；敏感项不会展示明文。
        /// Current effective value; sensitive values are not shown in plain text.
        /// </summary>
        public string CurrentValue { get; private set; }

        /// <summary>
        /// 当前值来源。
        /// Current value source.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// 是否敏感设置。
        /// Whether this setting is sensitive.
        /// </summary>
        public bool IsSensitive { get; private set; }

        /// <summary>
        /// 后续是否允许在线编辑。
        /// Whether this setting may be edited online later.
        /// </summary>
        public bool CanEditOnline { get; private set; }

        /// <summary>
        /// 变更后是否需要重启。
        /// Whether changes require restart.
        /// </summary>
        public bool RequiresRestart { get; private set; }
    }
}
