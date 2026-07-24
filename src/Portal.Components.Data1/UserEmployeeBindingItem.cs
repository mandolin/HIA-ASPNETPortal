using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户账号与员工绑定 EF 记录。</zh-CN>
    ///   <en>Portal-user to employee binding EF row.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实体只表达绑定数据本身，不声明用户或员工导航属性，避免扩大旧用户实体跟踪范围。</zh-CN>
    ///   <en>This entity represents the binding row only and intentionally avoids user or employee navigation properties so legacy user tracking remains narrow.</en>
    /// </lang>
    /// </remarks>
    [Table("PortalBiz_UserEmployeeBindings")]
    public class UserEmployeeBindingItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定标识。</zh-CN>
        ///   <en>Binding identifier.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int BindingId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户账号标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定状态。</zh-CN>
        ///   <en>Binding status.</en>
        /// </lang>
        /// </summary>
        public string BindingStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定创建时间 UTC。</zh-CN>
        ///   <en>Binding creation time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime BoundUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定创建人标识。</zh-CN>
        ///   <en>Binding creator identifier.</en>
        /// </lang>
        /// </summary>
        public string BoundBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定结束时间 UTC。</zh-CN>
        ///   <en>Binding end time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime? EndedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定结束人标识。</zh-CN>
        ///   <en>Binding ending-operator identifier.</en>
        /// </lang>
        /// </summary>
        public string EndedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非敏感绑定说明。</zh-CN>
        ///   <en>Non-sensitive binding reason.</en>
        /// </lang>
        /// </summary>
        public string Reason { get; set; }

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
