using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>系统设置元数据 registry。</zh-CN>
    ///   <en>Registry of system-setting metadata.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>registry 集中描述设置项，再由运行时解析器、数据库覆盖层和后台管理界面按其契约工作。已登记的定义是可受控读取和在线管理的唯一入口，不能以任意键绕过元数据校验；本类只提供静态策略目录，不读取实时配置、写数据库或实施页面授权。</zh-CN>
    ///   <en>The registry centralizes setting definitions, which are then used by the runtime resolver, database override layer, and administration UI. Registered definitions are the only controlled entry point for reads and online management, and arbitrary keys must not bypass metadata validation; this class provides a static policy catalog only and reads no live configuration, writes no database, and implements no page authorization.</en>
    /// </lang>
    /// </remarks>
    public static class PortalSettingsRegistry
    {
        // <lang>
        //   <zh-CN>注册与账户准入策略：这些静态定义描述受控默认值和在线编辑条件，但不会显示注册页、创建用户或绕过管理员授权。</zh-CN>
        //   <en>Registration and account-admission policy: these static definitions describe controlled defaults and online-editing conditions but do not display a registration page, create users, or bypass administrator authorization.</en>
        // </lang>
        /// <summary>
        /// <lang>
        ///   <zh-CN>控制是否开放用户自主注册的设置定义，默认关闭。</zh-CN>
        ///   <en>Setting definition controlling whether user self-registration is available; disabled by default.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition AllowSelfRegistration =
            new PortalSettingDefinition(
                PortalSettingKeys.AllowSelfRegistration,
                "是否允许自主注册",
                "控制是否显示注册链接并允许访问注册页；默认关闭。",
                PortalSettingValueType.Boolean,
                "false",
                true,
                false,
                "Admins",
                "Security");

        /// <summary>
        /// <lang>
        ///   <zh-CN>控制自主注册用户是否需要管理员审核的设置定义，默认启用。</zh-CN>
        ///   <en>Setting definition controlling whether self-registered users require administrator approval; enabled by default.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition RequireRegistrationApproval =
            new PortalSettingDefinition(
                PortalSettingKeys.RequireRegistrationApproval,
                "自主注册需要审核",
                "控制自主注册用户是否必须先由管理员批准才能登录；默认开启。",
                PortalSettingValueType.Boolean,
                "true",
                true,
                false,
                "Admins",
                "Security");

        // <lang>
        //   <zh-CN>密码与凭据提交策略：类型、硬范围和默认值只是消费方验证契约，不能降低既有密码安全实现或替代身份/权限校验。</zh-CN>
        //   <en>Password and credential-submission policy: type, hard ranges, and defaults are consumer validation contracts only and cannot lower established password security implementation or replace identity/permission checks.</en>
        // </lang>
        /// <summary>
        /// <lang>
        ///   <zh-CN>控制登录密码提交是否必须使用前端一次性公钥加密的设置定义。</zh-CN>
        ///   <en>Setting definition controlling whether login-password submission must use one-time public-key encryption on the client.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>P10.3 当前默认开启。后续会按客户端浏览器环境选择加密强度，但该开关仍表达“是否必须加密提交”的业务策略。</zh-CN>
        ///   <en>Enabled by default in P10.3. Later work will select encryption strength by client browser capability, while this setting continues to represent whether encrypted submission is required.</en>
        /// </lang>
        /// </remarks>
        public static readonly PortalSettingDefinition RequireEncryptedLoginPassword =
            new PortalSettingDefinition(
                PortalSettingKeys.RequireEncryptedLoginPassword,
                "登录密码必须前端加密",
                "控制登录密码是否必须通过一次性 RSA 公钥加密后提交；默认开启。",
                PortalSettingValueType.Boolean,
                "true",
                true,
                false,
                "Admins",
                "Security");

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码策略最小长度设置定义，硬下限为 8 位。</zh-CN>
        ///   <en>Setting definition for password-policy minimum length, with an 8-character hard lower bound.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition PasswordMinimumLength =
            new PortalSettingDefinition(
                PortalSettingKeys.PasswordMinimumLength,
                "密码最小长度",
                "控制新建、注册和重置密码的最小长度；不能低于 8 位。",
                PortalSettingValueType.Integer,
                "8",
                true,
                false,
                "Admins",
                "Security",
                minIntegerValue: 8,
                maxIntegerValue: 128);

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码策略要求的字符类别数量设置定义。</zh-CN>
        ///   <en>Setting definition for the required password character-category count.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition PasswordRequiredCategoryCount =
            new PortalSettingDefinition(
                PortalSettingKeys.PasswordRequiredCategoryCount,
                "密码字符类别数量",
                "控制密码需同时满足大写、小写、数字、特殊字符中的几类；当前允许 3 到 4 类。",
                PortalSettingValueType.Integer,
                "3",
                true,
                false,
                "Admins",
                "Security",
                minIntegerValue: 3,
                maxIntegerValue: 4);

        /// <summary>
        /// <lang>
        ///   <zh-CN>常见弱口令字典检测设置定义。</zh-CN>
        ///   <en>Setting definition for common weak-password dictionary checks.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition PasswordWeakDictionaryEnabled =
            new PortalSettingDefinition(
                PortalSettingKeys.PasswordWeakDictionaryEnabled,
                "启用弱口令字典",
                "控制是否拒绝常见弱口令和单字符重复密码；默认启用。",
                PortalSettingValueType.Boolean,
                "true",
                true,
                false,
                "Admins",
                "Security");

        /// <summary>
        /// <lang>
        ///   <zh-CN>禁止密码包含账号上下文词的设置定义。</zh-CN>
        ///   <en>Setting definition for disallowing account-context terms in passwords.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition PasswordDisallowContextTerms =
            new PortalSettingDefinition(
                PortalSettingKeys.PasswordDisallowContextTerms,
                "禁止账号相关密码",
                "控制是否拒绝包含用户名、邮箱、员工号、显示名等账号相关词的密码；默认启用。",
                PortalSettingValueType.Boolean,
                "true",
                true,
                false,
                "Admins",
                "Security");

        /// <summary>
        /// <lang>
        ///   <zh-CN>临时注册链接默认有效天数的设置定义。</zh-CN>
        ///   <en>Setting definition for the default validity period of temporary registration invite links.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition RegistrationInviteDefaultExpiryDays =
            new PortalSettingDefinition(
                PortalSettingKeys.RegistrationInviteDefaultExpiryDays,
                "注册链接默认有效天数",
                "临时注册链接默认有效天数；第一版用于设计与后续创建入口。",
                PortalSettingValueType.Integer,
                "7",
                true,
                false,
                "Admins",
                "Registration",
                minIntegerValue: 1);

        // <lang>
        //   <zh-CN>员工绑定与注册审核策略：这些值只约束既有准入流程，不能创建用户绑定、替代身份核验或绕过审批。</zh-CN>
        //   <en>Employee-binding and registration-approval policy: these values constrain established admission flows only and cannot create user bindings, replace identity validation, or bypass approval.</en>
        // </lang>
        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号尚未绑定时是否可继续进入待审核注册流程的设置定义。</zh-CN>
        ///   <en>Setting definition controlling whether a registration with pending employee binding may continue to approval.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition AllowPendingEmployeeBinding =
            new PortalSettingDefinition(
                PortalSettingKeys.AllowPendingEmployeeBinding,
                "允许待绑定员工号注册",
                "员工号绑定失败时是否仍允许注册进入待审核；默认不允许。",
                PortalSettingValueType.Boolean,
                "false",
                true,
                false,
                "Admins",
                "Registration");

        // <lang>
        //   <zh-CN>上传与文档安全策略：整数限制和扩展名 allowlist 由文件消费方再次验证，registry 本身不接收上传、写文件或开放路径。</zh-CN>
        //   <en>Upload and document-safety policy: file consumers validate integer limits and extension allowlist again; the registry itself accepts no upload, writes no file, and opens no path.</en>
        // </lang>
        /// <summary>
        /// <lang>
        ///   <zh-CN>文档模块允许上传的单个文件最大字节数设置定义。</zh-CN>
        ///   <en>Setting definition for the maximum bytes allowed for one document-module upload.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition MaxUploadBytes =
            new PortalSettingDefinition(
                PortalSettingKeys.MaxUploadBytes,
                "文档上传大小上限",
                "控制文档模块允许上传的单文件最大字节数。",
                PortalSettingValueType.Integer,
                "10485760",
                true,
                false,
                "Admins",
                "Documents",
                minIntegerValue: 1,
                maxIntegerValue: PortalDocumentPolicy.InfrastructureMaximumUploadBytes);

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档模块允许上传扩展名列表的设置定义。</zh-CN>
        ///   <en>Setting definition for the document-module upload-extension allowlist.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该文本设置只用于从 <see cref="PortalDocumentPolicy"/> 的硬允许集中选择子集；即使数据库覆盖值 包含其他扩展名，也不会使脚本、页面、配置或可执行文件可上传。</zh-CN>
        ///   <en>This text setting selects a subset of <see cref="PortalDocumentPolicy"/>'s hard allowlist only; a database override containing other extensions never makes scripts, pages, configuration files, or executables uploadable.</en>
        /// </lang>
        /// </remarks>
        public static readonly PortalSettingDefinition AllowedDocumentExtensions =
            new PortalSettingDefinition(
                PortalSettingKeys.AllowedDocumentExtensions,
                "文档上传允许扩展名",
                "以逗号分隔的上传允许扩展名；只能收紧内置安全类型集合。",
                PortalSettingValueType.String,
                "pdf,txt,csv,json,doc,docx,xls,xlsx,ppt,pptx,zip",
                true,
                false,
                "Admins",
                "Documents");

        // <lang>
        //   <zh-CN>主题选择策略：该枚举定义允许值的元数据边界，实际资源存在、路径安全和请求期主题选择仍由主题消费方负责。</zh-CN>
        //   <en>Theme-selection policy: this enumeration defines the metadata boundary for allowed values, while actual resource existence, path safety, and request-time theme choice remain the responsibility of theme consumers.</en>
        // </lang>
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户 Web Forms 主题名称的设置定义。</zh-CN>
        ///   <en>Setting definition for the Portal Web Forms theme name.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition ThemeName =
            new PortalSettingDefinition(
                PortalSettingKeys.ThemeName,
                "门户主题名",
                "当前 WebForms 主题名称；只允许选择已部署且通过清单校验的主题，非法或缺失时回退 Default。",
                PortalSettingValueType.Enum,
                "EnterpriseLight",
                true,
                false,
                "Admins",
                "Theme");

        // <lang>
        //   <zh-CN>诊断策略：详细错误、目录、容量、保留期和管理员详情可见性均为受控元数据；它们不直接读写诊断文件、暴露异常详情或授予访问。</zh-CN>
        //   <en>Diagnostics policy: detailed errors, directory, capacity, retention, and administrator-detail visibility are controlled metadata; they do not directly read or write diagnostic files, expose exception detail, or grant access.</en>
        // </lang>
        /// <summary>
        /// <lang>
        ///   <zh-CN>详细错误输出状态的只读设置定义。</zh-CN>
        ///   <en>Read-only setting definition for detailed-error output status.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition DiagnosticsDetailedErrors =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsDetailedErrors,
                "详细错误输出",
                "控制是否允许详细错误输出；本阶段只显示状态，不允许在线修改。",
                PortalSettingValueType.Boolean,
                "false",
                false,
                true,
                "Admins",
                "Diagnostics");

        /// <summary>
        /// <lang>
        ///   <zh-CN>诊断日志目录的部署级只读设置定义。</zh-CN>
        ///   <en>Deployment-level read-only setting definition for the diagnostics log directory.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition DiagnosticsLogDirectory =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsLogDirectory,
                "诊断日志目录",
                "诊断日志文件目录；留空或未配置时使用 App_Data/Logs。",
                PortalSettingValueType.Path,
                "App_Data/Logs",
                false,
                true,
                "Admins",
                "Diagnostics");

        /// <summary>
        /// <lang>
        ///   <zh-CN>结构化诊断日志单文件大小上限的部署级只读设置定义。</zh-CN>
        ///   <en>Deployment-level read-only setting definition for the structured diagnostics log-file size limit.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition DiagnosticsMaxFileBytes =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsMaxFileBytes,
                "诊断日志单文件大小上限",
                "结构化诊断日志单个文件允许的最大字节数；达到上限后自动滚动到下一个序号文件。",
                PortalSettingValueType.Integer,
                "10485760",
                false,
                true,
                "Admins",
                "Diagnostics",
                minIntegerValue: 1024,
                maxIntegerValue: 104857600);

        /// <summary>
        /// <lang>
        ///   <zh-CN>结构化诊断日志保留天数的部署级只读设置定义。</zh-CN>
        ///   <en>Deployment-level read-only setting definition for structured diagnostics-log retention days.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition DiagnosticsRetentionDays =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsRetentionDays,
                "诊断日志保留天数",
                "结构化诊断日志保留的天数；写入新事件时每天最多执行一次受限清理。",
                PortalSettingValueType.Integer,
                "90",
                false,
                true,
                "Admins",
                "Diagnostics",
                minIntegerValue: 1,
                maxIntegerValue: 3650);

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员查看已净化诊断详情权限的部署级只读设置定义。</zh-CN>
        ///   <en>Deployment-level read-only setting definition for administrator access to sanitized diagnostic details.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition DiagnosticsAllowAdminDetailView =
            new PortalSettingDefinition(
                PortalSettingKeys.DiagnosticsAllowAdminDetailView,
                "允许管理员查看诊断详情",
                "控制 Admins 是否可查看已净化的异常详情、物理路径、用户名、IP 和 User-Agent。",
                PortalSettingValueType.Boolean,
                "true",
                false,
                true,
                "Admins",
                "Diagnostics");

        // <lang>
        //   <zh-CN>HIA 外围实例标识策略：留空保持适配器不启用；此定义不是对外连接、密钥、端点或发布动作。</zh-CN>
        //   <en>HIA peripheral instance-identifier policy: blank keeps adapters disabled; this definition is not an external connection, secret, endpoint, or release action.</en>
        // </lang>
        /// <summary>
        /// <lang>
        ///   <zh-CN>HIA 外围契约使用的部署级门户实例标识设置定义。</zh-CN>
        ///   <en>Deployment-level Portal instance identifier setting definition for the HIA peripheral contract.</en>
        /// </lang>
        /// </summary>
        public static readonly PortalSettingDefinition HiaPortalInstanceId =
            new PortalSettingDefinition(
                PortalSettingKeys.HiaPortalInstanceId,
                "HIA 门户实例标识",
                "用于未来 HIA 外围能力描述的非敏感稳定实例标识；留空时不启用任何对外适配器。",
                PortalSettingValueType.String,
                string.Empty,
                false,
                true,
                "Admins",
                "HiaBoundary",
                sourceLevel: "AppSettings");

        /// <summary>
        /// <lang>
        ///   <zh-CN>按稳定声明顺序保存的全部受控设置定义只读集合。</zh-CN>
        ///   <en>Read-only collection of all controlled setting definitions in stable declaration order.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>顺序是 registry 输出契约的一部分；集合包装为只读，且包含的定义不可变。调用方不得据此推断配置已读取、权限已授予或数据库覆盖已采用。</zh-CN>
        ///   <en>Order is part of the registry output contract; the collection is wrapped as read-only and its definitions are immutable. Callers must not infer that configuration was read, permission granted, or a database override adopted from it.</en>
        /// </lang>
        /// </remarks>
        private static readonly IList<PortalSettingDefinition> AllDefinitions =
            // <lang>
            //   <zh-CN>集中列出所有受控定义并冻结集合，供精确键查找和后台展示复用；静态初始化不触发配置、数据库或请求访问。</zh-CN>
            //   <en>List every controlled definition centrally and freeze the collection for exact-key lookup and administration-display reuse; static initialization triggers no configuration, database, or request access.</en>
            // </lang>
            new List<PortalSettingDefinition>
            {
                AllowSelfRegistration,
                RequireRegistrationApproval,
                RequireEncryptedLoginPassword,
                PasswordMinimumLength,
                PasswordRequiredCategoryCount,
                PasswordWeakDictionaryEnabled,
                PasswordDisallowContextTerms,
                RegistrationInviteDefaultExpiryDays,
                AllowPendingEmployeeBinding,
                MaxUploadBytes,
                AllowedDocumentExtensions,
                ThemeName,
                DiagnosticsDetailedErrors,
                DiagnosticsLogDirectory,
                DiagnosticsMaxFileBytes,
                DiagnosticsRetentionDays,
                DiagnosticsAllowAdminDetailView,
                HiaPortalInstanceId
            }.AsReadOnly();

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前已登记的全部设置定义。</zh-CN>
        ///   <en>Gets all currently registered setting definitions.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>只读定义集合，调用方不应修改其中的定义。</zh-CN>
        ///   <en>Read-only definition collection; callers must not modify the contained definitions.</en>
        /// </l>
        /// </returns>
        public static IEnumerable<PortalSettingDefinition> GetAll()
        {
            // <lang>
            //   <zh-CN>返回同一只读集合而非可变副本；定义和顺序保持 registry 的静态契约，调用方只能枚举，不能借此改变策略。</zh-CN>
            //   <en>Return the same read-only collection rather than a mutable copy; definitions and order retain the registry's static contract, so callers can enumerate but cannot change policy through it.</en>
            // </lang>
            return AllDefinitions;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按稳定键名查找设置定义。</zh-CN>
        ///   <en>Finds a setting definition by its stable key.</en>
        /// </lang>
        /// </summary>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>要精确查找的稳定设置键；不做空白规范化或大小写折叠。</zh-CN>
        ///   <en>Stable setting key to find exactly; no whitespace normalization or case folding occurs.</en>
        /// </l>
        /// </param>
        /// <param name="definition">
        /// <l>
        ///   <zh-CN>找到时返回对应定义；否则为 <c>null</c>。</zh-CN>
        ///   <en>Matching definition when found; otherwise <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>找到已登记定义时为 <c>true</c>；找不到时输出为 <c>null</c>，不读取其它来源或产生授权结论。</zh-CN>
        ///   <en><c>true</c> when a registered definition is found; when absent, output is <c>null</c> and no other source is read or authorization conclusion produced.</en>
        /// </l>
        /// </returns>
        public static bool TryGet(string key, out PortalSettingDefinition definition)
        {
            // <lang>
            //   <zh-CN>以声明顺序遍历只读 registry；这不是配置来源回退，也不把未知键转换为动态定义。</zh-CN>
            //   <en>Traverse the read-only registry in declaration order; this is not configuration-source fallback and does not turn an unknown key into a dynamic definition.</en>
            // </lang>
            foreach (PortalSettingDefinition item in AllDefinitions)
            {
                // <lang>
                //   <zh-CN>使用现有 string 精确相等语义匹配稳定键；不去空白、不忽略大小写，也不检查调用方身份或页面权限。</zh-CN>
                //   <en>Match stable keys using the existing exact string-equality semantics; do not trim, ignore case, or check caller identity or page permission.</en>
                // </lang>
                if (item.Key == key)
                {
                    definition = item;
                    return true;
                }
            }

            // <lang>
            //   <zh-CN>未知或格式不同的键以 false/null 表示；调用方决定拒绝、回退或显示方式，registry 不执行配置读取、写入或诊断。</zh-CN>
            //   <en>Represent an unknown or differently formatted key by false/null; callers decide rejection, fallback, or display, while the registry performs no configuration read, write, or diagnostic action.</en>
            // </lang>
            definition = null;
            return false;
        }
    }
}
