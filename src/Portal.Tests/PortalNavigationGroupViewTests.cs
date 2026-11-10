using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>主导航两级分组展示视图的回归测试：分组归属与顺序、未分组页签不隐藏、显示下标不变、
    ///   选中与禁用态、非法配置降级、空组过滤、容器 id 稳定性与中英本地化，以及分组不参与授权的结构断言。</zh-CN>
    ///   <en>Regression tests for the two-level main-navigation grouped view: membership and order, never hiding ungrouped
    ///   tabs, preserved display indices, selected and disabled states, invalid-configuration degradation, empty-group
    ///   filtering, stable container ids, Chinese/English localization, and the structural assertion that grouping
    ///   takes no part in authorization.</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class PortalNavigationGroupViewTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>按封闭词表把 8 个种子页签归入 4 个非空分组，组顺序由 SortOrder 决定；
        ///   每个页签的显示下标等于它在输入集合中的原始位置。</zh-CN>
        ///   <en>The eight seeded tabs fall into four non-empty groups per the closed vocabulary, ordered by SortOrder;
        ///   each tab's display index equals its original position in the input collection.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_GroupsSeedTabsInVocabularyOrderAndPreservesDisplayIndex()
        {
            // <lang>
            //   <zh-CN>故意打乱输入顺序，验证分组顺序来自词表而非数据库排序。</zh-CN>
            //   <en>Deliberately shuffle the input so group order is proven to come from the vocabulary, not database ordering.</en>
            // </lang>
            IList<ITabItem> tabs = Tabs(
                Tab("CapabilityWorkbench", 60), Tab("Home", 61), Tab("MyWorkItems", 62),
                Tab("BusinessRequests", 63), Tab("Help", 64), Tab("MyProfile", 65),
                Tab("LeaveAndExpense", 66), Tab("Admin", 67));

            PortalNavigationGroupView.NavigationView view = PortalNavigationGroupView.Build(
                tabs, 61, string.Empty, null, CultureInfo.GetCultureInfo("zh-CN"));

            Assert.IsFalse(view.Degraded);
            Assert.AreEqual(0, view.UngroupedTabs.Count);
            Assert.AreEqual(0, view.EmptySectionCount);
            Assert.AreEqual(4, view.Sections.Count);
            CollectionAssert.AreEqual(
                new[]
                {
                    PortalNavigationGroupAlias.GroupKeys.WorkAndApproval,
                    PortalNavigationGroupAlias.GroupKeys.Business,
                    PortalNavigationGroupAlias.GroupKeys.PortalAndHelp,
                    PortalNavigationGroupAlias.GroupKeys.SystemAdministration
                },
                view.Sections.Select(section => section.GroupKey).ToArray());

            // <lang>
            //   <zh-CN>组内成员顺序沿用输入顺序（MyWorkItems 在 LeaveAndExpense 之前），下标仍是原始位置。</zh-CN>
            //   <en>Members keep input order (MyWorkItems before LeaveAndExpense) and their indices stay the original positions.</en>
            // </lang>
            CollectionAssert.AreEqual(
                new[] { "MyWorkItems", "LeaveAndExpense" },
                view.Sections[0].Tabs.Select(item => item.Tab.NavigationKey).ToArray());
            CollectionAssert.AreEqual(
                new[] { 2, 6 },
                view.Sections[0].Tabs.Select(item => item.DisplayIndex).ToArray());
            Assert.AreEqual(8, view.Sections.Sum(section => section.TabCount));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>不属于词表的稳定键（历史 Test.* 页签）与空稳定键一律进平铺区，绝不隐藏；
        ///   平铺区顺序沿用输入顺序，下标也保持原值。</zh-CN>
        ///   <en>Stable keys outside the vocabulary (legacy Test.* tabs) and blank stable keys always go to the flat
        ///   area and are never hidden; the flat area keeps input order and the original indices.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_UnknownAndBlankNavigationKeysAreRenderedFlatNeverHidden()
        {
            IList<ITabItem> tabs = Tabs(
                Tab("Home", 1), Tab("Test.Employee Info", 2), Tab("", 3),
                Tab(null, 4), Tab("Admin", 5));

            PortalNavigationGroupView.NavigationView view = PortalNavigationGroupView.Build(
                tabs, 1, string.Empty, null, CultureInfo.GetCultureInfo("en-US"));

            Assert.AreEqual(3, view.UngroupedTabs.Count);
            // <lang>
            //   <zh-CN>显示下标是输入集合中的 0 基位置，与 TabId 无关；三个未分组页签位于第 1/2/3 位。</zh-CN>
            //   <en>Display indices are zero-based positions in the input collection, unrelated to TabId; the three ungrouped tabs sit at positions 1/2/3.</en>
            // </lang>
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, view.UngroupedTabs.Select(item => item.DisplayIndex).ToArray());
            // <lang>
            //   <zh-CN>分组区仍有 Home 与 Admin 两个已知键，一个都不丢。</zh-CN>
            //   <en>Both known keys (Home and Admin) remain in the groups; none is lost.</en>
            // </lang>
            Assert.AreEqual(2, view.Sections.Sum(section => section.TabCount));
            Assert.AreEqual(5, view.Sections.Sum(section => section.TabCount) + view.UngroupedTabs.Count);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>空分组被过滤但计数留证：匿名态只有 Home / Help 两个页签时，只渲染「门户与帮助」一组，
        ///   不输出点了却没有内容的 disclosure 按钮。</zh-CN>
        ///   <en>Empty groups are filtered but counted as evidence: with only Home / Help visible to anonymous visitors,
        ///   only the Portal &amp; Help group renders, so no disclosure button reveals nothing.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_FiltersEmptyGroupsButKeepsThemCounted()
        {
            IList<ITabItem> tabs = Tabs(Tab("Home", 1), Tab("Help", 2));

            PortalNavigationGroupView.NavigationView view = PortalNavigationGroupView.Build(
                tabs, 1, string.Empty, null, CultureInfo.GetCultureInfo("zh-CN"));

            Assert.AreEqual(1, view.Sections.Count);
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.PortalAndHelp, view.Sections[0].GroupKey);
            Assert.AreEqual(3, view.EmptySectionCount);
            Assert.AreEqual(2, view.Sections[0].TabCount);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非法别名配置触发 fail-closed 降级标记，但分组数据仍按默认词表完整产出，
        ///   任何页签都不因配置错误而消失；渲染层据此选择一级平铺。</zh-CN>
        ///   <en>Invalid alias configuration sets the fail-closed degradation flag, yet grouped data is still produced
        ///   from defaults and no tab disappears; the renderer uses the flag to choose flat navigation.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_InvalidConfigurationIsDegradedButLosesNoTab()
        {
            IList<ITabItem> tabs = Tabs(
                Tab("Home", 1), Tab("MyWorkItems", 2), Tab("Admin", 3), Tab("Test.Legacy", 4));

            PortalNavigationGroupView.NavigationView view = PortalNavigationGroupView.Build(
                tabs, 3, "Home=NoSuchGroup", null, CultureInfo.GetCultureInfo("zh-CN"));

            Assert.IsTrue(view.Degraded);
            Assert.AreEqual(4, view.Sections.Sum(section => section.TabCount) + view.UngroupedTabs.Count);
            Assert.AreEqual(3, view.Sections.Sum(section => section.TabCount));
            Assert.AreEqual(1, view.UngroupedTabs.Count);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>合法别名覆盖把页签移到指定分组，且组顺序不变；覆盖不引入新分组键。</zh-CN>
        ///   <en>A valid alias override moves a tab to the requested group without changing group order or introducing a new group key.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_ValidAliasOverrideMovesTabWithoutAddingGroups()
        {
            IList<ITabItem> tabs = Tabs(Tab("Home", 1), Tab("MyWorkItems", 2));

            PortalNavigationGroupView.NavigationView view = PortalNavigationGroupView.Build(
                tabs, 1, "Home=" + PortalNavigationGroupAlias.GroupKeys.WorkAndApproval,
                null, CultureInfo.GetCultureInfo("zh-CN"));

            Assert.IsFalse(view.Degraded);
            Assert.AreEqual(1, view.Sections.Count);
            Assert.AreEqual(PortalNavigationGroupAlias.GroupKeys.WorkAndApproval, view.Sections[0].GroupKey);
            CollectionAssert.AreEqual(new[] { "Home", "MyWorkItems" },
                view.Sections[0].Tabs.Select(item => item.Tab.NavigationKey).ToArray());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>选中态只按 TabId 判定；禁用态与原因码直接取授权阶段结果，视图不重新判定权限。</zh-CN>
        ///   <en>The selected state is decided by TabId only; the disabled state and reason code come straight from the
        ///   authorization stage, with no permission decision repeated by the view.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_MarksSelectedAndBlockedTabsFromAuthorizationResults()
        {
            IList<ITabItem> tabs = Tabs(Tab("Home", 101), Tab("MyWorkItems", 102), Tab("Admin", 103));
            Dictionary<int, string> blocked = new Dictionary<int, string> { { 102, "NoModuleDefinition" } };

            PortalNavigationGroupView.NavigationView view = PortalNavigationGroupView.Build(
                tabs, 101, string.Empty, blocked, CultureInfo.GetCultureInfo("zh-CN"));

            PortalNavigationGroupView.GroupTabView selected = AllItems(view).Single(item => item.Tab.TabId == 101);
            PortalNavigationGroupView.GroupTabView blockedItem = AllItems(view).Single(item => item.Tab.TabId == 102);
            PortalNavigationGroupView.GroupTabView normal = AllItems(view).Single(item => item.Tab.TabId == 103);

            Assert.IsTrue(selected.IsSelected);
            Assert.IsFalse(selected.IsBlocked);
            Assert.IsFalse(blockedItem.IsSelected);
            Assert.IsTrue(blockedItem.IsBlocked);
            Assert.AreEqual("NoModuleDefinition", blockedItem.BlockedReason);
            Assert.IsFalse(normal.IsSelected);
            Assert.IsFalse(normal.IsBlocked);
            Assert.AreEqual(string.Empty, normal.BlockedReason);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>分组显示名随界面文化切换，容器 id 稳定、唯一、只含小写字母数字与连字符，
        ///   且带静态前缀，不会把请求数据拼进 DOM id。</zh-CN>
        ///   <en>Group labels follow the UI culture; container ids are stable, unique, limited to lowercase alphanumerics
        ///   and hyphens, and carry a static prefix so no request data is concatenated into a DOM id.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_LocalizesGroupNamesAndProducesStableContainerIds()
        {
            IList<ITabItem> tabs = Tabs(
                Tab("Home", 1), Tab("Help", 2), Tab("MyWorkItems", 3),
                Tab("BusinessRequests", 4), Tab("Admin", 5));

            PortalNavigationGroupView.NavigationView zh = PortalNavigationGroupView.Build(
                tabs, 1, string.Empty, null, CultureInfo.GetCultureInfo("zh-CN"));
            PortalNavigationGroupView.NavigationView en = PortalNavigationGroupView.Build(
                tabs, 1, string.Empty, null, CultureInfo.GetCultureInfo("en-US"));

            // <lang>
            //   <zh-CN>5 个页签分别落入「门户与帮助 / 工作与审批 / 业务办理 / 系统管理」四组，故非空分组为 4。</zh-CN>
            //   <en>The five tabs fall into Portal &amp; Help / Work &amp; Approvals / Business / System Administration, so there are four non-empty groups.</en>
            // </lang>
            Assert.AreEqual(4, zh.Sections.Count);
            Assert.AreEqual(4, en.Sections.Count);
            Assert.AreNotEqual(zh.Sections[0].DisplayName, en.Sections[0].DisplayName);
            Assert.IsTrue(zh.Sections.All(section => section.ContainerId.StartsWith(PortalNavigationGroupView.ContainerIdPrefix, StringComparison.Ordinal)));
            Assert.IsTrue(zh.Sections.All(section => System.Text.RegularExpressions.Regex.IsMatch(section.ContainerId, "^[a-z0-9-]+$")));
            Assert.AreEqual(zh.Sections.Count, zh.Sections.Select(section => section.ContainerId).Distinct(StringComparer.Ordinal).Count());
            CollectionAssert.AreEqual(
                zh.Sections.Select(section => section.ContainerId).ToArray(),
                en.Sections.Select(section => section.ContainerId).ToArray());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>空输入、null 集合与各处 null 参数都不抛异常；返回空分组、空平铺区，
        ///   保证配置源或数据源不可用时导航渲染不会整页失败。</zh-CN>
        ///   <en>Empty input, a null collection, and null parameters never throw; empty groups and flat area are returned so
        ///   an unavailable configuration or data source cannot fail the whole page render.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Build_NullAndEmptyInputsDegradeToEmptyViewWithoutThrowing()
        {
            PortalNavigationGroupView.NavigationView nullView = PortalNavigationGroupView.Build(null, 0, null, null, null);
            PortalNavigationGroupView.NavigationView emptyView = PortalNavigationGroupView.Build(new List<ITabItem>(), 0, string.Empty, null, null);

            Assert.AreEqual(0, nullView.Sections.Count);
            Assert.AreEqual(0, nullView.UngroupedTabs.Count);
            Assert.IsFalse(nullView.Degraded);
            Assert.AreEqual(4, nullView.EmptySectionCount);
            Assert.AreEqual(0, emptyView.Sections.Count);
            Assert.AreEqual(0, emptyView.UngroupedTabs.Count);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把分组区与平铺区的页签合成一个序列，便于按 TabId 断言三态。</zh-CN>
        ///   <en>Flatten grouped and flat tabs into one sequence so three-state assertions can look items up by TabId.</en>
        /// </lang>
        /// </summary>
        private static IEnumerable<PortalNavigationGroupView.GroupTabView> AllItems(PortalNavigationGroupView.NavigationView view)
        {
            return view.Sections.SelectMany(section => section.Tabs).Concat(view.UngroupedTabs);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造可变的页签集合；视图不得改写它，故用 List 而非只读集合。</zh-CN>
        ///   <en>Build a mutable tab collection; the view must not modify it, so a List is used rather than a read-only collection.</en>
        /// </lang>
        /// </summary>
        private static IList<ITabItem> Tabs(params ITabItem[] tabs)
        {
            return new List<ITabItem>(tabs);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造最小页签替身；只设置稳定键与 ID，其余字段保持默认。</zh-CN>
        ///   <en>Build a minimal tab stand-in; only the stable key and id are set, all other fields keep defaults.</en>
        /// </lang>
        /// </summary>
        private static ITabItem Tab(string navigationKey, int tabId)
        {
            return new FakeTab { NavigationKey = navigationKey, TabId = tabId, TabName = "tab-" + tabId.ToString(CultureInfo.InvariantCulture) };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>`ITabItem` 的测试替身；分组视图只读取 NavigationKey / TabId / 显示名相关字段。</zh-CN>
        ///   <en>Test stand-in for `ITabItem`; the group view reads only NavigationKey / TabId / name-related fields.</en>
        /// </lang>
        /// </summary>
        private sealed class FakeTab : ITabItem
        {
            /// <summary><l><zh-CN>同级排序值。</zh-CN><en>Sibling sort value.</en></l></summary>
            public int? TabOrder { get; set; }

            /// <summary><l><zh-CN>页签名（中文列兼默认回退列）。</zh-CN><en>Tab name (Chinese column and default fallback).</en></l></summary>
            public string TabName { get; set; }

            /// <summary><l><zh-CN>页签名英文列。</zh-CN><en>English tab name column.</en></l></summary>
            public string TabNameEn { get; set; }

            /// <summary><l><zh-CN>其它语言 JSON 列。</zh-CN><en>Other-languages JSON column.</en></l></summary>
            public string TabNameLocalizedJson { get; set; }

            /// <summary><l><zh-CN>导航门控稳定键。</zh-CN><en>Navigation gate stable key.</en></l></summary>
            public string NavigationKey { get; set; }

            /// <summary><l><zh-CN>页签标识。</zh-CN><en>Tab identifier.</en></l></summary>
            public int TabId { get; set; }

            /// <summary><l><zh-CN>访问角色串；分组视图不读取该字段。</zh-CN><en>Access-role string; the group view never reads it.</en></l></summary>
            public string AccessRoles { get; set; }

            /// <summary><l><zh-CN>历史移动端页签名。</zh-CN><en>Legacy mobile tab name.</en></l></summary>
            public string MobileTabName { get; set; }

            /// <summary><l><zh-CN>移动端英文列。</zh-CN><en>Mobile English column.</en></l></summary>
            public string MobileTabNameEn { get; set; }

            /// <summary><l><zh-CN>移动端其它语言列。</zh-CN><en>Mobile other-languages column.</en></l></summary>
            public string MobileTabNameLocalizedJson { get; set; }

            /// <summary><l><zh-CN>历史移动端展示开关。</zh-CN><en>Legacy mobile display flag.</en></l></summary>
            public bool? ShowMobile { get; set; }
        }
    }
}
