using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户用户的强哈希凭据记录。</zh-CN>
    ///   <en>Strong-hash credential record for a Portal user.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本表与旧 <c>Portal_Users.Password</c> 并行存在，作为 P5.2 渐进迁移的正式凭据来源。 旧字段只用于尚未升级用户的首次兼容验证；一旦本表存在记录，登录不得回退到旧 MD5 字段。</zh-CN>
    ///   <en>This table coexists with the legacy <c>Portal_Users.Password</c> column as the official credential source for the P5.2 gradual migration. The legacy column is used only for the first compatibility check of users that have not yet been upgraded; once this record exists, sign-in must not fall back to legacy MD5.</en>
    /// </lang>
    /// </remarks>
    [Table("Portal_UserCredentials")]
    public class UserCredentialItem
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
        ///   <zh-CN>凭据版本；当前初始版本为 <c>1</c>。</zh-CN>
        ///   <en>Credential version; the initial current version is <c>1</c>.</en>
        /// </lang>
        /// </summary>
        public int CredentialVersion { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码哈希格式标识，如 <c>PBKDF2-HMAC-SHA256</c>。</zh-CN>
        ///   <en>Password-hash format identifier, such as <c>PBKDF2-HMAC-SHA256</c>.</en>
        /// </lang>
        /// </summary>
        public string PasswordFormat { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>密码哈希二进制值，绝不能写入日志或审计摘要。</zh-CN>
        ///   <en>Binary password hash value, which must never be written to logs or audit summaries.</en>
        /// </lang>
        /// </summary>
        public byte[] PasswordHash { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>每个用户独立生成的随机盐，绝不能回显给用户。</zh-CN>
        ///   <en>Per-user random salt, which must never be echoed to users.</en>
        /// </lang>
        /// </summary>
        public byte[] PasswordSalt { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成当前哈希时使用的迭代次数。</zh-CN>
        ///   <en>Iteration count used to generate the current hash.</en>
        /// </lang>
        /// </summary>
        public int IterationCount { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>凭据创建 UTC 时间。</zh-CN>
        ///   <en>UTC time when the credential was created.</en>
        /// </lang>
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>凭据最近更新 UTC 时间。</zh-CN>
        ///   <en>UTC time when the credential was last updated.</en>
        /// </lang>
        /// </summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近一次成功验证 UTC 时间；可为空。</zh-CN>
        ///   <en>UTC time of the last successful verification; optional.</en>
        /// </lang>
        /// </summary>
        public DateTime? LastVerifiedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧 MD5 凭据成功迁移为强哈希的 UTC 时间；新建凭据可为空。</zh-CN>
        ///   <en>UTC time when a legacy MD5 credential was upgraded to a strong hash; new credentials may leave it empty.</en>
        /// </lang>
        /// </summary>
        public DateTime? LegacyUpgradedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否要求用户或管理员先重置凭据。</zh-CN>
        ///   <en>Whether the user or an administrator must reset the credential before sign-in.</en>
        /// </lang>
        /// </summary>
        public bool RequiresReset { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>要求重置的非敏感原因。</zh-CN>
        ///   <en>Non-sensitive reason for requiring a reset.</en>
        /// </lang>
        /// </summary>
        public string ResetReason { get; set; }

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
