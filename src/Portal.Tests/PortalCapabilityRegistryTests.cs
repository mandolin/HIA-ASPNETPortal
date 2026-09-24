using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖企业能力权威词表的稳定单元测试；W51 新增业务申请能力的回归锚点。</zh-CN>
    ///   <en>Stable unit tests for the enterprise-capability authority registry; the W51 regression anchor for the new business-application capability.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些测试只读取 <c>Portal.Components</c> 的纯内存契约，不连接数据库、不读取配置、不创建账号，也不依赖 IIS 或 HTTP 上下文。</zh-CN>
    ///   <en>These tests read only pure in-memory contracts from <c>Portal.Components</c>; they do not connect to databases, read configuration, create accounts, or depend on IIS or HTTP context.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalCapabilityRegistryTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 W51 新增的业务申请能力键常量值符合稳定词表约定。</zh-CN>
        ///   <en>Verifies that the W51 business-application capability-key constant follows the stable vocabulary convention.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ApplicationRequest_ConstantHasStableVocabularyForm()
        {
            // <lang>
            //   <zh-CN>期望值固定为 <c>BasicBusiness.ApplicationRequest</c>，与协同事项 <c>BasicBusiness.Collaboration</c> 同层同构。</zh-CN>
            //   <en>The expected value is pinned to <c>BasicBusiness.ApplicationRequest</c>, sharing the BasicBusiness layer with <c>BasicBusiness.Collaboration</c>.</en>
            // </lang>
            Assert.AreEqual(
                "BasicBusiness.ApplicationRequest",
                PortalCapabilityRegistry.ApplicationRequest,
                "业务申请能力键必须保持稳定的 <Domain>.<Capability> 形式。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 W51 后注册表同时持有协同事项与业务申请两条能力定义。</zh-CN>
        ///   <en>Verifies that the registry holds both the collaboration and the business-application definitions after W51.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Definitions_ContainsBothCollaborationAndApplicationRequest()
        {
            // <lang>
            //   <zh-CN>只读集合用于断言条数，新增能力不得改变既有协同事项条目，也不得引入重复键。</zh-CN>
            //   <en>The read-only set asserts the entry count: the new capability must not alter the existing collaboration entry nor introduce a duplicate key.</en>
            // </lang>
            Assert.AreEqual(
                2,
                PortalCapabilityRegistry.Definitions.Count,
                "W51 后应恰好有两条能力定义（Collaboration + ApplicationRequest）。");

            // <lang>
            //   <zh-CN>分别解析两条键，确认二者都可作为权威锚点被解析到。</zh-CN>
            //   <en>Resolve both keys separately to confirm each is resolvable as an authority anchor.</en>
            // </lang>
            PortalCapabilityDefinition collaboration;
            PortalCapabilityDefinition applicationRequest;
            Assert.IsTrue(
                PortalCapabilityRegistry.TryGet(PortalCapabilityRegistry.Collaboration, out collaboration),
                "协同事项能力必须仍可解析。");
            Assert.IsTrue(
                PortalCapabilityRegistry.TryGet(PortalCapabilityRegistry.ApplicationRequest, out applicationRequest),
                "业务申请能力必须可被解析（P51.1 已注册）。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证业务申请能力定义字段与 W51 复制基准完全一致（层、生命周期、主责包、权限前缀、双语名）。</zh-CN>
        ///   <en>Verifies that the business-application definition fields exactly match the W51 replication baseline (layer, lifecycle, primary module, permission prefix, bilingual names).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ApplicationRequest_DefinitionFieldsMatchReplicationBaseline()
        {
            // <lang>
            //   <zh-CN>解析结果变量保存被注册的能力定义，仅在当前断言内使用。</zh-CN>
            //   <en>The resolved variable holds the registered capability definition and is used only within the current assertion.</en>
            // </lang>
            PortalCapabilityDefinition definition;
            Assert.IsTrue(
                PortalCapabilityRegistry.TryGet(PortalCapabilityRegistry.ApplicationRequest, out definition));

            // <lang>
            //   <zh-CN>逐字段断言；权限键前缀复用既有 <c>Business.Application</c> 键族，零新增权限键。</zh-CN>
            //   <en>Assert each field; the permission-key prefix reuses the existing <c>Business.Application</c> family with zero new permission keys.</en>
            // </lang>
            Assert.AreEqual("BasicBusiness", definition.Layer, "业务申请能力必须归属 BasicBusiness 层。");
            Assert.AreEqual(
                PortalCapabilityLifecycleStates.Active,
                definition.LifecycleState,
                "业务申请能力初始应为 Active。");
            Assert.AreEqual(
                "HIA.BusinessApplicationRequest",
                definition.PrimaryModuleId,
                "业务申请能力主责包必须是 HIA.BusinessApplicationRequest。");
            Assert.AreEqual(
                "Business.Application",
                definition.PermissionKeyPrefix,
                "权限键前缀必须复用既有 Business.Application 键族。");
            Assert.AreEqual("业务申请", definition.DisplayNameZh, "中文显示名必须固定。");
            Assert.AreEqual("Business Application Request", definition.DisplayNameEn, "英文显示名必须固定。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证未注册的能力键不会被解析，确保词表边界严格（manifest 引用未注册键时 catalog 应拒包）。</zh-CN>
        ///   <en>Verifies that an unregistered capability key does not resolve, keeping the vocabulary boundary strict (catalog must reject a package referencing an unregistered key).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryGet_RejectsUnregisteredCapabilityKey()
        {
            // <lang>
            //   <zh-CN>输出参数在解析失败时应为 null，且方法返回 false；这是 catalog 硬拒包的前提。</zh-CN>
            //   <en>The out parameter must be null on failure and the method must return false; this is the premise for catalog's hard package rejection.</en>
            // </lang>
            PortalCapabilityDefinition definition;
            bool resolved = PortalCapabilityRegistry.TryGet("BasicBusiness.NotRegistered", out definition);

            Assert.IsFalse(resolved, "未注册能力键必须解析失败。");
            Assert.IsNull(definition, "解析失败时定义输出必须为 null。");
        }
    }
}
