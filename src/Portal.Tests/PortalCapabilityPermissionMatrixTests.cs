using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖能力权限矩阵渲染器与其"分类 → 能力层"受控映射的契约测试。</zh-CN>
    ///   <en>Contract tests covering the capability permission matrix renderer and its controlled category-to-layer mapping.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只构造纯内存输入，不读取 HttpContext、数据库或配置；渲染器为纯字符串生成，因此可与真实页面解耦地覆盖层级结构、三态呈现与空态降级。</zh-CN>
    ///   <en>The tests build pure in-memory input and read no HttpContext, database, or configuration; the renderer is pure string generation, so layer structure, three-state rendering, and empty-state degradation are covered independently of the real page.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalCapabilityPermissionMatrixTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>契约测试：注册表中**每一个**权限分类都必须显式登记能力层归属，防止新增键时忘记同步映射（否则会被默认层静默掩盖）。</zh-CN>
        ///   <en>Contract test: **every** permission category in the registry must declare its capability layer explicitly, preventing a new key from silently falling back to the default layer.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void EveryRegisteredPermissionCategoryDeclaresItsCapabilityLayer()
        {
            // <lang>
            //   <zh-CN>逐条检查注册表定义；失败信息带上分类名，便于直接定位需要补登记的分类。</zh-CN>
            //   <en>Check every registry definition and include the category name in the failure message so the missing registration is obvious.</en>
            // </lang>
            foreach (PortalPermissionDefinition definition in PortalPermissionRegistry.Definitions)
            {
                Assert.IsTrue(
                    PortalCapabilityPermissionMatrixRenderer.IsCategoryLayerRegistered(definition.Category),
                    "Permission category declares no capability layer: " + definition.Category + " (key " + definition.Key + ")");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证渲染结果按"能力层 → 权限分类 → 权限键"三级组织，且层顺序固定为基础核心 → 基础业务 → 平台能力。</zh-CN>
        ///   <en>Verifies the render output is organized as layer, then category, then key, with the layer order fixed as foundation, basic business, then platform.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Render_GroupsByLayerThenCategoryThenKey()
        {
            // <lang>
            //   <zh-CN>构造跨三层的最小定义集合：每层一个分类、每分类一个键。</zh-CN>
            //   <en>Build a minimal definition set spanning three layers: one category and one key per layer.</en>
            // </lang>
            List<PortalPermissionDefinition> definitions = new List<PortalPermissionDefinition>
            {
                new PortalPermissionDefinition("Settings.View", "Settings", "foundation sample"),
                new PortalPermissionDefinition("Business.Collaboration.Create", "Business.Collaboration", "basic business sample"),
                new PortalPermissionDefinition("EnterpriseCapability.Platform.Manage", "EnterpriseCapability.Platform", "platform sample")
            };

            string html = PortalCapabilityPermissionMatrixRenderer.Render(definitions, new List<RolePermissionEntry>(), null);

            // <lang>
            //   <zh-CN>层行顺序断言：基础核心必须出现在基础业务之前、平台能力之前；基础业务出现在平台能力之前。</zh-CN>
            //   <en>Layer-order assertions: foundation must appear before basic business and before platform, and basic business before platform.</en>
            // </lang>
            int foundationIndex = html.IndexOf("LayerFoundation", StringComparison.Ordinal);
            int basicBusinessIndex = html.IndexOf("LayerBasicBusiness", StringComparison.Ordinal);
            int platformIndex = html.IndexOf("LayerPlatform", StringComparison.Ordinal);

            Assert.IsTrue(foundationIndex >= 0, "Foundation layer row is missing.");
            Assert.IsTrue(basicBusinessIndex > foundationIndex, "Basic Business layer must follow Foundation.");
            Assert.IsTrue(platformIndex > basicBusinessIndex, "Platform layer must follow Basic Business.");

            // <lang>
            //   <zh-CN>分类行与键行必须出现，且键行位于其分类行之后（分类作为分组标题先行）。</zh-CN>
            //   <en>The category row and key row must appear, with the key row after its category row since the category acts as the group heading.</en>
            // </lang>
            Assert.IsTrue(html.IndexOf("Business.Collaboration", StringComparison.Ordinal) > basicBusinessIndex, "Category row is missing or misplaced.");
            Assert.IsTrue(
                html.IndexOf("Business.Collaboration.Create", StringComparison.Ordinal) >
                html.IndexOf("Business.Collaboration", StringComparison.Ordinal),
                "Key row must follow its category row.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证三态可区分：已授权、未授权、已禁用（映射存在但未生效）各自输出不同标记与语义类。</zh-CN>
        ///   <en>Verifies the three states stay distinguishable: granted, not granted, and disabled (mapping exists but is not effective) each emit different marks and semantic classes.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Render_DistinguishesGrantedNotGrantedAndDisabled()
        {
            // <lang>
            //   <zh-CN>构造 2 个权限键 × 2 个角色：Admins 两个键均已授权，Audit 只有 View 且被禁用 → 形成"已授权 2 / 已禁用 1 / 未授权 1"三态并存。注意角色列来自映射中出现的角色，因此三态必须由"同一角色缺某个键"来构造，而不是靠新增一个零映射角色。</zh-CN>
            //   <en>Build two permission keys across two roles: Admins holds both keys, while Audit holds only View and with IsEnabled set to false, producing two granted, one disabled, and one not-granted cell. Note that role columns come from the roles present in the mappings, so the not-granted state must be constructed by a role missing one key rather than by adding a zero-mapping role.</en>
            // </lang>
            List<PortalPermissionDefinition> definitions = new List<PortalPermissionDefinition>
            {
                new PortalPermissionDefinition("Settings.View", "Settings", "sample"),
                new PortalPermissionDefinition("Settings.Edit", "Settings", "sample")
            };

            List<RolePermissionEntry> grants = new List<RolePermissionEntry>
            {
                new RolePermissionEntry { RoleId = 1, RoleName = "Admins", PermissionKey = "Settings.View", IsEnabled = true },
                new RolePermissionEntry { RoleId = 1, RoleName = "Admins", PermissionKey = "Settings.Edit", IsEnabled = true },
                new RolePermissionEntry { RoleId = 2, RoleName = "Audit", PermissionKey = "Settings.View", IsEnabled = false }
            };

            string html = PortalCapabilityPermissionMatrixRenderer.Render(definitions, grants, null);

            // <lang>
            //   <zh-CN>三态断言：数量必须精确匹配，且禁用态带 aria-disabled，避免被读作可点击或未授权。</zh-CN>
            //   <en>Three-state assertions: the counts must match exactly, and the disabled cell must carry aria-disabled so it is neither clickable nor mistaken for not-granted.</en>
            // </lang>
            Assert.AreEqual(2, CountOccurrences(html, "portal-capability-matrix-granted"), "Expected exactly two granted cells.");
            Assert.AreEqual(1, CountOccurrences(html, "portal-capability-matrix-disabled"), "Expected exactly one disabled cell.");
            Assert.AreEqual(1, CountOccurrences(html, "portal-capability-matrix-none"), "Expected exactly one not-granted cell.");
            Assert.IsTrue(html.Contains("aria-disabled=\"true\""), "Disabled cell must announce aria-disabled.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证无权限定义时返回空态提示，而不是残缺表头。</zh-CN>
        ///   <en>Verifies that no permission definition yields the empty-state hint instead of a partial header.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Render_WithoutDefinitions_ReturnsEmptyState()
        {
            // <lang>
            //   <zh-CN>空集合与 null 两种输入都应走空态，且不输出表格标签。</zh-CN>
            //   <en>Both an empty collection and null must take the empty-state path and emit no table markup.</en>
            // </lang>
            string fromEmpty = PortalCapabilityPermissionMatrixRenderer.Render(new List<PortalPermissionDefinition>(), null, null);
            string fromNull = PortalCapabilityPermissionMatrixRenderer.Render(null, null, null);

            Assert.IsTrue(fromEmpty.Contains("EmptyDefinitions"), "Empty input must render the empty-state hint.");
            Assert.IsTrue(fromNull.Contains("EmptyDefinitions"), "Null input must render the empty-state hint.");
            Assert.IsFalse(fromEmpty.Contains("<table"), "Empty state must not emit a table.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证无任何角色映射时仍渲染表头与权限键，使维护者能确认权限定义是否已登记。</zh-CN>
        ///   <en>Verifies that with no role mapping the header and permission keys are still rendered so a maintainer can confirm the definitions are registered.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Render_WithoutGrants_StillRendersHeaderAndKeys()
        {
            // <lang>
            //   <zh-CN>只有一个定义、零映射时，表头列标题与键名都应出现，且不产生任何角色列。</zh-CN>
            //   <en>With a single definition and zero mappings, the header title and the key must appear while no role column is produced.</en>
            // </lang>
            List<PortalPermissionDefinition> definitions = new List<PortalPermissionDefinition>
            {
                new PortalPermissionDefinition("Settings.View", "Settings", "sample")
            };

            string html = PortalCapabilityPermissionMatrixRenderer.Render(definitions, new List<RolePermissionEntry>(), null);

            Assert.IsTrue(html.Contains("ColumnPermissionKey"), "Header title for the key column is missing.");
            Assert.IsTrue(html.Contains("Settings.View"), "Permission key row is missing.");
            Assert.IsFalse(html.Contains("portal-capability-matrix-granted"), "No granted cell can exist without mappings.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证未登记分类回退到默认层而不是抛异常，并验证层解析对已登记分类返回受控层标识。</zh-CN>
        ///   <en>Verifies an unregistered category falls back to the default layer instead of throwing, and that registered categories resolve to their controlled layer identifiers.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ResolveCapabilityLayer_UsesRegistryThenFallback()
        {
            // <lang>
            //   <zh-CN>已登记分类各自解析到预期层；这与契约 3.2.1 的层词表一致。</zh-CN>
            //   <en>Registered categories resolve to their expected layers, matching the layer vocabulary of contract section 3.2.1.</en>
            // </lang>
            Assert.AreEqual("Foundation", PortalCapabilityPermissionMatrixRenderer.ResolveCapabilityLayer("Administration"));
            Assert.AreEqual("BasicBusiness", PortalCapabilityPermissionMatrixRenderer.ResolveCapabilityLayer("Business.Collaboration"));
            Assert.AreEqual("Platform", PortalCapabilityPermissionMatrixRenderer.ResolveCapabilityLayer("Module"));

            // <lang>
            //   <zh-CN>未登记与空白分类都回退到默认层，且不抛异常（渲染器必须能容忍渐进登记）。</zh-CN>
            //   <en>Unregistered and blank categories fall back to the default layer without throwing, because the renderer must tolerate incremental registration.</en>
            // </lang>
            Assert.AreEqual("Platform", PortalCapabilityPermissionMatrixRenderer.ResolveCapabilityLayer("Unknown.Category"));
            Assert.AreEqual("Platform", PortalCapabilityPermissionMatrixRenderer.ResolveCapabilityLayer(null));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>统计子串出现次数，用于断言单元格三态的数量。</zh-CN>
        ///   <en>Counts substring occurrences so the three-state cell counts can be asserted.</en>
        /// </lang>
        /// </summary>
        /// <param name="text">
        /// <l>
        ///   <zh-CN>被搜索文本。</zh-CN>
        ///   <en>Text being searched.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待统计的子串。</zh-CN>
        ///   <en>Substring to count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>出现次数。</zh-CN>
        ///   <en>Number of occurrences.</en>
        /// </l>
        /// </returns>
        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = text.IndexOf(value, StringComparison.Ordinal);

            while (index >= 0)
            {
                count++;
                index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
            }

            return count;
        }
    }
}
