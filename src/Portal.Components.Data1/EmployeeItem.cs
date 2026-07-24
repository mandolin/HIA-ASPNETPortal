using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>P6.3 员工主数据 EF 记录。</zh-CN>
    ///   <en>P6.3 employee master-data EF row.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实体仅保存门户业务所需最小字段，不存手机号、身份证号等高敏个人信息。</zh-CN>
    ///   <en>This entity stores only minimal fields required by Portal business flows and excludes highly sensitive personal data such as phone numbers or government identifiers.</en>
    /// </lang>
    /// </remarks>
    [Table("PortalBiz_Employees")]
    public class EmployeeItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int EmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>全局唯一员工号。</zh-CN>
        ///   <en>Globally unique employee code.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工正式显示名。</zh-CN>
        ///   <en>Formal employee display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好称呼或昵称。</zh-CN>
        ///   <en>Preferred name or nickname.</en>
        /// </lang>
        /// </summary>
        public string PreferredName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>工作邮箱。</zh-CN>
        ///   <en>Work email address.</en>
        /// </lang>
        /// </summary>
        public string WorkEmail { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>所属组织单元标识。</zh-CN>
        ///   <en>Owning organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        public int? OrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工生命周期状态。</zh-CN>
        ///   <en>Employee lifecycle status.</en>
        /// </lang>
        /// </summary>
        public string EmploymentStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入职时间 UTC。</zh-CN>
        ///   <en>Joined time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime? JoinedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>离职时间 UTC。</zh-CN>
        ///   <en>Left time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime? LeftUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据来源系统。</zh-CN>
        ///   <en>Source system.</en>
        /// </lang>
        /// </summary>
        public string SourceSystem { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建时间 UTC。</zh-CN>
        ///   <en>Creation time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建人标识。</zh-CN>
        ///   <en>Creator identifier.</en>
        /// </lang>
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新时间 UTC。</zh-CN>
        ///   <en>Last update time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新人标识。</zh-CN>
        ///   <en>Last updater identifier.</en>
        /// </lang>
        /// </summary>
        public string UpdatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>SQL Server 并发版本。</zh-CN>
        ///   <en>SQL Server concurrency version.</en>
        /// </lang>
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
