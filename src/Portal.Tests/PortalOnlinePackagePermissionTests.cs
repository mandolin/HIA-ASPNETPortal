using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>在线能力包权限键测试：两键已登记、落在 Module 域、与能力层/装配态/包目录键互不混用，且为分离的双步权限。</zh-CN>
///   <en>Tests for online capability package permission keys: both keys are registered, live in the Module domain, are distinct from capability-layer / assembly-state / package-catalog keys, and form two separate steps.</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalOnlinePackagePermissionTests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>上传与确认键均已登记且键名精确；二者分离以支撑服务端二次鉴权。</zh-CN>
    ///   <en>Upload and confirm keys are both registered with exact names; their separation supports server-side re-authorization.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void OnlinePackageKeys_AreRegisteredAndDistinct()
    {
        Assert.IsTrue(PortalPermissionRegistry.IsDefined(PortalPermissionKeys.ModuleOnlinePackageUpload));
        Assert.IsTrue(PortalPermissionRegistry.IsDefined(PortalPermissionKeys.ModuleOnlinePackageConfirm));
        Assert.AreEqual("Module.OnlinePackage.Upload", PortalPermissionKeys.ModuleOnlinePackageUpload);
        Assert.AreEqual("Module.OnlinePackage.Confirm", PortalPermissionKeys.ModuleOnlinePackageConfirm);
        Assert.AreNotEqual(PortalPermissionKeys.ModuleOnlinePackageUpload,
                           PortalPermissionKeys.ModuleOnlinePackageConfirm);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>键落在 Module 点分域，而非能力层 `EnterpriseCapability.*`（Q2 纠偏：语义为能力层治理，与上传/安装动作错配）。</zh-CN>
    ///   <en>Keys use the Module dotted domain, not the capability-layer `EnterpriseCapability.*` family (Q2 correction: that family governs capability layers, not upload/install actions).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void OnlinePackageKeys_UseModuleDomainNotCapabilityLayer()
    {
        Assert.IsTrue(PortalPermissionKeys.ModuleOnlinePackageUpload.StartsWith("Module.", System.StringComparison.Ordinal));
        Assert.IsTrue(PortalPermissionKeys.ModuleOnlinePackageConfirm.StartsWith("Module.", System.StringComparison.Ordinal));
        Assert.IsFalse(PortalPermissionKeys.ModuleOnlinePackageUpload.StartsWith(
            "EnterpriseCapability", System.StringComparison.Ordinal));
        Assert.IsFalse(PortalPermissionKeys.ModuleOnlinePackageConfirm.StartsWith(
            "EnterpriseCapability", System.StringComparison.Ordinal));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>与既有模块包目录、模块定义、装配态、主题、内容键互不重名，证明「互不混用」而非复用同一键。</zh-CN>
    ///   <en>Keys differ from existing package-catalog, module-definition, assembly-state, theme, and content keys, proving "no mixing" rather than reuse.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void OnlinePackageKeys_DistinctFromOtherCategories()
    {
        string[] others =
        {
            PortalPermissionKeys.ModuleCatalogView,
            PortalPermissionKeys.ModuleCatalogEdit,
            PortalPermissionKeys.ModuleDefinitionEdit,
            PortalPermissionKeys.ModuleAssemblyStateView,
            PortalPermissionKeys.ModuleAssemblyStateEdit,
            PortalPermissionKeys.ThemeEdit,
            PortalPermissionKeys.ContentRawHtmlEdit,
            PortalPermissionKeys.ContentUploadManage,
            PortalPermissionKeys.EnterpriseCapabilityView,
            PortalPermissionKeys.EnterpriseCapabilityPlatformManage
        };

        foreach (string other in others)
        {
            Assert.AreNotEqual(other, PortalPermissionKeys.ModuleOnlinePackageUpload, "与 " + other + " 重名");
            Assert.AreNotEqual(other, PortalPermissionKeys.ModuleOnlinePackageConfirm, "与 " + other + " 重名");
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>A 级硬边界：两键都不是 Admins 角色名或管理员作用域键，也不存在「凭来源/能力声明自动授权」的键。</zh-CN>
    ///   <en>A-level hard boundary: neither key is the Admins role name or an administrator-scope key, and no key grants permission from an origin/capability claim.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void OnlinePackageKeys_DoNotRepresentAdminRoleOrClaims()
    {
        Assert.IsFalse(PortalPermissionKeys.ModuleOnlinePackageUpload.Contains("Admins"));
        Assert.IsFalse(PortalPermissionKeys.ModuleOnlinePackageConfirm.Contains("Admins"));
        Assert.IsFalse(PortalPermissionKeys.ModuleOnlinePackageUpload.Contains("Capability"));
        Assert.IsFalse(PortalPermissionKeys.ModuleOnlinePackageConfirm.Contains("Capability"));

        // <lang>
        //   <zh-CN>注册表不接受伪造的「来源声明即授权」键；未知键一律未定义。</zh-CN>
        //   <en>The registry rejects a forged "origin claim grants permission" key; unknown keys are never defined.</en>
        // </lang>
        Assert.IsFalse(PortalPermissionRegistry.IsDefined("Module.OnlinePackage.GrantByOrigin"));
        Assert.IsFalse(PortalPermissionRegistry.IsDefined("EnterpriseCapability.Manage"));
    }
}
