using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>模块装配态的封闭两值：生产预编译态与受控调试态。</zh-CN>
    ///   <en>Closed two-value vocabulary for module assembly modes: the production precompiled mode and the controlled debug mode.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>取值一经写入台账、迁移记录与文档即成为稳定契约，不得重命名；本类只定义词表，不判断当前环境是否允许调试态，也不执行装配动作。</zh-CN>
    ///   <en>These values become a stable contract once written into the ledger, migration records, and documentation, and must not be renamed; this class defines the vocabulary only, judges no environment permission, and performs no assembly action.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleAssemblyModes
    {
        /// <summary><lang><zh-CN>生产默认态：Web Application 预编译装配。</zh-CN><en>Production default: Web Application precompiled assembly.</en></lang></summary>
        public const string CodeBehind = "CodeBehind";

        /// <summary><lang><zh-CN>受控调试态：运行时编译装配；本周期不进入该态。</zh-CN><en>Controlled debug mode: runtime-compiled assembly; this cycle never enters it.</en></lang></summary>
        public const string CodeFile = "CodeFile";

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断文本是否为已登记的装配态值，采用精确大小写比较，不接受同义词或第三值。</zh-CN>
        ///   <en>Determines whether text is a registered assembly mode using exact case-sensitive comparison; synonyms and third values are rejected.</en>
        /// </lang>
        /// </summary>
        /// <param name="mode"><l zh-CN="待判定的装配态文本，可为 null。" en="Assembly-mode text to check; may be null." /></param>
        /// <returns><l zh-CN="仅当等于两个已登记值之一时为 true。" en="True only when equal to one of the two registered values." /></returns>
        public static bool IsRegistered(string mode)
        {
            // <lang>
            //   <zh-CN>两个常量即封闭集合全部成员；不做 Trim 与大小写折叠，避免相似输入被误当作已登记值。</zh-CN>
            //   <en>The two constants are the complete closed set; no trimming or case folding keeps merely similar input from being mistaken for a registered value.</en>
            // </lang>
            return string.Equals(mode, CodeBehind, StringComparison.Ordinal) ||
                   string.Equals(mode, CodeFile, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>独立部署侧装配态台账的只读快照：按稳定包标识保存已显式登记的装配态。</zh-CN>
    ///   <en>Read-only snapshot of the deployment-side module-assembly state ledger, holding explicitly registered modes keyed by stable package identifier.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>台账是装配态的唯一事实源，与包启用状态、能力归属、Profile gate 严格分离；无条目的包按受控缺省视为 CodeBehind。本类型不读取文件、数据库或 HTTP 请求，也不写回任何来源。</zh-CN>
    ///   <en>The ledger is the single source of truth for assembly mode and stays strictly separate from package enablement, capability ownership, and the Profile gate; a package without an entry uses the controlled CodeBehind default. This type reads no file, database, or HTTP request and writes back to no source.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalModuleAssemblyState
    {
        /// <summary><lang><zh-CN>稳定包标识到已登记装配态的内部映射；空集合表示全缺省态。</zh-CN><en>Internal map from stable package identifier to registered mode; empty means all-default.</en></lang></summary>
        private readonly IDictionary<string, string> _modesByPackageId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>以已校验的映射构造快照；调用方负责保证键与值均通过校验。</zh-CN>
        ///   <en>Constructs the snapshot from a validated map; callers must ensure keys and values passed validation.</en>
        /// </lang>
        /// </summary>
        /// <param name="modesByPackageId"><l zh-CN="已校验映射，不能为 null。" en="Validated map; cannot be null." /></param>
        /// <exception cref="ArgumentNullException"><l zh-CN="映射为 null 时引发。" en="Thrown when the map is null." /></exception>
        internal PortalModuleAssemblyState(IDictionary<string, string> modesByPackageId)
        {
            // <lang>
            //   <zh-CN>空映射是合法缺省；拒绝 null 避免把"未初始化"误表达成"所有包缺省"。</zh-CN>
            //   <en>An empty map is a valid default; rejecting null keeps "not initialized" from being misrepresented as "all packages default".</en>
            // </lang>
            if (modesByPackageId == null)
            {
                throw new ArgumentNullException("modesByPackageId");
            }

            // <lang>
            //   <zh-CN>复制为 Ordinal 比较的新字典，使快照不共享可变来源且键匹配口径固定。</zh-CN>
            //   <en>Copy into a new Ordinal-comparer dictionary so the snapshot shares no mutable source and key matching stays fixed.</en>
            // </lang>
            _modesByPackageId = new Dictionary<string, string>(modesByPackageId, StringComparer.Ordinal);
        }

        /// <summary><lang><zh-CN>显式登记条目数，供诊断与门禁判断是否全缺省。</zh-CN><en>Number of explicit entries, used by diagnostics and gates to detect an all-default ledger.</en></lang></summary>
        public int EntryCount
        {
            get { return _modesByPackageId.Count; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断台账中是否存在显式登记的受控调试态条目，供部署级开关与封版前复核点作为判定输入。</zh-CN>
        ///   <en>Determines whether the ledger contains any explicitly registered controlled-debug-mode entry, serving as input to the deployment-level switch and the pre-release review point.</en>
        /// </lang>
        /// </summary>
        /// <returns><l zh-CN="至少一个条目的装配态为 CodeFile 时为 true；全缺省或仅 CodeBehind 时为 false。" en="True when at least one entry is CodeFile; false for an all-default or CodeBehind-only ledger." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>这是"环境开关该不该拦"的纯函数判据，不读取配置、不读取请求、不修改任何状态；实际接线由部署侧与封版前复核门禁完成，本方法不实施页面授权。</zh-CN>
        ///   <en>This is the pure predicate behind "whether the environment switch should block"; it reads no config or request and mutates no state. Actual wiring belongs to deployment and the pre-release review gate; this method implements no page authorization.</en>
        /// </lang>
        /// </remarks>
        public bool HasCodeFileEntries()
        {
            // <lang>
            //   <zh-CN>逐条检查已登记的装配态；只做精确比较，缺省条目不在映射中因而天然不触发。</zh-CN>
            //   <en>Check each registered mode with exact comparison; default entries are absent from the map and therefore never trigger it.</en>
            // </lang>
            foreach (string mode in _modesByPackageId.Values)
            {
                // <lang>
                //   <zh-CN>命中受控调试态即返回，不继续遍历；调用方据此决定是否拦截。</zh-CN>
                //   <en>Return as soon as the controlled debug mode is found; callers decide whether to block.</en>
                // </lang>
                if (string.Equals(mode, PortalModuleAssemblyModes.CodeFile, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            // <lang>
            //   <zh-CN>无调试态条目即为合法的生产态台账，无需开关介入。</zh-CN>
            //   <en>No debug-mode entry means a legitimate production ledger with no need for switch intervention.</en>
            // </lang>
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按部署级开关判定当前台账是否允许被接受：开关关闭时，任何调试态条目都使台账不合格（fail-closed）。</zh-CN>
        ///   <en>Decides whether the ledger is acceptable under the deployment-level switch: when the switch is off, any debug-mode entry makes the ledger unacceptable (fail-closed).</en>
        /// </lang>
        /// </summary>
        /// <param name="allowCodeFile"><l zh-CN="部署级开关的已解析布尔值，应由部署侧配置提供，不得来自数据库覆盖或请求值。" en="Parsed deployment-level switch value, supplied by deployment config and never from a database override or request value." /></param>
        /// <returns><l zh-CN="台账在此环境下可接受时为 true。" en="True when the ledger is acceptable in this environment." /></returns>
        public bool IsAllowedByEnvironment(bool allowCodeFile)
        {
            // <lang>
            //   <zh-CN>开关显式开启时允许调试态条目；开启动作仍属部署侧受控流程，本方法不执行任何切换。</zh-CN>
            //   <en>An explicitly enabled switch permits debug-mode entries; enabling remains a controlled deployment action and this method performs no switch.</en>
            // </lang>
            if (allowCodeFile)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>开关关闭时只要出现调试态条目就拒绝整份台账，不返回"部分可接受"。</zh-CN>
            //   <en>With the switch off, any debug-mode entry rejects the whole ledger; no "partially acceptable" result exists.</en>
            // </lang>
            return !HasCodeFileEntries();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回一个没有任何显式条目的全缺省快照，供工具与诊断作为受控基线；每次调用返回独立实例。</zh-CN>
        ///   <en>Returns an all-default snapshot with no explicit entries as a controlled baseline for tools and diagnostics; each call returns an independent instance.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>不暴露构造器，避免调用方构造未通过封闭词表校验的映射；本工厂是取得空快照的唯一公开途径。</zh-CN>
        ///   <en>The constructor stays internal so callers cannot build a map that bypassed closed-vocabulary validation; this factory is the only public way to obtain an empty snapshot.</en>
        /// </lang>
        /// </remarks>
        public static PortalModuleAssemblyState Empty
        {
            // <lang>
            //   <zh-CN>新建空字典而非共享静态实例，保证任何调用方都无法通过共享引用污染基线。</zh-CN>
            //   <en>Create a fresh dictionary rather than sharing a static instance so no caller can pollute the baseline through a shared reference.</en>
            // </lang>
            get { return new PortalModuleAssemblyState(new Dictionary<string, string>(StringComparer.Ordinal)); }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回指定包的装配态；台账无条目时返回受控缺省 CodeBehind，绝不返回未登记值。</zh-CN>
        ///   <en>Returns the mode for a package; a missing entry yields the controlled CodeBehind default, and no unregistered value is ever returned.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId"><l zh-CN="稳定包标识；空白或非精确匹配按未登记处理。" en="Stable package identifier; blank or non-exact matches are unregistered." /></param>
        /// <returns><l zh-CN="已登记装配态或 CodeBehind 缺省。" en="A registered mode or the CodeBehind default." /></returns>
        public string GetMode(string packageId)
        {
            // <lang>
            //   <zh-CN>空白标识不可能是受信任包，直接走缺省，避免无意义的键查找。</zh-CN>
            //   <en>A blank identifier cannot be a trusted package, so take the default directly instead of a pointless lookup.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return PortalModuleAssemblyModes.CodeBehind;
            }

            // <lang>
            //   <zh-CN>承接精确键查找结果；局部值不缓存任何外部状态。</zh-CN>
            //   <en>Holds the exact-key lookup result; this local caches no external state.</en>
            // </lang>
            string mode;
            if (_modesByPackageId.TryGetValue(packageId, out mode))
            {
                // <lang>
                //   <zh-CN>映射在构造前已通过封闭词表校验，按不变量返回而不重复解析文本。</zh-CN>
                //   <en>The map passed closed-vocabulary validation before construction, so return it by invariant without re-parsing text.</en>
                // </lang>
                return mode;
            }

            // <lang>
            //   <zh-CN>无条目即生产缺省态，这是 B 裁定的显式语义而非"未知状态"。</zh-CN>
            //   <en>No entry means the production default; this is ruling B's explicit semantics, not an "unknown state".</en>
            // </lang>
            return PortalModuleAssemblyModes.CodeBehind;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从台账 JSON 文本解析只读快照；任何结构或取值不合法都整体失败（fail-closed），不返回部分结果。</zh-CN>
        ///   <en>Parses a read-only snapshot from ledger JSON text; any invalid structure or value fails the whole parse (fail-closed) with no partial result.</en>
        /// </lang>
        /// </summary>
        /// <param name="json"><l zh-CN="台账 UTF-8 文本；空白或 null 视为无效。" en="Ledger UTF-8 text; blank or null is invalid." /></param>
        /// <param name="state"><l zh-CN="成功时的快照；失败时为 null。" en="Snapshot on success; null on failure." /></param>
        /// <param name="reason"><l zh-CN="稳定的失败短语，不含文件内容或路径。" en="Stable failure phrase containing no file content or path." /></param>
        /// <returns><l zh-CN="解析且全部校验通过时为 true。" en="True when parsing and every validation pass." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>只接受本台账自身的 schemaVersion 1 与 entries 对象；不读取 module.json、不查询 PortalModuleCatalog、不访问包启用状态、能力词表或 Profile 配置，从结构上保证四分离。</zh-CN>
        ///   <en>Accepts only this ledger's own schemaVersion 1 and entries object; it never reads module.json, queries PortalModuleCatalog, or touches package enablement, the capability registry, or Profile configuration, structurally guaranteeing the four-way separation.</en>
        /// </lang>
        /// </remarks>
        public static bool TryParse(string json, out PortalModuleAssemblyState state, out string reason)
        {
            // <lang>
            //   <zh-CN>输出先置失败态，保证所有提前返回都不留下半成功快照或未初始化原因。</zh-CN>
            //   <en>Initialize outputs to the failure state so every early return leaves neither a half-success snapshot nor an uninitialized reason.</en>
            // </lang>
            state = null;
            reason = string.Empty;

            // <lang>
            //   <zh-CN>空白文本不是合法台账；按"缺文件即显式报缺"口径处理，不把空文件静默解释成全缺省。</zh-CN>
            //   <en>Blank text is not a valid ledger; a missing file is reported explicitly rather than silently treating an empty file as all-default.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(json))
            {
                reason = "Assembly state ledger text is blank.";
                return false;
            }

            // <lang>
            //   <zh-CN>承接框架序列化器的结果；异常只在本次解析内捕获，不向上抛到调用页面。</zh-CN>
            //   <en>Holds the framework serializer's result; exceptions are caught within this parse and never thrown to a calling page.</en>
            // </lang>
            object parsed;
            try
            {
                // <lang>
                //   <zh-CN>使用 .NET Framework 自带序列化器，不为台账引入新第三方依赖；解析为通用字典后再逐项白名单校验。</zh-CN>
                //   <en>Use the .NET Framework serializer so the ledger adds no new third-party dependency; parse into a generic dictionary before whitelist validation.</en>
                // </lang>
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                parsed = serializer.DeserializeObject(json);
            }
            catch (ArgumentException)
            {
                // <lang>
                //   <zh-CN>格式错误统一收敛为稳定失败短语，不回显原文，也不降级为缺省态。</zh-CN>
                //   <en>Format errors converge to a stable failure phrase without echoing raw text and are not downgraded to the default state.</en>
                // </lang>
                reason = "Assembly state ledger JSON is malformed.";
                return false;
            }
            catch (InvalidOperationException)
            {
                // <lang>
                //   <zh-CN>不合法结构也可能抛出此异常，按同一口径失败，避免结构差异改变安全结论。</zh-CN>
                //   <en>The serializer may throw this for invalid structures too; fail under the same convention so structural differences cannot change the safety conclusion.</en>
                // </lang>
                reason = "Assembly state ledger JSON is malformed.";
                return false;
            }

            // <lang>
            //   <zh-CN>根节点必须是键值对象；数组、标量与 null 都不是台账。</zh-CN>
            //   <en>The root must be a key-value object; arrays, scalars, and null are not a ledger.</en>
            // </lang>
            Dictionary<string, object> root = parsed as Dictionary<string, object>;
            if (root == null)
            {
                reason = "Assembly state ledger root must be an object.";
                return false;
            }

            // <lang>
            //   <zh-CN>承接 schemaVersion 原始值；要求存在且为整数 1，未知版本不做兼容性猜测。</zh-CN>
            //   <en>Holds the raw schemaVersion; require it to exist and be integer 1, with no compatibility guess for unknown versions.</en>
            // </lang>
            object schemaToken;
            if (!root.TryGetValue("schemaVersion", out schemaToken) ||
                !(schemaToken is int) ||
                (int)schemaToken != 1)
            {
                reason = "Assembly state ledger schemaVersion must be 1.";
                return false;
            }

            // <lang>
            //   <zh-CN>根节点只允许两个键；多余键一律拒绝，避免未知策略被静默忽略形成"声明了但不生效"。</zh-CN>
            //   <en>The root allows exactly two keys; extras are rejected so unknown policy cannot be silently ignored into a "declared but ineffective" state.</en>
            // </lang>
            foreach (string rootKey in root.Keys)
            {
                if (!string.Equals(rootKey, "schemaVersion", StringComparison.Ordinal) &&
                    !string.Equals(rootKey, "entries", StringComparison.Ordinal))
                {
                    reason = "Assembly state ledger contains an unknown root field.";
                    return false;
                }
            }

            // <lang>
            //   <zh-CN>entries 必须存在且为对象；缺失或形状错误都失败，不把空对象与缺失键混为一谈。</zh-CN>
            //   <en>entries must exist and be an object; a missing or wrongly shaped field fails, keeping an empty object distinct from a missing key.</en>
            // </lang>
            object entriesToken;
            if (!root.TryGetValue("entries", out entriesToken))
            {
                reason = "Assembly state ledger entries must be an object.";
                return false;
            }

            // <lang>
            //   <zh-CN>承接条目字典；单独转型使形状判定与上面的缺失判定各自可核。</zh-CN>
            //   <en>Holds the entries dictionary; a separate cast keeps the shape check verifiable independently from the missing-key check.</en>
            // </lang>
            Dictionary<string, object> entries = entriesToken as Dictionary<string, object>;
            if (entries == null)
            {
                reason = "Assembly state ledger entries must be an object.";
                return false;
            }

            // <lang>
            //   <zh-CN>承接逐条校验后的映射；只有全部条目合法才用于构造快照，保证无部分结果。</zh-CN>
            //   <en>Holds the validated map; it constructs the snapshot only after every entry passed, guaranteeing no partial result.</en>
            // </lang>
            IDictionary<string, string> validated = new Dictionary<string, string>(StringComparer.Ordinal);

            // <lang>
            //   <zh-CN>逐条校验包标识、条目形状、字段白名单与装配态；任一失败即整体失败。</zh-CN>
            //   <en>Validate each package identifier, entry shape, field whitelist, and mode; any single failure fails the whole parse.</en>
            // </lang>
            foreach (KeyValuePair<string, object> entry in entries)
            {
                // <lang>
                //   <zh-CN>只接受本仓受控命名空间的稳定包标识；不推断任意第三方标识，长度上限与审计列对齐。</zh-CN>
                //   <en>Accept only stable identifiers in this repository's controlled namespaces; no arbitrary third-party identifier is inferred, and the length cap aligns with audit columns.</en>
                // </lang>
                string packageId = entry.Key;
                if (string.IsNullOrWhiteSpace(packageId) ||
                    !(packageId.StartsWith("HIA.", StringComparison.Ordinal) ||
                      packageId.StartsWith("Legacy.", StringComparison.Ordinal)) ||
                    packageId.Length > 200)
                {
                    reason = "Assembly state ledger contains an invalid packageId.";
                    return false;
                }

                // <lang>
                //   <zh-CN>条目必须是键值对象；字符串或数组无法承载具名字段。</zh-CN>
                //   <en>An entry must be a key-value object; string or array shapes cannot carry named fields.</en>
                // </lang>
                Dictionary<string, object> fields = entry.Value as Dictionary<string, object>;
                if (fields == null)
                {
                    reason = "Assembly state ledger entry must be an object.";
                    return false;
                }

                // <lang>
                //   <zh-CN>条目只允许装配态与四个追溯字段；多余字段拒绝，防止台账被当作通用配置容器。</zh-CN>
                //   <en>An entry allows only the mode and four traceability fields; extras are rejected so the ledger is not misused as a general config container.</en>
                // </lang>
                foreach (string fieldKey in fields.Keys)
                {
                    if (!IsAllowedEntryField(fieldKey))
                    {
                        reason = "Assembly state ledger entry contains an unknown field.";
                        return false;
                    }
                }

                // <lang>
                //   <zh-CN>assemblyMode 必须存在、为字符串且属封闭两值；空值不被当作 CodeBehind 缺省。</zh-CN>
                //   <en>assemblyMode must exist, be a string, and belong to the closed two-value set; a blank value is not treated as the CodeBehind default.</en>
                // </lang>
                object modeToken;
                if (!fields.TryGetValue("assemblyMode", out modeToken))
                {
                    reason = "Assembly state ledger assemblyMode is invalid.";
                    return false;
                }

                // <lang>
                //   <zh-CN>承接装配态文本；单独转型使类型判定与缺失判定各自可核。</zh-CN>
                //   <en>Holds the mode text; a separate cast keeps the type check verifiable independently from the missing-key check.</en>
                // </lang>
                string mode = modeToken as string;
                if (mode == null || !PortalModuleAssemblyModes.IsRegistered(mode))
                {
                    reason = "Assembly state ledger assemblyMode is invalid.";
                    return false;
                }

                // <lang>
                //   <zh-CN>四个追溯字段若出现则必须是字符串，避免结构化对象混入只供审计展示的文本列。</zh-CN>
                //   <en>The four traceability fields, when present, must be strings so structured objects cannot leak into audit-display text columns.</en>
                // </lang>
                foreach (string traceKey in new string[] { "recordedUtc", "recordedBy", "sourceHash", "note" })
                {
                    // <lang>
                    //   <zh-CN>承接本轮追溯字段的原始值；只判形状不判内容，内容净化由写入方负责。</zh-CN>
                    //   <en>Holds the current trace field's raw value; only its shape is checked, while content sanitization belongs to the writer.</en>
                    // </lang>
                    object traceToken;
                    if (fields.TryGetValue(traceKey, out traceToken) && !(traceToken is string))
                    {
                        reason = "Assembly state ledger trace field must be a string.";
                        return false;
                    }
                }

                // <lang>
                //   <zh-CN>全部校验通过后写入映射；键集合由反序列化器去重，此处不做静默覆盖。</zh-CN>
                //   <en>Add to the map only after all checks pass; deserialization already de-duplicates keys, so no silent overwrite occurs here.</en>
                // </lang>
                validated.Add(packageId, mode);
            }

            // <lang>
            //   <zh-CN>构造只读快照并清空失败原因，标志着本次解析完整成功而非部分成功。</zh-CN>
            //   <en>Construct the read-only snapshot and clear the failure reason, marking a complete rather than partial success.</en>
            // </lang>
            state = new PortalModuleAssemblyState(validated);
            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断字段名是否属于条目白名单（装配态 + 四个追溯字段）。</zh-CN>
        ///   <en>Determines whether a field name belongs to the entry whitelist (mode plus four traceability fields).</en>
        /// </lang>
        /// </summary>
        /// <param name="fieldKey"><l zh-CN="待判定的字段名。" en="Field name to check." /></param>
        /// <returns><l zh-CN="属于白名单时为 true。" en="True when whitelisted." /></returns>
        private static bool IsAllowedEntryField(string fieldKey)
        {
            // <lang>
            //   <zh-CN>五个字段名构成封闭白名单；精确比较避免大小写变体绕过校验。</zh-CN>
            //   <en>Five field names form the closed whitelist; exact comparison keeps case variants from bypassing validation.</en>
            // </lang>
            return string.Equals(fieldKey, "assemblyMode", StringComparison.Ordinal) ||
                   string.Equals(fieldKey, "recordedUtc", StringComparison.Ordinal) ||
                   string.Equals(fieldKey, "recordedBy", StringComparison.Ordinal) ||
                   string.Equals(fieldKey, "sourceHash", StringComparison.Ordinal) ||
                   string.Equals(fieldKey, "note", StringComparison.Ordinal);
        }
    }
}
