using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户用户的会话安全版本状态。</zh-CN>
    ///   <en>Session security-version state for a Portal user.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>安全版本写入 Forms Authentication 票据和角色 Cookie。密码重置、角色成员关系变化等高价值状态变更会递增该版本，从而让旧票据在下一次请求时失效。</zh-CN>
    ///   <en>The security version is written into both the Forms Authentication ticket and the role cookie. High-value state changes such as password reset or role-membership updates increment the version so older tickets become invalid on the next request.</en>
    /// </lang>
    /// </remarks>
    [Table("Portal_UserSecurityStates")]
    public class UserSecurityStateItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用户标识，同时也是本表主键。</zh-CN>
        ///   <en>User identifier, also used as the table primary key.</en>
        /// </lang>
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前安全版本；初始值为 <c>1</c>。</zh-CN>
        ///   <en>Current security version; the initial value is <c>1</c>.</en>
        /// </lang>
        /// </summary>
        public long SecurityVersion { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>版本最近变化 UTC 时间。</zh-CN>
        ///   <en>UTC time of the latest version change.</en>
        /// </lang>
        /// </summary>
        public DateTime ChangedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近变化的非敏感原因。</zh-CN>
        ///   <en>Non-sensitive reason for the latest change.</en>
        /// </lang>
        /// </summary>
        public string ChangeReason { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>SQL Server 并发版本列。</zh-CN>
        ///   <en>SQL Server concurrency-version column.</en>
        /// </lang>
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
