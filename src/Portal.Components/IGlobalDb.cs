namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户全局配置的数据访问契约。</zh-CN>
    ///     <en>Data access contract for legacy portal global configuration.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该契约只覆盖旧全局配置表中的门户名称和编辑按钮策略。系统设置、主题设置、审计和权限判断
    ///       已在后来的配置体系中分层处理，不应混入这里。
    ///     </zh-CN>
    ///     <en>
    ///       This contract only covers the portal name and edit-button policy stored in the legacy global configuration table.
    ///       System settings, theme settings, auditing, and authorization checks are layered elsewhere and should not be mixed into this contract.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IGlobalsDb
    {
        /// <summary>
        ///   <lang>
        ///     <zh-CN>获取单个门户的旧全局配置。</zh-CN>
        ///     <en>Gets the legacy global configuration for one portal.</en>
        ///   </lang>
        /// </summary>
        /// <param name="portalId">
        ///   <l>
        ///     <zh-CN>门户标识。</zh-CN>
        ///     <en>Portal identifier.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>匹配的全局配置项。</zh-CN>
        ///     <en>The matching global configuration item.</en>
        ///   </l>
        /// </returns>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>实现可采用严格单行语义；缺失或重复配置应暴露为配置错误，而不是静默回退。</zh-CN>
        ///     <en>Implementations may use strict single-row semantics; missing or duplicated configuration should surface as configuration errors instead of silent fallback.</en>
        ///   </lang>
        /// </remarks>
        IGlobalItem GetSinglePortal(int portalId);

        /// <summary>
        ///   <lang>
        ///     <zh-CN>更新旧全局配置中的门户名称和编辑按钮策略。</zh-CN>
        ///     <en>Updates the portal name and edit-button policy in legacy global configuration.</en>
        ///   </lang>
        /// </summary>
        /// <param name="portalId">
        ///   <l>
        ///     <zh-CN>门户标识。</zh-CN>
        ///     <en>Portal identifier.</en>
        ///   </l>
        /// </param>
        /// <param name="portalName">
        ///   <l>
        ///     <zh-CN>要保存的门户名称。</zh-CN>
        ///     <en>Portal name to save.</en>
        ///   </l>
        /// </param>
        /// <param name="alwaysShow">
        ///   <l>
        ///     <zh-CN>是否始终显示模块编辑按钮。</zh-CN>
        ///     <en>Whether module edit buttons should always be shown.</en>
        ///   </l>
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>调用页面负责管理员权限、输入长度、审计和缓存刷新；该契约只表示持久化写入。</zh-CN>
        ///     <en>The calling page owns administrator authorization, input length checks, auditing, and cache refresh; this contract only represents persistence.</en>
        ///   </lang>
        /// </remarks>
        void UpdatePortalInfo(int portalId, string portalName, bool alwaysShow);
    }
}
