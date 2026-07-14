using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：受支持的系统设置值类型。
    ///
    /// English: Supported system setting value types.
    /// </summary>
    public enum PortalSettingValueType
    {
        /// <summary>
        /// 中文：布尔开关值。
        /// English: Boolean switch value.
        /// </summary>
        Boolean,

        /// <summary>
        /// 中文：32 位整数值，可受定义的范围限制。
        /// English: 32-bit integer value that can be constrained by the definition's range.
        /// </summary>
        Integer,

        /// <summary>
        /// 中文：普通文本值。
        /// English: Ordinary text value.
        /// </summary>
        String,

        /// <summary>
        /// 中文：来自预定义候选集的文本值。
        /// English: Text value selected from a predefined candidate set.
        /// </summary>
        Enum,

        /// <summary>
        /// 中文：文件系统或应用相对路径值。
        /// English: File-system or application-relative path value.
        /// </summary>
        Path,

        /// <summary>
        /// 中文：持续时间或保留期等时间长度值。
        /// English: Duration value, such as a retention period.
        /// </summary>
        Duration
    }

    /// <summary>
    /// 中文：描述一个系统设置项的元数据契约。
    ///
    /// English: Metadata contract for one system setting.
    /// </summary>
    /// <remarks>
    /// 中文：registry 定义设置契约；运行期可在受限数据库覆盖层、appSettings 与代码默认值之间解析。
    /// 此类型不保存实际密钥或敏感设置值。
    ///
    /// English: The registry defines the setting contract; runtime values resolve through a restricted
    /// database override layer, appSettings, and code defaults. This type does not retain actual secrets
    /// or sensitive setting values.
    /// </remarks>
    public sealed class PortalSettingDefinition
    {
        /// <summary>
        /// 中文：初始化一个不可变的系统设置元数据定义。
        ///
        /// English: Initializes an immutable system-setting metadata definition.
        /// </summary>
        /// <param name="key">中文：稳定且非空的设置键。English: Stable, non-empty setting key.</param>
        /// <param name="displayName">中文：管理界面显示名称。English: Display name for administration UI.</param>
        /// <param name="description">中文：面向管理员的设置说明。English: Administrator-facing setting description.</param>
        /// <param name="valueType">中文：值的基础类型。English: Base type of the value.</param>
        /// <param name="defaultValue">中文：无法从较高优先级来源解析时使用的文本默认值。English: Text default used when higher-priority sources cannot be resolved.</param>
        /// <param name="canEditOnline">中文：是否允许通过受控管理界面创建或修改数据库覆盖值。English: Whether a controlled administration UI may create or update a database override.</param>
        /// <param name="requiresRestart">中文：变更是否需要应用重启或重新加载才生效。English: Whether a change requires application restart or reload to take effect.</param>
        /// <param name="permission">中文：当前阶段要求的角色或权限表达式。English: Role or permission expression required in the current phase.</param>
        /// <param name="auditCategory">中文：设置变更的审计分类。English: Audit category for setting changes.</param>
        /// <param name="isSensitive">中文：设置是否敏感；敏感设置不能由数据库在线覆盖。English: Whether the setting is sensitive; sensitive settings cannot be overridden online in the database.</param>
        /// <param name="sourceLevel">中文：该定义的主要配置来源层级。English: Primary configuration source level for this definition.</param>
        /// <param name="minIntegerValue">中文：整数值的可选最小边界。English: Optional minimum bound for integer values.</param>
        /// <param name="maxIntegerValue">中文：整数值的可选最大边界。English: Optional maximum bound for integer values.</param>
        /// <exception cref="ArgumentException">中文：<paramref name="key"/> 为空白时引发。English: Thrown when <paramref name="key"/> is blank.</exception>
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
        /// 中文：稳定键名，用于 registry、配置源和审计记录之间的关联。
        ///
        /// English: Stable key that links the registry, configuration sources, and audit records.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 中文：后台管理界面显示名称。
        ///
        /// English: Display name for administration UI.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// 中文：面向管理员的设置说明。
        ///
        /// English: Administrator-facing setting description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// 中文：设置值的基础类型。
        ///
        /// English: Base type of the setting value.
        /// </summary>
        public PortalSettingValueType ValueType { get; private set; }

        /// <summary>
        /// 中文：文本形式的默认值；读取时再根据 <see cref="ValueType"/> 进行校验和转换。
        ///
        /// English: Default value stored as text; it is validated and converted according to <see cref="ValueType"/> when read.
        /// </summary>
        public string DefaultValue { get; private set; }

        /// <summary>
        /// 中文：是否允许后台在线创建或修改数据库覆盖值。
        ///
        /// English: Whether administration UI may create or update a database override online.
        /// </summary>
        public bool CanEditOnline { get; private set; }

        /// <summary>
        /// 中文：设置变更后是否需要应用重启或重新加载才会生效。
        ///
        /// English: Whether a setting change requires application restart or reload to take effect.
        /// </summary>
        public bool RequiresRestart { get; private set; }

        /// <summary>
        /// 中文：当前阶段所需的角色或权限表达式。
        ///
        /// English: Role or permission expression required in the current phase.
        /// </summary>
        public string Permission { get; private set; }

        /// <summary>
        /// 中文：设置变更使用的审计分类。
        ///
        /// English: Audit category used for setting changes.
        /// </summary>
        public string AuditCategory { get; private set; }

        /// <summary>
        /// 中文：设置是否敏感；敏感设置不允许通过数据库在线覆盖。
        ///
        /// English: Whether the setting is sensitive; sensitive settings cannot be overridden online in the database.
        /// </summary>
        public bool IsSensitive { get; private set; }

        /// <summary>
        /// 中文：当前主要配置来源层级。
        ///
        /// English: Current primary configuration source level.
        /// </summary>
        public string SourceLevel { get; private set; }

        /// <summary>
        /// 中文：整数设置可接受的最小值；未指定时不设下限。
        ///
        /// English: Minimum accepted integer value; no lower bound when unspecified.
        /// </summary>
        public int? MinIntegerValue { get; private set; }

        /// <summary>
        /// 中文：整数设置可接受的最大值；未指定时不设上限。
        ///
        /// English: Maximum accepted integer value; no upper bound when unspecified.
        /// </summary>
        public int? MaxIntegerValue { get; private set; }
    }
}
