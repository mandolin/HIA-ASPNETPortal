using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户用户的会话安全版本状态。
    ///
    /// English: Session security-version state for a Portal user.
    /// </summary>
    /// <remarks>
    /// 中文：安全版本写入 Forms Authentication 票据和角色 Cookie。密码重置、角色成员关系变化等高价值状态
    /// 变更会递增该版本，从而让旧票据在下一次请求时失效。
    ///
    /// English: The security version is written into both the Forms Authentication ticket and the role cookie.
    /// High-value state changes such as password reset or role-membership updates increment the version so older
    /// tickets become invalid on the next request.
    /// </remarks>
    [Table("Portal_UserSecurityStates")]
    public class UserSecurityStateItem
    {
        /// <summary>
        /// 中文：用户标识，同时也是本表主键。
        ///
        /// English: User identifier, also used as the table primary key.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        /// <summary>
        /// 中文：当前安全版本；初始值为 <c>1</c>。
        ///
        /// English: Current security version; the initial value is <c>1</c>.
        /// </summary>
        public long SecurityVersion { get; set; }

        /// <summary>
        /// 中文：版本最近变化 UTC 时间。
        ///
        /// English: UTC time of the latest version change.
        /// </summary>
        public DateTime ChangedUtc { get; set; }

        /// <summary>
        /// 中文：最近变化的非敏感原因。
        ///
        /// English: Non-sensitive reason for the latest change.
        /// </summary>
        public string ChangeReason { get; set; }

        /// <summary>
        /// 中文：SQL Server 并发版本列。
        ///
        /// English: SQL Server concurrency-version column.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
