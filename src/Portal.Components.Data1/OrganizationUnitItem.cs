using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>P6.3 组织单元 EF 记录。</zh-CN>
    ///   <en>P6.3 organization-unit EF row.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实体不声明导航属性，组织树由服务层基于 <c>ParentOrganizationUnitId</c> 显式组装。</zh-CN>
    ///   <en>This entity does not declare navigation properties; services explicitly assemble the tree from <c>ParentOrganizationUnitId</c>.</en>
    /// </lang>
    /// </remarks>
    [Table("PortalBiz_OrganizationUnits")]
    public class OrganizationUnitItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元标识。</zh-CN>
        ///   <en>Organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int OrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>父级组织单元标识。</zh-CN>
        ///   <en>Parent organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        public int? ParentOrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织编码。</zh-CN>
        ///   <en>Organization code.</en>
        /// </lang>
        /// </summary>
        public string OrganizationCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织显示名。</zh-CN>
        ///   <en>Organization display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>排序值。</zh-CN>
        ///   <en>Sort order.</en>
        /// </lang>
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否启用。</zh-CN>
        ///   <en>Whether the unit is active.</en>
        /// </lang>
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建时间 UTC。</zh-CN>
        ///   <en>Creation time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建人标识。</zh-CN>
        ///   <en>Creator identifier.</en>
        /// </lang>
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新时间 UTC。</zh-CN>
        ///   <en>Last update time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新人标识。</zh-CN>
        ///   <en>Last updater identifier.</en>
        /// </lang>
        /// </summary>
        public string UpdatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>SQL Server 并发版本。</zh-CN>
        ///   <en>SQL Server concurrency version.</en>
        /// </lang>
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
