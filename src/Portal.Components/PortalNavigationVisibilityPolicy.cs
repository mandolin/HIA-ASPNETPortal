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
