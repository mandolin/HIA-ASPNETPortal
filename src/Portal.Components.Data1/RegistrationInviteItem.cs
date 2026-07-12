using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 临时注册链接记录。
    /// Temporary registration invitation link record.
    /// </summary>
    [Table("PortalCfg_RegistrationInvites")]
    public class RegistrationInviteItem
    {
        /// <summary>
        /// 注册邀请代码。
        /// Registration invitation code.
        /// </summary>
        [Key]
        [StringLength(64)]
        public string InviteCode { get; set; }

        /// <summary>
        /// 邀请说明。
        /// Invitation description.
        /// </summary>
        [StringLength(200)]
        public string Description { get; set; }

        /// <summary>
        /// 过期 UTC 时间。
        /// Expiration UTC time.
        /// </summary>
        public DateTime ExpiresUtc { get; set; }

        /// <summary>
        /// 最大使用次数；空值表示不限。
        /// Maximum uses; null means unlimited.
        /// </summary>
        public int? MaxUses { get; set; }

        /// <summary>
        /// 已使用次数。
        /// Used count.
        /// </summary>
        public int UsedCount { get; set; }

        /// <summary>
        /// 是否启用。
        /// Whether the invite is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 是否要求填写员工号。
        /// Whether employee code is required.
        /// </summary>
        public bool RequireEmployeeCode { get; set; }

        /// <summary>
        /// 创建人。
        /// Creator.
        /// </summary>
        [StringLength(100)]
        public string CreatedBy { get; set; }

        /// <summary>
        /// 创建 UTC 时间。
        /// Creation UTC time.
        /// </summary>
        public DateTime CreatedUtc { get; set; }
    }
}
