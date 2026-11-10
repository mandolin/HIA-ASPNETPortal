using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>主导航分组配置别名机制的回归测试：封闭词表稳定性、fail-closed 解析、本地化、
    ///   不参与授权的结构性边界，以及主题设置入口的入链可达性。</zh-CN>
    ///   <en>Regression tests for the main-navigation group configuration alias mechanism: closed vocabulary stability,
    ///   fail-closed parsing, localization, the structural boundary that keeps groups out of authorization, and
    ///   inbound-link reachability of the theme settings entry.</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class PortalNavigationGroupAliasTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>词表必须恰好覆盖四个功能族分组，且顺序由 SortOrder 决定；防止后续悄悄增删分组改变信息架构。</zh-CN>
        ///   <en>The vocabulary must contain exactly four functional-family groups ordered by SortOrder, preventing silent
        ///   group additions or removals from changing the information architecture.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetGroups_ReturnsClosedVocabularyInPresentationOrder()
        {
            IList<PortalNavigationGroupAlias.GroupDefinition> groups = PortalNavigationGroupAlias.GetGroups();

            Assert.AreEqual(4, groups.Count);
            CollectionAssert.AreEqual(
                new[]
                {
                    PortalNavigationGroupAlias.GroupKeys.WorkAndApproval,
                    PortalNavigationGroupAlias.GroupKeys.Business,
                    PortalNavigationGroupAlias.GroupKeys.PortalAndHelp,
                    PortalNavigationGroupAlias.GroupKeys.SystemAdministration
                },
                groups.Select(group => group.GroupKey).ToArray());
            CollectionAssert.AreEqual(
                new[] { 10, 20, 30, 40 },
                groups.Select(group => group.SortOrder).ToArray());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>词表默认成员必须覆盖 `Portal_NavigationSeed.sql` 的八个 NavigationKey，且不重复、不跨组，
        ///   保证任何种子页签都能归入某个分组或明确按未分组平铺展示。</zh-CN>
        ///   <en>Default members must cover the eight NavigationKey values from `Portal_NavigationSeed.sql`, with no
        ///   duplicates or cross-group repeats, so every seeded tab either belongs to a group or is clearly rendered flat.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void DefaultMembership_CoversAllSeedNavigationKeysExactlyOnce()
        {
            IList<PortalNavigationGroupAlias.GroupDefinition> groups = PortalNavigationGroupAlias.GetGroups();
            List<string> allMembers = groups.SelectMany(group => group.DefaultNavigationKeys).ToList();

            Assert.AreEqual(8, allMembers.Count);
            Assert.AreEqual(8, allMembers.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            CollectionAssert.AreEquivalent(
                new[] { "Home", "Help", "MyWorkItems", "LeaveAndExpense", "BusinessRequests", "MyProfile", "Admin", "CapabilityWorkbench" },
                allMembers);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>封闭词表按稳定键精确查找，忽略大小写与首尾空白；未知键不得猜测分组。</zh-CN>
        ///   <en>The closed vocabulary finds groups by stable key, ignoring case and surrounding whitespace; unknown keys must not be guessed.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryFindByKey_IsExactAndCaseInsensitive()
        {
            PortalNavigationGroupAlias.GroupDefinition definition;

            Assert.IsTrue(PortalNavigationGroupAlias.TryFindByKey("  business ", out definition));
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.Business, definition.GroupKey);
            Assert.IsTrue(PortalNavigationGroupAlias.TryFindByKey("WORKANDAPPROVAL", out definition));
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.WorkAndApproval, definition.GroupKey);
            Assert.IsFalse(PortalNavigationGroupAlias.TryFindByKey("Unknown.Group", out definition));
            Assert.IsNull(definition);
            Assert.IsFalse(PortalNavigationGroupAlias.TryFindByKey("  ", out definition));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>分组显示名按界面文化切换中英，并与 `PortalNavigationEntry.GetDisplayName` 同口径；
        ///   其它语言走英文，避免中英混排。</zh-CN>
        ///   <en>Group labels switch Chinese/English by UI culture using the same convention as
        ///   `PortalNavigationEntry.GetDisplayName`; other languages use English to avoid a mixed-language surface.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetDisplayName_SwitchesByUiCulture()
        {
            PortalNavigationGroupAlias.GroupDefinition definition;
            Assert.IsTrue(PortalNavigationGroupAlias.TryFindByKey(PortalNavigationGroupAlias.GroupKeys.WorkAndApproval, out definition));

            Assert.AreEqual("工作与审批", definition.GetDisplayName(new CultureInfo("zh-CN")));
            Assert.AreEqual("工作与审批", definition.GetDisplayName(new CultureInfo("zh-TW")));
            Assert.AreEqual("Work & Approvals", definition.GetDisplayName(new CultureInfo("en-US")));
            Assert.AreEqual("Work & Approvals", definition.GetDisplayName(new CultureInfo("fr-FR")));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>空配置是合法的无覆盖状态；合法覆盖按页签键忽略大小写生效。</zh-CN>
        ///   <en>Empty configuration is a legal no-override state; valid overrides apply by tab key, case-insensitively.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryParseAliases_AcceptsEmptyAndValidOverrides()
        {
            IDictionary<string, string> aliases;
            string reason;

            Assert.IsTrue(PortalNavigationGroupAlias.TryParseAliases(null, out aliases, out reason));
            Assert.AreEqual(0, aliases.Count);
            Assert.AreEqual(string.Empty, reason);

            Assert.IsTrue(PortalNavigationGroupAlias.TryParseAliases(
                " MyWorkItems = Business , Help=SystemAdministration ", out aliases, out reason));
            Assert.AreEqual(2, aliases.Count);
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.Business, aliases["myworkitems"]);
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.SystemAdministration, aliases["HELP"]);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>畸形条目、未知分组键、重复页签键一律让**整份配置** fail-closed 失败并返回可诊断的原因码，
        ///   不接受部分覆盖（避免部署结果依赖条目顺序）。</zh-CN>
        ///   <en>Malformed pairs, unknown group keys, and duplicate tab keys make the **entire configuration** fail closed
        ///   with a diagnosable reason code; no partial override is accepted so deployment results never depend on entry order.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryParseAliases_FailsClosedOnAnyInvalidEntry()
        {
            IDictionary<string, string> aliases;
            string reason;

            Assert.IsFalse(PortalNavigationGroupAlias.TryParseAliases("MyWorkItems", out aliases, out reason));
            Assert.IsNull(aliases);
            Assert.AreEqual("MalformedPair", reason);

            Assert.IsFalse(PortalNavigationGroupAlias.TryParseAliases("MyWorkItems=Business=PortalAndHelp", out aliases, out reason));
            Assert.AreEqual("MalformedPair", reason);

            Assert.IsFalse(PortalNavigationGroupAlias.TryParseAliases("MyWorkItems=", out aliases, out reason));
            Assert.AreEqual("MalformedPair", reason);

            Assert.IsFalse(PortalNavigationGroupAlias.TryParseAliases("=Business", out aliases, out reason));
            Assert.AreEqual("MalformedPair", reason);

            Assert.IsFalse(PortalNavigationGroupAlias.TryParseAliases("MyWorkItems=UnknownGroup", out aliases, out reason));
            Assert.AreEqual("UnknownGroupKey", reason);

            Assert.IsFalse(PortalNavigationGroupAlias.TryParseAliases("MyWorkItems=Business,MyWorkItems=PortalAndHelp", out aliases, out reason));
            Assert.AreEqual("DuplicateNavigationKey", reason);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析覆盖优先于词表默认；非法配置降级但仍按默认词表返回分组，绝不隐藏页签；
        ///   未知页签返回未分组（由渲染层平铺展示）。</zh-CN>
        ///   <en>Overrides take precedence over vocabulary defaults; invalid configuration degrades yet still resolves through
        ///   defaults so no tab is hidden; an unknown tab is ungrouped and rendered flat by the caller.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryResolveGroup_OverrideWins_InvalidDegradesWithoutHiding()
        {
            PortalNavigationGroupAlias.GroupDefinition definition;
            bool degraded;

            Assert.IsTrue(PortalNavigationGroupAlias.TryResolveGroup("MyWorkItems", null, out definition, out degraded));
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.WorkAndApproval, definition.GroupKey);
            Assert.IsFalse(degraded);

            Assert.IsTrue(PortalNavigationGroupAlias.TryResolveGroup("MyWorkItems", "MyWorkItems=Business", out definition, out degraded));
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.Business, definition.GroupKey);
            Assert.IsFalse(degraded);

            Assert.IsTrue(PortalNavigationGroupAlias.TryResolveGroup("MyWorkItems", "MyWorkItems=UnknownGroup", out definition, out degraded));
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.WorkAndApproval, definition.GroupKey);
            Assert.IsTrue(degraded);

            Assert.IsFalse(PortalNavigationGroupAlias.TryResolveGroup("FutureTab", null, out definition, out degraded));
            Assert.IsNull(definition);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>分组视图按词表顺序产出四组，成员顺序跟随输入；未分组页签如实返回且不被丢弃。</zh-CN>
        ///   <en>The grouped view produces four groups in vocabulary order with members following input order; ungrouped tabs
        ///   are returned truthfully and never dropped.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void BuildGroupedView_KeepsEveryTabAndOrdersGroupsByVocabulary()
        {
            IList<string> ungrouped;
            bool degraded;

            IList<KeyValuePair<PortalNavigationGroupAlias.GroupDefinition, IList<string>>> view =
                PortalNavigationGroupAlias.BuildGroupedView(
                    new[] { "Home", "MyWorkItems", "FutureTab", "Admin" }, null,
                    out ungrouped, out degraded);

            Assert.AreEqual(4, view.Count);
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.WorkAndApproval, view[0].Key.GroupKey);
            CollectionAssert.AreEqual(new[] { "MyWorkItems" }, view[0].Value.ToArray());
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.PortalAndHelp, view[2].Key.GroupKey);
            CollectionAssert.AreEqual(new[] { "Home" }, view[2].Value.ToArray());
            CollectionAssert.AreEqual(new[] { "Admin" }, view[3].Value.ToArray());
            CollectionAssert.AreEqual(new[] { "FutureTab" }, ungrouped.ToArray());
            Assert.IsFalse(degraded);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>A 级硬边界的结构性证据：分组定义与别名静态类不得携带任何角色/权限/授权相关公开成员或字段，
        ///   防止后续把分组悄悄变成授权依据；分组只能描述展示顺序与本地化名称。</zh-CN>
        ///   <en>Structural evidence for the class-A boundary: the group definition and alias static type must expose no
        ///   role/permission/authorization public member or field, preventing groups from silently becoming an
        ///   authorization basis; groups may describe only presentation order and localized labels.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GroupTypes_CarryNoAuthorizationSurface()
        {
            string[] forbidden = { "role", "permission", "authoriz", "access", "isadmin", "ensurepermission", "security" };

            foreach (Type type in new[] { typeof(PortalNavigationGroupAlias), typeof(PortalNavigationGroupAlias.GroupDefinition) })
            {
                IEnumerable<string> publicNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(member => member.Name)
                    .Concat(type.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Select(field => field.Name));

                foreach (string name in publicNames)
                {
                    foreach (string needle in forbidden)
                    {
                        Assert.IsFalse(
                            name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0,
                            type.Name + " must not expose an authorization-related member: " + name);
                    }
                }
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>主题设置入口别名必须挂在运行期可达的 `Admin.Ops` 组且可导航，
        ///   使 `RenderActions` 能从系统健康等已可达页产出到 `Admin/ThemeSettings.aspx` 的入链；
        ///   同时角色与权限要求须与既有条目一致，不得因别名放宽授权。</zh-CN>
        ///   <en>The theme settings entry alias must belong to the reachable `Admin.Ops` group and be navigable so
        ///   `RenderActions` can produce an inbound link to `Admin/ThemeSettings.aspx` from reachable pages such as System
        ///   Health; its role and permission requirements must match the existing entry so the alias never relaxes authorization.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ThemeSettingsAlias_ProducesInboundLinkWithoutRelaxingAuthorization()
        {
            PortalNavigationEntry alias = PortalNavigationRegistry.FindByKey("Admin.Ops.ThemeSettingsEntry");
            PortalNavigationEntry original = PortalNavigationRegistry.FindByKey("Admin.ThemeSettings");
            PortalNavigationEntry reachableHost = PortalNavigationRegistry.FindByKey("Admin.SystemHealth");

            Assert.IsNotNull(alias);
            Assert.IsNotNull(original);
            Assert.IsNotNull(reachableHost);
            Assert.AreEqual("Admin/ThemeSettings.aspx", alias.Target);
            Assert.AreEqual(PortalNavigationLifecycleState.Active, alias.LifecycleState);
            Assert.AreEqual(PortalNavigationVisibilityMode.AdminOnly, alias.VisibilityMode);

            string aliasGroup = PortalNavigationVisibilityPolicy.GetGroupKey(alias);
            string hostGroup = PortalNavigationVisibilityPolicy.GetGroupKey(reachableHost);
            Assert.AreEqual(hostGroup, aliasGroup);
            Assert.IsTrue(PortalNavigationVisibilityPolicy.IsNavigable(alias));

            CollectionAssert.AreEquivalent(original.RequiredRoles.ToArray(), alias.RequiredRoles.ToArray());
            CollectionAssert.AreEquivalent(original.RequiredPermissionKeys.ToArray(), alias.RequiredPermissionKeys.ToArray());

            IList<PortalNavigationEntry> related = PortalNavigationVisibilityPolicy.GetRelatedEntries(reachableHost.EntryKey, 4);
            Assert.IsTrue(related.Any(entry => string.Equals(entry.EntryKey, alias.EntryKey, StringComparison.OrdinalIgnoreCase)),
                "The reachable admin host must render an inbound link to the theme settings page.");
        }
    }
}
