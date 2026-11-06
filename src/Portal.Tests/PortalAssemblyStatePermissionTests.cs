using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>装配管理权限键测试：验证新键已登记、与模块包管理/主题/内容编辑互不混用（键名不同、不重复）。</zh-CN>
///   <en>Tests for the assembly-management permission keys: verify the new keys are registered, distinct from module-package management, theme, and content editing (different names, no duplicates).</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalAssemblyStatePermissionTests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>新键在权限注册表中被登记，且可在运行期被识别为已定义权限。</zh-CN>
    ///   <en>The new keys are registered in the permission registry and are recognized as defined permissions at runtime.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void AssemblyStateKeys_AreRegisteredAndDefined()
    {
        Assert.IsTrue(PortalPermissionRegistry.IsDefined(PortalPermissionKeys.ModuleAssemblyStateView));
        Assert.IsTrue(PortalPermissionRegistry.IsDefined(PortalPermissionKeys.ModuleAssemblyStateEdit));
        Assert.AreEqual("Module.AssemblyState.View", PortalPermissionKeys.ModuleAssemblyStateView);
        Assert.AreEqual("Module.AssemblyState.Edit", PortalPermissionKeys.ModuleAssemblyStateEdit);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>装配管理键与既有模块包管理、主题 CSS、内容编辑键互不相同，证明「互不混用」而非复用同一键。</zh-CN>
    ///   <en>Assembly-management keys differ from existing module-package, theme CSS, and content-editing keys, proving "no mixing" rather than reusing one key.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void AssemblyStateKeys_DistinctFromOtherCategories()
    {
        string[] others =
        {
            PortalPermissionKeys.ModuleCatalogView,
            PortalPermissionKeys.ModuleCatalogEdit,
            PortalPermissionKeys.ModuleDefinitionEdit,
            PortalPermissionKeys.ThemeEdit,
            PortalPermissionKeys.ContentRawHtmlEdit,
            PortalPermissionKeys.ContentUploadManage
        };

        foreach (string other in others)
        {
            Assert.AreNotEqual(other, PortalPermissionKeys.ModuleAssemblyStateView, "与 " + other + " 重名");
            Assert.AreNotEqual(other, PortalPermissionKeys.ModuleAssemblyStateEdit, "与 " + other + " 重名");
        }
    }
}
