using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖主导航 Tab 门控契约的测试：开关解析、稳定键构造、放行兜底与阻断判定。</zh-CN>
    ///   <en>Tests covering the main-navigation tab gate contract: switch parsing, stable-key building, allow-by-default fallbacks, and blocking decisions.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只构造纯内存上下文，不读取配置、数据库、HTTP 上下文或门户 Tab 数据；Tab 条目本身来自静态注册表，因此断言的是"登记与判定"的一致性，而不是某次部署的实际 Tab 列表。</zh-CN>
    ///   <en>The tests build pure in-memory contexts only and read no configuration, database, HTTP context, or portal tab data; tab entries come from the static registry, so the assertions cover registration-to-decision consistency rather than any single deployment's actual tab list.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalNavigationTabGateTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证只有显式的真值才启用门控：空白、缺失与不可识别值一律按关闭处理，使配置写错时退回现状。</zh-CN>
        ///   <en>Verifies that only explicit truthy values enable the gate: blank, missing, and unrecognized values are treated as off so a bad configuration falls back to current behaviour.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsTabGateEnabled_OnlyExplicitTruthyValuesEnable()
        {
            // <lang>
            //   <zh-CN>关闭侧样本包含配置缺失（null/空白）与常见假值写法，覆盖"默认关闭"这一回滚点。</zh-CN>
            //   <en>The off-side samples include missing configuration (null/blank) and common falsy spellings, covering the "off by default" rollback point.</en>
            // </lang>
            string[] disabledValues = { null, string.Empty, "   ", "false", "False", "0", "no", "off", "enabled", "truee" };

            foreach (string disabledValue in disabledValues)
            {
                Assert.IsFalse(
                    PortalNavigationVisibilityPolicy.IsTabGateEnabled(disabledValue),
                    "Expected the gate to stay off for value: " + (disabledValue ?? "<null>"));
            }

            // <lang>
            //   <zh-CN>启用侧样本覆盖大写与前后空白，说明解析对写法宽容但对语义严格。</zh-CN>
            //   <en>The on-side samples cover uppercase and surrounding whitespace, showing that parsing is tolerant of spelling yet strict about meaning.</en>
            // </lang>
            string[] enabledValues = { "true", "TRUE", "True", "  true  ", "1", "yes", "YES", "on", "On" };

            foreach (string enabledValue in enabledValues)
            {
                Assert.IsTrue(
                    PortalNavigationVisibilityPolicy.IsTabGateEnabled(enabledValue),
                    "Expected the gate to be on for value: " + enabledValue);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证稳定键由固定前缀加去空白的 Tab 名构成，且不做大小写折叠或字符替换。</zh-CN>
        ///   <en>Verifies that the stable key is the fixed prefix plus the trimmed tab name, with no case folding or character replacement.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void BuildTabEntryKey_UsesPrefixAndTrimWithoutCaseFolding()
        {
            // <lang>
            //   <zh-CN>带前后空白的名称应归一为已登记键；大小写保持原样，避免登记与运行结果不一致。</zh-CN>
            //   <en>A name with surrounding whitespace normalizes to the registered key; casing is preserved so registration and runtime cannot diverge.</en>
            // </lang>
            Assert.AreEqual("Tab.Home", PortalNavigationVisibilityPolicy.BuildTabEntryKey("Home"));
            Assert.AreEqual("Tab.Home", PortalNavigationVisibilityPolicy.BuildTabEntryKey("  Home  "));
            Assert.AreEqual("Tab.Product Info", PortalNavigationVisibilityPolicy.BuildTabEntryKey("Product Info"));
            Assert.AreEqual("Tab.home", PortalNavigationVisibilityPolicy.BuildTabEntryKey("home"));

            // <lang>
            //   <zh-CN>空白名没有可判定对象，返回空字符串而不是前缀本身。</zh-CN>
            //   <en>A blank name has nothing to evaluate, so an empty string is returned instead of the bare prefix.</en>
            // </lang>
            Assert.AreEqual(string.Empty, PortalNavigationVisibilityPolicy.BuildTabEntryKey(null));
            Assert.AreEqual(string.Empty, PortalNavigationVisibilityPolicy.BuildTabEntryKey("   "));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证未登记 Tab、空白名与缺失上下文一律放行，这是门控的零回归兜底。</zh-CN>
        ///   <en>Verifies that unregistered tabs, blank names, and a missing context are always allowed, which is the gate's zero-regression fallback.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetTabBlockedReason_AllowsUnregisteredBlankAndContextlessInputs()
        {
            PortalNavigationVisibilityContext context = CreateContext(isAdministrator: false, "All Users");

            // <lang>
            //   <zh-CN>未登记名称不会被猜测为已登记项；返回值必须是"放行"而不是某个阻断原因。</zh-CN>
            //   <en>An unregistered name is never guessed to be a registered entry; the result must be allowed rather than some blocking reason.</en>
            // </lang>
            Assert.IsNull(PortalNavigationVisibilityPolicy.GetTabBlockedReason("Nonexistent Tab", context));
            Assert.IsNull(PortalNavigationVisibilityPolicy.GetTabBlockedReason(null, context));
            Assert.IsNull(PortalNavigationVisibilityPolicy.GetTabBlockedReason("   ", context));

            // <lang>
            //   <zh-CN>上下文缺失时同样放行：门控只在能完成判定时才允许阻断，避免治理增强变成导航消失事故。</zh-CN>
            //   <en>A missing context is allowed as well: the gate may block only when a decision can be completed, so a governance improvement cannot turn into navigation loss.</en>
            // </lang>
            Assert.IsNull(PortalNavigationVisibilityPolicy.GetTabBlockedReason("Home", null));
            Assert.IsNull(PortalNavigationVisibilityPolicy.GetTabBlockedReason("Admin", null));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证无依赖的已登记 Tab 对普通用户可见，以及管理员专属 Tab 按角色阻断与放行。</zh-CN>
        ///   <en>Verifies that a registered dependency-free tab stays visible to an ordinary user, and that the administrator-only tab blocks and allows by role.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetTabBlockedReason_AppliesRegisteredDependencies()
        {
            PortalNavigationVisibilityContext ordinaryUser = CreateContext(isAdministrator: false, "All Users");

            // <lang>
            //   <zh-CN>Home 条目未声明任何依赖，因此对普通用户也是放行；门控不会凭空隐藏已登记且无约束的 Tab。</zh-CN>
            //   <en>The Home entry declares no dependency, so it is allowed even for an ordinary user; the gate never hides a registered tab without a reason.</en>
            // </lang>
            Assert.IsNull(PortalNavigationVisibilityPolicy.GetTabBlockedReason("Home", ordinaryUser));

            // <lang>
            //   <zh-CN>Admin 条目声明为管理员专属：普通用户被角色原因阻断，管理员放行。</zh-CN>
            //   <en>The Admin entry is declared administrator-only: an ordinary user is blocked with the role reason while an administrator is allowed.</en>
            // </lang>
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.ReasonRole,
                PortalNavigationVisibilityPolicy.GetTabBlockedReason("Admin", ordinaryUser));

            PortalNavigationVisibilityContext administrator = CreateContext(isAdministrator: true, "Admins");

            // <lang>
            //   <zh-CN>管理员视角下同一 Tab 不再被阻断，说明判定确实读取上下文而不是无条件隐藏。</zh-CN>
            //   <en>For an administrator the same tab is no longer blocked, showing that the decision really reads the context instead of hiding unconditionally.</en>
            // </lang>
            Assert.IsNull(PortalNavigationVisibilityPolicy.GetTabBlockedReason("Admin", administrator));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 Tab 条目自成一组的判定先于业务前缀，且不会混入 Admin 动作区的相关入口组。</zh-CN>
        ///   <en>Verifies that tab entries form their own group ahead of business prefixes and never leak into an Admin action-area related group.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TabEntriesStayInDedicatedGroupAndOutOfAdminActionGroups()
        {
            // <lang>
            //   <zh-CN>分组必须显式命中 Tab 组，而不是依赖"未知前缀返回空"的隐式行为。</zh-CN>
            //   <en>The group must explicitly resolve to the tab group instead of relying on the implicit "unknown prefix returns empty" behaviour.</en>
            // </lang>
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupTab,
                PortalNavigationVisibilityPolicy.GetGroupKey("Tab.Home"));
            Assert.AreEqual(
                PortalNavigationVisibilityPolicy.GroupTab,
                PortalNavigationVisibilityPolicy.GetGroupKey(
                    PortalNavigationVisibilityPolicy.BuildTabEntryKey("About the Portal")));

            // <lang>
            //   <zh-CN>逐个 Admin 动作区入口检查相关入口组：任何 Tab 前缀条目出现都视为回归，因为 Tab 条目只承载门控元数据。</zh-CN>
            //   <en>Check the related group of every Admin action-area entry: any tab-prefixed entry is a regression, because tab entries carry gate metadata only.</en>
            // </lang>
            string[] adminActionEntryKeys =
            {
                "Admin.Ops.DiagnosticsLogs",
                "Admin.Ops.DiagnosticLogDetail",
                "Admin.Ops.OperationAudits",
                "Admin.Modules.Definitions",
                "Admin.Modules.Settings",
                "Admin.Modules.TabLayout",
                "Admin.Capability.EmployeeDirectory",
                "Admin.Capability.BusinessApplications",
                "Admin.Account.ManageUsers"
            };

            foreach (string entryKey in adminActionEntryKeys)
            {
                IList<PortalNavigationEntry> relatedEntries = PortalNavigationVisibilityPolicy.GetRelatedEntries(entryKey);

                foreach (PortalNavigationEntry relatedEntry in relatedEntries)
                {
                    Assert.IsFalse(
                        relatedEntry.EntryKey.StartsWith(
                            PortalNavigationVisibilityPolicy.TabEntryKeyPrefix,
                            System.StringComparison.OrdinalIgnoreCase),
                        "Tab entry leaked into the related group of " + entryKey + ": " + relatedEntry.EntryKey);
                }
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造纯内存可见性上下文，供各用例复用；不接触配置、数据库或 HTTP 上下文。</zh-CN>
        ///   <en>Builds a pure in-memory visibility context for reuse across cases, touching no configuration, database, or HTTP context.</en>
        /// </lang>
        /// </summary>
        /// <param name="isAdministrator">
        /// <l>
        ///   <zh-CN>当前用户是否管理员。</zh-CN>
        ///   <en>Whether the current user is an administrator.</en>
        /// </l>
        /// </param>
        /// <param name="roleNames">
        /// <l>
        ///   <zh-CN>当前用户角色名；为空时使用空集合。</zh-CN>
        ///   <en>Current user role names; an empty set is used when blank.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可用于可见性判定的上下文。</zh-CN>
        ///   <en>Context usable for visibility decisions.</en>
        /// </l>
        /// </returns>
        private static PortalNavigationVisibilityContext CreateContext(bool isAdministrator, params string[] roleNames)
        {
            // <lang>
            //   <zh-CN>权限、包与 Profile 一律留空：Tab 门控的既有登记未声明这些依赖，测试也应显式表达"未声明即不限制"。</zh-CN>
            //   <en>Permissions, packages, and Profiles stay empty: the registered tab entries declare none of them, and the tests should express "undeclared means unrestricted" explicitly.</en>
            // </lang>
            return new PortalNavigationVisibilityContext(
                isAdministrator,
                roleNames,
                new string[0],
                new string[0],
                string.Empty,
                new string[0]);
        }
    }
}
