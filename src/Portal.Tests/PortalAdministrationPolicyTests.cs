using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>`PortalAdministrationPolicy.IsProtectedAdministrationTabName` 单元测试。
    ///   覆盖"页签名本地化"最危险的连带破坏点：后台入口保护按名识别，一旦把 `Admin` 改成中文（如"后台管理"），
    ///   必须仍能靠**导航稳定键**识别并保护，否则保护会静默失效。</zh-CN>
    ///   <en>Unit tests for `PortalAdministrationPolicy.IsProtectedAdministrationTabName`.
    ///   Covers the most dangerous side effect of tab-name localization: the admin-entry protection recognizes by name, so once
    ///   `Admin` is renamed to Chinese (e.g. "后台管理") it must still be recognized and protected by the **navigation stable key**,
    ///   otherwise the protection silently fails.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些断言固化的是 `W-anp-P162` 引入的稳定键解耦行为，是 `C-anp-P29` 门控键不漂移承诺的底层保证。
    ///   任一断言失败都意味着"改名即解除保护"的回归回来了。</zh-CN>
    ///   <en>These assertions pin the stable-key decoupling introduced by `W-anp-P162`, the underlying guarantee of `C-anp-P29`'s
    ///   "the gate key does not drift" promise. Any failing assertion means the "rename equals unprotected" regression has returned.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public class PortalAdministrationPolicyTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>历史单参重载：显示名为 `Admin`（区分大小写不敏感）时仍受保护 —— 兼容未回填稳定键的旧数据。</zh-CN>
        ///   <en>The legacy single-argument overload: the display name `Admin` (case-insensitive) is still protected — compatible with
        ///   legacy data whose stable key has not been backfilled.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsProtected_LegacyByName_Admin_ReturnsTrue()
        {
            Assert.IsTrue(PortalAdministrationPolicy.IsProtectedAdministrationTabName("Admin"));
            Assert.IsTrue(PortalAdministrationPolicy.IsProtectedAdministrationTabName("admin"));
            Assert.IsTrue(PortalAdministrationPolicy.IsProtectedAdministrationTabName("ADMIN"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解耦重载的回归核心：显示名已是中文"后台管理"，但只要稳定键仍是 `Admin`，就必须受保护。
        ///   这正是页签名本地化最危险的破坏点 —— 改名不能解除保护。</zh-CN>
        ///   <en>The regression core of the decoupled overload: the display name is already the Chinese "后台管理", yet with the stable
        ///   key still "Admin" it must remain protected. This is the most dangerous localization breakage point — renaming must not
        ///   lift protection.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsProtected_Decoupled_ChineseNameWithAdminKey_ReturnsTrue()
        {
            Assert.IsTrue(PortalAdministrationPolicy.IsProtectedAdministrationTabName("后台管理", "Admin"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>稳定键大小写不敏感：显示名中文、稳定键为小写 `admin` 仍受保护。</zh-CN>
        ///   <en>The stable key is case-insensitive: a Chinese display name with a lowercase `admin` key is still protected.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsProtected_Decoupled_AdminKeyCaseInsensitive_ReturnsTrue()
        {
            Assert.IsTrue(PortalAdministrationPolicy.IsProtectedAdministrationTabName("后台管理", "admin"));
            Assert.IsTrue(PortalAdministrationPolicy.IsProtectedAdministrationTabName("后台管理", "ADMIN"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>无稳定键且显示名非 `Admin` 时不受保护（含中文名）：这正是旧逻辑会误判、新逻辑通过稳定键避免的对比点。</zh-CN>
        ///   <en>Without a stable key and with a non-`Admin` display name (including a Chinese name) it is not protected: the contrast
        ///   point where the old logic misjudged and the new logic avoids via the stable key.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsProtected_NoKey_NonAdminName_ReturnsFalse()
        {
            Assert.IsFalse(PortalAdministrationPolicy.IsProtectedAdministrationTabName("后台管理", null));
            Assert.IsFalse(PortalAdministrationPolicy.IsProtectedAdministrationTabName("员工信息", null));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>有稳定键但稳定键非 `Admin` 时不冒充受保护 —— 防止任意键被当作保护键。</zh-CN>
        ///   <en>A stable key that is not `Admin` does not masquerade as protected — prevents any key from being treated as the
        ///   protection key.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsProtected_Decoupled_NonAdminKey_ReturnsFalse()
        {
            Assert.IsFalse(PortalAdministrationPolicy.IsProtectedAdministrationTabName("后台管理", "NotAdmin"));
            Assert.IsFalse(PortalAdministrationPolicy.IsProtectedAdministrationTabName("Admin", "Home"));
        }
    }
}
