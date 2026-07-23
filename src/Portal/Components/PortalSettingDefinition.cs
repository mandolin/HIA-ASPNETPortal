using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>受支持的系统设置值类型。</zh-CN>
    ///   <en>Supported system setting value types.</en>
    /// </lang>
    /// </summary>
    public enum PortalSettingValueType
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>布尔开关值。</zh-CN>
        ///   <en>Boolean switch value.</en>
        /// </lang>
        /// </summary>
        Boolean,

        /// <summary>
        /// <lang>
        ///   <zh-CN>32 位整数值，可受定义的范围限制。</zh-CN>
        ///   <en>32-bit integer value that can be constrained by the definition's range.</en>
        /// </lang>
        /// </summary>
        Integer,

        /// <summary>
        /// <lang>
        ///   <zh-CN>普通文本值。</zh-CN>
        ///   <en>Ordinary text value.</en>
        /// </lang>
        /// </summary>
        String,

        /// <summary>
        /// <lang>
        ///   <zh-CN>来自预定义候选集的文本值。</zh-CN>
        ///   <en>Text value selected from a predefined candidate set.</en>
        /// </lang>
        /// </summary>
        Enum,

        /// <summary>
        /// <lang>
        ///   <zh-CN>文件系统或应用相对路径值。</zh-CN>
        ///   <en>File-system or application-relative path value.</en>
        /// </lang>
        /// </summary>
        Path,

        /// <summary>
        /// <lang>
        ///   <zh-CN>持续时间或保留期等时间长度值。</zh-CN>
        ///   <en>Duration value, such as a retention period.</en>
        /// </lang>
        /// </summary>
        Duration
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>描述一个系统设置项的元数据契约。</zh-CN>
    ///   <en>Metadata contract for one system setting.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>registry 定义设置契约；运行期可在受限数据库覆盖层、appSettings 与代码默认值之间解析。 此类型不保存实际密钥或敏感设置值。</zh-CN>
    ///   <en>The registry defines the setting contract; runtime values resolve through a restricted database override layer, appSettings, and code defaults. This type does not retain actual secrets or sensitive setting values.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalSettingDefinition
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化一个不可变的系统设置元数据定义。</zh-CN>
        ///   <en>Initializes an immutable system-setting metadata definition.</en>
        /// </lang>
        /// </summary>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>稳定且非空的设置键。</zh-CN>
        ///   <en>Stable, non-empty setting key.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>管理界面显示名称。</zh-CN>
        ///   <en>Display name for administration UI.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>面向管理员的设置说明。</zh-CN>
        ///   <en>Administrator-facing setting description.</en>
        /// </l>
        /// </param>
        /// <param name="valueType">
        /// <l>
        ///   <zh-CN>值的基础类型。</zh-CN>
        ///   <en>Base type of the value.</en>
        /// </l>
        /// </param>
        /// <param name="defaultValue">
        /// <l>
        ///   <zh-CN>无法从较高优先级来源解析时使用的文本默认值。</zh-CN>
        ///   <en>Text default used when higher-priority sources cannot be resolved.</en>
        /// </l>
        /// </param>
        /// <param name="canEditOnline">
        /// <l>
        ///   <zh-CN>是否允许通过受控管理界面创建或修改数据库覆盖值。</zh-CN>
        ///   <en>Whether a controlled administration UI may create or update a database override.</en>
        /// </l>
        /// </param>
        /// <param name="requiresRestart">
        /// <l>
        ///   <zh-CN>变更是否需要应用重启或重新加载才生效。</zh-CN>
        ///   <en>Whether a change requires application restart or reload to take effect.</en>
        /// </l>
        /// </param>
        /// <param name="permission">
        /// <l>
        ///   <zh-CN>当前阶段要求的角色或权限表达式。</zh-CN>
        ///   <en>Role or permission expression required in the current phase.</en>
        /// </l>
        /// </param>
        /// <param name="auditCategory">
        /// <l>
        ///   <zh-CN>设置变更的审计分类。</zh-CN>
        ///   <en>Audit category for setting changes.</en>
        /// </l>
        /// </param>
        /// <param name="isSensitive">
        /// <l>
        ///   <zh-CN>设置是否敏感；敏感设置不能由数据库在线覆盖。</zh-CN>
        ///   <en>Whether the setting is sensitive; sensitive settings cannot be overridden online in the database.</en>
        /// </l>
        /// </param>
        /// <param name="sourceLevel">
        /// <l>
        ///   <zh-CN>该定义的主要配置来源层级。</zh-CN>
        ///   <en>Primary configuration source level for this definition.</en>
        /// </l>
        /// </param>
        /// <param name="minIntegerValue">
        /// <l>
        ///   <zh-CN>整数值的可选最小边界。</zh-CN>
        ///   <en>Optional minimum bound for integer values.</en>
        /// </l>
        /// </param>
        /// <param name="maxIntegerValue">
        /// <l>
        ///   <zh-CN>整数值的可选最大边界。</zh-CN>
        ///   <en>Optional maximum bound for integer values.</en>
        /// </l>
        /// </param>
        /// <exception cref="ArgumentException">
        /// <l>
        ///   <zh-CN><paramref name="key"/> 为空白时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="key"/> is blank.</en>
        /// </l>
        /// </exception>
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
        /// <lang>
        ///   <zh-CN>稳定键名，用于 registry、配置源和审计记录之间的关联。</zh-CN>
        ///   <en>Stable key that links the registry, configuration sources, and audit records.</en>
        /// </lang>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>后台管理界面显示名称。</zh-CN>
        ///   <en>Display name for administration UI.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>面向管理员的设置说明。</zh-CN>
        ///   <en>Administrator-facing setting description.</en>
        /// </lang>
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置值的基础类型。</zh-CN>
        ///   <en>Base type of the setting value.</en>
        /// </lang>
        /// </summary>
        public PortalSettingValueType ValueType { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>文本形式的默认值；读取时再根据 <see cref="ValueType"/> 进行校验和转换。</zh-CN>
        ///   <en>Default value stored as text; it is validated and converted according to <see cref="ValueType"/> when read.</en>
        /// </lang>
        /// </summary>
        public string DefaultValue { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否允许后台在线创建或修改数据库覆盖值。</zh-CN>
        ///   <en>Whether administration UI may create or update a database override online.</en>
        /// </lang>
        /// </summary>
        public bool CanEditOnline { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置变更后是否需要应用重启或重新加载才会生效。</zh-CN>
        ///   <en>Whether a setting change requires application restart or reload to take effect.</en>
        /// </lang>
        /// </summary>
        public bool RequiresRestart { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前阶段所需的角色或权限表达式。</zh-CN>
        ///   <en>Role or permission expression required in the current phase.</en>
        /// </lang>
        /// </summary>
        public string Permission { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置变更使用的审计分类。</zh-CN>
        ///   <en>Audit category used for setting changes.</en>
        /// </lang>
        /// </summary>
        public string AuditCategory { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置是否敏感；敏感设置不允许通过数据库在线覆盖。</zh-CN>
        ///   <en>Whether the setting is sensitive; sensitive settings cannot be overridden online in the database.</en>
        /// </lang>
        /// </summary>
        public bool IsSensitive { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前主要配置来源层级。</zh-CN>
        ///   <en>Current primary configuration source level.</en>
        /// </lang>
        /// </summary>
        public string SourceLevel { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>整数设置可接受的最小值；未指定时不设下限。</zh-CN>
        ///   <en>Minimum accepted integer value; no lower bound when unspecified.</en>
        /// </lang>
        /// </summary>
        public int? MinIntegerValue { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>整数设置可接受的最大值；未指定时不设上限。</zh-CN>
        ///   <en>Maximum accepted integer value; no upper bound when unspecified.</en>
        /// </lang>
        /// </summary>
        public int? MaxIntegerValue { get; private set; }
    }
}
