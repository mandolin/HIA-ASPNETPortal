using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料确认模块的当前用户视图。</zh-CN>
    ///   <en>Current-user view for the employee-profile confirmation module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本视图只包含第一版允许员工自查的低敏基础字段和最近确认状态，不包含身份证号、手机号、 薪资、绩效等高敏字段。</zh-CN>
    ///   <en>This view contains only low-sensitivity foundation fields and the latest confirmation state allowed for employee self-check in the first version. It excludes high-sensitivity fields such as government ids, mobile phone numbers, compensation, and performance data.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileConfirmationView
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建员工资料确认模块当前用户视图。</zh-CN>
        ///   <en>Creates a current-user view for the employee-profile confirmation module.</en>
        /// </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近一次确认记录标识。</zh-CN>
        ///   <en>Latest confirmation-record identifier.</en>
        /// </lang>
        /// </summary>
        public long? LastConfirmationId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近一次确认 UTC 时间。</zh-CN>
        ///   <en>Latest confirmation UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime? LastConfirmedUtc { get; private set; }
    }
}
