using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：P6.3 员工主数据 EF 记录。
    ///
    /// English: P6.3 employee master-data EF row.
    /// </summary>
    /// <remarks>
    /// 中文：本实体仅保存门户业务所需最小字段，不存手机号、身份证号等高敏个人信息。
    ///
    /// English: This entity stores only minimal fields required by Portal business flows and excludes highly
    /// sensitive personal data such as phone numbers or government identifiers.
    /// </remarks>
    [Table("PortalBiz_Employees")]
    public class EmployeeItem
    {
        /// <summary>中文：员工标识。English: Employee identifier.</summary>
        [Key]
        public int EmployeeId { get; set; }

        /// <summary>中文：全局唯一员工号。English: Globally unique employee code.</summary>
        public string EmployeeCode { get; set; }

        /// <summary>中文：员工正式显示名。English: Formal employee display name.</summary>
        public string DisplayName { get; set; }

        /// <summary>中文：偏好称呼或昵称。English: Preferred name or nickname.</summary>
        public string PreferredName { get; set; }

        /// <summary>中文：工作邮箱。English: Work email address.</summary>
        public string WorkEmail { get; set; }

        /// <summary>中文：所属组织单元标识。English: Owning organization-unit identifier.</summary>
        public int? OrganizationUnitId { get; set; }

        /// <summary>中文：员工生命周期状态。English: Employee lifecycle status.</summary>
        public string EmploymentStatus { get; set; }

        /// <summary>中文：入职时间 UTC。English: Joined time in UTC.</summary>
        public DateTime? JoinedUtc { get; set; }

        /// <summary>中文：离职时间 UTC。English: Left time in UTC.</summary>
        public DateTime? LeftUtc { get; set; }

        /// <summary>中文：数据来源系统。English: Source system.</summary>
        public string SourceSystem { get; set; }

        /// <summary>中文：创建时间 UTC。English: Creation time in UTC.</summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>中文：创建人标识。English: Creator identifier.</summary>
        public string CreatedBy { get; set; }

        /// <summary>中文：最近更新时间 UTC。English: Last update time in UTC.</summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>中文：最近更新人标识。English: Last updater identifier.</summary>
        public string UpdatedBy { get; set; }

        /// <summary>中文：SQL Server 并发版本。English: SQL Server concurrency version.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
