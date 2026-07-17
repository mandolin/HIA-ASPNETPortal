using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：组织单元只读视图的默认实现。
    ///
    /// English: Default implementation of the organization-unit read-only view.
    /// </summary>
    public sealed class OrganizationUnitInfo : IOrganizationUnitInfo
    {
        /// <summary>
        /// 中文：创建组织单元只读视图。
        ///
        /// English: Creates an organization-unit read-only view.
        /// </summary>
        public OrganizationUnitInfo(
            int organizationUnitId,
            int? parentOrganizationUnitId,
            string organizationCode,
            string displayName,
            int sortOrder,
            bool isActive,
            DateTime createdUtc,
            DateTime updatedUtc)
        {
            OrganizationUnitId = organizationUnitId;
            ParentOrganizationUnitId = parentOrganizationUnitId;
            OrganizationCode = organizationCode ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SortOrder = sortOrder;
            IsActive = isActive;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
        }

        /// <inheritdoc />
        public int OrganizationUnitId { get; private set; }

        /// <inheritdoc />
        public int? ParentOrganizationUnitId { get; private set; }

        /// <inheritdoc />
        public string OrganizationCode { get; private set; }

        /// <inheritdoc />
        public string DisplayName { get; private set; }

        /// <inheritdoc />
        public int SortOrder { get; private set; }

        /// <inheritdoc />
        public bool IsActive { get; private set; }

        /// <inheritdoc />
        public DateTime CreatedUtc { get; private set; }

        /// <inheritdoc />
        public DateTime UpdatedUtc { get; private set; }
    }
}
