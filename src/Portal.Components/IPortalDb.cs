namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户基础存储过程的数据访问契约。</zh-CN>
    ///     <en>Data access contract for legacy portal base stored procedures.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       当前只暴露旧 `Portal_DeleteModule` 入口，供模块实例删除流程清理模块配置和相关业务数据。
    ///       调用方必须先确认模块归属、管理员权限、引用关系和破坏性操作边界。
    ///     </zh-CN>
    ///     <en>
    ///       This currently exposes only the legacy `Portal_DeleteModule` entry used by module-instance deletion flows to clean
    ///       module configuration and related business data. Callers must verify module ownership, administrator authorization, references, and destructive-operation boundaries first.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IPortalDb
    {
        /// <summary>
        ///   <lang>
        ///     <zh-CN>删除指定模块实例及其旧存储过程覆盖的关联数据。</zh-CN>
        ///     <en>Deletes the specified module instance and the associated data covered by the legacy stored procedure.</en>
        ///   </lang>
        /// </summary>
        /// <param name="moduleId">
        ///   <l>
        ///     <zh-CN>要删除的模块实例标识。</zh-CN>
        ///     <en>Identifier of the module instance to delete.</en>
        ///   </l>
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>该操作是破坏性操作，不应由页面直接裸调；应通过模块管理流程完成权限、归属和确认检查。</zh-CN>
        ///     <en>This is a destructive operation and should not be called naked from a page; module management flows should perform authorization, ownership, and confirmation checks.</en>
        ///   </lang>
        /// </remarks>
        void DeleteModule(int moduleId);
    }
}
