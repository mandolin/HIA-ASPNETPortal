using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：组织单元只读视图，供页面和服务层读取 P6.3 组织树基础数据。
    ///
    /// English: Read-only organization-unit view for pages and services that consume the P6.3 organization tree foundation.
    /// </summary>
    public interface IOrganizationUnitInfo
    {
        /// <summary>
        /// 中文：组织单元数值标识。
        ///
        /// English: Numeric organization-unit identifier.
        /// </summary>
        int OrganizationUnitId { get; }

        /// <summary>
        /// 中文：父级组织单元标识；顶级组织为空。
        ///
        /// English: Parent organization-unit identifier; null for top-level units.
        /// </summary>
        int? ParentOrganizationUnitId { get; }

        /// <summary>
        /// 中文：可选组织编码。
        ///
        /// English: Optional organization code.
        /// </summary>
        string OrganizationCode { get; }

        /// <summary>
        /// 中文：组织显示名称。
        ///
        /// English: Display name of the organization unit.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 中文：同级排序值。
        ///
        /// English: Sibling sort order.
        /// </summary>
        int SortOrder { get; }

        /// <summary>
        /// 中文：组织单元是否启用。
        ///
        /// English: Whether the organization unit is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 中文：创建时间 UTC。
        ///
        /// English: Creation time in UTC.
        /// </summary>
        DateTime CreatedUtc { get; }

        /// <summary>
        /// 中文：最近更新时间 UTC。
        ///
        /// English: Last update time in UTC.
        /// </summary>
        DateTime UpdatedUtc { get; }
    }
}
