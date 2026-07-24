using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求模块中当前用户可见的低敏员工资料视图。</zh-CN>
    ///   <en>Low-sensitivity employee-profile view visible to the current user in the correction-request module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此视图复用 P6.4.1 的员工绑定边界，只暴露员工号、姓名、称呼、工作邮箱、组织和在职状态。</zh-CN>
    ///   <en>This view reuses the P6.4.1 employee-binding boundary and exposes only employee code, name, preferred name, work email, organization, and employment status.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileCorrectionProfileView
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建当前用户员工资料更正视图。</zh-CN>
        ///   <en>Creates a current-user employee-profile correction view.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeId">
        /// <l>
        ///   <zh-CN>员工主键标识。</zh-CN>
        ///   <en>Employee primary identifier.</en>
        /// </l>
        /// </param>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>企业员工号。</zh-CN>
        ///   <en>Enterprise employee code.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>正式姓名或显示名。</zh-CN>
        ///   <en>Formal name or display name.</en>
        /// </l>
        /// </param>
        /// <param name="preferredName">
        /// <l>
        ///   <zh-CN>员工偏好称呼。</zh-CN>
        ///   <en>Employee preferred name.</en>
        /// </l>
        /// </param>
        /// <param name="workEmail">
        /// <l>
        ///   <zh-CN>工作邮箱。</zh-CN>
        ///   <en>Work email address.</en>
        /// </l>
        /// </param>
        /// <param name="organizationDisplayName">
        /// <l>
        ///   <zh-CN>组织显示名。</zh-CN>
        ///   <en>Organization display name.</en>
        /// </l>
        /// </param>
        /// <param name="employmentStatus">
        /// <l>
        ///   <zh-CN>员工在职状态。</zh-CN>
        ///   <en>Employee employment status.</en>
        /// </l>
        /// </param>
        /// <param name="bindingId">
        /// <l>
        ///   <zh-CN>当前有效用户员工绑定标识。</zh-CN>
        ///   <en>Current active user-employee binding identifier.</en>
        /// </l>
        /// </param>
        /// <param name="boundUtc">
        /// <l>
        ///   <zh-CN>绑定创建 UTC 时间。</zh-CN>
        ///   <en>UTC time when the binding was created.</en>
        /// </l>
        /// </param>
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
            // <lang>
            //   <zh-CN>视图对象直接供 Web Forms 绑定使用，字符串统一归一为空串，避免旧模板层对 null 做重复判断。</zh-CN>
            //   <en>The view object is bound directly by Web Forms, so strings are normalized to empty values to avoid repeated null checks in legacy templates.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号。</zh-CN>
        ///   <en>Employee code.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>正式显示名。</zh-CN>
        ///   <en>Formal display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好称呼。</zh-CN>
        ///   <en>Preferred name.</en>
        /// </lang>
        /// </summary>
        public string PreferredName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>工作邮箱。</zh-CN>
        ///   <en>Work email.</en>
        /// </lang>
        /// </summary>
        public string WorkEmail { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织显示名。</zh-CN>
        ///   <en>Organization display name.</en>
        /// </lang>
        /// </summary>
        public string OrganizationDisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工状态。</zh-CN>
        ///   <en>Employee status.</en>
        /// </lang>
        /// </summary>
        public string EmploymentStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前有效绑定标识。</zh-CN>
        ///   <en>Current active binding identifier.</en>
        /// </lang>
        /// </summary>
        public int BindingId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定创建 UTC 时间。</zh-CN>
        ///   <en>Binding creation UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime BoundUtc { get; private set; }
    }
}
