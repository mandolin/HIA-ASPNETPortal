using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业能力生命周期的稳定值。</zh-CN>
    ///   <en>Stable lifecycle states for enterprise capabilities.</en>
    /// </lang>
    /// </summary>
    public static class PortalCapabilityLifecycleStates
    {
        /// <summary><lang><zh-CN>已规划，尚未有主责模块实现。</zh-CN><en>Planned, not yet implemented by a primary module.</en></lang></summary>
        public const string Planned = "Planned";

        /// <summary><lang><zh-CN>已激活，可被模块声明为主责。</zh-CN><en>Active and available as a primary-module target.</en></lang></summary>
        public const string Active = "Active";

        /// <summary><lang><zh-CN>已弃用，不再接受新的主责声明。</zh-CN><en>Deprecated and no longer accepts new primary declarations.</en></lang></summary>
        public const string Deprecated = "Deprecated";
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>企业能力权威词表中的一条能力定义。</zh-CN>
    ///   <en>One capability definition in the enterprise-capability authority registry.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>能力是抽象分类，模块是声明方。本定义只登记能力键、所属层、生命周期、主责模块与权限键前缀，不收模块实现细节，也不授予任何权限。</zh-CN>
    ///   <en>A capability is an abstract classification while modules are the declaring side. This definition only registers the capability key, layer, lifecycle, primary module, and permission-key prefix; it holds no module implementation detail and grants no permission.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalCapabilityDefinition
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一条能力定义。</zh-CN>
        ///   <en>Creates a capability definition.</en>
        /// </lang>
        /// </summary>
        internal PortalCapabilityDefinition(
            string capabilityId,
            string layer,
            string lifecycleState,
            string primaryModuleId,
            string permissionKeyPrefix,
            string displayNameZh,
            string displayNameEn)
        {
            CapabilityId = capabilityId ?? string.Empty;
            Layer = layer ?? string.Empty;
            LifecycleState = lifecycleState ?? string.Empty;
            PrimaryModuleId = primaryModuleId ?? string.Empty;
            PermissionKeyPrefix = permissionKeyPrefix ?? string.Empty;
            DisplayNameZh = displayNameZh ?? string.Empty;
            DisplayNameEn = displayNameEn ?? string.Empty;
        }

        /// <summary><lang><zh-CN>稳定能力键，形如 <c>BasicBusiness.Collaboration</c>。</zh-CN><en>Stable capability key such as <c>BasicBusiness.Collaboration</c>.</en></lang></summary>
        public string CapabilityId { get; private set; }

        /// <summary><lang><zh-CN>所属能力层（取自 P19.2 的 6 层词表）。</zh-CN><en>Owning capability layer from the P19.2 six-layer vocabulary.</en></lang></summary>
        public string Layer { get; private set; }

        /// <summary><lang><zh-CN>生命周期状态。</zh-CN><en>Lifecycle state.</en></lang></summary>
        public string LifecycleState { get; private set; }

        /// <summary><lang><zh-CN>主责模块包标识（能力锚点，不含全部实现）。</zh-CN><en>Primary module package identifier (the capability anchor, not the full implementation).</en></lang></summary>
        public string PrimaryModuleId { get; private set; }

        /// <summary><lang><zh-CN>该能力的权限键前缀。</zh-CN><en>Permission-key prefix for this capability.</en></lang></summary>
        public string PermissionKeyPrefix { get; private set; }

        /// <summary><lang><zh-CN>中文显示名。</zh-CN><en>Chinese display name.</en></lang></summary>
        public string DisplayNameZh { get; private set; }

        /// <summary><lang><zh-CN>英文显示名。</zh-CN><en>English display name.</en></lang></summary>
        public string DisplayNameEn { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>企业能力权威词表。</zh-CN>
    ///   <en>Enterprise-capability authority registry.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P46 契约落地：能力词表是权威、模块是声明方。manifest 声明的 <c>capabilityId</c> 必须能在此解析，否则该包不能声明该能力。分层是分类不是等级；层之间不蕴含。</zh-CN>
    ///   <en>P46 contract: the capability registry is authoritative and modules are declarers. A manifest <c>capabilityId</c> must resolve here or the package cannot declare that capability. A layer is a classification, not a maturity level; layers do not imply one another.</en>
    /// </lang>
    /// </remarks>
    public static class PortalCapabilityRegistry
    {
        /// <summary><lang><zh-CN>企业协同事项能力键。</zh-CN><en>Enterprise collaboration-item capability key.</en></lang></summary>
        public const string Collaboration = "BasicBusiness.Collaboration";

        /// <summary><lang><zh-CN>业务申请能力键；与 <c>BasicBusiness.Collaboration</c> 同属基础业务层，复用既有 <c>Business.Application</c> 权限键族。</zh-CN><en>Business application request capability key; shares the BasicBusiness layer with <c>BasicBusiness.Collaboration</c> and reuses the existing <c>Business.Application</c> permission-key family.</en></lang></summary>
        public const string ApplicationRequest = "BasicBusiness.ApplicationRequest";

        private static readonly PortalCapabilityDefinition[] DefinitionArray =
        {
            new PortalCapabilityDefinition(
                Collaboration,
                "BasicBusiness",
                PortalCapabilityLifecycleStates.Active,
                "HIA.EnterpriseCapabilityWorkbench",
                "Business.Collaboration",
                "企业协同事项",
                "Enterprise Collaboration Item"),
            new PortalCapabilityDefinition(
                ApplicationRequest,
                "BasicBusiness",
                PortalCapabilityLifecycleStates.Active,
                "HIA.BusinessApplicationRequest",
                "Business.Application",
                "业务申请",
                "Business Application Request")
        };

        private static readonly IList<PortalCapabilityDefinition> ReadOnlyDefinitions =
            new ReadOnlyCollection<PortalCapabilityDefinition>(DefinitionArray);

        /// <summary><lang><zh-CN>当前已登记的能力定义只读集合。</zh-CN><en>Read-only set of currently registered capability definitions.</en></lang></summary>
        public static IList<PortalCapabilityDefinition> Definitions
        {
            get { return ReadOnlyDefinitions; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按稳定能力键解析一条能力定义。</zh-CN>
        ///   <en>Resolves one capability definition by stable capability key.</en>
        /// </lang>
        /// </summary>
        public static bool TryGet(string capabilityId, out PortalCapabilityDefinition definition)
        {
            definition = DefinitionArray.FirstOrDefault(item =>
                string.Equals(item.CapabilityId, capabilityId, StringComparison.Ordinal));
            return definition != null;
        }
    }
}
