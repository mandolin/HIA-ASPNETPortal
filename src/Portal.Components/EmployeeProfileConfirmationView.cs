using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料确认模块的当前用户视图。
    ///
    /// English: Current-user view for the employee-profile confirmation module.
    /// </summary>
    /// <remarks>
    /// 中文：本视图只包含第一版允许员工自查的低敏基础字段和最近确认状态，不包含身份证号、手机号、
    /// 薪资、绩效等高敏字段。
    ///
    /// English: This view contains only low-sensitivity foundation fields and the latest confirmation state allowed
    /// for employee self-check in the first version. It excludes high-sensitivity fields such as government ids,
    /// mobile phone numbers, compensation, and performance data.
    /// </remarks>
    public sealed class EmployeeProfileConfirmationView
    {
        /// <summary>
        /// 中文：创建员工资料确认模块当前用户视图。
        ///
        /// English: Creates a current-user view for the employee-profile confirmation module.
        /// </summary>
        public EmployeeProfileConfirmationView(
            int employeeId,
            string employeeCode,
            string displayName,
            string preferredName,
            string workEmail,
            string organizationDisplayName,
            string employmentStatus,
            int bindingId,
            DateTime boundUtc,
            long? lastConfirmationId,
            DateTime? lastConfirmedUtc)
        {
            EmployeeId = employeeId;
            EmployeeCode = employeeCode ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            PreferredName = preferredName ?? string.Empty;
            WorkEmail = workEmail ?? string.Empty;
            OrganizationDisplayName = organizationDisplayName ?? string.Empty;
            EmploymentStatus = employmentStatus ?? string.Empty;
            BindingId = bindingId;
            BoundUtc = boundUtc;
            LastConfirmationId = lastConfirmationId;
            LastConfirmedUtc = lastConfirmedUtc;
        }

        /// <summary>中文：员工标识。English: Employee identifier.</summary>
        public int EmployeeId { get; private set; }

        /// <summary>中文：员工号。English: Employee code.</summary>
        public string EmployeeCode { get; private set; }

        /// <summary>中文：正式显示名。English: Formal display name.</summary>
        public string DisplayName { get; private set; }

        /// <summary>中文：偏好称呼。English: Preferred name.</summary>
        public string PreferredName { get; private set; }

        /// <summary>中文：工作邮箱。English: Work email.</summary>
        public string WorkEmail { get; private set; }

        /// <summary>中文：组织显示名。English: Organization display name.</summary>
        public string OrganizationDisplayName { get; private set; }

        /// <summary>中文：员工状态。English: Employee status.</summary>
        public string EmploymentStatus { get; private set; }

        /// <summary>中文：当前有效绑定标识。English: Current active binding identifier.</summary>
        public int BindingId { get; private set; }

        /// <summary>中文：绑定创建 UTC 时间。English: Binding creation UTC time.</summary>
        public DateTime BoundUtc { get; private set; }

        /// <summary>中文：最近一次确认记录标识。English: Latest confirmation-record identifier.</summary>
        public long? LastConfirmationId { get; private set; }

        /// <summary>中文：最近一次确认 UTC 时间。English: Latest confirmation UTC time.</summary>
        public DateTime? LastConfirmedUtc { get; private set; }
    }
}
