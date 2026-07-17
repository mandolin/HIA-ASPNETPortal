using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料更正请求模块中当前用户可见的低敏员工资料视图。
    ///
    /// English: Low-sensitivity employee-profile view visible to the current user in the correction-request module.
    /// </summary>
    /// <remarks>
    /// 中文：此视图复用 P6.4.1 的员工绑定边界，只暴露员工号、姓名、称呼、工作邮箱、组织和在职状态。
    ///
    /// English: This view reuses the P6.4.1 employee-binding boundary and exposes only employee code, name,
    /// preferred name, work email, organization, and employment status.
    /// </remarks>
    public sealed class EmployeeProfileCorrectionProfileView
    {
        /// <summary>
        /// 中文：创建当前用户员工资料更正视图。
        ///
        /// English: Creates a current-user employee-profile correction view.
        /// </summary>
        public EmployeeProfileCorrectionProfileView(
            int employeeId,
            string employeeCode,
            string displayName,
            string preferredName,
            string workEmail,
            string organizationDisplayName,
            string employmentStatus,
            int bindingId,
            DateTime boundUtc)
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
    }
}
