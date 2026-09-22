using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖"后台页动作区改由渲染器提供"后的分组解析契约：确保相关入口非空、不含自身，且只含受 registry 管辖的可导航入口。</zh-CN>
    ///   <en>Contract tests for group resolution after the Admin action area is served by the renderer: related entries must be non-empty, must exclude the current entry, and must stay inside the registry-governed navigable set.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些页面原先用成对的硬编码链接指向同组后台页；改为渲染器后，一旦分组解析失效，动作区会整块消失而不报错，因此用测试锁住"非空"这一前提。</zh-CN>
    ///   <en>These pages previously hardcoded paired links to same-group Admin pages; after switching to the renderer, a broken group resolution would silently remove the whole action area, so the non-empty precondition is locked by tests.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalNavigationRendererGroupTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证相关入口非空、排除自身、且只含 Active 且非 DiagnosticOnly 的入口。</zh-CN>
        ///   <en>Verifies related entries are non-empty, exclude the current entry, and contain only Active, non-DiagnosticOnly entries.</en>
        /// </lang>
        /// </summary>
        /// <param name="entryKey">
        /// <l>
        ///   <zh-CN>已改用渲染器提供动作区的入口键。</zh-CN>
        ///   <en>Entry key whose action area is now served by the renderer.</en>
        /// </l>
        /// </param>
        [TestMethod]
        [DataRow("Admin.ThemeSettings")]
        [DataRow("Admin.Ops.DiagnosticsLogs")]
        [DataRow("Admin.Ops.OperationAudits")]
        [DataRow("Admin.Ops.DiagnosticLogDetail")]
        [DataRow("Admin.CollaborationItems")]
        [DataRow("Admin.WorkItems")]
        [DataRow("Admin.Capability.BusinessApplications")]
        [DataRow("Admin.Capability.EmployeeDirectory")]
        [DataRow("Admin.Capability.EmployeeEdit")]
        [DataRow("Admin.Capability.OrganizationUnitEdit")]
        [DataRow("Admin.Capability.UserEmployeeBindingEdit")]
        [DataRow("Admin.Capability.EmployeeProfileCorrectionRequests")]
        [DataRow("Admin.Account.ManageUsers")]
        public void GetRelatedEntries_ReturnsGovernedEntriesAndExcludesSelf(string entryKey)
        {
            IList<PortalNavigationEntry> related = PortalNavigationVisibilityPolicy.GetRelatedEntries(entryKey, 5);

            Assert.IsTrue(
                related.Count > 0,
                entryKey + " 必须有受 registry 管辖的相关入口；否则渲染器返回空字符串，动作区会整块消失。");

            foreach (PortalNavigationEntry entry in related)
            {
                Assert.AreNotEqual(entryKey, entry.EntryKey, "相关入口不得包含当前入口自身，否则页面会出现自链接。");
                Assert.AreEqual(
                    PortalNavigationLifecycleState.Active,
                    entry.LifecycleState,
                    entry.EntryKey + " 必须处于 Active 生命周期。");
                Assert.AreNotEqual(
                    PortalNavigationVisibilityMode.DiagnosticOnly,
                    entry.VisibilityMode,
                    entry.EntryKey + " 不得是 DiagnosticOnly 入口。");
            }
        }
    }
}
