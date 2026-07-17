using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：P6.2 企业用户资料扩展记录，与旧 <c>Portal_Users</c> 一对一关联。
    ///
    /// English: P6.2 enterprise user-profile extension row, associated one-to-one with legacy
    /// <c>Portal_Users</c>.
    /// </summary>
    /// <remarks>
    /// 中文：本表保存登录名、显示名、昵称、偏好邮箱和账号状态等非密码资料。密码、Cookie、Token、
    /// 证件号和手机号等敏感内容不得写入此表。
    ///
    /// English: This table stores non-password profile data such as login name, display name, nickname,
    /// preferred email, and account status. Passwords, cookies, tokens, identity numbers, phone numbers,
    /// and similar sensitive data must not be written here.
    /// </remarks>
    [Table("PortalBiz_UserProfiles")]
    public class UserProfileItem
    {
        /// <summary>
        /// 中文：旧门户用户标识，同时作为本扩展表主键。
        ///
        /// English: Legacy Portal user identifier, also the primary key of this extension table.
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// 中文：稳定登录名；迁移初始值来自旧 <c>Portal_Users.Name</c>。
        ///
        /// English: Stable login name; initial migration value comes from legacy <c>Portal_Users.Name</c>.
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 中文：正式显示名。
        ///
        /// English: Formal display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 中文：昵称或偏好称呼。
        ///
        /// English: Nickname or preferred name.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// 中文：偏好邮箱；非空时可作为登录标识。
        ///
        /// English: Preferred email; when non-empty it may be used as a sign-in identifier.
        /// </summary>
        public string PreferredEmail { get; set; }

        /// <summary>
        /// 中文：账号生命周期状态。
        ///
        /// English: Account lifecycle status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 中文：非敏感状态说明。
        ///
        /// English: Non-sensitive status reason.
        /// </summary>
        public string StatusReason { get; set; }

        /// <summary>
        /// 中文：创建时间 UTC。
        ///
        /// English: Creation time in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// 中文：创建人标识。
        ///
        /// English: Creator identifier.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// 中文：最近更新时间 UTC。
        ///
        /// English: Last update time in UTC.
        /// </summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>
        /// 中文：最近更新人标识。
        ///
        /// English: Last updater identifier.
        /// </summary>
        public string UpdatedBy { get; set; }

        /// <summary>
        /// 中文：SQL Server 并发版本。
        ///
        /// English: SQL Server concurrency version.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
