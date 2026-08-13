using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工主数据只读视图的默认实现。</zh-CN>
    ///   <en>Default implementation of the employee master-data read-only view.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象是数据访问层返回给页面和服务的稳定投影，不参与授权判断。构造函数会把可空文本归一化为空字符串，便于旧 Web Forms 绑定控件直接展示。</zh-CN>
    ///   <en>This object is a stable projection returned from the data layer to pages and services and does not participate in authorization. The constructor normalizes nullable text to empty strings so legacy Web Forms binding controls can display it directly.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeInfo : IEmployeeInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建员工主数据只读视图。</zh-CN>
        ///   <en>Creates an employee master-data read-only view.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeId">
        /// <l>
        ///   <zh-CN>员工记录标识。</zh-CN>
        ///   <en>The employee record identifier.</en>
        /// </l>
        /// </param>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>企业员工号；可用于工号登录标识解析，但不是口令或密钥。</zh-CN>
        ///   <en>The enterprise employee code; it may be used by employee-code login resolution but is not a password or secret.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>员工显示名。</zh-CN>
        ///   <en>The employee display name.</en>
        /// </l>
        /// </param>
        /// <param name="preferredName">
        /// <l>
        ///   <zh-CN>员工偏好称呼。</zh-CN>
        ///   <en>The employee preferred name.</en>
        /// </l>
        /// </param>
        /// <param name="workEmail">
        /// <l>
        ///   <zh-CN>工作邮箱；作为联系方式展示，不作为已验证身份凭据。</zh-CN>
        ///   <en>The work email shown as contact information, not a verified identity credential.</en>
        /// </l>
        /// </param>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>所属组织单元标识；无组织时为空。</zh-CN>
        ///   <en>The owning organization-unit identifier, or empty when no organization is assigned.</en>
        /// </l>
        /// </param>
        /// <param name="organizationDisplayName">
        /// <l>
        ///   <zh-CN>所属组织显示名。</zh-CN>
        ///   <en>The owning organization display name.</en>
        /// </l>
        /// </param>
        /// <param name="employmentStatus">
        /// <l>
        ///   <zh-CN>员工生命周期状态字符串。</zh-CN>
        ///   <en>The employee lifecycle status string.</en>
        /// </l>
        /// </param>
        /// <param name="joinedUtc">
        /// <l>
        ///   <zh-CN>入职 UTC 时间；未知时为空。</zh-CN>
        ///   <en>The joined UTC time, or empty when unknown.</en>
        /// </l>
        /// </param>
        /// <param name="leftUtc">
        /// <l>
        ///   <zh-CN>离职 UTC 时间；仍在职或未知时为空。</zh-CN>
        ///   <en>The left UTC time, or empty when the employee is active or the value is unknown.</en>
        /// </l>
        /// </param>
        /// <param name="sourceSystem">
        /// <l>
        ///   <zh-CN>资料来源系统标识。</zh-CN>
        ///   <en>The source-system identifier for the profile data.</en>
        /// </l>
        /// </param>
        /// <param name="updatedUtc">
        /// <l>
        ///   <zh-CN>记录最近更新 UTC 时间。</zh-CN>
        ///   <en>The latest record update time in UTC.</en>
        /// </l>
        /// </param>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工数值标识。</zh-CN>
        ///   <en>Numeric employee identifier.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>全局唯一员工号；可作为后续可选登录标识，但不是认证凭据。</zh-CN>
        ///   <en>Globally unique employee code; it may become an optional sign-in identifier later but is not a credential.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工正式显示名。</zh-CN>
        ///   <en>Formal employee display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好称呼或昵称。</zh-CN>
        ///   <en>Preferred name or nickname.</en>
        /// </lang>
        /// </summary>
        public string PreferredName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>工作邮箱，可为空且第一版不保证唯一。</zh-CN>
        ///   <en>Work email address; optional and not guaranteed to be unique in the first version.</en>
        /// </lang>
        /// </summary>
        public string WorkEmail { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>所属组织单元标识。</zh-CN>
        ///   <en>Owning organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        public int? OrganizationUnitId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>所属组织显示名称，缺少组织或未部署组织表时为空。</zh-CN>
        ///   <en>Owning organization display name, empty when the organization is absent or the organization table is not deployed.</en>
        /// </lang>
        /// </summary>
        public string OrganizationDisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工生命周期状态。</zh-CN>
        ///   <en>Employee lifecycle status.</en>
        /// </lang>
        /// </summary>
        public string EmploymentStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入职时间 UTC，可为空。</zh-CN>
        ///   <en>Joined time in UTC, when known.</en>
        /// </lang>
        /// </summary>
        public DateTime? JoinedUtc { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>离职时间 UTC，可为空。</zh-CN>
        ///   <en>Left time in UTC, when known.</en>
        /// </lang>
        /// </summary>
        public DateTime? LeftUtc { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据来源系统标识。</zh-CN>
        ///   <en>Source-system identifier.</en>
        /// </lang>
        /// </summary>
        public string SourceSystem { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新时间 UTC，用于后台编辑页的轻量并发检查。</zh-CN>
        ///   <en>Last update time in UTC, used by administration edit pages for lightweight concurrency checks.</en>
        /// </lang>
        /// </summary>
        public DateTime UpdatedUtc { get; private set; }
    }
}
