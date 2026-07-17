using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：组织单元后台保存请求。
    ///
    /// English: Administration save request for an organization unit.
    /// </summary>
    /// <remarks>
    /// 中文：`OrganizationUnitId` 为零表示新增。更新时必须携带页面读取到的 `OriginalUpdatedUtc`，
    /// 由数据层执行轻量并发保护。
    ///
    /// English: An `OrganizationUnitId` of zero means creation. Updates must carry the `OriginalUpdatedUtc`
    /// read by the page so the data layer can perform lightweight concurrency protection.
    /// </remarks>
    public sealed class OrganizationUnitSaveRequest
    {
        /// <summary>中文：组织单元标识；零表示新增。English: Organization-unit id; zero means create.</summary>
        public int OrganizationUnitId { get; set; }

        /// <summary>中文：父级组织单元标识。English: Parent organization-unit id.</summary>
        public int? ParentOrganizationUnitId { get; set; }

        /// <summary>中文：可选组织编码。English: Optional organization code.</summary>
        public string OrganizationCode { get; set; }

        /// <summary>中文：组织显示名称。English: Organization display name.</summary>
        public string DisplayName { get; set; }

        /// <summary>中文：同级排序值。English: Sibling sort order.</summary>
        public int SortOrder { get; set; }

        /// <summary>中文：组织是否启用。English: Whether the organization unit is active.</summary>
        public bool IsActive { get; set; }

        /// <summary>中文：更新前读取到的 UTC 更新时间。English: UTC update time read before editing.</summary>
        public DateTime? OriginalUpdatedUtc { get; set; }

        /// <summary>中文：当前操作者标识。English: Current actor identifier.</summary>
        public string ActorName { get; set; }
    }
}
