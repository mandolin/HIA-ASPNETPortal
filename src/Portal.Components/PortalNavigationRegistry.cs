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
        ///   <zh-CN>已登记但尚未进入可用导航的入口。</zh-CN>
        ///   <en>Registered entry that has not entered active navigation.</en>
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
        ///   <zh-CN>为历史兼容性保留登记的入口。</zh-CN>
        ///   <en>Entry retained in the registry for legacy compatibility.</en>
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
    ///     P22.4 connects the metadata to real navigation rendering.
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
        /// <param name="entryKey"><l zh-CN="不能为空的稳定入口键。" en="Nonblank stable entry key." /></param>
        /// <param name="entryKind"><l zh-CN="入口的固定类型。" en="Fixed kind of entry." /></param>
        /// <param name="displayNameZhCn"><l zh-CN="可为空的中文显示文本。" en="Chinese display text, which may be blank." /></param>
        /// <param name="displayNameEn"><l zh-CN="可为空的英文显示文本。" en="English display text, which may be blank." /></param>
        /// <param name="target"><l zh-CN="可为空的注册目标元数据；本构造器不解析或访问它。" en="Registered target metadata, which may be blank; this constructor does not resolve or access it." /></param>
        /// <param name="visibilityMode"><l zh-CN="导航消费方使用的显示策略提示。" en="Visibility-policy hint used by navigation consumers." /></param>
        /// <param name="lifecycleState"><l zh-CN="入口在注册表中的生命周期状态。" en="Entry lifecycle state in the registry." /></param>
        /// <param name="sortOrder"><l zh-CN="稳定导航排序值。" en="Stable navigation sort value." /></param>
        /// <param name="requiredRoles"><l zh-CN="可为空的旧角色依赖序列。" en="Legacy-role dependency sequence, which may be null." /></param>
        /// <param name="requiredPermissionKeys"><l zh-CN="可为空的细粒度权限键依赖序列。" en="Fine-grained permission-key dependency sequence, which may be null." /></param>
        /// <param name="requiredPackageIds"><l zh-CN="可为空的部署包依赖序列。" en="Deployment-package dependency sequence, which may be null." /></param>
        /// <param name="requiredProfiles"><l zh-CN="可为空的部署 Profile 依赖序列。" en="Deployment-Profile dependency sequence, which may be null." /></param>
        /// <param name="notes"><l zh-CN="可为空的维护说明。" en="Maintainer note, which may be blank." /></param>
        /// <exception cref="ArgumentException">
        /// <l>
        ///   <zh-CN><paramref name="entryKey"/> 为空白时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="entryKey"/> is blank.</en>
        /// </l>
        /// </exception>
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
            // <lang>
            //   <zh-CN>构造器只建立静态导航元数据：稳定键必须存在；显示、目标和说明可为空；它不读取配置、不执行路由，也不将任一依赖解释为授权成功。</zh-CN>
            //   <en>The constructor creates static navigation metadata only: the stable key must exist while display, target, and notes may be blank; it reads no configuration, performs no routing, and does not interpret any dependency as successful authorization.</en>
            // </lang>
            EntryKey = NormalizeRequired(entryKey, "entryKey");

            // <lang>
            //   <zh-CN>枚举、排序和可选文本按调用方登记值保存；真正的可访问性仍由消费边界验证，不能由元数据对象自行放行。</zh-CN>
            //   <en>Store enum, order, and optional text as registered by the caller; actual accessibility remains verified by consuming boundaries and cannot be granted by this metadata object itself.</en>
            // </lang>
            EntryKind = entryKind;
            DisplayNameZhCn = displayNameZhCn ?? string.Empty;
            DisplayNameEn = displayNameEn ?? string.Empty;
            Target = target ?? string.Empty;
            VisibilityMode = visibilityMode;
            LifecycleState = lifecycleState;
            SortOrder = sortOrder;

            // <lang>
            //   <zh-CN>四类依赖序列分别复制、去空白、按大小写不敏感去重并只读化，防止外部枚举在注册后改变可见性提示。</zh-CN>
            //   <en>Copy, trim, case-insensitively deduplicate, and make all four dependency sequences read-only so external enumeration cannot alter visibility hints after registration.</en>
            // </lang>
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
        ///   <zh-CN>导航消费方参考的旧角色依赖；本属性本身不授予角色能力。</zh-CN>
        ///   <en>Legacy role dependencies consulted by navigation consumers; this property does not grant role capability itself.</en>
        /// </lang>
        /// </summary>
        public IList<string> RequiredRoles { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>导航消费方参考的细粒度权限依赖；实际权限检查由消费边界完成。</zh-CN>
        ///   <en>Fine-grained permission dependencies consulted by navigation consumers; consuming boundaries perform actual permission checks.</en>
        /// </lang>
        /// </summary>
        public IList<string> RequiredPermissionKeys { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>导航消费方参考的模块包依赖；它不表示模块已启用。</zh-CN>
        ///   <en>Module-package dependencies consulted by navigation consumers; they do not mean the module is enabled.</en>
        /// </lang>
        /// </summary>
        public IList<string> RequiredPackageIds { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>导航消费方参考的部署 Profile 依赖；它不替代实际 Profile 解析。</zh-CN>
        ///   <en>Deployment-Profile dependencies consulted by navigation consumers; they do not replace actual Profile resolution.</en>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证并规范化一个必需的稳定导航元数据值。</zh-CN>
        ///   <en>Validates and normalizes one required stable navigation-metadata value.</en>
        /// </lang>
        /// </summary>
        /// <param name="value"><l zh-CN="待验证的候选文本。" en="Candidate text to validate." /></param>
        /// <param name="argumentName"><l zh-CN="用于受控参数异常的固定参数名。" en="Fixed parameter name for the controlled argument exception." /></param>
        /// <returns><l zh-CN="已去除边缘空白的必需值。" en="Required value with edge whitespace removed." /></returns>
        /// <exception cref="ArgumentException"><l zh-CN="候选值为空白时引发。" en="Thrown when the candidate is blank." /></exception>
        private static string NormalizeRequired(string value, string argumentName)
        {
            // <lang>
            //   <zh-CN>空白稳定键会使按键查找和排序失去确定性，因此在对象创建时以固定消息和参数名拒绝，而不回退为显示名或目标。</zh-CN>
            //   <en>A blank stable key would make key lookup and ordering indeterminate, so reject it at object creation with a fixed message and parameter name rather than falling back to display text or target.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Navigation entry value is required.", argumentName);
            }

            // <lang>
            //   <zh-CN>只去除边缘空白；不改变大小写或字符内容，保持注册键与消费方使用的稳定标识一致。</zh-CN>
            //   <en>Trim edge whitespace only; do not alter casing or character content so the registered key remains consistent with the stable identifier used by consumers.</en>
            // </lang>
            return value.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>复制并规范化可选导航依赖列表。</zh-CN>
        ///   <en>Copies and normalizes an optional navigation-dependency list.</en>
        /// </lang>
        /// </summary>
        /// <param name="values"><l zh-CN="可为空的依赖候选序列。" en="Dependency candidate sequence, which may be null." /></param>
        /// <returns><l zh-CN="已去空白、按大小写不敏感去重的只读列表。" en="Read-only list with blanks removed and case-insensitive duplicates removed." /></returns>
        private static IList<string> NormalizeList(IEnumerable<string> values)
        {
            // <lang>
            //   <zh-CN>空序列统一为稳定空数组，不保留 null 语义给导航消费方，也不生成占位依赖。</zh-CN>
            //   <en>Converge a null sequence to a stable empty array, leaving no null semantics for navigation consumers and creating no placeholder dependency.</en>
            // </lang>
            if (values == null)
            {
                return new string[0];
            }

            // <lang>
            //   <zh-CN>依赖项仅做去空白和 ordinal-ignore-case 去重后复制为只读列表；本 helper 不验证角色、权限、包或 Profile 是否真实存在。</zh-CN>
            //   <en>Trim and ordinal-ignore-case deduplicate dependency items, then copy them to a read-only list; this helper does not validate whether roles, permissions, packages, or Profiles actually exist.</en>
            // </lang>
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
    /// <remarks>
    /// <lang>
    ///   <zh-CN>注册表只发布可审查的导航元数据和显示依赖；它不读取当前用户、模块状态或部署配置，也不执行路由和授权。消费方必须在显示或访问入口时独立完成实际检查。</zh-CN>
    ///   <en>The registry publishes reviewable navigation metadata and visibility dependencies only; it reads no current user, module state, or deployment configuration and performs no routing or authorization. Consumers must complete actual checks independently when showing or accessing an entry.</en>
    /// </lang>
    /// </remarks>
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

        // <lang>
        //   <zh-CN>注册数组是启动期固定导航契约的唯一内部来源；其中的对象已复制其依赖列表，数组本身不向调用方直接暴露。</zh-CN>
        //   <en>The registration array is the sole internal source for the fixed startup navigation contract; its objects already copied their dependency lists and the array itself is never exposed directly to callers.</en>
        // </lang>
        private static readonly PortalNavigationEntry[] EntryArray =
        {
            // <lang>
            //   <zh-CN>普通用户协同工作台：显示提示同时声明 All Users、协同权限、企业包和推荐 Profile；任何一项不等同于页面访问的授权通过。</zh-CN>
            //   <en>Ordinary-user collaboration workbench: visibility hints declare All Users, collaboration permissions, enterprise package, and recommended Profile together; none alone means page-access authorization passed.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>协同事项后台：仅声明管理员显示策略和后台权限依赖；实际管理员身份、权限与页面访问控制仍在消费/页面边界复核。</zh-CN>
            //   <en>Collaboration-item administration: declares only administrator visibility and back-office permission dependencies; consuming and page boundaries still recheck actual administrator identity, permissions, and page access.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>业务待办后台：保持独立的管理员显示、查看和管理权限元数据，不借用协同事项的权限结论。</zh-CN>
            //   <en>Business work-items administration: retains distinct administrator-visibility, view, and manage permission metadata and does not borrow the collaboration-item permission conclusion.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>模块目录后台：为包/Profile 可见性诊断登记的管理员入口；此说明不使任意包或 Profile 自动可见。</zh-CN>
            //   <en>Module-catalog administration: administrator entry registered for package/Profile visibility diagnostics; this declaration does not make any package or Profile automatically visible.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>系统健康后台：只登记只读运维入口的显示依赖；健康数据和诊断详情各自仍有受限读取路径。</zh-CN>
            //   <en>System-health administration: registers only visibility dependencies for a read-only operations entry; health data and diagnostic details retain their own restricted read paths.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>主题设置后台：显示策略与 ThemeView 权限键仅作导航元数据，不能绕过页面写入前的权限和输入验证。</zh-CN>
            //   <en>Theme-settings administration: visibility policy and ThemeView permission key are navigation metadata only and cannot bypass page write-time authorization or input validation.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>旧注册入口仅为治理/诊断登记且不进入普通导航；它不改变实际注册流程的可用性或账户策略。</zh-CN>
            //   <en>Legacy registration is registered only for governance/diagnostics and stays out of ordinary navigation; it does not alter the actual registration flow's availability or account policy.</en>
            // </lang>
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
        /// <returns>
        /// <l zh-CN="按排序值和稳定键排序的只读入口副本。" en="Read-only entry copy ordered by sort value and stable key." />
        /// </returns>
        public static IList<PortalNavigationEntry> GetEntries()
        {
            // <lang>
            //   <zh-CN>每次调用从内部数组投影一个新只读列表；先按数值排序、再按稳定键大小写不敏感排序，使相同排序值不依赖数组声明顺序。</zh-CN>
            //   <en>Project a new read-only list from the internal array on every call; order first by number then by stable key case-insensitively so equal sort values do not rely on array declaration order.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>空白键没有注册含义，稳定返回 null 而不尝试以显示名、目标或部分匹配猜测入口。</zh-CN>
            //   <en>A blank key has no registered meaning, so return null stably and do not guess an entry by display text, target, or partial matching.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(entryKey))
            {
                return null;
            }

            // <lang>
            //   <zh-CN>仅对去空白后的稳定键进行 ordinal-ignore-case 精确匹配；查找结果是元数据，调用方仍须实施可见性和访问检查。</zh-CN>
            //   <en>Perform an ordinal-ignore-case exact match only on the trimmed stable key; the result is metadata and callers still must enforce visibility and access checks.</en>
            // </lang>
            return EntryArray.FirstOrDefault(entry =>
                string.Equals(entry.EntryKey, entryKey.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
