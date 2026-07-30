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
    ///   <zh-CN>registry 定义设置契约；运行期可在受限数据库覆盖层、appSettings 与代码默认值之间解析。本类型只保存策略元数据，不读取真实来源、写数据库或实施页面授权；敏感设置值不应由该契约承载。</zh-CN>
    ///   <en>The registry defines the setting contract; runtime values resolve through a restricted database override layer, appSettings, and code defaults. This type stores policy metadata only: it reads no real source, writes no database, and implements no page authorization; sensitive setting values must not be carried by this contract.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalSettingDefinition
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化一个不可变的系统设置元数据定义；仅保证稳定键非空，不在构造期解析默认值、验证来源或实施授权。</zh-CN>
        ///   <en>Initializes an immutable system-setting metadata definition; it guarantees only a nonblank stable key and does not parse default values, validate sources, or implement authorization during construction.</en>
        /// </lang>
        /// </summary>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>稳定且非空的设置键；按原样保留，唯一性和 registry 登记由外层负责。</zh-CN>
        ///   <en>Stable, nonblank setting key; retained as supplied, while uniqueness and registry registration belong to the outer layer.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>管理界面显示名称；空值归一为空文本，编码和显示策略由消费方负责。</zh-CN>
        ///   <en>Display name for administration UI; null normalizes to empty text, while encoding and display policy belong to consumers.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>面向管理员的设置说明；空值归一为空文本，不作为访问控制规则。</zh-CN>
        ///   <en>Administrator-facing setting description; null normalizes to empty text and it is not an access-control rule.</en>
        /// </l>
        /// </param>
        /// <param name="valueType">
        /// <l>
        ///   <zh-CN>值的基础类型；运行时解析器据此验证和转换，而构造器不解析文本。</zh-CN>
        ///   <en>Base type of the value; the runtime resolver validates and converts by it, while the constructor parses no text.</en>
        /// </l>
        /// </param>
        /// <param name="defaultValue">
        /// <l>
        ///   <zh-CN>无法从较高优先级来源解析时使用的文本默认值；构造器只保留文本，采用前仍须按类型和范围校验。</zh-CN>
        ///   <en>Text default used when higher-priority sources cannot be resolved; the constructor retains text only, and it must still be validated by type and range before adoption.</en>
        /// </l>
        /// </param>
        /// <param name="canEditOnline">
        /// <l>
        ///   <zh-CN>是否允许通过受控管理界面创建或修改数据库覆盖值；该元数据不替代页面或用户授权。</zh-CN>
        ///   <en>Whether a controlled administration UI may create or update a database override; this metadata does not replace page or user authorization.</en>
        /// </l>
        /// </param>
        /// <param name="requiresRestart">
        /// <l>
        ///   <zh-CN>变更是否需要应用重启或重新加载才生效；仅为调用方提示，不触发重启。</zh-CN>
        ///   <en>Whether a change requires application restart or reload to take effect; it is caller guidance only and triggers no restart.</en>
        /// </l>
        /// </param>
        /// <param name="permission">
        /// <l>
        ///   <zh-CN>当前阶段要求的角色或权限表达式；仅记录策略，实际授权由页面/服务消费方执行。</zh-CN>
        ///   <en>Role or permission expression required in the current phase; it records policy only, while actual authorization is executed by page/service consumers.</en>
        /// </l>
        /// </param>
        /// <param name="auditCategory">
        /// <l>
        ///   <zh-CN>设置变更的审计分类；仅供写入调用方归类，不在此产生审计记录。</zh-CN>
        ///   <en>Audit category for setting changes; it classifies write callers only and produces no audit record here.</en>
        /// </l>
        /// </param>
        /// <param name="isSensitive">
        /// <l>
        ///   <zh-CN>设置是否敏感；敏感设置不能由数据库在线覆盖，且不应被回显或记录到诊断。</zh-CN>
        ///   <en>Whether the setting is sensitive; sensitive settings cannot be overridden online in the database and must not be echoed or recorded in diagnostics.</en>
        /// </l>
        /// </param>
        /// <param name="sourceLevel">
        /// <l>
        ///   <zh-CN>该定义的主要配置来源层级；它是解析提示，不会在构造时读取配置。</zh-CN>
        ///   <en>Primary configuration source level for this definition; it is resolution guidance and reads no configuration during construction.</en>
        /// </l>
        /// </param>
        /// <param name="minIntegerValue">
        /// <l>
        ///   <zh-CN>整数值的可选最小边界；未指定时不设下限，比较由运行时解析器执行。</zh-CN>
        ///   <en>Optional minimum bound for integer values; no lower bound exists when omitted, and comparison is executed by the runtime resolver.</en>
        /// </l>
        /// </param>
        /// <param name="maxIntegerValue">
        /// <l>
        ///   <zh-CN>整数值的可选最大边界；未指定时不设上限，比较由运行时解析器执行。</zh-CN>
        ///   <en>Optional maximum bound for integer values; no upper bound exists when omitted, and comparison is executed by the runtime resolver.</en>
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
            // <lang>
            //   <zh-CN>构造器只拒绝空白稳定键，避免构造无法被 registry/配置源可靠关联的定义；它不检查键冲突、来源存在、默认文本、范围关系或权限有效性。</zh-CN>
            //   <en>The constructor rejects only a blank stable key, avoiding a definition that registry/configuration sources cannot link reliably; it checks no key collision, source existence, default text, range relation, or permission validity.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Setting key is required.", "key");
            }

            // <lang>
            //   <zh-CN>稳定键按调用方给定文本保留，用于跨 registry、来源和审计关联；显示名称和说明仅归一 null，不在此编码或信任为授权信息。</zh-CN>
            //   <en>Retain the stable key as supplied for registry/source/audit correlation; display name and description normalize null only and are neither encoded here nor trusted as authorization information.</en>
            // </lang>
            Key = key;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;

            // <lang>
            //   <zh-CN>值类型、默认文本和数值范围共同描述后续验证契约；本构造器不把文本转换为类型，也不比较最小/最大边界或确认默认值在范围内。</zh-CN>
            //   <en>Value type, default text, and numeric bounds together describe the later validation contract; this constructor neither converts text to type, compares minimum/maximum bounds, nor confirms the default lies within range.</en>
            // </lang>
            ValueType = valueType;
            DefaultValue = defaultValue ?? string.Empty;

            // <lang>
            //   <zh-CN>这些策略标志和标签供运行时解析、后台写入与页面消费方使用；赋值不会读取配置、写数据库、重启应用、实施授权或产生审计。</zh-CN>
            //   <en>These policy flags and labels are for runtime resolution, administration writes, and page consumers; assignment reads no configuration, writes no database, restarts no application, implements no authorization, and produces no audit.</en>
            // </lang>
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
        ///   <zh-CN>按构造输入原样保留的稳定键名，用于 registry、配置源和审计记录之间的关联；本类型不保证唯一或已登记。</zh-CN>
        ///   <en>Stable key retained exactly from constructor input for registry, configuration-source, and audit correlation; this type guarantees neither uniqueness nor registration.</en>
        /// </lang>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>后台管理界面显示名称；可为空文本，显示编码由消费方负责。</zh-CN>
        ///   <en>Display name for administration UI; it may be empty text, and display encoding is the responsibility of consumers.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>面向管理员的设置说明；可为空文本，不作为授权或输入验证规则。</zh-CN>
        ///   <en>Administrator-facing setting description; it may be empty text and is neither authorization nor input-validation rule.</en>
        /// </lang>
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置值的基础类型；运行时解析器据此验证和转换，定义构造本身不解析文本。</zh-CN>
        ///   <en>Base type of the setting value; the runtime resolver validates and converts by it, while definition construction itself parses no text.</en>
        /// </lang>
        /// </summary>
        public PortalSettingValueType ValueType { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>文本形式的默认值；读取时再根据 <see cref="ValueType"/> 和整数边界进行校验和转换，未保证构造时已可采用。</zh-CN>
        ///   <en>Default value stored as text; it is validated and converted by <see cref="ValueType"/> and integer bounds when read, and construction does not guarantee it is already adoptable.</en>
        /// </lang>
        /// </summary>
        public string DefaultValue { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否允许后台在线创建或修改数据库覆盖值；仍须由页面/服务执行实际授权与值校验。</zh-CN>
        ///   <en>Whether administration UI may create or update a database override online; pages/services must still execute actual authorization and value validation.</en>
        /// </lang>
        /// </summary>
        public bool CanEditOnline { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置变更后是否需要应用重启或重新加载才会生效；仅为提示元数据，不在此触发应用动作。</zh-CN>
        ///   <en>Whether a setting change requires application restart or reload to take effect; it is guidance metadata only and triggers no application action here.</en>
        /// </lang>
        /// </summary>
        public bool RequiresRestart { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前阶段所需的角色或权限表达式；它是消费方的策略输入，不会自行授予访问。</zh-CN>
        ///   <en>Role or permission expression required in the current phase; it is policy input for consumers and grants no access by itself.</en>
        /// </lang>
        /// </summary>
        public string Permission { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置变更使用的审计分类；写入消费方使用该标签，定义本身不产生审计事件。</zh-CN>
        ///   <en>Audit category used for setting changes; write consumers use this label, while the definition itself produces no audit event.</en>
        /// </lang>
        /// </summary>
        public string AuditCategory { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置是否敏感；敏感设置不允许通过数据库在线覆盖，且消费方不得在页面或诊断中回显其值。</zh-CN>
        ///   <en>Whether the setting is sensitive; sensitive settings cannot be overridden online in the database, and consumers must not echo their values in pages or diagnostics.</en>
        /// </lang>
        /// </summary>
        public bool IsSensitive { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前主要配置来源层级；仅提供解析提示，不代表来源已访问或值已采用。</zh-CN>
        ///   <en>Current primary configuration source level; it provides resolution guidance only and does not mean the source was accessed or a value adopted.</en>
        /// </lang>
        /// </summary>
        public string SourceLevel { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>整数设置可接受的最小值；未指定时不设下限，运行时解析器负责实际比较。</zh-CN>
        ///   <en>Minimum accepted integer value; no lower bound exists when unspecified, and the runtime resolver performs actual comparison.</en>
        /// </lang>
        /// </summary>
        public int? MinIntegerValue { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>整数设置可接受的最大值；未指定时不设上限，运行时解析器负责实际比较。</zh-CN>
        ///   <en>Maximum accepted integer value; no upper bound exists when unspecified, and the runtime resolver performs actual comparison.</en>
        /// </lang>
        /// </summary>
        public int? MaxIntegerValue { get; private set; }
    }
}
