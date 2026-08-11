namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户角色项契约。</zh-CN>
    ///     <en>Contract for a legacy portal role item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口用于后台角色列表、模块编辑角色选择和用户角色分配。角色名称仍为旧角色体系的显示/匹配值；
    ///       权限判断应通过统一授权服务或角色解析工具完成，不应只依赖页面局部字符串比较。
    ///     </zh-CN>
    ///     <en>
    ///       This interface is used by administration role lists, module edit-role selectors, and user role assignment. Role names
    ///       remain display/matching values from the legacy role system; authorization should go through shared authorization services or role parsing helpers instead of page-local string comparison alone.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IRoleItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>角色的旧数据库主键。</zh-CN>
        ///     <en>Legacy database primary key of the role.</en>
        ///   </l>
        /// </summary>
        int RoleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>角色所属门户标识。</zh-CN>
        ///     <en>Portal identifier that owns the role.</en>
        ///   </l>
        /// </summary>
        int PortalId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>角色名称；用于显示和旧角色字符串匹配。</zh-CN>
        ///     <en>Role name used for display and legacy role-string matching.</en>
        ///   </l>
        /// </summary>
        string RoleName { get; set; }
    }
}
