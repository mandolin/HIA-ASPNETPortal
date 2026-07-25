using System;
using System.Security.Cryptography;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户 P5.2 强密码哈希辅助器。</zh-CN>
    ///   <en>Strong password-hash helper for Portal P5.2.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此类只在数据访问层内部使用。默认采用 PBKDF2-HMAC-SHA256，并把迭代次数写入每条凭据，便于之后按用户渐进提高成本参数。调用方不得记录输入密码、盐或哈希。</zh-CN>
    ///   <en>This helper is used only inside the data-access layer. It defaults to PBKDF2-HMAC-SHA256 and stores the iteration count on each credential, allowing later per-user cost upgrades. Callers must not log the submitted password, salt, or hash.</en>
    /// </lang>
    /// </remarks>
    internal static class PortalPasswordHasher
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前强哈希格式名称，作为数据库凭据记录中的持久化算法标识。</zh-CN>
        ///   <en>Current strong-hash format name, persisted as the algorithm identifier in database credential records.</en>
        /// </lang>
        /// </summary>
        internal const string Format = "PBKDF2-HMAC-SHA256";

        /// <summary>
        /// <lang>
        ///   <zh-CN>新凭据默认使用的 PBKDF2 迭代次数。</zh-CN>
        ///   <en>Default PBKDF2 iteration count used for new credentials.</en>
        /// </lang>
        /// </summary>
        internal const int DefaultIterationCount = 210000;

        /// <summary>
        /// <lang>
        ///   <zh-CN>随机盐长度，单位为字节。</zh-CN>
        ///   <en>Random salt length in bytes.</en>
        /// </lang>
        /// </summary>
        private const int SaltLength = 32;

        /// <summary>
        /// <lang>
        ///   <zh-CN>派生哈希长度，单位为字节。</zh-CN>
        ///   <en>Derived hash length in bytes.</en>
        /// </lang>
        /// </summary>
        private const int HashLength = 32;

        /// <summary>
        /// <lang>
        ///   <zh-CN>为明文密码创建新的强哈希记录。</zh-CN>
        ///   <en>Creates a new strong-hash record for a plain-text password.</en>
        /// </lang>
        /// </summary>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>仅在内存中短暂使用的明文密码。</zh-CN>
        ///   <en>The plain-text password used briefly in memory only.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>包含格式、迭代次数、随机盐和派生哈希的凭据哈希对象。</zh-CN>
        ///   <en>A credential-hash object containing the format, iteration count, random salt, and derived hash.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN><paramref name="password"/> 为空引用时抛出。</zh-CN>
        ///   <en>Thrown when <paramref name="password"/> is null.</en>
        /// </l>
        /// </exception>
        internal static PortalPasswordHash CreateHash(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            // <lang>
            //   <zh-CN>每次创建凭据都生成独立随机盐，避免相同密码产生相同哈希。</zh-CN>
            //   <en>Generate an independent random salt for each credential so identical passwords do not produce identical hashes.</en>
            // </lang>
            byte[] salt = new byte[SaltLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return new PortalPasswordHash(
                Format,
                DefaultIterationCount,
                salt,
                DeriveHash(password, salt, DefaultIterationCount));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证明文密码是否匹配已保存的强哈希凭据。</zh-CN>
        ///   <en>Verifies whether a plain-text password matches a saved strong-hash credential.</en>
        /// </lang>
        /// </summary>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>用户提交的明文密码。</zh-CN>
        ///   <en>The plain-text password submitted by the user.</en>
        /// </l>
        /// </param>
        /// <param name="passwordFormat">
        /// <l>
        ///   <zh-CN>凭据记录中的算法格式标识。</zh-CN>
        ///   <en>The algorithm-format identifier stored on the credential record.</en>
        /// </l>
        /// </param>
        /// <param name="passwordSalt">
        /// <l>
        ///   <zh-CN>凭据记录中的随机盐。</zh-CN>
        ///   <en>The random salt stored on the credential record.</en>
        /// </l>
        /// </param>
        /// <param name="expectedHash">
        /// <l>
        ///   <zh-CN>凭据记录中的预期哈希。</zh-CN>
        ///   <en>The expected hash stored on the credential record.</en>
        /// </l>
        /// </param>
        /// <param name="iterationCount">
        /// <l>
        ///   <zh-CN>凭据记录中的 PBKDF2 迭代次数。</zh-CN>
        ///   <en>The PBKDF2 iteration count stored on the credential record.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>输入完整、格式匹配且哈希固定时间比较成功时返回 <c>true</c>。</zh-CN>
        ///   <en>Returns <c>true</c> when inputs are complete, the format matches, and fixed-time hash comparison succeeds.</en>
        /// </l>
        /// </returns>
        internal static bool Verify(
            string password,
            string passwordFormat,
            byte[] passwordSalt,
            byte[] expectedHash,
            int iterationCount)
        {
            if (password == null ||
                !string.Equals(passwordFormat, Format, StringComparison.Ordinal) ||
                passwordSalt == null ||
                expectedHash == null ||
                iterationCount <= 0)
            {
                // <lang>
                //   <zh-CN>凭据字段缺失或算法不匹配时直接失败，不尝试兼容解释，避免把损坏数据误判为有效密码。</zh-CN>
                //   <en>When credential fields are missing or the algorithm does not match, fail directly instead of attempting compatibility interpretation so damaged data is not accepted as a valid password.</en>
                // </lang>
                return false;
            }

            byte[] actualHash = DeriveHash(password, passwordSalt, iterationCount);
            return FixedTimeEquals(actualHash, expectedHash);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按指定盐和迭代次数派生 PBKDF2-HMAC-SHA256 哈希。</zh-CN>
        ///   <en>Derives a PBKDF2-HMAC-SHA256 hash with the specified salt and iteration count.</en>
        /// </lang>
        /// </summary>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>仅在内存中短暂使用的明文密码。</zh-CN>
        ///   <en>The plain-text password used briefly in memory only.</en>
        /// </l>
        /// </param>
        /// <param name="salt">
        /// <l>
        ///   <zh-CN>随机盐。</zh-CN>
        ///   <en>The random salt.</en>
        /// </l>
        /// </param>
        /// <param name="iterationCount">
        /// <l>
        ///   <zh-CN>PBKDF2 迭代次数。</zh-CN>
        ///   <en>The PBKDF2 iteration count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>固定长度的派生哈希字节数组。</zh-CN>
        ///   <en>The fixed-length derived hash byte array.</en>
        /// </l>
        /// </returns>
        private static byte[] DeriveHash(string password, byte[] salt, int iterationCount)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterationCount, HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(HashLength);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用固定时间策略比较两个哈希字节数组。</zh-CN>
        ///   <en>Compares two hash byte arrays using a fixed-time strategy.</en>
        /// </lang>
        /// </summary>
        /// <param name="left">
        /// <l>
        ///   <zh-CN>左侧哈希。</zh-CN>
        ///   <en>The left hash.</en>
        /// </l>
        /// </param>
        /// <param name="right">
        /// <l>
        ///   <zh-CN>右侧哈希。</zh-CN>
        ///   <en>The right hash.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>两个数组长度和值都一致时返回 <c>true</c>。</zh-CN>
        ///   <en>Returns <c>true</c> when both arrays have identical length and values.</en>
        /// </l>
        /// </returns>
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            int difference = left.Length ^ right.Length;
            int count = Math.Min(left.Length, right.Length);
            // <lang>
            //   <zh-CN>即使长度不同，也比较共同长度内的所有字节，减少基于首个差异位置的时间侧信道。</zh-CN>
            //   <en>Even when lengths differ, compare every byte in the shared length to reduce timing side channels based on the first differing position.</en>
            // </lang>
            for (int i = 0; i < count; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>强密码哈希的内部传输对象。</zh-CN>
    ///   <en>Internal transfer object for a strong password hash.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象只在数据访问层保存凭据字段时短暂使用；不要把它写入诊断日志、审计摘要或页面输出。</zh-CN>
    ///   <en>This object is used briefly while the data-access layer stores credential fields; do not write it to diagnostic logs, audit summaries, or page output.</en>
    /// </lang>
    /// </remarks>
    internal sealed class PortalPasswordHash
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化强密码哈希传输对象。</zh-CN>
        ///   <en>Initializes a strong password-hash transfer object.</en>
        /// </lang>
        /// </summary>
        /// <param name="format">
        /// <l>
        ///   <zh-CN>算法格式标识。</zh-CN>
        ///   <en>The algorithm-format identifier.</en>
        /// </l>
        /// </param>
        /// <param name="iterationCount">
        /// <l>
        ///   <zh-CN>PBKDF2 迭代次数。</zh-CN>
        ///   <en>The PBKDF2 iteration count.</en>
        /// </l>
        /// </param>
        /// <param name="salt">
        /// <l>
        ///   <zh-CN>随机盐。</zh-CN>
        ///   <en>The random salt.</en>
        /// </l>
        /// </param>
        /// <param name="hash">
        /// <l>
        ///   <zh-CN>派生哈希。</zh-CN>
        ///   <en>The derived hash.</en>
        /// </l>
        /// </param>
        internal PortalPasswordHash(string format, int iterationCount, byte[] salt, byte[] hash)
        {
            Format = format;
            IterationCount = iterationCount;
            Salt = salt;
            Hash = hash;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取算法格式标识。</zh-CN>
        ///   <en>Gets the algorithm-format identifier.</en>
        /// </lang>
        /// </summary>
        internal string Format { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取 PBKDF2 迭代次数。</zh-CN>
        ///   <en>Gets the PBKDF2 iteration count.</en>
        /// </lang>
        /// </summary>
        internal int IterationCount { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取随机盐。</zh-CN>
        ///   <en>Gets the random salt.</en>
        /// </lang>
        /// </summary>
        internal byte[] Salt { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取派生哈希。</zh-CN>
        ///   <en>Gets the derived hash.</en>
        /// </lang>
        /// </summary>
        internal byte[] Hash { get; private set; }
    }
}
