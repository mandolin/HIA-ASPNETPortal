using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖入口显示名按界面文化解析的契约测试。</zh-CN>
    ///   <en>Contract tests covering culture-driven resolution of entry display names.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>解析依据是界面文化，与 Web.config 的 globalization uiCulture 及 App_GlobalResources/lang.* 同源；测试只构造纯内存对象，不读写配置或资源文件。</zh-CN>
    ///   <en>Resolution is driven by the UI culture, sharing its source with the Web.config globalization uiCulture and App_GlobalResources/lang.*; the tests build pure in-memory objects and touch no configuration or resource files.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalNavigationEntryDisplayNameTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证显示名随界面文化切换：中文文化取中文名，英文文化取英文名，中文区域变体同样取中文名。</zh-CN>
        ///   <en>Verifies that the display name follows the UI culture: Chinese cultures use the Chinese name, English cultures use the English name, and Chinese regional variants also use the Chinese name.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetDisplayName_FollowsConfiguredUiCulture()
        {
            PortalNavigationEntry entry = PortalNavigationRegistry.FindByKey("Core.Admin.SiteSettings");
            Assert.IsNotNull(entry, "该入口应已在 registry 中登记。");

            Assert.AreEqual("站点设置", entry.GetDisplayName(new CultureInfo("zh-CN")));
            Assert.AreEqual("Site Settings", entry.GetDisplayName(new CultureInfo("en-US")));
            Assert.AreEqual("站点设置", entry.GetDisplayName(new CultureInfo("zh-TW")));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证缺失一侧时回落到另一侧，避免出现空标签。</zh-CN>
        ///   <en>Verifies that a missing side falls back to the other so no blank label appears.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetDisplayName_FallsBackToTheOtherLanguage()
        {
            var englishOnly = new PortalNavigationEntry(
                "Test.EnglishOnly",
                PortalNavigationEntryKind.AdminPage,
                string.Empty,
                "English Only",
                "Admin/SystemHealth.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                1,
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                "Test-only entry.");
            Assert.AreEqual("English Only", englishOnly.GetDisplayName(new CultureInfo("zh-CN")));

            var chineseOnly = new PortalNavigationEntry(
                "Test.ChineseOnly",
                PortalNavigationEntryKind.AdminPage,
                "仅中文名称",
                string.Empty,
                "Admin/SystemHealth.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                1,
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                "Test-only entry.");
            Assert.AreEqual("仅中文名称", chineseOnly.GetDisplayName(new CultureInfo("en-US")));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证两侧都缺失时返回稳定键，保证标签永远不为空。</zh-CN>
        ///   <en>Verifies that the stable key is returned when both sides are missing, so the label is never blank.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetDisplayName_FallsBackToEntryKeyWhenBothSidesMissing()
        {
            var blank = new PortalNavigationEntry(
                "Test.BlankDisplayName",
                PortalNavigationEntryKind.AdminPage,
                string.Empty,
                string.Empty,
                "Admin/SystemHealth.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Active,
                1,
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                "Test-only entry.");
            Assert.AreEqual("Test.BlankDisplayName", blank.GetDisplayName(new CultureInfo("en-US")));
            Assert.AreEqual("Test.BlankDisplayName", blank.GetDisplayName(new CultureInfo("zh-CN")));
        }
    }
}
