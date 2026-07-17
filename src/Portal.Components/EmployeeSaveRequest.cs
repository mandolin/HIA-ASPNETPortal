using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工主数据后台保存请求。
    ///
    /// English: Administration save request for employee master data.
    /// </summary>
    /// <remarks>
    /// 中文：本请求只覆盖 P6.3 第一版最小字段，不承载手机号、身份证号、住址等高敏个人资料。
    ///
    /// English: This request covers only the first-version P6.3 minimal fields and does not carry highly sensitive
    /// personal data such as phone numbers, government identifiers, or addresses.
    /// </remarks>
    public sealed class EmployeeSaveRequest
    {
        /// <summary>中文：员工标识；零表示新增。English: Employee id; zero means create.</summary>
        public int EmployeeId { get; set; }

        /// <summary>中文：全局唯一员工号。English: Globally unique employee code.</summary>
        public string EmployeeCode { get; set; }

        /// <summary>中文：正式显示名。English: Formal display name.</summary>
        public string DisplayName { get; set; }

        /// <summary>中文：偏好称呼或昵称。English: Preferred name or nickname.</summary>
        public string PreferredName { get; set; }

        /// <summary>中文：工作邮箱。English: Work email address.</summary>
        public string WorkEmail { get; set; }

        /// <summary>中文：所属组织单元标识。English: Owning organization-unit id.</summary>
        public int? OrganizationUnitId { get; set; }

        /// <summary>中文：员工生命周期状态。English: Employee lifecycle status.</summary>
        public string EmploymentStatus { get; set; }

        /// <summary>中文：入职时间 UTC。English: Joined time in UTC.</summary>
        public DateTime? JoinedUtc { get; set; }

        /// <summary>中文：离职时间 UTC。English: Left time in UTC.</summary>
        public DateTime? LeftUtc { get; set; }

        /// <summary>中文：数据来源系统。English: Source system.</summary>
        public string SourceSystem { get; set; }

        /// <summary>中文：更新前读取到的 UTC 更新时间。English: UTC update time read before editing.</summary>
        public DateTime? OriginalUpdatedUtc { get; set; }

        /// <summary>中文：当前操作者标识。English: Current actor identifier.</summary>
        public string ActorName { get; set; }
    }
}
