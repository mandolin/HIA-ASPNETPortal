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
        /// <param name="key">
        /// <l>
        ///   <zh-CN>稳定设置键；空值归一为空字符串。</zh-CN>
        ///   <en>Stable setting key; null normalizes to an empty string.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>管理界面展示名称；空值归一为空字符串。</zh-CN>
        ///   <en>Administration display name; null normalizes to an empty string.</en>
        /// </l>
        /// </param>
        /// <param name="valueType">
        /// <l>
        ///   <zh-CN>已登记值类型的展示文本。</zh-CN>
        ///   <en>Display text for the registered value type.</en>
        /// </l>
        /// </param>
        /// <param name="currentValue">
        /// <l>
        ///   <zh-CN>当前有效值的展示文本；敏感值由调用方先替换为占位。</zh-CN>
        ///   <en>Display text for the effective value; callers replace sensitive values with a placeholder first.</en>
        /// </l>
        /// </param>
        /// <param name="source">
        /// <l>
        ///   <zh-CN>当前值来源层级展示文本。</zh-CN>
        ///   <en>Display text for the current value's source layer.</en>
        /// </l>
        /// </param>
        /// <param name="isSensitive">
        /// <l>
        ///   <zh-CN>该设置是否敏感；此 DTO 不据此重新读取或解密值。</zh-CN>
        ///   <en>Whether the setting is sensitive; this DTO does not reread or decrypt the value.</en>
        /// </l>
        /// </param>
        /// <param name="canEditOnline">
        /// <l>
        ///   <zh-CN>是否允许在线编辑的策略展示标志，不等同于当前用户已授权。</zh-CN>
        ///   <en>Policy display flag for online editing; it does not mean the current user is authorized.</en>
        /// </l>
        /// </param>
        /// <param name="requiresRestart">
        /// <l>
        ///   <zh-CN>变更是否需要重启的提示标志，不在 DTO 构造时触发重启。</zh-CN>
        ///   <en>Advisory flag for whether a change requires restart; construction triggers no restart.</en>
        /// </l>
        /// </param>
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
            // <lang>
            //   <zh-CN>归一稳定键文本，保证健康行可安全展示且不保存 null 键状态。</zh-CN>
            //   <en>Normalize the stable key so the health row is safely displayable without retaining a null-key state.</en>
            // </lang>
            Key = key ?? string.Empty;

            // <lang>
            //   <zh-CN>归一管理展示名称；该文本只作为标签，不被解释为授权或配置来源。</zh-CN>
            //   <en>Normalize the administration label; it remains display text and is not interpreted as authorization or a configuration source.</en>
            // </lang>
            DisplayName = displayName ?? string.Empty;

            // <lang>
            //   <zh-CN>保留值类型展示文本，实际解析契约由 Registry/运行时设置解析器负责。</zh-CN>
            //   <en>Retain value-type display text while Registry and the runtime resolver own the actual parsing contract.</en>
            // </lang>
            ValueType = valueType ?? string.Empty;

            // <lang>
            //   <zh-CN>保存调用方已经决定可展示的当前值；敏感值不应在到达此构造器前仍为明文。</zh-CN>
            //   <en>Store the current value already approved for display; a sensitive value must not still be plaintext before reaching this constructor.</en>
            // </lang>
            CurrentValue = currentValue ?? string.Empty;

            // <lang>
            //   <zh-CN>归一来源层级文本；来源只用于解释当前解析结果，不授予读取或修改权限。</zh-CN>
            //   <en>Normalize the source-layer text; it explains resolution only and grants no read or write permission.</en>
            // </lang>
            Source = source ?? string.Empty;

            // <lang>
            //   <zh-CN>保存敏感标志供展示层继续隐藏值，不在此根据标志访问任何秘密来源。</zh-CN>
            //   <en>Retain the sensitivity flag so display layers can continue hiding values without accessing any secret source here.</en>
            // </lang>
            IsSensitive = isSensitive;

            // <lang>
            //   <zh-CN>保存在线编辑策略事实；实际页面/用户授权由消费方执行。</zh-CN>
            //   <en>Retain the online-edit policy fact; pages and consumers execute actual user authorization.</en>
            // </lang>
            CanEditOnline = canEditOnline;

            // <lang>
            //   <zh-CN>保存重启提示元数据，不在展示 DTO 生命周期内执行应用动作。</zh-CN>
            //   <en>Retain restart advisory metadata without performing an application action during DTO construction.</en>
            // </lang>
            RequiresRestart = requiresRestart;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置键名。</zh-CN>
        ///   <en>Setting key.</en>
        /// </lang>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>展示名称。</zh-CN>
        ///   <en>Display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>值类型。</zh-CN>
        ///   <en>Value type.</en>
        /// </lang>
        /// </summary>
        public string ValueType { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前有效值；敏感项不会展示明文。</zh-CN>
        ///   <en>Current effective value; sensitive values are not shown in plain text.</en>
        /// </lang>
        /// </summary>
        public string CurrentValue { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前值来源。</zh-CN>
        ///   <en>Current value source.</en>
        /// </lang>
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否敏感设置。</zh-CN>
        ///   <en>Whether this setting is sensitive.</en>
        /// </lang>
        /// </summary>
        public bool IsSensitive { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否允许在线编辑。</zh-CN>
        ///   <en>Whether this setting may be edited online.</en>
        /// </lang>
        /// </summary>
        public bool CanEditOnline { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>变更后是否需要重启。</zh-CN>
        ///   <en>Whether changes require restart.</en>
        /// </lang>
        /// </summary>
        public bool RequiresRestart { get; private set; }
    }
}
