using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户导航入口的稳定类型。</zh-CN>
    ///   <en>Stable type of a Portal navigation entry.</en>
    /// </lang>
    /// </summary>
    public enum PortalNavigationEntryKind
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户 Tab 入口。</zh-CN>
        ///   <en>Legacy Portal Tab entry.</en>
        /// </lang>
        /// </summary>
        Tab,

        /// <summary>
        /// <lang>
        ///   <zh-CN>可挂载到 Tab 的桌面模块入口。</zh-CN>
        ///   <en>Desktop module entry that can be mounted to a Tab.</en>
        /// </lang>
        /// </summary>
        DesktopModule,

        /// <summary>
        /// <lang>
        ///   <zh-CN>后台管理页面入口。</zh-CN>
        ///   <en>Administration page entry.</en>
        /// </lang>
        /// </summary>
        AdminPage,

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档、帮助或外部资料入口。</zh-CN>
        ///   <en>Documentation, help, or external-reference entry.</en>
        /// </lang>
        /// </summary>
        Documentation,

        /// <summary>
        /// <lang>
        ///   <zh-CN>历史账号入口，例如旧注册页。</zh-CN>
        ///   <en>Legacy account entry, such as the old registration page.</en>
        /// </lang>
        /// </summary>
        LegacyAccount
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>导航入口在当前治理阶段的生命周期。</zh-CN>
    ///   <en>Lifecycle state of a navigation entry in the current governance phase.</en>
    /// </lang>
    /// </summary>
    public enum PortalNavigationLifecycleState
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>设计中或等待后续实现。</zh-CN>
        ///   <en>Designed or waiting for later implementation.</en>
        /// </lang>
        /// </summary>
        Draft,

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前可用入口。</zh-CN>
        ///   <en>Currently active entry.</en>
        /// </lang>
        /// </summary>
        Active,

        /// <summary>
        /// <lang>
        ///   <zh-CN>历史兼容入口，后续会被更正式的入口替代。</zh-CN>
        ///   <en>Legacy-compatible entry that will later be replaced by a formal entry.</en>
        /// </lang>
        /// </summary>
        Legacy,

        /// <summary>
        /// <lang>
        ///   <zh-CN>不建议继续使用的入口。</zh-CN>
        ///   <en>Entry that should no longer be used for new flows.</en>
        /// </lang>
        /// </summary>
        Deprecated
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>导航入口对普通用户和管理员的显示策略。</zh-CN>
    ///   <en>Visibility policy for ordinary users and administrators.</en>
    /// </lang>
    /// </summary>
    public enum PortalNavigationVisibilityMode
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>满足权限、Profile 和包状态时才显示。</zh-CN>
        ///   <en>Visible only when role, Profile, and package-state checks pass.</en>
        /// </lang>
        /// </summary>
        VisibleWhenAllowed,

        /// <summary>
        /// <lang>
        ///   <zh-CN>普通用户隐藏不可用入口，管理员诊断可显示不可用原因。</zh-CN>
        ///   <en>Hide unavailable entries from ordinary users while allowing administrator diagnostics to show reasons.</en>
        /// </lang>
        /// </summary>
        HideWhenBlocked,

        /// <summary>
        /// <lang>
        ///   <zh-CN>仅管理员可见。</zh-CN>
        ///   <en>Visible only to administrators.</en>
        /// </lang>
        /// </summary>
        AdminOnly,

        /// <summary>
        /// <lang>
        ///   <zh-CN>作为历史或诊断资料登记，不进入普通导航。</zh-CN>
        ///   <en>Registered as legacy or diagnostic reference, not shown in ordinary navigation.</en>
        /// </lang>
        /// </summary>
        DiagnosticOnly
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>门户导航入口的第一版静态元数据。</zh-CN>
    ///   <en>First-version static metadata for one Portal navigation entry.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>
    ///     本类型只表达入口契约，不直接读取数据库、HTTP 上下文或外部配置。P22.3 先让入口的
    ///     Tab、模块包、Profile、角色和权限依赖可被审查；P22.4 再把这些元数据接入实际导航渲染。
    ///   </zh-CN>
    ///   <en>
    ///     This type describes the entry contract only. It does not read databases, HTTP context, or external
    ///     configuration. P22.3 makes Tab, package, Profile, role, and permission dependencies reviewable;
    ///     P22.4 will connect the metadata to real navigation rendering.
    ///   </en>
    /// </lang>
    /// </remarks>
    public sealed class PortalNavigationEntry
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一个导航入口元数据对象。</zh-CN>
        ///   <en>Creates a navigation-entry metadata object.</en>
        /// </lang>
        /// </summary>
        public PortalNavigationEntry(
            string entryKey,
            PortalNavigationEntryKind entryKind,
            string displayNameZhCn,
            string displayNameEn,
            string target,
            PortalNavigationVisibilityMode visibilityMode,
            PortalNavigationLifecycleState lifecycleState,
            int sortOrder,
            IEnumerable<string> requiredRoles,
            IEnumerable<string> requiredPermissionKeys,
            IEnumerable<string> requiredPackageIds,
            IEnumerable<string> requiredProfiles,
            string notes)
        {
            EntryKey = NormalizeRequired(entryKey, "entryKey");
            EntryKind = entryKind;
            DisplayNameZhCn = displayNameZhCn ?? string.Empty;
            DisplayNameEn = displayNameEn ?? string.Empty;
            Target = target ?? string.Empty;
            VisibilityMode = visibilityMode;
            LifecycleState = lifecycleState;
            SortOrder = sortOrder;
            RequiredRoles = NormalizeList(requiredRoles);
            RequiredPermissionKeys = NormalizeList(requiredPermissionKeys);
            RequiredPackageIds = NormalizeList(requiredPackageIds);
            RequiredProfiles = NormalizeList(requiredProfiles);
            Notes = notes ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>不随显示名变化的稳定入口键。</zh-CN>
        ///   <en>Stable entry key that does not change with display text.</en>
        /// </lang>
        /// </summary>
        public string EntryKey { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入口类型。</zh-CN>
        ///   <en>Entry type.</en>
        /// </lang>
        /// </summary>
        public PortalNavigationEntryKind EntryKind { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>中文显示名。</zh-CN>
        ///   <en>Chinese display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayNameZhCn { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>英文显示名。</zh-CN>
        ///   <en>English display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayNameEn { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入口目标，可能是模块入口、后台页或文档路径。</zh-CN>
        ///   <en>Entry target, such as a module entry, admin page, or document path.</en>
        /// </lang>
        /// </summary>
        public string Target { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入口显示策略。</zh-CN>
        ///   <en>Entry visibility policy.</en>
        /// </lang>
        /// </summary>
        public PortalNavigationVisibilityMode VisibilityMode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入口生命周期。</zh-CN>
        ///   <en>Entry lifecycle state.</en>
        /// </lang>
        /// </summary>
        public PortalNavigationLifecycleState LifecycleState { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>导航排序值。</zh-CN>
        ///   <en>Navigation sort value.</en>
        /// </lang>
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧角色依赖。</zh-CN>
        ///   <en>Legacy role dependencies.</en>
        /// </lang>
        /// </summary>
        public IList<string> RequiredRoles { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>细粒度权限依赖。</zh-CN>
        ///   <en>Fine-grained permission dependencies.</en>
        /// </lang>
        /// </summary>
        public IList<string> RequiredPermissionKeys { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块包依赖。</zh-CN>
        ///   <en>Module-package dependencies.</en>
        /// </lang>
        /// </summary>
        public IList<string> RequiredPackageIds { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>推荐或要求的 Profile 依赖。</zh-CN>
        ///   <en>Recommended or required Profile dependencies.</en>
        /// </lang>
        /// </summary>
        public IList<string> RequiredProfiles { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>维护说明。</zh-CN>
        ///   <en>Maintainer note.</en>
        /// </lang>
        /// </summary>
        public string Notes { get; private set; }

        private static string NormalizeRequired(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Navigation entry value is required.", argumentName);
            }

            return value.Trim();
        }

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
    ///   <zh-CN>门户导航入口静态注册表。</zh-CN>
    ///   <en>Static registry for Portal navigation entries.</en>
    /// </lang>
    /// </summary>
    public static class PortalNavigationRegistry
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>企业能力工作台的推荐 Profile 名称。</zh-CN>
        ///   <en>Recommended Profile name for the enterprise-capability workbench.</en>
        /// </lang>
        /// </summary>
        public const string EnterpriseWorkbenchProfile = "EnterpriseWorkbench";

        /// <summary>
        /// <lang>
        ///   <zh-CN>企业能力工作台的稳定 package id。</zh-CN>
        ///   <en>Stable package id for the enterprise-capability workbench.</en>
        /// </lang>
        /// </summary>
        public const string EnterpriseWorkbenchPackageId = "HIA.EnterpriseCapabilityWorkbench";

        private static readonly PortalNavigationEntry[] EntryArray =
        {
            new PortalNavigationEntry(
                "Enterprise.Capability.Workbench",
                PortalNavigationEntryKind.DesktopModule,
                "企业能力工作台",
                "Enterprise Capability Workbench",
                "DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx",
                PortalNavigationVisibilityMode.HideWhenBlocked,
                PortalNavigationLifecycleState.Active,
                100,
                new[] { PortalRoleNames.AllUsers },
                new[] { PortalPermissionKeys.BusinessCollaborationCreate, PortalPermissionKeys.BusinessCollaborationViewOwn },
                new[] { EnterpriseWorkbenchPackageId },
                new[] { EnterpriseWorkbenchProfile },
                "P22.4 front-end entry for ordinary users to submit and review their own collaboration items."),

            new PortalNavigationEntry(
                "Admin.CollaborationItems",
                PortalNavigationEntryKind.AdminPage,
                "企业协同事项后台",
                "Collaboration Items Administration",
                "Admin/CollaborationItems.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                200,
                new[] { PortalRoleNames.Administrators },
                new[] { PortalPermissionKeys.BusinessCollaborationViewAll, PortalPermissionKeys.BusinessCollaborationAdmin },
                new string[0],
                new string[0],
                "Current administration entry for P21 collaboration-item proof."),

            new PortalNavigationEntry(
                "Admin.WorkItems",
                PortalNavigationEntryKind.AdminPage,
                "业务待办后台",
                "Work Items Administration",
                "Admin/WorkItems.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                210,
                new[] { PortalRoleNames.Administrators },
                new[] { PortalPermissionKeys.BusinessWorkItemsView, PortalPermissionKeys.BusinessWorkItemsAdmin },
                new string[0],
                new string[0],
                "Administration and diagnostic entry for business work items."),

            new PortalNavigationEntry(
                "Admin.ModuleCatalog",
                PortalNavigationEntryKind.AdminPage,
                "模块包目录",
                "Module Catalog",
                "Admin/ModuleCatalog.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                300,
                new[] { PortalRoleNames.Administrators },
                new[] { PortalPermissionKeys.ModuleCatalogView },
                new string[0],
                new string[0],
                "Preferred place to diagnose package/Profile visibility before adding another admin page."),

            new PortalNavigationEntry(
                "Admin.SystemHealth",
                PortalNavigationEntryKind.AdminPage,
                "系统健康状态",
                "System Health",
                "Admin/SystemHealth.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                310,
                new[] { PortalRoleNames.Administrators },
                new[] { PortalPermissionKeys.OpsHealthView },
                new string[0],
                new string[0],
                "Existing operations entry that may host read-only navigation diagnostics."),

            new PortalNavigationEntry(
                "Admin.ThemeSettings",
                PortalNavigationEntryKind.AdminPage,
                "主题设置",
                "Theme Settings",
                "Admin/ThemeSettings.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                320,
                new[] { PortalRoleNames.Administrators },
                new[] { PortalPermissionKeys.ThemeView },
                new string[0],
                new string[0],
                "Existing theme administration entry."),

            new PortalNavigationEntry(
                "Account.Register.Legacy",
                PortalNavigationEntryKind.LegacyAccount,
                "旧注册入口",
                "Legacy Registration Entry",
                "Admin/Register.aspx",
                PortalNavigationVisibilityMode.DiagnosticOnly,
                PortalNavigationLifecycleState.Legacy,
                900,
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                "Registered for governance only; actual registration flow is deferred to an account-system phase.")
        };

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取全部静态注册入口。</zh-CN>
        ///   <en>Gets every statically registered navigation entry.</en>
        /// </lang>
        /// </summary>
        public static IList<PortalNavigationEntry> GetEntries()
        {
            return EntryArray
                .OrderBy(entry => entry.SortOrder)
                .ThenBy(entry => entry.EntryKey, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按稳定键查找入口。</zh-CN>
        ///   <en>Finds an entry by stable key.</en>
        /// </lang>
        /// </summary>
        /// <param name="entryKey">
        /// <l zh-CN="稳定入口键。" en="Stable entry key." />
        /// </param>
        /// <returns>
        /// <lang>
        ///   <zh-CN>匹配入口；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching entry, or <c>null</c> when absent.</en>
        /// </lang>
        /// </returns>
        public static PortalNavigationEntry FindByKey(string entryKey)
        {
            if (string.IsNullOrWhiteSpace(entryKey))
            {
                return null;
            }

            return EntryArray.FirstOrDefault(entry =>
                string.Equals(entry.EntryKey, entryKey.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
