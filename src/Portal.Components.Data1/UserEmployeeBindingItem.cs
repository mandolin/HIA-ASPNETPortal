using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户账号与员工绑定 EF 记录。
    ///
    /// English: Portal-user to employee binding EF row.
    /// </summary>
    /// <remarks>
    /// 中文：本实体只表达绑定数据本身，不声明用户或员工导航属性，避免扩大旧用户实体跟踪范围。
    ///
    /// English: This entity represents the binding row only and intentionally avoids user or employee navigation
    /// properties so legacy user tracking remains narrow.
    /// </remarks>
    [Table("PortalBiz_UserEmployeeBindings")]
    public class UserEmployeeBindingItem
    {
        /// <summary>中文：绑定标识。English: Binding identifier.</summary>
        [Key]
        public int BindingId { get; set; }

        /// <summary>中文：门户账号标识。English: Portal user identifier.</summary>
        public int UserId { get; set; }

        /// <summary>中文：员工标识。English: Employee identifier.</summary>
        public int EmployeeId { get; set; }

        /// <summary>中文：绑定状态。English: Binding status.</summary>
        public string BindingStatus { get; set; }

        /// <summary>中文：绑定创建时间 UTC。English: Binding creation time in UTC.</summary>
        public DateTime BoundUtc { get; set; }

        /// <summary>中文：绑定创建人标识。English: Binding creator identifier.</summary>
        public string BoundBy { get; set; }

        /// <summary>中文：绑定结束时间 UTC。English: Binding end time in UTC.</summary>
        public DateTime? EndedUtc { get; set; }

        /// <summary>中文：绑定结束人标识。English: Binding ending-operator identifier.</summary>
        public string EndedBy { get; set; }

        /// <summary>中文：非敏感绑定说明。English: Non-sensitive binding reason.</summary>
        public string Reason { get; set; }

        /// <summary>中文：最近更新时间 UTC。English: Last update time in UTC.</summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>中文：最近更新人标识。English: Last updater identifier.</summary>
        public string UpdatedBy { get; set; }

        /// <summary>中文：SQL Server 并发版本。English: SQL Server concurrency version.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
