using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>解析当前部署环境允许的模块 Profile 与模块包集合。</zh-CN>
    ///   <en>Resolves the module Profile and package set allowed by the current deployment environment.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P20.3 的解析器只读取 Web.config / 环境 JSON appSettings，不读取数据库设置表，也不提供在线启停能力。它表达“当前部署允许哪些能力进入门户”，模块状态表随后再表达“允许范围内是否启用”。</zh-CN>
    ///   <en>The P20.3 resolver reads only Web.config / environment JSON appSettings. It does not read the database setting table or provide online switching. It represents which capabilities this deployment allows into the Portal; the module state table then represents enablement inside that allowed scope.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleProfileResolver
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>未进入当前 Profile 时写入诊断原因的稳定前缀。</zh-CN>
        ///   <en>Stable reason prefix used when a package is not allowed by the current Profile.</en>
        /// </lang>
        /// </summary>
        public const string NotAllowedReasonPrefix = "ModuleProfile.NotAllowed:";

        // <lang>
        //   <zh-CN>未配置、空白或非法活动 Profile 的固定安全回退；它只保留核心能力，不能把未知 Profile 扩展为默认全量包。</zh-CN>
        //   <en>Fixed safe fallback for a missing, blank, or invalid active Profile; it retains only core capability and must not expand an unknown Profile into all packages.</en>
        // </lang>
        private const string CoreOnlyProfile = "CoreOnly";

        // <lang>
        //   <zh-CN>部署级活动 Profile 的受控 appSettings 键；键名稳定，不能由请求或数据库设置替换。</zh-CN>
        //   <en>Controlled appSettings key for the deployment-level active Profile; its stable name cannot be replaced by a request or database setting.</en>
        // </lang>
        private const string ActiveProfileKey = "Portal.ModuleProfiles.Active";

        // <lang>
        //   <zh-CN>为当前部署额外允许包的受控 appSettings 键；它只追加到 Profile 白名单，不表示模块已启用或调用者已获授权。</zh-CN>
        //   <en>Controlled appSettings key for packages additionally allowed by this deployment; it only appends to the Profile allowlist and does not mean a module is enabled or a caller is authorized.</en>
        // </lang>
        private const string EnabledPackagesKey = "Portal.ModulePackages.Enabled";

        // <lang>
        //   <zh-CN>Profile 包列表配置键的固定后缀；与稳定 Profile 名拼接以限制可读取的 appSettings 命名空间。</zh-CN>
        //   <en>Fixed suffix for a Profile package-list key; it joins a stable Profile name to constrain the appSettings namespace that may be read.</en>
        // </lang>
        private const string ProfilePackagesSuffix = ".Packages";

        // <lang>
        //   <zh-CN>Profile include 列表配置键的固定后缀；include 仍须经过名称门禁和循环检查。</zh-CN>
        //   <en>Fixed suffix for a Profile include-list key; every include still passes the name gate and cycle check.</en>
        // </lang>
        private const string ProfileIncludesSuffix = ".Includes";

        // <lang>
        //   <zh-CN>所有 Profile 相关 appSettings 的固定前缀，避免由未规范化输入构造任意配置键。</zh-CN>
        //   <en>Fixed prefix for every Profile-related appSettings entry, preventing unnormalized input from constructing arbitrary configuration keys.</en>
        // </lang>
        private const string ProfileKeyPrefix = "Portal.ModuleProfiles.";

        // <lang>
        //   <zh-CN>Profile 名、include 名和包 id 共享的稳定名称白名单：限制长度和字符集，供配置键拼接及集合比较安全复用。</zh-CN>
        //   <en>Shared stable-name allowlist for Profile names, include names, and package ids: it constrains length and characters for safe reuse in configuration-key composition and set comparison.</en>
        // </lang>
        private static readonly Regex StableNamePattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_.-]{0,99}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // <lang>
        //   <zh-CN>内置 Profile 到包白名单的部署默认映射；仅在对应配置为空时使用，不能由其推断任意模块均可进入门户。</zh-CN>
        //   <en>Deployment-default mapping from built-in Profiles to package allowlists; used only when the corresponding configuration is blank and never evidence that arbitrary modules may enter the Portal.</en>
        // </lang>
        private static readonly IDictionary<string, string> DefaultProfilePackages =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "EnterpriseBase", "HIA.EmployeeProfileConfirm,HIA.EmployeeProfileCorrectionRequest" },
                { "EnterpriseWorkbench", PortalNavigationRegistry.EnterpriseWorkbenchPackageId },
                { "BusinessWorkflow", "HIA.BusinessApplicationRequest" },
                { "LegacyContent", "Legacy.Announcements,Legacy.Contacts,Legacy.Discussion,Legacy.Document,Legacy.Events,Legacy.HtmlModule,Legacy.ImageModule,Legacy.Links,Legacy.QuickLinks,Legacy.XmlModule" },
                { "DevProbe", "HIA.ModuleProbe" }
            };

        // <lang>
        //   <zh-CN>内置 Profile 的默认 include 图；仅表达能力组合，递归展开仍需防循环且不改变被包含 Profile 的独立配置边界。</zh-CN>
        //   <en>Default include graph for built-in Profiles; it expresses capability composition only, while recursive expansion still prevents cycles and preserves each included Profile's own configuration boundary.</en>
        // </lang>
        private static readonly IDictionary<string, string> DefaultProfileIncludes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BusinessWorkflow", "EnterpriseBase,EnterpriseWorkbench" }
            };

        // <lang>
        //   <zh-CN>历史桌面控件路径到虚拟 Legacy 包 id 的固定映射；它用于统一 Profile 判断，不把文件路径本身当作授权决定。</zh-CN>
        //   <en>Fixed mapping from legacy desktop-control paths to virtual Legacy package ids; it unifies Profile checks and does not treat a file path itself as an authorization decision.</en>
        // </lang>
        private static readonly IDictionary<string, string> LegacyPackagesBySource =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "DesktopModules/Announcements.ascx", "Legacy.Announcements" },
                { "DesktopModules/Contacts.ascx", "Legacy.Contacts" },
                { "DesktopModules/Discussion.ascx", "Legacy.Discussion" },
                { "DesktopModules/Document.ascx", "Legacy.Document" },
                { "DesktopModules/Events.ascx", "Legacy.Events" },
                { "DesktopModules/HtmlModule.ascx", "Legacy.HtmlModule" },
                { "DesktopModules/ImageModule.ascx", "Legacy.ImageModule" },
                { "DesktopModules/Links.ascx", "Legacy.Links" },
                { "DesktopModules/QuickLinks.ascx", "Legacy.QuickLinks" },
                { "DesktopModules/XmlModule.ascx", "Legacy.XmlModule" }
            };

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析当前 Profile 快照。</zh-CN>
        ///   <en>Resolves the current Profile snapshot.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>可选 HTTP 上下文；当前解析不读取它，以保持上层调用形状稳定。</zh-CN>
        ///   <en>Optional HTTP context; the current resolver does not read it, keeping the upstream call shape stable.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>不可变的 Profile 快照。</zh-CN>
        ///   <en>Immutable Profile snapshot.</en>
        /// </l>
        /// </returns>
        public static PortalModuleProfileSnapshot Resolve(HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>收集受控的配置问题摘要而非抛出原始配置文本；快照调用方可诊断部署错误，同时仍取得安全的允许包集合。</zh-CN>
            //   <en>Collect controlled summaries of configuration issues instead of throwing raw configuration text; snapshot consumers can diagnose deployment faults while still receiving a safe allowed-package set.</en>
            // </lang>
            var invalidEntries = new List<string>();

            // <lang>
            //   <zh-CN>只读取固定活动键；HTTP 上下文和任何数据库覆盖均不参与决定本部署的模块能力范围。</zh-CN>
            //   <en>Read only the fixed active key; neither HTTP context nor any database override participates in deciding this deployment's module-capability scope.</en>
            // </lang>
            string configuredActiveProfile = ConfigurationManager.AppSettings[ActiveProfileKey];

            // <lang>
            //   <zh-CN>活动值必须成为稳定名称，否则收敛到 CoreOnly；回退不保留非法文本作为 Profile 或配置键的一部分。</zh-CN>
            //   <en>The active value must become a stable name or converge to CoreOnly; fallback does not retain invalid text as part of a Profile or configuration key.</en>
            // </lang>
            string activeProfile = NormalizeStableName(configuredActiveProfile, CoreOnlyProfile, invalidEntries, "active profile");

            // <lang>
            //   <zh-CN>允许集合使用 ordinal-ignore-case 语义，确保 Profile、include 和显式包重复出现时不会因大小写差异扩大或重复快照。</zh-CN>
            //   <en>The allowed set uses ordinal-ignore-case semantics so duplicate Profile, include, and explicit packages do not enlarge or duplicate the snapshot through casing differences.</en>
            // </lang>
            var allowedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // <lang>
            //   <zh-CN>递归展开活动 Profile 与其 include；独立已访问和展开中集合分别处理重复引用与真正循环。</zh-CN>
            //   <en>Recursively expand the active Profile and its includes; separate visited and expanding sets distinguish repeated references from actual cycles.</en>
            // </lang>
            AddProfileAndIncludes(
                activeProfile,
                allowedPackages,
                invalidEntries,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            // <lang>
            //   <zh-CN>仅追加受控部署键中的显式包；这些包同样经稳定名称验证，且不跳过模块状态或页面授权。</zh-CN>
            //   <en>Append only explicit packages from the controlled deployment key; they pass the same stable-name validation and do not bypass module state or page authorization.</en>
            // </lang>
            AddPackageList(ConfigurationManager.AppSettings[EnabledPackagesKey], allowedPackages, invalidEntries, EnabledPackagesKey);

            // <lang>
            //   <zh-CN>按稳定大小写不敏感顺序发布不可变快照，使同一配置产生可预测的展示与审计顺序。</zh-CN>
            //   <en>Publish an immutable snapshot in stable case-insensitive order so the same configuration yields predictable display and audit ordering.</en>
            // </lang>
            return new PortalModuleProfileSnapshot(
                activeProfile,
                allowedPackages.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
                invalidEntries);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断一个已规范化桌面入口是否属于 Core 能力。</zh-CN>
        ///   <en>Determines whether a normalized desktop entry belongs to Core capability.</en>
        /// </lang>
        /// </summary>
        /// <param name="normalizedDesktopSource">
        /// <l>
        ///   <zh-CN>已去掉 ~/ 的桌面控件路径。</zh-CN>
        ///   <en>Desktop control path without ~/.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>属于 Core 时为 true。</zh-CN>
        ///   <en>True when the entry belongs to Core.</en>
        /// </l>
        /// </returns>
        public static bool IsCoreDesktopSource(string normalizedDesktopSource)
        {
            // <lang>
            //   <zh-CN>先将历史路径收敛为相对正斜杠形式，再仅识别登录入口和 Admin 前缀；该分类不替代访问该入口所需的授权检查。</zh-CN>
            //   <en>First converge a legacy path to relative forward-slash form, then recognize only the sign-in entry and Admin prefix; this classification does not replace authorization required to access that entry.</en>
            // </lang>
            string source = NormalizeSource(normalizedDesktopSource);
            return string.Equals(source, "DesktopModules/SignIn.ascx", StringComparison.OrdinalIgnoreCase) ||
                   source.StartsWith("Admin/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试将旧模块入口映射为内置虚拟 package id。</zh-CN>
        ///   <en>Attempts to map a legacy module entry to a built-in virtual package id.</en>
        /// </lang>
        /// </summary>
        /// <param name="normalizedDesktopSource">
        /// <l>
        ///   <zh-CN>已规范化桌面入口。</zh-CN>
        ///   <en>Normalized desktop entry.</en>
        /// </l>
        /// </param>
        /// <param name="packageId">
        /// <l>
        ///   <zh-CN>成功时返回虚拟 package id。</zh-CN>
        ///   <en>Virtual package id when successful.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>找到映射时为 true。</zh-CN>
        ///   <en>True when a mapping is found.</en>
        /// </l>
        /// </returns>
        public static bool TryGetLegacyPackageId(string normalizedDesktopSource, out string packageId)
        {
            // <lang>
            //   <zh-CN>映射前使用同一来源规范化，避免 ~/、起始斜杠或目录分隔符差异绕过固定 Legacy 映射表。</zh-CN>
            //   <en>Apply the same source normalization before mapping so ~/ prefixes, leading slashes, or directory-separator differences cannot bypass the fixed Legacy mapping table.</en>
            // </lang>
            return LegacyPackagesBySource.TryGetValue(NormalizeSource(normalizedDesktopSource), out packageId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取 Legacy 虚拟包映射的只读快照。</zh-CN>
        ///   <en>Gets a read-only snapshot of the Legacy virtual package mappings.</en>
        /// </lang>
        /// </summary>
        public static IDictionary<string, string> GetLegacyPackageMappings()
        {
            // <lang>
            //   <zh-CN>返回独立副本而非内部可变字典，调用方的枚举或修改不能影响当前进程的 Profile 决策。</zh-CN>
            //   <en>Return an independent copy rather than the mutable internal dictionary so caller enumeration or mutation cannot affect Profile decisions in the current process.</en>
            // </lang>
            return new Dictionary<string, string>(LegacyPackagesBySource, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并验证一个 Profile 的直接 include 名称。</zh-CN>
        ///   <en>Reads and validates the direct include names for one Profile.</en>
        /// </lang>
        /// </summary>
        /// <param name="profileName">
        /// <l>
        ///   <zh-CN>已规范化的父 Profile 名称。</zh-CN>
        ///   <en>Normalized parent Profile name.</en>
        /// </l>
        /// </param>
        /// <param name="invalidEntries">
        /// <l>
        ///   <zh-CN>用于追加受控无效配置摘要的集合。</zh-CN>
        ///   <en>Collection that receives controlled invalid-configuration summaries.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已过滤自身引用和无效名称的直接 include 序列。</zh-CN>
        ///   <en>Direct include sequence with self-references and invalid names filtered out.</en>
        /// </l>
        /// </returns>
        private static IEnumerable<string> ReadProfileIncludes(string profileName, IList<string> invalidEntries)
        {
            // <lang>
            //   <zh-CN>该键只由已经通过稳定名称门禁的 Profile 与固定后缀组成，避免未验证配置文本控制 appSettings 读取位置。</zh-CN>
            //   <en>This key consists only of a Profile that already passed the stable-name gate and a fixed suffix, preventing unvalidated configuration text from controlling the appSettings read location.</en>
            // </lang>
            string key = ProfileKeyPrefix + profileName + ProfileIncludesSuffix;

            // <lang>
            //   <zh-CN>部署配置优先；只有空白时才使用同名内置默认 include，不把缺失配置解释为任意包或任意 Profile。</zh-CN>
            //   <en>Deployment configuration takes precedence; use the same-name built-in default include only when blank, never interpreting a missing entry as arbitrary packages or Profiles.</en>
            // </lang>
            string configured = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(configured) && DefaultProfileIncludes.ContainsKey(profileName))
            {
                configured = DefaultProfileIncludes[profileName];
            }

            // <lang>
            //   <zh-CN>保留顺序化直接 include 列表供递归器处理；跨分支去重和循环判断由调用方的集合承担。</zh-CN>
            //   <en>Keep an ordered direct-include list for the recursive expander; cross-branch deduplication and cycle detection belong to the caller's sets.</en>
            // </lang>
            var includes = new List<string>();
            foreach (string include in SplitCsv(configured))
            {
                // <lang>
                //   <zh-CN>每个 CSV 片段再次通过稳定名称验证，并以当前配置键记载受控问题摘要而不抛出原始配置内容。</zh-CN>
                //   <en>Each CSV segment passes stable-name validation again and records a controlled issue summary under the current configuration key without throwing raw configuration content.</en>
                // </lang>
                string normalized = NormalizeStableName(include, string.Empty, invalidEntries, key);
                if (!string.IsNullOrEmpty(normalized) &&
                    !string.Equals(normalized, profileName, StringComparison.OrdinalIgnoreCase))
                {
                    // <lang>
                    //   <zh-CN>直接自包含没有能力增益且会干扰递归图；将其静默排除，较长循环仍由展开栈报告。</zh-CN>
                    //   <en>A direct self-include adds no capability and obscures the recursive graph; filter it silently while longer cycles remain reported by the expansion stack.</en>
                    // </lang>
                    includes.Add(normalized);
                }
            }

            return includes;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将一个 Profile、其包和其递归 include 合并到允许集合。</zh-CN>
        ///   <en>Adds one Profile, its packages, and its recursive includes to the allowed set.</en>
        /// </lang>
        /// </summary>
        /// <param name="profileName">
        ///   <l>
        ///     <zh-CN>已规范化的待展开 Profile 名称。</zh-CN>
        ///     <en>Normalized Profile name to expand.</en>
        ///   </l>
        /// </param>
        /// <param name="allowedPackages">
        ///   <l>
        ///     <zh-CN>按大小写不敏感语义合并的包白名单。</zh-CN>
        ///     <en>Package allowlist merged with case-insensitive semantics.</en>
        ///   </l>
        /// </param>
        /// <param name="invalidEntries">
        ///   <l>
        ///     <zh-CN>接收受控无效或循环摘要的集合。</zh-CN>
        ///     <en>Collection receiving controlled invalid or cycle summaries.</en>
        ///   </l>
        /// </param>
        /// <param name="visitedProfiles">
        ///   <l>
        ///     <zh-CN>已完整展开的 Profile 集合。</zh-CN>
        ///     <en>Set of Profiles whose expansion completed.</en>
        ///   </l>
        /// </param>
        /// <param name="expandingProfiles">
        ///   <l>
        ///     <zh-CN>当前递归栈中的 Profile 集合。</zh-CN>
        ///     <en>Set of Profiles on the current recursion stack.</en>
        ///   </l>
        /// </param>
        private static void AddProfileAndIncludes(
            string profileName,
            ISet<string> allowedPackages,
            IList<string> invalidEntries,
            ISet<string> visitedProfiles,
            ISet<string> expandingProfiles)
        {
            // <lang>
            //   <zh-CN>空名称不能构成 Profile 或配置键，直接停止此分支，不为其添加任意默认包。</zh-CN>
            //   <en>An empty name cannot form a Profile or configuration key, so stop this branch without adding any default packages.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return;
            }

            // <lang>
            //   <zh-CN>已完成分支再次被引用时不重复展开，保留集合去重和首次解析顺序的既有语义。</zh-CN>
            //   <en>Do not expand an already completed branch again, retaining the established set-deduplication and first-resolution semantics.</en>
            // </lang>
            if (visitedProfiles.Contains(profileName))
            {
                return;
            }

            // <lang>
            //   <zh-CN>只有当前展开栈中的重复才是 include 循环；记录受控 Profile 名称后停止该边，防止配置图无限递归。</zh-CN>
            //   <en>Only a duplicate on the current expansion stack is an include cycle; record the controlled Profile name and stop that edge to prevent unbounded recursion in the configuration graph.</en>
            // </lang>
            if (!expandingProfiles.Add(profileName))
            {
                invalidEntries.Add("profile include cycle=" + profileName);
                return;
            }

            // <lang>
            //   <zh-CN>先合并当前 Profile 的直接包，再递归其有效 include；不因 include 失败回滚此前已通过验证的包。</zh-CN>
            //   <en>Merge the current Profile's direct packages before recursively processing valid includes; an include failure does not roll back packages that already passed validation.</en>
            // </lang>
            AddProfilePackages(profileName, allowedPackages, invalidEntries);
            foreach (string includedProfile in ReadProfileIncludes(profileName, invalidEntries))
            {
                // <lang>
                //   <zh-CN>同一访问/展开集合在整个深度优先链共享，使重复引用与循环判断基于完整 Profile 图而不是单一局部列表。</zh-CN>
                //   <en>Share the same visited and expanding sets across the depth-first chain so repeated-reference and cycle decisions apply to the complete Profile graph rather than one local list.</en>
                // </lang>
                AddProfileAndIncludes(includedProfile, allowedPackages, invalidEntries, visitedProfiles, expandingProfiles);
            }

            // <lang>
            //   <zh-CN>离开当前分支时先从展开栈移除，再标记为已完成；这一顺序确保同级重复是去重而非误报循环。</zh-CN>
            //   <en>Remove the current branch from the expansion stack before marking it complete; this order ensures a sibling repeat is deduplicated rather than falsely reported as a cycle.</en>
            // </lang>
            expandingProfiles.Remove(profileName);
            visitedProfiles.Add(profileName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并合并一个 Profile 的直接包列表。</zh-CN>
        ///   <en>Reads and merges the direct package list for one Profile.</en>
        /// </lang>
        /// </summary>
        /// <param name="profileName">
        ///   <l>
        ///     <zh-CN>已规范化的 Profile 名称。</zh-CN>
        ///     <en>Normalized Profile name.</en>
        ///   </l>
        /// </param>
        /// <param name="allowedPackages">
        ///   <l>
        ///     <zh-CN>接收有效包 id 的允许集合。</zh-CN>
        ///     <en>Allowed set receiving valid package ids.</en>
        ///   </l>
        /// </param>
        /// <param name="invalidEntries">
        ///   <l>
        ///     <zh-CN>接收受控配置问题摘要的集合。</zh-CN>
        ///     <en>Collection receiving controlled configuration-issue summaries.</en>
        ///   </l>
        /// </param>
        private static void AddProfilePackages(
            string profileName,
            ISet<string> allowedPackages,
            IList<string> invalidEntries)
        {
            // <lang>
            //   <zh-CN>包列表键仅以稳定 Profile 名和固定后缀构造，维持读取范围在 Profile 配置命名空间内。</zh-CN>
            //   <en>Construct the package-list key only from the stable Profile name and fixed suffix, keeping the read scope inside the Profile configuration namespace.</en>
            // </lang>
            string key = ProfileKeyPrefix + profileName + ProfilePackagesSuffix;

            // <lang>
            //   <zh-CN>空白部署项才采用内置默认包；已提供的非空配置即使包含无效片段，也不与默认列表隐式合并。</zh-CN>
            //   <en>Use built-in default packages only for a blank deployment entry; a supplied nonblank configuration, even with invalid segments, is not implicitly merged with defaults.</en>
            // </lang>
            string configured = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(configured) && DefaultProfilePackages.ContainsKey(profileName))
            {
                configured = DefaultProfilePackages[profileName];
            }

            AddPackageList(configured, allowedPackages, invalidEntries, key);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将 CSV 包列表中的稳定包 id 合并到允许集合。</zh-CN>
        ///   <en>Merges stable package ids from a CSV package list into the allowed set.</en>
        /// </lang>
        /// </summary>
        /// <param name="configured">
        ///   <l>
        ///     <zh-CN>来自受控默认值或 appSettings 的 CSV 文本。</zh-CN>
        ///     <en>CSV text from a controlled default or appSettings.</en>
        ///   </l>
        /// </param>
        /// <param name="allowedPackages">
        ///   <l>
        ///     <zh-CN>接收有效包 id 的允许集合。</zh-CN>
        ///     <en>Allowed set receiving valid package ids.</en>
        ///   </l>
        /// </param>
        /// <param name="invalidEntries">
        ///   <l>
        ///     <zh-CN>接收非法包摘要的集合。</zh-CN>
        ///     <en>Collection receiving invalid-package summaries.</en>
        ///   </l>
        /// </param>
        /// <param name="sourceKey">
        ///   <l>
        ///     <zh-CN>用于受控问题摘要的配置来源键。</zh-CN>
        ///     <en>Configuration source key used in controlled issue summaries.</en>
        ///   </l>
        /// </param>
        private static void AddPackageList(
            string configured,
            ISet<string> allowedPackages,
            IList<string> invalidEntries,
            string sourceKey)
        {
            foreach (string packageId in SplitCsv(configured))
            {
                // <lang>
                //   <zh-CN>包 id 不满足稳定名称规则时只记录来源键和受控文本并跳过，避免让任意配置片段进入允许集合。</zh-CN>
                //   <en>When a package id fails the stable-name rule, record only the source key and controlled text then skip it, preventing arbitrary configuration segments from entering the allowed set.</en>
                // </lang>
                string normalized = NormalizeStableName(packageId, string.Empty, invalidEntries, sourceKey);
                if (!string.IsNullOrEmpty(normalized))
                {
                    // <lang>
                    //   <zh-CN>集合按大小写不敏感语义去重；添加包只形成部署允许范围，不改变模块状态或用户权限。</zh-CN>
                    //   <en>The set deduplicates case-insensitively; adding a package only forms the deployment allow scope and does not alter module state or user permissions.</en>
                    // </lang>
                    allowedPackages.Add(normalized);
                }
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将配置候选值限制为稳定名称，或返回调用方指定的安全回退。</zh-CN>
        ///   <en>Constrains a configuration candidate to a stable name or returns the caller-specified safe fallback.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        ///   <l>
        ///     <zh-CN>待验证的配置候选文本。</zh-CN>
        ///     <en>Configuration candidate text to validate.</en>
        ///   </l>
        /// </param>
        /// <param name="fallback">
        ///   <l>
        ///     <zh-CN>候选为空或非法时的既定回退值。</zh-CN>
        ///     <en>Established fallback when the candidate is blank or invalid.</en>
        ///   </l>
        /// </param>
        /// <param name="invalidEntries">
        ///   <l>
        ///     <zh-CN>接收受控非法输入摘要的集合。</zh-CN>
        ///     <en>Collection receiving controlled invalid-input summaries.</en>
        ///   </l>
        /// </param>
        /// <param name="sourceName">
        ///   <l>
        ///     <zh-CN>用于标识配置来源的受控名称。</zh-CN>
        ///     <en>Controlled name identifying the configuration source.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>通过验证的去空白稳定名称，或既定回退值。</zh-CN>
        ///     <en>Validated trimmed stable name, or the established fallback.</en>
        ///   </l>
        /// </returns>
        private static string NormalizeStableName(
            string candidate,
            string fallback,
            IList<string> invalidEntries,
            string sourceName)
        {
            // <lang>
            //   <zh-CN>空白候选不进入正则或问题集合，直接采用调用方已经确定的安全回退，避免把缺省配置误记为攻击性输入。</zh-CN>
            //   <en>A blank candidate does not enter the regex or issue collection; use the caller's already determined safe fallback directly rather than misclassifying absent configuration as hostile input.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return fallback;
            }

            // <lang>
            //   <zh-CN>去除边缘空白后才按固定 ASCII 名称规则验证，使配置键拼接、字典查询和集合比较使用同一稳定表示。</zh-CN>
            //   <en>Trim edge whitespace before validation against the fixed ASCII-name rule so configuration-key composition, dictionary lookup, and set comparison use one stable representation.</en>
            // </lang>
            string trimmed = candidate.Trim();
            if (StableNamePattern.IsMatch(trimmed))
            {
                return trimmed;
            }

            // <lang>
            //   <zh-CN>非法值只作为受控来源摘要记录，随后返回既定回退；不会将其用于构造配置键、路径或授权结论。</zh-CN>
            //   <en>Record an invalid value only as a controlled source summary, then return the established fallback; it is never used to construct a configuration key, path, or authorization conclusion.</en>
            // </lang>
            invalidEntries.Add(sourceName + "=" + trimmed);
            return fallback;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从 CSV 文本产生已去空白的非空片段。</zh-CN>
        ///   <en>Produces trimmed, nonempty segments from CSV text.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        ///   <l>
        ///     <zh-CN>可能为空的 CSV 文本。</zh-CN>
        ///     <en>CSV text that may be blank.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>不含空项的延迟片段序列。</zh-CN>
        ///     <en>Deferred segment sequence without blank entries.</en>
        ///   </l>
        /// </returns>
        private static IEnumerable<string> SplitCsv(string value)
        {
            // <lang>
            //   <zh-CN>空白配置产生稳定空序列，不创建占位 Profile 或包，也不抛出配置格式异常。</zh-CN>
            //   <en>Blank configuration produces a stable empty sequence, creates no placeholder Profile or package, and does not throw a configuration-format exception.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(value))
            {
                return new string[0];
            }

            // <lang>
            //   <zh-CN>仅以逗号分隔、移除空项并去除每项边缘空白；名称合法性仍由上层稳定名称门禁负责。</zh-CN>
            //   <en>Split only on commas, remove empty segments, and trim each edge; the higher-level stable-name gate still owns name validity.</en>
            // </lang>
            return value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将历史桌面来源收敛为用于固定映射比较的相对正斜杠路径。</zh-CN>
        ///   <en>Converges a legacy desktop source to a relative forward-slash path for fixed mapping comparison.</en>
        /// </lang>
        /// </summary>
        /// <param name="source">
        ///   <l>
        ///     <zh-CN>可能带 ~/、起始斜杠或反斜杠的来源文本。</zh-CN>
        ///     <en>Source text that may contain ~/, a leading slash, or backslashes.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>可与固定来源映射比较的规范化相对路径。</zh-CN>
        ///     <en>Normalized relative path suitable for fixed source-map comparison.</en>
        ///   </l>
        /// </returns>
        private static string NormalizeSource(string source)
        {
            // <lang>
            //   <zh-CN>空值先收敛为空字符串，再移除虚拟根标记和起始分隔符并统一为正斜杠；该转换仅供映射比较，不执行文件系统访问。</zh-CN>
            //   <en>Converge null to an empty string, then remove virtual-root markers and leading separators and normalize to forward slashes; this conversion is for mapping comparison only and performs no file-system access.</en>
            // </lang>
            return (source ?? string.Empty).Trim().TrimStart('~', '/').Replace('\\', '/');
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>一次模块 Profile 解析的不可变快照。</zh-CN>
    ///   <en>Immutable snapshot for one module Profile resolution.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalModuleProfileSnapshot
    {
        // <lang>
        //   <zh-CN>内部集合从公开只读列表复制而来，用于大小写不敏感成员查询；不暴露给调用方，防止其修改影响解析快照。</zh-CN>
        //   <en>Internal set copied from the public read-only list for case-insensitive membership queries; it is not exposed to callers so their mutation cannot affect the resolved snapshot.</en>
        // </lang>
        private readonly HashSet<string> allowedPackageSet;

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一次部署 Profile 解析的不可变结果。</zh-CN>
        ///   <en>Creates the immutable result of one deployment Profile resolution.</en>
        /// </lang>
        /// </summary>
        /// <param name="activeProfile">
        ///   <l>
        ///     <zh-CN>已解析的活动 Profile；空值收敛为空字符串。</zh-CN>
        ///     <en>Resolved active Profile; null converges to an empty string.</en>
        ///   </l>
        /// </param>
        /// <param name="allowedPackageIds">
        ///   <l>
        ///     <zh-CN>解析得到的包 id 列表；复制为只读快照。</zh-CN>
        ///     <en>Resolved package-id list; copied into a read-only snapshot.</en>
        ///   </l>
        /// </param>
        /// <param name="invalidEntries">
        ///   <l>
        ///     <zh-CN>解析期间的受控问题摘要；复制为只读快照。</zh-CN>
        ///     <en>Controlled issue summaries from parsing; copied into a read-only snapshot.</en>
        ///   </l>
        /// </param>
        internal PortalModuleProfileSnapshot(
            string activeProfile,
            IList<string> allowedPackageIds,
            IList<string> invalidEntries)
        {
            // <lang>
            //   <zh-CN>将构造输入复制到只读集合，避免解析器完成后外部可变列表改变已发布的 Profile 结论。</zh-CN>
            //   <en>Copy constructor inputs into read-only collections so externally mutable lists cannot alter a published Profile conclusion after resolution completes.</en>
            // </lang>
            ActiveProfile = activeProfile ?? string.Empty;
            AllowedPackageIds = new List<string>(allowedPackageIds ?? new List<string>()).AsReadOnly();
            InvalidEntries = new List<string>(invalidEntries ?? new List<string>()).AsReadOnly();

            // <lang>
            //   <zh-CN>成员集合以公开快照为唯一来源并使用大小写不敏感比较，保证列表与 IsPackageAllowed 的可见结论一致。</zh-CN>
            //   <en>Build the membership set solely from the public snapshot with case-insensitive comparison so the list and IsPackageAllowed expose consistent conclusions.</en>
            // </lang>
            allowedPackageSet = new HashSet<string>(AllowedPackageIds, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前活动 Profile 名称。</zh-CN>
        ///   <en>Current active Profile name.</en>
        /// </lang>
        /// </summary>
        public string ActiveProfile { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前 Profile 允许的 package id 列表。</zh-CN>
        ///   <en>Package id list allowed by the current Profile.</en>
        /// </lang>
        /// </summary>
        public IList<string> AllowedPackageIds { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析时发现的非法 Profile、include 或 package id 条目。</zh-CN>
        ///   <en>Invalid Profile, include, or package id entries found during resolution.</en>
        /// </lang>
        /// </summary>
        public IList<string> InvalidEntries { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断 package id 是否允许进入当前部署 Profile。</zh-CN>
        ///   <en>Determines whether a package id is allowed by the current deployment Profile.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId">
        /// <l>
        ///   <zh-CN>待判断 package id。</zh-CN>
        ///   <en>Package id to check.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>允许时为 true。</zh-CN>
        ///   <en>True when allowed.</en>
        /// </l>
        /// </returns>
        public bool IsPackageAllowed(string packageId)
        {
            // <lang>
            //   <zh-CN>空白 id 不是允许包；其余值只去除边缘空白后查询不可变集合，本方法不检查模块启用状态或用户权限。</zh-CN>
            //   <en>A blank id is not an allowed package; otherwise trim edge whitespace then query the immutable set, and do not check module enablement or user permissions here.</en>
            // </lang>
            return !string.IsNullOrWhiteSpace(packageId) && allowedPackageSet.Contains(packageId.Trim());
        }
    }
}
