namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>模块在能力中的角色稳定值。</zh-CN>
    ///   <en>Stable role values for a module within a capability.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>角色描述模块与能力的关系：主责拥有能力事实与生命周期，支撑提供部分功能，投影只做呈现，适配连接能力，构建块是内部基础件。</zh-CN>
    ///   <en>The role describes a module's relation to a capability: Primary owns the capability's facts and lifecycle, Contributor supplies partial functionality, Projection only renders, Adapter connects capabilities, and Block is an internal building block.</en>
    /// </lang>
    /// </remarks>
    public static class PortalCapabilityRoles
    {
        /// <summary><lang><zh-CN>主责：拥有能力的事实、状态与生命周期。</zh-CN><en>Primary: owns the capability's facts, state, and lifecycle.</en></lang></summary>
        public const string Primary = "Primary";

        /// <summary><lang><zh-CN>支撑：为该能力提供部分功能，不自建该能力事实。</zh-CN><en>Contributor: supplies partial functionality without owning the capability's facts.</en></lang></summary>
        public const string Contributor = "Contributor";

        /// <summary><lang><zh-CN>投影：门户/工作台呈现，无事实。</zh-CN><en>Projection: portal/workbench rendering without facts.</en></lang></summary>
        public const string Projection = "Projection";

        /// <summary><lang><zh-CN>适配：连接两个能力或对接外部系统，不修改主模块。</zh-CN><en>Adapter: connects two capabilities or integrates external systems without modifying primary modules.</en></lang></summary>
        public const string Adapter = "Adapter";

        /// <summary><lang><zh-CN>构建块：内部基础件，不单独对外暴露。</zh-CN><en>Block: an internal building block not exposed on its own.</en></lang></summary>
        public const string Block = "Block";
    }
}
