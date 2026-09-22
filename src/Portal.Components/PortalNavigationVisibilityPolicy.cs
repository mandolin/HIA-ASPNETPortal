using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>导航可见性判定所需的当前上下文（纯数据，不读取 HttpContext、数据库或配置）。</zh-CN>
    ///   <en>Current context required for navigation visibility decisions (pure data; it reads no HttpContext, database, or configuration).</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>Web 项目负责用 PortalAuthorization、Profile resolver 等组装本对象，再交给 PortalNavigationVisibilityPolicy 判定；这样判定逻辑保持纯内存、可被 Portal.Tests 覆盖。</zh-CN>
    ///   <en>The web project assembles this object from PortalAuthorization, the Profile resolver, and similar sources, then hands it to PortalNavigationVisibilityPolicy; this keeps decisions pure in-memory and testable by Portal.Tests.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalNavigationVisibilityContext
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>使用给定的当前用户与部署状态初始化可见性上下文。</zh-CN>
        ///   <en>Initializes the visibility context from the supplied current user and deployment state.</en>
        /// </lang>
        /// </summary>
        /// <param name="isAdministrator">
        /// <l>
        ///   <zh-CN>当前用户是否管理员；管理员可见被阻断入口的原因。</zh-CN>
        ///   <en>Whether the current user is an administrator; administrators may see why an entry is blocked.</en>
        /// </l>
        /// </param>
        /// <param name="roleNames">
        /// <l>
        ///   <zh-CN>当前用户所属旧角色名集合；可为 null。</zh-CN>
        ///   <en>Legacy role names the current user belongs to; may be null.</en>
        /// </l>
        /// </param>
        /// <param name="permissionKeys">
        /// <l>
        ///   <zh-CN>当前用户已授予的细粒度权限键集合；可为 null。</zh-CN>
        ///   <en>Fine-grained permission keys granted to the current user; may be null.</en>
        /// </l>
        /// </param>
        /// <param name="allowedPackageIds">
        /// <l>
        ///   <zh-CN>当前 Profile 允许的部署包集合；可为 null。</zh-CN>
        ///   <en>Deployment packages allowed by the current Profile; may be null.</en>
        /// </l>
        /// </param>
        /// <param name="activeProfile">
        /// <l>
        ///   <zh-CN>当前启用的 Profile 名称；可为空。</zh-CN>
        ///   <en>Currently active Profile name; may be blank.</en>
        /// </l>
        /// </param>
        /// <param name="includedProfiles">
        /// <l>
        ///   <zh-CN>当前 Profile 通过 Includes 传递包含的 Profile 集合；可为 null。</zh-CN>
        ///   <en>Profiles transitively included by the current Profile through Includes; may be null.</en>
        /// </l>
        /// </param>
        public PortalNavigationVisibilityContext(
            bool isAdministrator,
            IEnumerable<string> roleNames,
            IEnumerable<string> permissionKeys,
            IEnumerable<string> allowedPackageIds,
            string activeProfile,
            IEnumerable<string> includedProfiles)
        {
            // <lang>
            //   <zh-CN>逐字段保存并规范化：集合去空白、按大小写不敏感去重、只读化，防止调用方在判定期间修改输入。</zh-CN>
            //   <en>Store and normalize each field: trim, case-insensitively deduplicate, and make collections read-only so callers cannot mutate inputs during evaluation.</en>
            // </lang>
            IsAdministrator = isAdministrator;
            RoleNames = NormalizeList(roleNames);
            PermissionKeys = NormalizeList(permissionKeys);
            AllowedPackageIds = NormalizeList(allowedPackageIds);
            ActiveProfile = activeProfile ?? string.Empty;
            IncludedProfiles = NormalizeList(includedProfiles);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前用户是否管理员。</zh-CN>
        ///   <en>Whether the current user is an administrator.</en>
        /// </lang>
        /// </summary>
        public bool IsAdministrator { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前用户所属旧角色名（只读，已去重）。</zh-CN>
        ///   <en>Legacy role names of the current user (read-only, deduplicated).</en>
        /// </lang>
        /// </summary>
        public IList<string> RoleNames { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前用户已授予的权限键（只读，已去重）。</zh-CN>
        ///   <en>Permission keys granted to the current user (read-only, deduplicated).</en>
        /// </lang>
        /// </summary>
        public IList<string> PermissionKeys { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前 Profile 允许的部署包（只读，已去重）。</zh-CN>
        ///   <en>Deployment packages allowed by the current Profile (read-only, deduplicated).</en>
        /// </lang>
        /// </summary>
        public IList<string> AllowedPackageIds { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前启用的 Profile 名称。</zh-CN>
        ///   <en>Currently active Profile name.</en>
        /// </lang>
        /// </summary>
        public string ActiveProfile { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前 Profile 传递包含的 Profile（只读，已去重）。</zh-CN>
        ///   <en>Profiles transitively included by the current Profile (read-only, deduplicated).</en>
        /// </lang>
        /// </summary>
        public IList<string> IncludedProfiles { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把可空字符串序列规范化为只读去重列表；空输入返回空列表而非 null。</zh-CN>
        ///   <en>Normalize a nullable string sequence into a read-only deduplicated list; empty input yields an empty list instead of null.</en>
        /// </lang>
        /// </summary>
        private static IList<string> NormalizeList(IEnumerable<string> values)
        {
            if (values == null)
            {
                return new string[0];
            }

            return values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>入口分组、相关入口选择与可见性判定的纯策略（不读取 HttpContext、数据库或配置）。</zh-CN>
    ///   <en>Pure policy for entry grouping, related-entry selection, and visibility decisions (reads no HttpContext, database, or configuration).</en>
    /// </lang>
    /// </summary>
    public static class PortalNavigationVisibilityPolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>相关入口默认返回条数上限。</zh-CN>
        ///   <en>Default maximum number of related entries to return.</en>
        /// </lang>
        /// </summary>
        public const int DefaultMaxRelatedEntries = 4;

        /// <summary>
        /// <lang>
        ///   <zh-CN>核心包的稳定标识；核心包始终可用，不受 Profile 阻断。</zh-CN>
        ///   <en>Stable identifier of the core package; the core package is always available and is never blocked by a Profile.</en>
        /// </lang>
        /// </summary>
        public const string CorePackageId = "Core";

        /// <summary>
        /// <lang>
        ///   <zh-CN>相关入口数量下限；同组入口不足时用常用入口兜底补足，避免动作区因分组过窄而整块消失。</zh-CN>
        ///   <en>Minimum number of related entries; common entries top up the list when a group is too narrow, preventing the action area from vanishing entirely.</en>
        /// </lang>
        /// </summary>
        public const int MinimumRelatedEntries = 2;

        /// <summary>
        /// <lang>
        ///   <zh-CN>常用入口兜底键；仅在同组入口不足下限时使用，且仍受 registry 与可见性策略管辖。</zh-CN>
        ///   <en>Fallback keys for common entries; used only when a group falls below the minimum, and still governed by the registry and the visibility policy.</en>
        /// </lang>
        /// </summary>
        private static readonly string[] CommonFallbackEntryKeys =
        {
            "Admin.SystemHealth",
            "Admin.ModuleCatalog"
        };

        /// <summary>
        /// <lang>
        ///   <zh-CN>核心管理组。</zh-CN>
        ///   <en>Core administration group.</en>
        /// </lang>
        /// </summary>
        public const string GroupCoreAdmin = "Core.Admin";

        /// <summary>
        /// <lang>
        ///   <zh-CN>企业能力管理组。</zh-CN>
        ///   <en>Enterprise capability administration group.</en>
        /// </lang>
        /// </summary>
        public const string GroupAdminCapability = "Admin.Capability";

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块治理组。</zh-CN>
        ///   <en>Module governance group.</en>
        /// </lang>
        /// </summary>
        public const string GroupAdminModules = "Admin.Modules";

        /// <summary>
        /// <lang>
        ///   <zh-CN>运维诊断组。</zh-CN>
        ///   <en>Operations diagnostics group.</en>
        /// </lang>
        /// </summary>
        public const string GroupAdminOps = "Admin.Ops";

        /// <summary>
        /// <lang>
        ///   <zh-CN>呈现与主题组。</zh-CN>
        ///   <en>Presentation and theme group.</en>
        /// </lang>
        /// </summary>
        public const string GroupAdminPresentation = "Admin.Presentation";

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号入口组。</zh-CN>
        ///   <en>Account entry group.</en>
        /// </lang>
        /// </summary>
        public const string GroupAdminAccount = "Admin.Account";

        /// <summary>
        /// <lang>
        ///   <zh-CN>错误与占位组（不进入普通导航）。</zh-CN>
        ///   <en>Error and placeholder group (never shown in ordinary navigation).</en>
        /// </lang>
        /// </summary>
        public const string GroupAdminError = "Admin.Error";

        /// <summary>
        /// <lang>
        ///   <zh-CN>企业能力前台入口组。</zh-CN>
        ///   <en>Enterprise capability front-end group.</en>
        /// </lang>
        /// </summary>
        public const string GroupEnterprise = "Enterprise";

        /// <summary>
        /// <lang>
        ///   <zh-CN>主导航 Tab 入口组。Tab 条目只承载门控元数据，既不出现在 Admin 动作区的相关入口组中，也不生成动作区链接。</zh-CN>
        ///   <en>Main-navigation tab entry group. Tab entries carry gate metadata only: they never appear in Admin action-area related groups and never produce action-area links.</en>
        /// </lang>
        /// </summary>
        public const string GroupTab = "Tab";

        /// <summary>
        /// <lang>
        ///   <zh-CN>阻断原因：入口生命周期不允许显示。</zh-CN>
        ///   <en>Blocked because the entry lifecycle forbids display.</en>
        /// </lang>
        /// </summary>
        public const string ReasonLifecycle = "LifecycleNotDisplayable";

        /// <summary>
        /// <lang>
        ///   <zh-CN>阻断原因：当前用户角色不满足。</zh-CN>
        ///   <en>Blocked because the current user roles do not satisfy the requirement.</en>
        /// </lang>
        /// </summary>
        public const string ReasonRole = "RoleMissing";

        /// <summary>
        /// <lang>
        ///   <zh-CN>阻断原因：当前用户权限键不满足。</zh-CN>
        ///   <en>Blocked because the current user permission keys do not satisfy the requirement.</en>
        /// </lang>
        /// </summary>
        public const string ReasonPermission = "PermissionMissing";

        /// <summary>
        /// <lang>
        ///   <zh-CN>阻断原因：所需部署包未启用。</zh-CN>
        ///   <en>Blocked because a required deployment package is not enabled.</en>
        /// </lang>
        /// </summary>
        public const string ReasonPackage = "PackageNotEnabled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>阻断原因：所需 Profile 未启用。</zh-CN>
        ///   <en>Blocked because a required Profile is not enabled.</en>
        /// </lang>
        /// </summary>
        public const string ReasonProfile = "ProfileNotEnabled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>阻断原因：可见性策略本身不进入普通导航。</zh-CN>
        ///   <en>Blocked because the visibility policy itself keeps the entry out of ordinary navigation.</en>
        /// </lang>
        /// </summary>
        public const string ReasonVisibilityMode = "NotInOrdinaryNavigation";

        /// <summary>
        /// <lang>
        ///   <zh-CN>既有稳定键到分组的兼容映射；稳定键不可改写，因此分组由映射表而非改键实现。</zh-CN>
        ///   <en>Compatibility map from existing stable keys to groups; stable keys must not be rewritten, so grouping is achieved by this map rather than by renaming keys.</en>
        /// </lang>
        /// </summary>
        private static readonly IDictionary<string, string> LegacyGroupMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Admin.CollaborationItems", GroupAdminCapability },
                { "Admin.WorkItems", GroupAdminCapability },
                { "Admin.ModuleCatalog", GroupAdminModules },
                { "Admin.SystemHealth", GroupAdminOps },
                { "Admin.ThemeSettings", GroupAdminPresentation },
                { "Account.Register.Legacy", GroupAdminAccount }
            };

        /// <summary>
        /// <lang>
        ///   <zh-CN>按稳定键推导入口所属分组；优先使用兼容映射，其次按前缀判定。</zh-CN>
        ///   <en>Derive the entry's group from its stable key: the compatibility map wins, then prefix matching applies.</en>
        /// </lang>
        /// </summary>
        /// <param name="entry">
        /// <l>
        ///   <zh-CN>入口元数据；为 null 时返回空字符串。</zh-CN>
        ///   <en>Entry metadata; returns an empty string when null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>分组名；无法判定时为空字符串。</zh-CN>
        ///   <en>Group name; empty when it cannot be determined.</en>
        /// </l>
        /// </returns>
        public static string GetGroupKey(PortalNavigationEntry entry)
        {
            return GetGroupKey(entry == null ? null : entry.EntryKey);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按稳定键文本推导分组名。</zh-CN>
        ///   <en>Derive the group name from the stable key text.</en>
        /// </lang>
        /// </summary>
        /// <param name="entryKey">
        /// <l>
        ///   <zh-CN>入口稳定键；为空白时返回空字符串。</zh-CN>
        ///   <en>Entry stable key; returns an empty string when blank.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>分组名。</zh-CN>
        ///   <en>Group name.</en>
        /// </l>
        /// </returns>
        public static string GetGroupKey(string entryKey)
        {
            // <lang>
            //   <zh-CN>空键无法分组，直接返回空字符串，避免把未知入口误归入某个业务组。</zh-CN>
            //   <en>A blank key cannot be grouped; return an empty string so unknown entries are never mis-assigned to a business group.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(entryKey))
            {
                return string.Empty;
            }

            string mapped;
            if (LegacyGroupMap.TryGetValue(entryKey.Trim(), out mapped))
            {
                return mapped;
            }

            // <lang>
            //   <zh-CN>按稳定键前缀判定新命名入口的分组；前缀不匹配时返回空字符串。</zh-CN>
            //   <en>Match newly named entries by stable-key prefix; return an empty string when no prefix matches.</en>
            // </lang>
            if (entryKey.StartsWith("Core.Admin.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupCoreAdmin;
            }

            if (entryKey.StartsWith("Admin.Capability.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupAdminCapability;
            }

            if (entryKey.StartsWith("Admin.Modules.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupAdminModules;
            }

            if (entryKey.StartsWith("Admin.Ops.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupAdminOps;
            }

            if (entryKey.StartsWith("Admin.Presentation.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupAdminPresentation;
            }

            if (entryKey.StartsWith("Admin.Account.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupAdminAccount;
            }

            if (entryKey.StartsWith("Admin.Error.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupAdminError;
            }

            // <lang>
            //   <zh-CN>Tab 条目自成一组的判定必须早于普通业务前缀：它只做门控，不能因为键形相近而被并入 Admin 动作区的相关入口。</zh-CN>
            //   <en>The tab-entry group check must run before ordinary business prefixes: tab entries exist for gating only and must never be folded into an Admin action-area related group because their keys look similar.</en>
            // </lang>
            if (entryKey.StartsWith(TabEntryKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return GroupTab;
            }

            if (entryKey.StartsWith("Enterprise.", StringComparison.OrdinalIgnoreCase))
            {
                return GroupEnterprise;
            }

            return string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取指定入口的同组相关入口：排除自身、错误组和不可导航的生命周期状态，按排序值取前若干条。</zh-CN>
        ///   <en>Get same-group related entries for an entry: exclude itself, the error group, and non-navigable lifecycle states, then take the first entries by sort order.</en>
        /// </lang>
        /// </summary>
        /// <param name="entryKey">
        /// <l>
        ///   <zh-CN>当前入口稳定键。</zh-CN>
        ///   <en>Stable key of the current entry.</en>
        /// </l>
        /// </param>
        /// <param name="maxCount">
        /// <l>
        ///   <zh-CN>返回条数上限；非正值时回落到默认值。</zh-CN>
        ///   <en>Maximum number of entries to return; non-positive values fall back to the default.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>只读相关入口列表；无同组入口时为空列表。</zh-CN>
        ///   <en>Read-only related entry list; empty when no same-group entry exists.</en>
        /// </l>
        /// </returns>
        public static IList<PortalNavigationEntry> GetRelatedEntries(string entryKey, int maxCount = DefaultMaxRelatedEntries)
        {
            // <lang>
            //   <zh-CN>上限非正时回落默认值，避免调用方误传 0 导致动作区为空或负数导致异常。</zh-CN>
            //   <en>Fall back to the default when the limit is non-positive, preventing an empty action area from a mistaken 0 or an exception from a negative value.</en>
            // </lang>
            if (maxCount <= 0)
            {
                maxCount = DefaultMaxRelatedEntries;
            }

            string group = GetGroupKey(entryKey);

            // <lang>
            //   <zh-CN>分组未知时不猜配：返回空列表，避免把无关入口塞进动作区。</zh-CN>
            //   <en>Do not guess when the group is unknown: return an empty list so unrelated entries never enter the action area.</en>
            // </lang>
            if (string.IsNullOrEmpty(group))
            {
                return new List<PortalNavigationEntry>().AsReadOnly();
            }

            List<PortalNavigationEntry> related = PortalNavigationRegistry.GetEntries()
                .Where(entry => string.Equals(GetGroupKey(entry), group, StringComparison.OrdinalIgnoreCase))
                .Where(entry => !string.Equals(entry.EntryKey, entryKey, StringComparison.OrdinalIgnoreCase))
                .Where(entry => entry.LifecycleState == PortalNavigationLifecycleState.Active)
                .Where(entry => entry.VisibilityMode != PortalNavigationVisibilityMode.DiagnosticOnly)
                .Where(IsNavigable)
                .OrderBy(entry => entry.SortOrder)
                .ThenBy(entry => entry.EntryKey, StringComparer.OrdinalIgnoreCase)
                .Take(maxCount)
                .ToList();

            // <lang>
            //   <zh-CN>同组入口不足下限时用显式常用入口补足：避免只因分组过窄就让动作区整块消失，同时补的是受 registry 管辖的入口而非新硬编码。</zh-CN>
            //   <en>When same-group entries fall below the minimum, top up with explicit common entries: this prevents the action area from vanishing only because a group is narrow, and the top-up entries still come from the registry rather than new hardcoded links.</en>
            // </lang>
            if (related.Count < MinimumRelatedEntries)
            {
                foreach (string fallbackKey in CommonFallbackEntryKeys)
                {
                    if (related.Count >= MinimumRelatedEntries)
                    {
                        break;
                    }

                    if (string.Equals(fallbackKey, entryKey, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (related.Any(entry => string.Equals(entry.EntryKey, fallbackKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    PortalNavigationEntry fallback = PortalNavigationRegistry.FindByKey(fallbackKey);
                    if (fallback != null && IsNavigable(fallback))
                    {
                        related.Add(fallback);
                    }
                }
            }

            return related
                .Take(maxCount)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断入口是否可直接导航；只有页面型目标才可生成链接，模块控件（.ascx）不是可导航入口。</zh-CN>
        ///   <en>Decide whether an entry is directly navigable; only page-style targets may become links, while module controls (.ascx) are not navigable entries.</en>
        /// </lang>
        /// </summary>
        /// <param name="entry">
        /// <l>
        ///   <zh-CN>待判断入口；为 null 时返回 false。</zh-CN>
        ///   <en>Entry to check; returns false when null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>目标为页面时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the target is a page.</en>
        /// </l>
        /// </returns>
        public static bool IsNavigable(PortalNavigationEntry entry)
        {
            // <lang>
            //   <zh-CN>以 .aspx 作为可导航目标的判定依据：Admin/*.ascx 是模块控件，直接生成 href 会指向不可访问的资源。</zh-CN>
            //   <en>Use the .aspx extension as the navigability test: Admin/*.ascx are module controls, and generating an href to them would point at an unreachable resource.</en>
            // </lang>
            if (entry == null || string.IsNullOrWhiteSpace(entry.Target))
            {
                return false;
            }

            return entry.Target.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按六步判定入口是否对当前上下文可见；任一步不满足即不可见，且从不抛异常。</zh-CN>
        ///   <en>Decide whether an entry is visible to the current context through six steps; failing any step hides it, and this never throws.</en>
        /// </lang>
        /// </summary>
        /// <param name="entry">
        /// <l>
        ///   <zh-CN>待判定入口；为 null 时返回 false。</zh-CN>
        ///   <en>Entry to evaluate; returns false when null.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前用户与部署状态；为 null 时返回 false（无上下文即不可见）。</zh-CN>
        ///   <en>Current user and deployment state; returns false when null (no context means not visible).</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可见时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when visible.</en>
        /// </l>
        /// </returns>
        public static bool IsVisible(PortalNavigationEntry entry, PortalNavigationVisibilityContext context)
        {
            return string.IsNullOrEmpty(GetBlockedReason(entry, context));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回入口被阻断的原因代码；可见时返回 <c>null</c>。管理员可据此显示禁用态与原因。</zh-CN>
        ///   <en>Return the blocked reason code for an entry; <c>null</c> when visible. Administrators use it to render a disabled state with a reason.</en>
        /// </lang>
        /// </summary>
        /// <param name="entry">
        /// <l>
        ///   <zh-CN>待判定入口。</zh-CN>
        ///   <en>Entry to evaluate.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前用户与部署状态。</zh-CN>
        ///   <en>Current user and deployment state.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>阻断原因代码；可见时为 <c>null</c>。</zh-CN>
        ///   <en>Blocked reason code; <c>null</c> when visible.</en>
        /// </l>
        /// </returns>
        public static string GetBlockedReason(PortalNavigationEntry entry, PortalNavigationVisibilityContext context)
        {
            // <lang>
            //   <zh-CN>空入口或空上下文一律不可见：调用方缺失上下文时宁可不显示，也不放行。</zh-CN>
            //   <en>A missing entry or context is not visible: when the caller lacks context, prefer hiding over granting access.</en>
            // </lang>
            if (entry == null || context == null)
            {
                return ReasonVisibilityMode;
            }

            // <lang>
            //   <zh-CN>第一步：生命周期。只有 Active 进入普通导航，Legacy 仅兼容视图，草稿与废弃不显示。</zh-CN>
            //   <en>Step 1: lifecycle. Only Active enters ordinary navigation; Legacy belongs to compatibility views, while Draft and Deprecated never show.</en>
            // </lang>
            if (entry.LifecycleState != PortalNavigationLifecycleState.Active)
            {
                return ReasonLifecycle;
            }

            // <lang>
            //   <zh-CN>第二步：可见性策略。诊断专用入口不进普通导航；管理员专属入口对非管理员隐藏。</zh-CN>
            //   <en>Step 2: visibility policy. Diagnostics-only entries stay out of ordinary navigation; admin-only entries hide from non-administrators.</en>
            // </lang>
            if (entry.VisibilityMode == PortalNavigationVisibilityMode.DiagnosticOnly)
            {
                return ReasonVisibilityMode;
            }

            if (entry.VisibilityMode == PortalNavigationVisibilityMode.AdminOnly && !context.IsAdministrator)
            {
                return ReasonRole;
            }

            // <lang>
            //   <zh-CN>第三步：角色依赖。声明为空表示不限制；否则命中任一声明角色即视为满足（角色是可选关系）。</zh-CN>
            //   <en>Step 3: role dependencies. An empty declaration means no restriction; otherwise matching any declared role satisfies it (roles are alternatives).</en>
            // </lang>
            if (entry.RequiredRoles.Count > 0 &&
                !entry.RequiredRoles.Any(role => context.RoleNames.Contains(role)))
            {
                return ReasonRole;
            }

            // <lang>
            //   <zh-CN>第四步：权限键依赖。权限是叠加要求，必须全部满足。</zh-CN>
            //   <en>Step 4: permission-key dependencies. Permissions are cumulative and must all be satisfied.</en>
            // </lang>
            if (entry.RequiredPermissionKeys.Count > 0 &&
                !entry.RequiredPermissionKeys.All(key => context.PermissionKeys.Contains(key)))
            {
                return ReasonPermission;
            }

            // <lang>
            //   <zh-CN>第五步：部署包依赖。核心包始终可用；其余包必须在当前 Profile 允许列表中。</zh-CN>
            //   <en>Step 5: deployment-package dependencies. The core package is always available; other packages must appear in the Profile allow list.</en>
            // </lang>
            if (entry.RequiredPackageIds.Count > 0 &&
                !entry.RequiredPackageIds.All(package => IsPackageAllowed(package, context)))
            {
                return ReasonPackage;
            }

            // <lang>
            //   <zh-CN>第六步：Profile 依赖。声明为空表示不限制；否则当前 Profile 或其传递包含的 Profile 命中即满足。</zh-CN>
            //   <en>Step 6: Profile dependencies. An empty declaration means no restriction; otherwise the active Profile or any transitively included Profile must match.</en>
            // </lang>
            if (entry.RequiredProfiles.Count > 0 &&
                !entry.RequiredProfiles.Any(profile => IsProfileActive(profile, context)))
            {
                return ReasonProfile;
            }

            return null;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取当前上下文下可见的相关入口：先取同组入口，再逐个做可见性判定后取前若干条。</zh-CN>
        ///   <en>Get visible related entries for the current context: take same-group entries, evaluate each, then take the first visible ones.</en>
        /// </lang>
        /// </summary>
        /// <param name="entryKey">
        /// <l>
        ///   <zh-CN>当前入口稳定键。</zh-CN>
        ///   <en>Stable key of the current entry.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前用户与部署状态。</zh-CN>
        ///   <en>Current user and deployment state.</en>
        /// </l>
        /// </param>
        /// <param name="maxCount">
        /// <l>
        ///   <zh-CN>返回条数上限。</zh-CN>
        ///   <en>Maximum number of entries to return.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>只读可见相关入口列表。</zh-CN>
        ///   <en>Read-only list of visible related entries.</en>
        /// </l>
        /// </returns>
        public static IList<PortalNavigationEntry> GetVisibleRelatedEntries(
            string entryKey,
            PortalNavigationVisibilityContext context,
            int maxCount = DefaultMaxRelatedEntries)
        {
            return GetRelatedEntries(entryKey, maxCount)
                .Where(entry => IsVisible(entry, context))
                .Take(maxCount <= 0 ? DefaultMaxRelatedEntries : maxCount)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>Tab 级入口键前缀：Tab 条目的稳定键统一为 <c>Tab.&lt;TabName&gt;</c>，不使用 TabId，避免库数据进入稳定键。</zh-CN>
        ///   <en>Entry-key prefix for tab-level entries: a tab entry's stable key is always <c>Tab.&lt;TabName&gt;</c> and never includes the TabId, keeping database data out of stable keys.</en>
        /// </lang>
        /// </summary>
        public const string TabEntryKeyPrefix = "Tab.";

        /// <summary>
        /// <lang>
        ///   <zh-CN>由门户配置中的 Tab 名构造稳定入口键；空白名返回空字符串，表示没有可判定的 Tab 键。</zh-CN>
        ///   <en>Builds the stable entry key from a portal-configuration tab name; a blank name yields an empty string, meaning there is no tab key to evaluate.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabName">
        /// <l>
        ///   <zh-CN>门户配置中的 Tab 名称。</zh-CN>
        ///   <en>Tab name from portal configuration.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>稳定入口键；Tab 名为空白时为空字符串。</zh-CN>
        ///   <en>Stable entry key; empty when the tab name is blank.</en>
        /// </l>
        /// </returns>
        public static string BuildTabEntryKey(string tabName)
        {
            // <lang>
            //   <zh-CN>只做去空白拼接，不做大小写折叠或字符替换：稳定键必须可被 registry 精确命中，任何隐式改写都会让登记与运行结果不一致。</zh-CN>
            //   <en>Only trim and concatenate; no case folding or character replacement, because the stable key must be matched exactly by the registry and any implicit rewrite would desynchronize registration from runtime behaviour.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(tabName))
            {
                return string.Empty;
            }

            return TabEntryKeyPrefix + tabName.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析主导航 Tab 门控开关的原始配置值；关闭是默认值，只有显式的真值才启用。</zh-CN>
        ///   <en>Parses the raw configuration value of the main-navigation tab gate switch; off is the default and only explicit truthy values enable it.</en>
        /// </lang>
        /// </summary>
        /// <param name="configuredValue">
        /// <l>
        ///   <zh-CN>配置中的开关原始文本；由调用方读取，本策略不访问配置源。</zh-CN>
        ///   <en>Raw switch text from configuration, read by the caller because this policy never touches configuration sources.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>显式启用时为 <c>true</c>；空白或不能识别时为 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> only for explicit enablement; <c>false</c> when blank or unrecognized.</en>
        /// </l>
        /// </returns>
        public static bool IsTabGateEnabled(string configuredValue)
        {
            // <lang>
            //   <zh-CN>识别范围保持保守：只接受 true/1/yes/on（忽略大小写与空白）。无法识别的一律按关闭处理，使配置写错时退回现状而不是改变导航。</zh-CN>
            //   <en>Recognition stays conservative: only true/1/yes/on (case-insensitive, trimmed) count. Anything unrecognized is treated as off, so a bad configuration falls back to current behaviour instead of changing navigation.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                return false;
            }

            string normalized = configuredValue.Trim();
            return string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "1", StringComparison.Ordinal)
                || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判定门户配置中的 Tab 是否被入口 gate 阻断；只有"已登记且依赖不满足"才返回原因码。</zh-CN>
        ///   <en>Decides whether a portal-configuration tab is blocked by the entry gate; a reason code is returned only when the tab is registered and its dependencies are unsatisfied.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabName">
        /// <l>
        ///   <zh-CN>门户配置中的 Tab 名称。</zh-CN>
        ///   <en>Tab name from portal configuration.</en>
        /// </l>
        /// </param>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前用户与部署状态；为 null 时按放行处理（见备注）。</zh-CN>
        ///   <en>Current user and deployment state; treated as allowed when null (see remarks).</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>阻断原因码；放行时为 <c>null</c>。</zh-CN>
        ///   <en>Blocked reason code; <c>null</c> when the tab is allowed.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法刻意采用与动作区相反的失败方向：未登记 Tab、空白名或上下文缺失一律**放行**。原因是主导航在调用本方法前已按角色过滤，放行不会泄露超权入口；反之若 gate 故障导致全部 Tab 消失，会把"治理增强"变成可用性事故。阻断判定本身仍复用统一的六步可见性判定，不另立规则。</zh-CN>
        ///   <en>This method deliberately fails in the opposite direction from the action area: an unregistered tab, a blank name, or a missing context is always allowed. The main navigation has already been filtered by role before this call, so allowing cannot leak an over-permissioned entry, whereas losing every tab to a gate fault would turn a governance improvement into an availability incident. The blocking decision itself still reuses the shared six-step visibility evaluation rather than defining new rules.</en>
        /// </lang>
        /// </remarks>
        public static string GetTabBlockedReason(string tabName, PortalNavigationVisibilityContext context)
        {
            // <lang>
            //   <zh-CN>空白 Tab 名没有可判定对象：返回放行，让渲染层按既有"数据缺失不渲染该项"的规则处理，而不是在这里制造一种新的隐藏原因。</zh-CN>
            //   <en>A blank tab name has nothing to evaluate: return allowed so the rendering layer applies its existing "skip entries with missing data" rule instead of inventing a new hiding reason here.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(tabName))
            {
                return null;
            }

            // <lang>
            //   <zh-CN>上下文缺失即放行：本阶段不引入新的失败模式，gate 只有在能完成判定时才可能阻断。</zh-CN>
            //   <en>A missing context is allowed: this phase introduces no new failure mode, so the gate can block only when a decision can actually be completed.</en>
            // </lang>
            if (context == null)
            {
                return null;
            }

            string entryKey = BuildTabEntryKey(tabName);
            if (entryKey.Length == 0)
            {
                return null;
            }

            // <lang>
            //   <zh-CN>未登记即为放行（零回归兜底）：registry 只对**已登记**的 Tab 施加门控，未登记 Tab 继续沿用既有的角色过滤结果。</zh-CN>
            //   <en>Unregistered means allowed (zero-regression fallback): the registry only gates **registered** tabs, while unregistered tabs keep the existing role-filtered outcome.</en>
            // </lang>
            PortalNavigationEntry entry = PortalNavigationRegistry.FindByKey(entryKey);
            if (entry == null)
            {
                return null;
            }

            return GetBlockedReason(entry, context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断部署包是否可用：核心包恒定可用，其余需在允许列表内（大小写不敏感）。</zh-CN>
        ///   <en>Decide whether a deployment package is available: the core package is always available, others must be in the allow list (case-insensitive).</en>
        /// </lang>
        /// </summary>
        private static bool IsPackageAllowed(string packageId, PortalNavigationVisibilityContext context)
        {
            if (string.Equals(packageId, CorePackageId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return context.AllowedPackageIds.Contains(packageId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断 Profile 是否生效：当前 Profile 或其 Includes 传递包含的 Profile 命中即生效。</zh-CN>
        ///   <en>Decide whether a Profile is active: it matches when the active Profile or any transitively included Profile equals it.</en>
        /// </lang>
        /// </summary>
        private static bool IsProfileActive(string profile, PortalNavigationVisibilityContext context)
        {
            if (string.Equals(profile, context.ActiveProfile, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return context.IncludedProfiles.Contains(profile);
        }
    }
}
