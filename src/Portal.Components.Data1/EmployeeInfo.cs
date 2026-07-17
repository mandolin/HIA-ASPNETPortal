using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工主数据只读视图的默认实现。
    ///
    /// English: Default implementation of the employee master-data read-only view.
    /// </summary>
    public sealed class EmployeeInfo : IEmployeeInfo
    {
        /// <summary>
        /// 中文：创建员工主数据只读视图。
        ///
        /// English: Creates an employee master-data read-only view.
        /// </summary>
        public EmployeeInfo(
            int employeeId,
            string employeeCode,
            string displayName,
            string preferredName,
            string workEmail,
            int? organizationUnitId,
            string organizationDisplayName,
            string employmentStatus,
            DateTime? joinedUtc,
            DateTime? leftUtc,
            string sourceSystem,
            DateTime updatedUtc)
        {
            EmployeeId = employeeId;
            EmployeeCode = employeeCode ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            PreferredName = preferredName ?? string.Empty;
            WorkEmail = workEmail ?? string.Empty;
            OrganizationUnitId = organizationUnitId;
            OrganizationDisplayName = organizationDisplayName ?? string.Empty;
            EmploymentStatus = employmentStatus ?? string.Empty;
            JoinedUtc = joinedUtc;
            LeftUtc = leftUtc;
            SourceSystem = sourceSystem ?? string.Empty;
            UpdatedUtc = updatedUtc;
        }

        /// <inheritdoc />
        public int EmployeeId { get; private set; }

        /// <inheritdoc />
        public string EmployeeCode { get; private set; }

        /// <inheritdoc />
        public string DisplayName { get; private set; }

        /// <inheritdoc />
        public string PreferredName { get; private set; }

        /// <inheritdoc />
        public string WorkEmail { get; private set; }

        /// <inheritdoc />
        public int? OrganizationUnitId { get; private set; }

        /// <inheritdoc />
        public string OrganizationDisplayName { get; private set; }

        /// <inheritdoc />
        public string EmploymentStatus { get; private set; }

        /// <inheritdoc />
        public DateTime? JoinedUtc { get; private set; }

        /// <inheritdoc />
        public DateTime? LeftUtc { get; private set; }

        /// <inheritdoc />
        public string SourceSystem { get; private set; }

        /// <inheritdoc />
        public DateTime UpdatedUtc { get; private set; }
    }
}
