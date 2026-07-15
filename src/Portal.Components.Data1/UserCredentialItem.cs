using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户用户的强哈希凭据记录。
    ///
    /// English: Strong-hash credential record for a Portal user.
    /// </summary>
    /// <remarks>
    /// 中文：本表与旧 <c>Portal_Users.Password</c> 并行存在，作为 P5.2 渐进迁移的正式凭据来源。
    /// 旧字段只用于尚未升级用户的首次兼容验证；一旦本表存在记录，登录不得回退到旧 MD5 字段。
    ///
    /// English: This table coexists with the legacy <c>Portal_Users.Password</c> column as the official credential
    /// source for the P5.2 gradual migration. The legacy column is used only for the first compatibility check of
    /// users that have not yet been upgraded; once this record exists, sign-in must not fall back to legacy MD5.
    /// </remarks>
    [Table("Portal_UserCredentials")]
    public class UserCredentialItem
    {
        /// <summary>
        /// 中文：用户标识，同时也是本表主键。
        ///
        /// English: User identifier, also used as the table primary key.
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// 中文：凭据版本；当前初始版本为 <c>1</c>。
        ///
        /// English: Credential version; the initial current version is <c>1</c>.
        /// </summary>
        public int CredentialVersion { get; set; }

        /// <summary>
        /// 中文：密码哈希格式标识，如 <c>PBKDF2-HMAC-SHA256</c>。
        ///
        /// English: Password-hash format identifier, such as <c>PBKDF2-HMAC-SHA256</c>.
        /// </summary>
        public string PasswordFormat { get; set; }

        /// <summary>
        /// 中文：密码哈希二进制值，绝不能写入日志或审计摘要。
        ///
        /// English: Binary password hash value, which must never be written to logs or audit summaries.
        /// </summary>
        public byte[] PasswordHash { get; set; }

        /// <summary>
        /// 中文：每个用户独立生成的随机盐，绝不能回显给用户。
        ///
        /// English: Per-user random salt, which must never be echoed to users.
        /// </summary>
        public byte[] PasswordSalt { get; set; }

        /// <summary>
        /// 中文：生成当前哈希时使用的迭代次数。
        ///
        /// English: Iteration count used to generate the current hash.
        /// </summary>
        public int IterationCount { get; set; }

        /// <summary>
        /// 中文：凭据创建 UTC 时间。
        ///
        /// English: UTC time when the credential was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// 中文：凭据最近更新 UTC 时间。
        ///
        /// English: UTC time when the credential was last updated.
        /// </summary>
        public DateTime UpdatedUtc { get; set; }

        /// <summary>
        /// 中文：最近一次成功验证 UTC 时间；可为空。
        ///
        /// English: UTC time of the last successful verification; optional.
        /// </summary>
        public DateTime? LastVerifiedUtc { get; set; }

        /// <summary>
        /// 中文：旧 MD5 凭据成功迁移为强哈希的 UTC 时间；新建凭据可为空。
        ///
        /// English: UTC time when a legacy MD5 credential was upgraded to a strong hash; new credentials may leave it empty.
        /// </summary>
        public DateTime? LegacyUpgradedUtc { get; set; }

        /// <summary>
        /// 中文：是否要求用户或管理员先重置凭据。
        ///
        /// English: Whether the user or an administrator must reset the credential before sign-in.
        /// </summary>
        public bool RequiresReset { get; set; }

        /// <summary>
        /// 中文：要求重置的非敏感原因。
        ///
        /// English: Non-sensitive reason for requiring a reset.
        /// </summary>
        public string ResetReason { get; set; }

        /// <summary>
        /// 中文：SQL Server 并发版本列。
        ///
        /// English: SQL Server concurrency-version column.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
