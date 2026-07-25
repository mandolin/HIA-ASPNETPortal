namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>系统设置 registry 的只读展示信息。</zh-CN>
    ///   <en>Read-only display row for one registered system setting.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalSettingHealthInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一条系统设置健康展示信息。</zh-CN>
        ///   <en>Creates one system-setting health display row.</en>
        /// </lang>
        /// </summary>
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
        /// <l>
        ///   <zh-CN>设置键名。</zh-CN>
        ///   <en>Setting key.</en>
        /// </l>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>展示名称。</zh-CN>
        ///   <en>Display name.</en>
        /// </l>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>值类型。</zh-CN>
        ///   <en>Value type.</en>
        /// </l>
        /// </summary>
        public string ValueType { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>当前有效值；敏感项不会展示明文。</zh-CN>
        ///   <en>Current effective value; sensitive values are not shown in plain text.</en>
        /// </l>
        /// </summary>
        public string CurrentValue { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>当前值来源。</zh-CN>
        ///   <en>Current value source.</en>
        /// </l>
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>是否敏感设置。</zh-CN>
        ///   <en>Whether this setting is sensitive.</en>
        /// </l>
        /// </summary>
        public bool IsSensitive { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>是否允许在线编辑。</zh-CN>
        ///   <en>Whether this setting may be edited online.</en>
        /// </l>
        /// </summary>
        public bool CanEditOnline { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>变更后是否需要重启。</zh-CN>
        ///   <en>Whether changes require restart.</en>
        /// </l>
        /// </summary>
        public bool RequiresRestart { get; private set; }
    }
}
