using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>P6.2 企业用户资料扩展记录，与旧 <c>Portal_Users</c> 一对一关联。</zh-CN>
    ///   <en>P6.2 enterprise user-profile extension row, associated one-to-one with legacy <c>Portal_Users</c>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本表保存登录名、显示名、昵称、偏好邮箱和账号状态等非密码资料。密码、Cookie、Token、 证件号和手机号等敏感内容不得写入此表。</zh-CN>
    ///   <en>This table stores non-password profile data such as login name, display name, nickname, preferred email, and account status. Passwords, cookies, tokens, identity numbers, phone numbers, and similar sensitive data must not be written here.</en>
    /// </lang>
    /// </remarks>
    [Table("PortalBiz_UserProfiles")]
    public class UserProfileItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户用户标识，同时作为本扩展表主键。</zh-CN>
        ///   <en>Legacy Portal user identifier, also the primary key of this extension table.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>稳定登录名；迁移初始值来自旧 <c>Portal_Users.Name</c>。</zh-CN>
        ///   <en>Stable login name; initial migration value comes from legacy <c>Portal_Users.Name</c>.</en>
        /// </lang>
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>正式显示名。</zh-CN>
        ///   <en>Formal display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>昵称或偏好称呼。</zh-CN>
        ///   <en>Nickname or preferred name.</en>
        /// </lang>
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好邮箱；非空时可作为登录标识。</zh-CN>
        ///   <en>Preferred email; when non-empty it may be used as a sign-in identifier.</en>
        /// </lang>
        /// </summary>
        public string PreferredEmail { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号生命周期状态。</zh-CN>
        ///   <en>Account lifecycle status.</en>
        /// </lang>
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非敏感状态说明。</zh-CN>
        ///   <en>Non-sensitive status reason.</en>
        /// </lang>
        /// </summary>
        public string StatusReason { get; set; }

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
