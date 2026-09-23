using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>角色与权限键映射的只读投影，用于能力权限矩阵的展示。</zh-CN>
    ///   <en>Read-only projection of a role-to-permission-key mapping, used by the capability permission matrix.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型是查询结果载体，不是领域实体：它不表达"已授权"的最终结论（`IsEnabled` 只表示该映射是否生效），也不提供任何写入路径。属性使用公共 setter 是为了适配既有的 SqlQuery 投影方式。</zh-CN>
    ///   <en>This type is a query-result carrier rather than a domain entity: it does not represent a final authorization conclusion (`IsEnabled` only states whether the mapping is effective) and exposes no write path. Public setters exist to fit the established SqlQuery projection style.</en>
    /// </lang>
    /// </remarks>
    public sealed class RolePermissionEntry
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>角色数值标识。</zh-CN>
        ///   <en>Numeric role identifier.</en>
        /// </lang>
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>角色名（取自旧角色表，用于矩阵列标题）。</zh-CN>
        ///   <en>Role name taken from the legacy role table and used as the matrix column title.</en>
        /// </lang>
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>稳定权限键名。</zh-CN>
        ///   <en>Stable permission key.</en>
        /// </lang>
        /// </summary>
        public string PermissionKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>该映射是否生效；false 表示映射存在但被禁用，展示时必须与"未授权"区分。</zh-CN>
        ///   <en>Whether the mapping is effective; false means the mapping exists but is disabled, and rendering must distinguish it from "not granted".</en>
        /// </lang>
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
