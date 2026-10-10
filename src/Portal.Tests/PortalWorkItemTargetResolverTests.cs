using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖待办目标反查的测试：业务类型到能力键的映射、各环节落空时的降级原因，以及多实例下的页签取舍规则。</zh-CN>
    ///   <en>Tests covering the work-item target reverse lookup: business-kind to capability-key mapping, the degradation reason for each failing link, and the tab-selection rule under multiple instances.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只构造内存端口，不读配置、数据库、部署清单或 HTTP 上下文；能力词表与模块包标识取自真实注册表，因此断言的是"反查链与词表一致"，而不是某次部署的实际挂载情况。角色判定用简化替身（含 <c>All Users</c> 即放行），真实策略由 PortalSecurity 承担，不在本测试范围。</zh-CN>
    ///   <en>The tests build in-memory ports only and read no configuration, database, deployment manifest, or HTTP context; capability keys and package identifiers come from the real registry, so the assertions cover consistency between the lookup chain and the vocabulary rather than any deployment's actual mounting. Role checking uses a simplified stand-in (anything containing <c>All Users</c> passes) because the real policy lives in PortalSecurity and is out of scope here.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalWorkItemTargetResolverTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证五个已知业务类型各自映射到权威词表中的能力键，未知、空白与 <c>null</c> 一律返回空串而不猜测。</zh-CN>
        ///   <en>Verifies that the five known business kinds map to their authority-registry capability keys, while unknown, blank, and <c>null</c> values return an empty string instead of a guess.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetCapabilityId_MapsKnownKindsAndRejectsUnknown()
        {
            Assert.AreEqual(
                PortalCapabilityRegistry.Collaboration,
                PortalWorkItemTargetResolver.GetCapabilityId(PortalWorkItemBusinessKinds.CollaborationItem));
            Assert.AreEqual(
                PortalCapabilityRegistry.ApplicationRequest,
                PortalWorkItemTargetResolver.GetCapabilityId(PortalWorkItemBusinessKinds.BusinessApplication));
            Assert.AreEqual(
                PortalCapabilityRegistry.EmployeeProfileCorrectionRequest,
                PortalWorkItemTargetResolver.GetCapabilityId(PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest));
            Assert.AreEqual(
                PortalCapabilityRegistry.LeaveRequest,
                PortalWorkItemTargetResolver.GetCapabilityId(PortalWorkItemBusinessKinds.LeaveRequest));
            Assert.AreEqual(
                PortalCapabilityRegistry.ExpenseReimbursement,
                PortalWorkItemTargetResolver.GetCapabilityId(PortalWorkItemBusinessKinds.ExpenseReimbursement));

            // <lang>
            //   <zh-CN>未知与空输入必须返回空串：反查链的第一环落空即降级，不能回退到某个"默认业务"。</zh-CN>
            //   <en>Unknown and blank input must yield an empty string: when the first link falls through the chain degrades and must not fall back to some default business.</en>
            // </lang>
            Assert.AreEqual(string.Empty, PortalWorkItemTargetResolver.GetCapabilityId("SomethingElse"));
            Assert.AreEqual(string.Empty, PortalWorkItemTargetResolver.GetCapabilityId(string.Empty));
            Assert.AreEqual(string.Empty, PortalWorkItemTargetResolver.GetCapabilityId(null));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证能力词表中的协同事项能力确实锚定在协同工作台包上，使"业务类型 → 包"这一跳有据可依。</zh-CN>
        ///   <en>Verifies that the collaboration capability really anchors on the workbench package, giving the business-kind-to-package hop a documented basis.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CollaborationCapability_AnchorsOnWorkbenchPackage()
        {
            PortalCapabilityDefinition definition;
            Assert.IsTrue(
                PortalCapabilityRegistry.TryGet(PortalCapabilityRegistry.Collaboration, out definition),
                "The collaboration capability must exist in the authority registry.");

            Assert.AreEqual("HIA.EnterpriseCapabilityWorkbench", definition.PrimaryModuleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证反查链各环节的降级原因可区分：未部署包、未注册定义、定义未挂载、页签不可达各自给出不同原因码。</zh-CN>
        ///   <en>Verifies that each failing link is distinguishable: an undeployed package, an unregistered definition, an unmounted definition, and an unreachable tab each produce a different reason code.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_ReportsDistinctReasonForEachFailingLink()
        {
            const string kind = PortalWorkItemBusinessKinds.CollaborationItem;
            string packageId = GetPrimaryModuleId(PortalCapabilityRegistry.Collaboration);
            const string DesktopEntry = "DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx";

            // <lang>
            //   <zh-CN>① 当前请求没有部署该包：桌面入口为空，无法继续反查。</zh-CN>
            //   <en>(1) The current request has no such deployed package, so the desktop entry is blank and the lookup cannot continue.</en>
            // </lang>
            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonNoDesktopEntry,
                PortalWorkItemTargetResolver.Resolve(kind, new FakeTargetSource()).ReasonCode);

            // <lang>
            //   <zh-CN>② 包已部署但没有模块定义引用该入口。</zh-CN>
            //   <en>(2) The package is deployed but no module definition references that entry.</en>
            // </lang>
            FakeTargetSource noDefinition = new FakeTargetSource();
            noDefinition.DesktopEntries[packageId] = DesktopEntry;
            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonNoModuleDefinition,
                PortalWorkItemTargetResolver.Resolve(kind, noDefinition).ReasonCode);

            // <lang>
            //   <zh-CN>③ 定义存在但没有任何归属页签的实例。</zh-CN>
            //   <en>(3) The definition exists but has no instance owning a tab.</en>
            // </lang>
            FakeTargetSource noInstance = CreateSourceWithDefinition(packageId, DesktopEntry, 1013);
            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonNoModuleInstance,
                PortalWorkItemTargetResolver.Resolve(kind, noInstance).ReasonCode);

            // <lang>
            //   <zh-CN>④ 实例归属页签，但当前用户不满足该页签的访问角色。</zh-CN>
            //   <en>(4) The instance owns a tab, but the current user does not satisfy that tab's access roles.</en>
            // </lang>
            FakeTargetSource noAccess = CreateSourceWithDefinition(packageId, DesktopEntry, 1013);
            noAccess.ModuleIdsByDefinition[1013] = new List<int> { 1013 };
            noAccess.Modules[1013] = new FakeModule { ModuleId = 1013, TabId = 1010 };
            noAccess.Tabs[1010] = new FakeTab { TabId = 1010, TabName = "Workbench", AccessRoles = "Admins;", TabOrder = 1 };
            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonNoAccessibleTab,
                PortalWorkItemTargetResolver.Resolve(kind, noAccess).ReasonCode);

            // <lang>
            //   <zh-CN>⑤ 实例归属页签，但页签记录本身缺失：与"无权访问"同码，因对用户的结论相同。</zh-CN>
            //   <en>(5) The instance owns a tab id whose tab record is missing: the same code as "no access" because the conclusion for the user is identical.</en>
            // </lang>
            FakeTargetSource missingTab = CreateSourceWithDefinition(packageId, DesktopEntry, 1013);
            missingTab.ModuleIdsByDefinition[1013] = new List<int> { 1013 };
            missingTab.Modules[1013] = new FakeModule { ModuleId = 1013, TabId = 1010 };
            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonNoAccessibleTab,
                PortalWorkItemTargetResolver.Resolve(kind, missingTab).ReasonCode);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证端口缺失与未知业务类型的降级：两者都属于"无法判定"，都不允许抛出。</zh-CN>
        ///   <en>Verifies degradation for a missing port and an unknown business kind: both are indecidable and neither may throw.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_DegradesWithoutThrowingOnMissingPortOrUnknownKind()
        {
            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonSourceUnavailable,
                PortalWorkItemTargetResolver.Resolve(PortalWorkItemBusinessKinds.CollaborationItem, null).ReasonCode);

            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonUnknownBusinessKind,
                PortalWorkItemTargetResolver.Resolve(null, new FakeTargetSource()).ReasonCode);

            Assert.AreEqual(
                PortalWorkItemTargetResolver.ReasonUnknownBusinessKind,
                PortalWorkItemTargetResolver.Resolve("NotAKind", new FakeTargetSource()).ReasonCode);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证多实例取舍：**先过滤权限、再取 <c>TabOrder</c> 最小**。顺序最靠前但用户无权的页签必须被跳过，否则会把用户送到 403。</zh-CN>
        ///   <en>Verifies multi-instance selection: filter by permission first, then take the smallest <c>TabOrder</c>. A low-order tab the user cannot access must be skipped, otherwise the user is sent to a 403.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_SelectsSmallestTabOrderAmongAccessibleTabs()
        {
            const string kind = PortalWorkItemBusinessKinds.CollaborationItem;
            string packageId = GetPrimaryModuleId(PortalCapabilityRegistry.Collaboration);
            const string DesktopEntry = "DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx";

            FakeTargetSource source = CreateSourceWithDefinition(packageId, DesktopEntry, 1013);

            // <lang>
            //   <zh-CN>三个实例分别指向：顺序最前但无权的页签、顺序最优且有权、顺序居中且有权。</zh-CN>
            //   <en>Three instances point at: the lowest-order tab without access, the best accessible order, and a middle accessible order.</en>
            // </lang>
            source.ModuleIdsByDefinition[1013] = new List<int> { 1, 2, 3 };
            source.Modules[1] = new FakeModule { ModuleId = 1, TabId = 900 };
            source.Modules[2] = new FakeModule { ModuleId = 2, TabId = 901 };
            source.Modules[3] = new FakeModule { ModuleId = 3, TabId = 902 };
            source.Tabs[900] = new FakeTab { TabId = 900, TabName = "Hidden", AccessRoles = "Admins;", TabOrder = 1 };
            source.Tabs[901] = new FakeTab { TabId = 901, TabName = "Workbench", AccessRoles = "All Users;", TabOrder = 9 };
            source.Tabs[902] = new FakeTab { TabId = 902, TabName = "Middle", AccessRoles = "All Users;", TabOrder = 5 };

            PortalWorkItemTargetResolution resolution = PortalWorkItemTargetResolver.Resolve(kind, source);

            Assert.IsTrue(resolution.IsResolved);
            Assert.AreEqual(string.Empty, resolution.ReasonCode);
            Assert.AreEqual(902, resolution.Tab.TabId);

            // <lang>
            //   <zh-CN>同一输入重复解析必须得到同一页签：并列时保留枚举先后，使结果可复现而不是依赖排序稳定性假设。</zh-CN>
            //   <en>Repeating the same input must yield the same tab: enumeration order is preserved on ties so the result is reproducible rather than reliant on an assumption about sort stability.</en>
            // </lang>
            Assert.AreEqual(902, PortalWorkItemTargetResolver.Resolve(kind, source).Tab.TabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 <c>TabOrder</c> 为空视为最大：有值的可访问页签优先，避免"未设顺序"的实例抢占有序配置的页签。</zh-CN>
        ///   <en>Verifies that a null <c>TabOrder</c> counts as largest, so an accessible tab with a value wins and an unset order never displaces an ordered configuration.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_TreatsNullTabOrderAsLargest()
        {
            const string kind = PortalWorkItemBusinessKinds.CollaborationItem;
            string packageId = GetPrimaryModuleId(PortalCapabilityRegistry.Collaboration);
            const string DesktopEntry = "DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx";

            FakeTargetSource source = CreateSourceWithDefinition(packageId, DesktopEntry, 1013);
            source.ModuleIdsByDefinition[1013] = new List<int> { 1, 2 };
            source.Modules[1] = new FakeModule { ModuleId = 1, TabId = 900 };
            source.Modules[2] = new FakeModule { ModuleId = 2, TabId = 901 };
            source.Tabs[900] = new FakeTab { TabId = 900, TabName = "Unordered", AccessRoles = "All Users;", TabOrder = null };
            source.Tabs[901] = new FakeTab { TabId = 901, TabName = "Ordered", AccessRoles = "All Users;", TabOrder = 3 };

            Assert.AreEqual(901, PortalWorkItemTargetResolver.Resolve(kind, source).Tab.TabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证定义匹配与模块目录页同口径：桌面入口比较不区分大小写，避免两处对"是否已注册"得出不同结论。</zh-CN>
        ///   <en>Verifies that definition matching follows the module-catalog rule: desktop entries compare case-insensitively so the two places cannot disagree about whether a package is registered.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_MatchesDesktopEntryCaseInsensitively()
        {
            const string kind = PortalWorkItemBusinessKinds.CollaborationItem;
            string packageId = GetPrimaryModuleId(PortalCapabilityRegistry.Collaboration);
            const string StoredEntry = "desktopmodules/enterprisecapabilityworkbench/enterprisecapabilityworkbench.ASCX";
            const string ManifestEntry = "DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx";

            FakeTargetSource source = CreateSourceWithDefinition(packageId, ManifestEntry, 1013);
            source.Definitions[0].DesktopSourceFile = StoredEntry;
            source.ModuleIdsByDefinition[1013] = new List<int> { 1013 };
            source.Modules[1013] = new FakeModule { ModuleId = 1013, TabId = 1010 };
            source.Tabs[1010] = new FakeTab { TabId = 1010, TabName = "Workbench", AccessRoles = "All Users;", TabOrder = 1 };

            PortalWorkItemTargetResolution resolution = PortalWorkItemTargetResolver.Resolve(kind, source);

            Assert.IsTrue(resolution.IsResolved);
            Assert.AreEqual(1010, resolution.Tab.TabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证页签地址形状唯一且数值固定区域性格式化：本地化数字分隔符绝不能进入地址。</zh-CN>
        ///   <en>Verifies that the tab URL shape is single-sourced and its numbers use invariant formatting, so a localized digit separator can never enter a URL.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void BuildDesktopTabUrl_UsesSingleInvariantShape()
        {
            Assert.AreEqual(
                "/DesktopDefault.aspx?tabindex=2&tabid=1010",
                PortalDesktopTabUrl.Build(string.Empty, 2, 1010));
            Assert.AreEqual(
                "/portal/DesktopDefault.aspx?tabindex=0&tabid=1",
                PortalDesktopTabUrl.Build("/portal", 0, 1));

            // <lang>
            //   <zh-CN>空应用路径不得产生 <c>null</c> 前缀或双斜杠：调用方可能没有虚拟目录。</zh-CN>
            //   <en>A blank application path must not produce a <c>null</c> prefix or a double slash, because the caller may have no virtual directory.</en>
            // </lang>
            Assert.AreEqual(
                "/DesktopDefault.aspx?tabindex=1&tabid=2",
                PortalDesktopTabUrl.Build(null, 1, 2));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取能力词表登记的主责模块包标识，供用例构造端口。</zh-CN>
        ///   <en>Reads the primary module package identifier registered in the capability registry so cases can build their ports.</en>
        /// </lang>
        /// </summary>
        /// <param name="capabilityId"><l><zh-CN>能力键。</zh-CN><en>The capability key.</en></l></param>
        /// <returns><l><zh-CN>主责模块包标识。</zh-CN><en>The primary module package identifier.</en></l></returns>
        private static string GetPrimaryModuleId(string capabilityId)
        {
            PortalCapabilityDefinition definition;
            Assert.IsTrue(
                PortalCapabilityRegistry.TryGet(capabilityId, out definition),
                "Expected the capability to exist: " + capabilityId);

            return definition.PrimaryModuleId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造一个"包已部署、定义已注册"的端口，供多个用例续接后续环节。</zh-CN>
        ///   <en>Builds a port whose package is deployed and definition registered, for cases that continue with later links.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId"><l><zh-CN>模块包标识。</zh-CN><en>The module package identifier.</en></l></param>
        /// <param name="desktopEntry"><l><zh-CN>已验证桌面入口。</zh-CN><en>The validated desktop entry.</en></l></param>
        /// <param name="definitionId"><l><zh-CN>模块定义标识。</zh-CN><en>The module-definition identifier.</en></l></param>
        /// <returns><l><zh-CN>已登记包与定义的端口。</zh-CN><en>A port with the package and definition registered.</en></l></returns>
        private static FakeTargetSource CreateSourceWithDefinition(string packageId, string desktopEntry, int definitionId)
        {
            FakeTargetSource source = new FakeTargetSource();
            source.DesktopEntries[packageId] = desktopEntry;
            source.Definitions.Add(new FakeModuleDefinition
            {
                ModuleDefId = definitionId,
                DesktopSourceFile = desktopEntry,
                FriendlyName = "Test module"
            });

            return source;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>内存反查端口替身：只实现解析器用到的六个操作，不做任何 IO。</zh-CN>
        ///   <en>In-memory reverse-lookup port stand-in implementing only the six operations the resolver uses, with no IO.</en>
        /// </lang>
        /// </summary>
        private sealed class FakeTargetSource : IPortalModuleTargetSource
        {
            /// <summary><l><zh-CN>包标识到桌面入口的映射。</zh-CN><en>Package identifier to desktop entry.</en></l></summary>
            public readonly IDictionary<string, string> DesktopEntries =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary><l><zh-CN>已注册模块定义。</zh-CN><en>Registered module definitions.</en></l></summary>
            public readonly IList<FakeModuleDefinition> Definitions = new List<FakeModuleDefinition>();

            /// <summary><l><zh-CN>定义标识到模块实例标识的映射。</zh-CN><en>Definition identifier to module-instance identifiers.</en></l></summary>
            public readonly IDictionary<int, IList<int>> ModuleIdsByDefinition = new Dictionary<int, IList<int>>();

            /// <summary><l><zh-CN>模块实例。</zh-CN><en>Module instances.</en></l></summary>
            public readonly IDictionary<int, FakeModule> Modules = new Dictionary<int, FakeModule>();

            /// <summary><l><zh-CN>页签。</zh-CN><en>Tabs.</en></l></summary>
            public readonly IDictionary<int, FakeTab> Tabs = new Dictionary<int, FakeTab>();

            /// <summary><l><zh-CN>按包标识返回桌面入口；不存在时为空串。</zh-CN><en>Returns the desktop entry by package identifier, or an empty string when absent.</en></l></summary>
            /// <param name="packageId"><l><zh-CN>模块包标识。</zh-CN><en>The module package identifier.</en></l></param>
            /// <returns><l><zh-CN>桌面入口或空串。</zh-CN><en>The desktop entry or an empty string.</en></l></returns>
            public string GetDesktopEntry(string packageId)
            {
                string entry;
                return DesktopEntries.TryGetValue(packageId ?? string.Empty, out entry) ? entry : string.Empty;
            }

            /// <summary><l><zh-CN>返回已注册模块定义。</zh-CN><en>Returns the registered module definitions.</en></l></summary>
            /// <returns><l><zh-CN>模块定义集合。</zh-CN><en>The module-definition collection.</en></l></returns>
            public IEnumerable<IModuleDefinitionItem> GetModuleDefinitions()
            {
                return Definitions;
            }

            /// <summary><l><zh-CN>按定义标识返回模块实例标识；无登记时为空集合。</zh-CN><en>Returns module-instance identifiers by definition identifier, or an empty collection when unregistered.</en></l></summary>
            /// <param name="definitionId"><l><zh-CN>模块定义标识。</zh-CN><en>The module-definition identifier.</en></l></param>
            /// <returns><l><zh-CN>模块实例标识集合。</zh-CN><en>The module-instance identifier collection.</en></l></returns>
            public IEnumerable<int> GetModuleIdsByDefinitionId(int definitionId)
            {
                IList<int> ids;
                return ModuleIdsByDefinition.TryGetValue(definitionId, out ids) ? ids : new List<int>();
            }

            /// <summary><l><zh-CN>按标识返回模块实例；不存在时为 <c>null</c>。</zh-CN><en>Returns a module instance by identifier, or <c>null</c> when absent.</en></l></summary>
            /// <param name="moduleId"><l><zh-CN>模块实例标识。</zh-CN><en>The module-instance identifier.</en></l></param>
            /// <returns><l><zh-CN>模块实例或 <c>null</c>。</zh-CN><en>The module instance or <c>null</c>.</en></l></returns>
            public IModuleItem FindModuleById(int moduleId)
            {
                FakeModule module;
                return Modules.TryGetValue(moduleId, out module) ? module : null;
            }

            /// <summary><l><zh-CN>按标识返回页签；不存在时为 <c>null</c>。</zh-CN><en>Returns a tab by identifier, or <c>null</c> when absent.</en></l></summary>
            /// <param name="tabId"><l><zh-CN>页签标识。</zh-CN><en>The tab identifier.</en></l></param>
            /// <returns><l><zh-CN>页签或 <c>null</c>。</zh-CN><en>The tab or <c>null</c>.</en></l></returns>
            public ITabItem FindTabById(int tabId)
            {
                FakeTab tab;
                return Tabs.TryGetValue(tabId, out tab) ? tab : null;
            }

            /// <summary><l><zh-CN>简化角色判定：含 <c>All Users</c> 视为放行，用于验证"先过滤权限"的取舍顺序。</zh-CN><en>Simplified role check: anything containing <c>All Users</c> passes, which is enough to verify the permission-first selection order.</en></l></summary>
            /// <param name="accessRoles"><l><zh-CN>页签访问角色串。</zh-CN><en>The tab access-role string.</en></l></param>
            /// <returns><l><zh-CN>满足时为 <c>true</c>。</zh-CN><en><c>true</c> when satisfied.</en></l></returns>
            public bool IsInRoles(string accessRoles)
            {
                return accessRoles != null &&
                    accessRoles.IndexOf(PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary><lang><zh-CN>模块定义替身。</zh-CN><en>Module-definition stand-in.</en></lang></summary>
        private sealed class FakeModuleDefinition : IModuleDefinitionItem
        {
            /// <summary><l><zh-CN>模块类型名称。</zh-CN><en>Module type name.</en></l></summary>
            public string FriendlyName { get; set; }

            /// <summary><l><zh-CN>历史移动端入口。</zh-CN><en>Legacy mobile entry.</en></l></summary>
            public string MobileSourceFile { get; set; }

            /// <summary><l><zh-CN>桌面端入口虚拟路径。</zh-CN><en>Desktop entry virtual path.</en></l></summary>
            public string DesktopSourceFile { get; set; }

            /// <summary><l><zh-CN>模块定义标识。</zh-CN><en>Module-definition identifier.</en></l></summary>
            public int ModuleDefId { get; set; }
        }

        /// <summary><lang><zh-CN>模块实例替身。</zh-CN><en>Module-instance stand-in.</en></lang></summary>
        private sealed class FakeModule : IModuleItem
        {
            /// <summary><l><zh-CN>模块在 pane 内的排序值。</zh-CN><en>Module order inside its pane.</en></l></summary>
            public int? ModuleOrder { get; set; }

            /// <summary><l><zh-CN>模块实例标题。</zh-CN><en>Module-instance title.</en></l></summary>
            public string ModuleTitle { get; set; }

            /// <summary><l><zh-CN>所在 pane 名称。</zh-CN><en>Owning pane name.</en></l></summary>
            public string PaneName { get; set; }

            /// <summary><l><zh-CN>模块实例标识。</zh-CN><en>Module-instance identifier.</en></l></summary>
            public int ModuleId { get; set; }

            /// <summary><l><zh-CN>模块定义标识。</zh-CN><en>Module-definition identifier.</en></l></summary>
            public int? ModuleDefId { get; set; }

            /// <summary><l><zh-CN>允许编辑该实例的角色串。</zh-CN><en>Role string allowed to edit this instance.</en></l></summary>
            public string EditRoles { get; set; }

            /// <summary><l><zh-CN>缓存超时设置。</zh-CN><en>Cache timeout setting.</en></l></summary>
            public int? CacheTimeout { get; set; }

            /// <summary><l><zh-CN>归属页签标识。</zh-CN><en>Owning tab identifier.</en></l></summary>
            public int? TabId { get; set; }

            /// <summary><l><zh-CN>历史移动端展示开关；反查不读取该字段。</zh-CN><en>Legacy mobile-display flag; the reverse lookup never reads it.</en></l></summary>
            public bool? ShowMobile { get; set; }
        }

        /// <summary><lang><zh-CN>页签替身。</zh-CN><en>Tab stand-in.</en></lang></summary>
        private sealed class FakeTab : ITabItem
        {
            /// <summary><l><zh-CN>同级排序值。</zh-CN><en>Sort value among siblings.</en></l></summary>
            public int? TabOrder { get; set; }

            /// <summary><l><zh-CN>页签名称。</zh-CN><en>Tab name.</en></l></summary>
            public string TabName { get; set; }

            /// <summary><l><zh-CN>页签标识。</zh-CN><en>Tab identifier.</en></l></summary>
            public int TabId { get; set; }

            /// <summary><l><zh-CN>访问角色串。</zh-CN><en>Access-role string.</en></l></summary>
            public string AccessRoles { get; set; }

            /// <summary><l><zh-CN>历史移动端页签名。</zh-CN><en>Legacy mobile tab name.</en></l></summary>
            public string MobileTabName { get; set; }

            /// <summary><l><zh-CN>历史移动端展示开关；反查不读取该字段。</zh-CN><en>Legacy mobile-display flag; the reverse lookup never reads it.</en></l></summary>
            public bool? ShowMobile { get; set; }
        }
    }
}
