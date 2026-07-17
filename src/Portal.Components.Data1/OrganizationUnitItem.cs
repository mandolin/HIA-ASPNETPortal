using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：P6.3 组织单元 EF 记录。
    ///
    /// English: P6.3 organization-unit EF row.
    /// </summary>
    /// <remarks>
    /// 中文：本实体不声明导航属性，组织树由服务层基于 <c>ParentOrganizationUnitId</c> 显式组装。
    ///
    /// English: This entity does not declare navigation properties; services explicitly assemble the tree from
    /// <c>ParentOrganizationUnitId</c>.
    /// </remarks>
    [Table("PortalBiz_OrganizationUnits")]
    public class OrganizationUnitItem
    {
        /// <summary>中文：组织单元标识。English: Organization-unit identifier.</summary>
        [Key]
        public int OrganizationUnitId { get; set; }

        /// <summary>中文：父级组织单元标识。English: Parent organization-unit identifier.</summary>
        public int? ParentOrganizationUnitId { get; set; }

        /// <summary>中文：组织编码。English: Organization code.</summary>
        public string OrganizationCode { get; set; }

        /// <summary>中文：组织显示名。English: Organization display name.</summary>
        public string DisplayName { get; set; }

        /// <summary>中文：排序值。English: Sort order.</summary>
        public int SortOrder { get; set; }

        /// <summary>中文：是否启用。English: Whether the unit is active.</summary>
        public bool IsActive { get; set; }

        /// <summary>中文：创建时间 UTC。English: Creation time in UTC.</summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>中文：创建人标识。English: Creator identifier.</summary>
        public string CreatedBy { get; set; }

        /// <summary>中文：最近更新时间 UTC。English: Last update time in UTC.</summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>中文：最近更新人标识。English: Last updater identifier.</summary>
        public string UpdatedBy { get; set; }

        /// <summary>中文：SQL Server 并发版本。English: SQL Server concurrency version.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
