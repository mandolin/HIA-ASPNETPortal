using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>组织单元只读视图，供页面和服务层读取 P6.3 组织树基础数据。</zh-CN>
    ///   <en>Read-only organization-unit view for pages and services that consume the P6.3 organization tree foundation.</en>
    /// </lang>
    /// </summary>
    public interface IOrganizationUnitInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元数值标识。</zh-CN>
        ///   <en>Numeric organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        int OrganizationUnitId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>父级组织单元标识；顶级组织为空。</zh-CN>
        ///   <en>Parent organization-unit identifier; null for top-level units.</en>
        /// </lang>
        /// </summary>
        int? ParentOrganizationUnitId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选组织编码。</zh-CN>
        ///   <en>Optional organization code.</en>
        /// </lang>
        /// </summary>
        string OrganizationCode { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织显示名称。</zh-CN>
        ///   <en>Display name of the organization unit.</en>
        /// </lang>
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>同级排序值。</zh-CN>
        ///   <en>Sibling sort order.</en>
        /// </lang>
        /// </summary>
        int SortOrder { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元是否启用。</zh-CN>
        ///   <en>Whether the organization unit is active.</en>
        /// </lang>
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建时间 UTC。</zh-CN>
        ///   <en>Creation time in UTC.</en>
        /// </lang>
        /// </summary>
        DateTime CreatedUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新时间 UTC。</zh-CN>
        ///   <en>Last update time in UTC.</en>
        /// </lang>
        /// </summary>
        DateTime UpdatedUtc { get; }
    }
}
