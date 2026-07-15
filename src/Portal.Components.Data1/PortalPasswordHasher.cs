using System;
using System.Security.Cryptography;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户 P5.2 强密码哈希辅助器。
    ///
    /// English: Strong password-hash helper for Portal P5.2.
    /// </summary>
    /// <remarks>
    /// 中文：此类只在数据访问层内部使用。默认采用 PBKDF2-HMAC-SHA256，并把迭代次数写入每条凭据，
    /// 便于后续按用户渐进提高成本参数。调用方不得记录输入密码、盐或哈希。
    ///
    /// English: This helper is used only inside the data-access layer. It defaults to PBKDF2-HMAC-SHA256 and stores
    /// the iteration count on each credential, allowing later per-user cost upgrades. Callers must not log the
    /// submitted password, salt, or hash.
    /// </remarks>
    internal static class PortalPasswordHasher
    {
        internal const string Format = "PBKDF2-HMAC-SHA256";
        internal const int DefaultIterationCount = 210000;
        private const int SaltLength = 32;
        private const int HashLength = 32;

        internal static PortalPasswordHash CreateHash(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

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
                return false;
            }

            byte[] actualHash = DeriveHash(password, passwordSalt, iterationCount);
            return FixedTimeEquals(actualHash, expectedHash);
        }

        private static byte[] DeriveHash(string password, byte[] salt, int iterationCount)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterationCount, HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(HashLength);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            int difference = left.Length ^ right.Length;
            int count = Math.Min(left.Length, right.Length);
            for (int i = 0; i < count; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }

    internal sealed class PortalPasswordHash
    {
        internal PortalPasswordHash(string format, int iterationCount, byte[] salt, byte[] hash)
        {
            Format = format;
            IterationCount = iterationCount;
            Salt = salt;
            Hash = hash;
        }

        internal string Format { get; private set; }

        internal int IterationCount { get; private set; }

        internal byte[] Salt { get; private set; }

        internal byte[] Hash { get; private set; }
    }
}
