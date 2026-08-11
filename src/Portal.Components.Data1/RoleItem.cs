using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户角色表的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for the legacy portal role table.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射 <c>Portal_Roles</c>，并通过 EF 多对多导航与用户实体关联。业务授权仍应通过
    ///       统一权限目录和角色解析工具执行，不能只依赖实体导航集合做安全判断。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps <c>Portal_Roles</c> and links to user entities through the EF many-to-many
    ///       navigation. Business authorization must still go through the shared permission catalog and role
    ///       parser, not rely only on the entity navigation collection.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("Portal_Roles")]
    public class RoleItem : IRoleItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该角色的用户导航集合，由 EF 旧成员关系映射填充。</zh-CN>
        ///     <en>User navigation collection populated by the legacy EF membership mapping.</en>
        ///   </l>
        /// </summary>
        public virtual ICollection<UserItem> Users { get; set; }

        #region IRoleItem Members

        /// <summary>
        ///   <l>
        ///     <zh-CN>角色主键。</zh-CN>
        ///     <en>Role primary key.</en>
        ///   </l>
        /// </summary>
        [Key]
        public int RoleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>角色所属门户标识。</zh-CN>
        ///     <en>Identifier of the portal that owns this role.</en>
        ///   </l>
        /// </summary>
        public int PortalId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>角色名称；旧页面和分号角色字符串均以该值匹配。</zh-CN>
        ///     <en>Role name matched by legacy pages and semicolon-delimited role strings.</en>
        ///   </l>
        /// </summary>
        public string RoleName { get; set; }

        #endregion
    }
}
