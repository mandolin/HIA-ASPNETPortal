using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 用户注册审核记录。
    /// User registration review record.
    /// </summary>
    [Table("PortalCfg_UserRegistrations")]
    public class UserRegistrationItem
    {
        /// <summary>
        /// 注册审核记录ID。
        /// Registration review record id.
        /// </summary>
        [Key]
        public int RegistrationId { get; set; }

        /// <summary>
        /// 用户ID。
        /// User id.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 审核状态。
        /// Review status.
        /// </summary>
        [StringLength(30)]
        public string Status { get; set; }

        /// <summary>
        /// 是否需要审核。
        /// Whether approval is required.
        /// </summary>
        public bool RequiresApproval { get; set; }

        /// <summary>
        /// 员工号。
        /// Employee code.
        /// </summary>
        [StringLength(100)]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// 注册邀请代码。
        /// Registration invitation code.
        /// </summary>
        [StringLength(64)]
        public string InviteCode { get; set; }

        /// <summary>
        /// 注册提交 UTC 时间。
        /// Registration submission UTC time.
        /// </summary>
        public DateTime RegisteredUtc { get; set; }

        /// <summary>
        /// 批准 UTC 时间。
        /// Approval UTC time.
        /// </summary>
        public DateTime? ApprovedUtc { get; set; }

        /// <summary>
        /// 批准人。
        /// Approval operator.
        /// </summary>
        [StringLength(100)]
        public string ApprovedBy { get; set; }

        /// <summary>
        /// 拒绝 UTC 时间。
        /// Rejection UTC time.
        /// </summary>
        public DateTime? RejectedUtc { get; set; }

        /// <summary>
        /// 拒绝人。
        /// Rejection operator.
        /// </summary>
        [StringLength(100)]
        public string RejectedBy { get; set; }

        /// <summary>
        /// 审核备注。
        /// Review note.
        /// </summary>
        [StringLength(500)]
        public string ReviewNote { get; set; }
    }
}
