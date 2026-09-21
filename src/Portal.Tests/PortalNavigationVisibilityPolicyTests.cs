using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖入口分组、相关入口选择与可见性判定纯策略的契约测试。</zh-CN>
    ///   <en>Contract tests covering the pure policy for entry grouping, related-entry selection, and visibility decisions.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只构造纯内存上下文，不读取配置、数据库、HTTP 上下文或真实主题。</zh-CN>
    ///   <en>The tests build pure in-memory contexts only and read no configuration, database, HTTP context, or real theme.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalNavigationVisibilityPolicyTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证既有稳定键通过兼容映射归入正确分组，稳定键本身不被改写。</zh-CN>
        ///   <en>Verifies that existing stable keys are assigned to the right group through the compatibility map without rewriting the keys.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetGroupKey_MapsLegacyStableKeys()
        {
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupAdminCapability,
                PortalNavigationVisibilityPolicy.GetGroupKey("Admin.CollaborationItems"));
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupAdminModules,
                PortalNavigationVisibilityPolicy.GetGroupKey("Admin.ModuleCatalog"));
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupAdminOps,
                PortalNavigationVisibilityPolicy.GetGroupKey("Admin.SystemHealth"));
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupAdminPresentation,
                PortalNavigationVisibilityPolicy.GetGroupKey("Admin.ThemeSettings"));
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupAdminAccount,
                PortalNavigationVisibilityPolicy.GetGroupKey("Account.Register.Legacy"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证新命名入口按前缀推导分组，且未知键返回空分组而不猜配。</zh-CN>
        ///   <en>Verifies that newly named entries derive their group from the prefix, and that unknown keys return an empty group instead of guessing.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetGroupKey_DerivesFromPrefixAndRejectsUnknown()
        {
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupCoreAdmin,
                PortalNavigationVisibilityPolicy.GetGroupKey("Core.Admin.SiteSettings"));
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupAdminError,
                PortalNavigationVisibilityPolicy.GetGroupKey("Admin.Error.NotImplemented"));
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupEnterprise,
                PortalNavigationVisibilityPolicy.GetGroupKey("Enterprise.Capability.Workbench"));
            Assert.AreEqual(
                string.Empty,
                PortalNavigationVisibilityPolicy.GetGroupKey("Some.Unknown.Key"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证相关入口只取同组、排除自身，且按排序值返回。</zh-CN>
        ///   <en>Verifies that related entries come from the same group only, exclude the entry itself, and follow sort order.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetRelatedEntries_ReturnsSameGroupExcludingSelf()
        {
            var related = PortalNavigationVisibilityPolicy.GetRelatedEntries("Core.Admin.SiteSettings");

            // <lang>
            //   <zh-CN>核心管理组共 5 个入口，去掉自身应剩 4 个，正好等于默认上限。</zh-CN>
            //   <en>The core administration group has five entries, so excluding itself leaves four, exactly the default limit.</en>
            // </lang>
            Assert.AreEqual(4, related.Count);
            Assert.IsFalse(
                System.Linq.Enumerable.Any(related, entry => entry.EntryKey == "Core.Admin.SiteSettings"),
                "相关入口不应包含自身。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证相关入口排除诊断专用入口，并遵守条数上限。</zh-CN>
        ///   <en>Verifies that related entries exclude diagnostics-only entries and honour the count limit.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetRelatedEntries_ExcludesDiagnosticOnlyAndHonoursLimit()
        {
            var opsEntries = PortalNavigationVisibilityPolicy.GetRelatedEntries("Admin.Ops.DiagnosticsLogs", 10);
            Assert.IsFalse(
                System.Linq.Enumerable.Any(opsEntries, entry => entry.EntryKey == "Admin.Ops.DiagnosticLogDetail"),
                "诊断专用详情页不应进入相关入口。");

            var limited = PortalNavigationVisibilityPolicy.GetRelatedEntries("Core.Admin.SiteSettings", 2);
            Assert.AreEqual(2, limited.Count, "条数上限应被遵守。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证管理员专属入口对非管理员隐藏，对管理员放行（在其它依赖均满足时）。</zh-CN>
        ///   <en>Verifies that admin-only entries hide from non-administrators and show to administrators when other dependencies are met.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsVisible_RespectsAdminOnlyVisibility()
        {
            var entry = PortalNavigationRegistry.FindByKey("Admin.Ops.OperationAudits");
            Assert.IsNotNull(entry, "该入口应已在 registry 中登记。");

            var nonAdmin = new PortalNavigationVisibilityContext(
                false, new string[0], new string[0], new string[0], "CoreOnly", new string[0]);
            Assert.IsFalse(
                PortalNavigationVisibilityPolicy.IsVisible(entry, nonAdmin),
                "非管理员不应看到管理员专属入口。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证核心包恒定可用：即使允许列表为空，核心管理入口在权限满足时仍可见。</zh-CN>
        ///   <en>Verifies that the core package is always available: core administration entries stay visible when permissions are met even with an empty allow list.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsVisible_TreatsCorePackageAsAlwaysAvailable()
        {
            var entry = PortalNavigationRegistry.FindByKey("Core.Admin.SiteSettings");
            Assert.IsNotNull(entry);

            var admin = new PortalNavigationVisibilityContext(
                true,
                new[] { PortalRoleNames.Administrators },
                new[] { PortalPermissionKeys.SettingsView },
                new string[0],
                "CoreOnly",
                new string[0]);

            Assert.IsTrue(
                PortalNavigationVisibilityPolicy.IsVisible(entry, admin),
                "核心包恒定可用，权限与角色满足时应可见。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证权限键缺失时按 PermissionMissing 阻断。</zh-CN>
        ///   <en>Verifies that a missing permission key blocks the entry with PermissionMissing.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetBlockedReason_ReportsMissingPermission()
        {
            var entry = PortalNavigationRegistry.FindByKey("Core.Admin.SiteSettings");
            Assert.IsNotNull(entry);

            var adminWithoutPermission = new PortalNavigationVisibilityContext(
                true,
                new[] { PortalRoleNames.Administrators },
                new string[0],
                new string[0],
                "CoreOnly",
                new string[0]);

            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.ReasonPermission,
                PortalNavigationVisibilityPolicy.GetBlockedReason(entry, adminWithoutPermission));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 Profile 未启用时阻断，且 Includes 传递包含可使其生效。</zh-CN>
        ///   <en>Verifies that an inactive Profile blocks the entry, while a transitively included Profile makes it active.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetBlockedReason_RespectsProfileDependencies()
        {
            var entry = PortalNavigationRegistry.FindByKey("Enterprise.Capability.Workbench");
            Assert.IsNotNull(entry);

            var fullContext = new PortalNavigationVisibilityContext(
                true,
                new[] { PortalRoleNames.AllUsers },
                new[] { PortalPermissionKeys.BusinessCollaborationCreate, PortalPermissionKeys.BusinessCollaborationViewOwn },
                new[] { PortalNavigationRegistry.EnterpriseWorkbenchPackageId },
                "CoreOnly",
                new string[0]);
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.ReasonProfile,
                PortalNavigationVisibilityPolicy.GetBlockedReason(entry, fullContext),
                "所需 Profile 未启用时应阻断。");

            var includedContext = new PortalNavigationVisibilityContext(
                true,
                new[] { PortalRoleNames.AllUsers },
                new[] { PortalPermissionKeys.BusinessCollaborationCreate, PortalPermissionKeys.BusinessCollaborationViewOwn },
                new[] { PortalNavigationRegistry.EnterpriseWorkbenchPackageId },
                "BusinessWorkflow",
                new[] { "EnterpriseBase", "EnterpriseWorkbench" });
            Assert.IsTrue(
                PortalNavigationVisibilityPolicy.IsVisible(entry, includedContext),
                "Includes 传递包含所需 Profile 时应可见。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证生命周期与可见性策略对错误/占位入口的阻断原因。</zh-CN>
        ///   <en>Verifies lifecycle and visibility-policy blocking reasons for error and placeholder entries.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetBlockedReason_ReportsLifecycleAndVisibilityMode()
        {
            var placeholder = PortalNavigationRegistry.FindByKey("Admin.Error.NotImplemented");
            Assert.IsNotNull(placeholder);
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.ReasonLifecycle,
                PortalNavigationVisibilityPolicy.GetBlockedReason(placeholder, new PortalNavigationVisibilityContext(
                    true, new string[0], new string[0], new string[0], "CoreOnly", new string[0])),
                "废弃入口应按生命周期阻断。");

            var denialTarget = PortalNavigationRegistry.FindByKey("Admin.Error.AccessDenied");
            Assert.IsNotNull(denialTarget);
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.ReasonVisibilityMode,
                PortalNavigationVisibilityPolicy.GetBlockedReason(denialTarget, new PortalNavigationVisibilityContext(
                    true, new string[0], new string[0], new string[0], "CoreOnly", new string[0])),
                "诊断专用入口不进普通导航。");
        }
    }
}
