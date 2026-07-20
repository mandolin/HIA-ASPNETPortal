using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：登录密码前端加密的服务端 RSA 支撑工具。
    ///
    /// English: Server-side RSA support for client-side login-password encryption.
    /// </summary>
    /// <remarks>
    /// 中文：P10.3 第一版固定使用 2048 位一次性密钥；后续会按客户端浏览器环境选择加密强度。
    /// 私钥只保存在当前 Session 中，并在登录提交时一次性消费，不写入日志、数据库或页面。
    ///
    /// English: The first P10.3 version uses a fixed 2048-bit one-time key; later work will select encryption
    /// strength by client browser capability. The private key stays only in the current Session and is consumed
    /// once by the login post. It is never written to logs, the database, or the page.
    /// </remarks>
    public static class PortalLoginPasswordCrypto
    {
        /// <summary>
        /// 中文：当前默认 RSA 密钥位数。
        ///
        /// English: Current default RSA key size.
        /// </summary>
        public const int DefaultKeySizeBits = 2048;

        private const string PrivateKeySessionKey = "Portal.Security.LoginPassword.PrivateKeyXml";
        private const string IssuedUtcSessionKey = "Portal.Security.LoginPassword.IssuedUtc";
        private static readonly TimeSpan KeyLifetime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 中文：读取登录密码提交是否必须使用前端加密。
        ///
        /// English: Reads whether login-password submission must use client-side encryption.
        /// </summary>
        /// <returns>中文：必须使用加密提交时为 <c>true</c>。English: <c>true</c> when encrypted submission is required.</returns>
        public static bool IsEncryptedSubmissionRequired()
        {
            return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.RequireEncryptedLoginPassword);
        }

        /// <summary>
        /// 中文：为当前 Session 签发一个登录密码一次性公钥。
        ///
        /// English: Issues a one-time login-password public key for the current Session.
        /// </summary>
        /// <param name="context">中文：当前 HTTP 上下文，必须带 Session。English: Current HTTP context; Session is required.</param>
        /// <returns>中文：PEM 公钥和密钥位数。English: PEM public key and key size.</returns>
        /// <exception cref="ArgumentNullException">中文：<paramref name="context"/> 为 <c>null</c> 时引发。English: Thrown when <paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">中文：当前请求没有 Session 时引发。English: Thrown when the current request has no Session.</exception>
        public static PortalLoginPasswordPublicKey IssueLoginPasswordKey(HttpContext context)
        {
            EnsureSession(context);

            using (var rsa = new RSACryptoServiceProvider(DefaultKeySizeBits))
            {
                RSAParameters publicParameters = rsa.ExportParameters(false);
                context.Session[PrivateKeySessionKey] = rsa.ToXmlString(true);
                context.Session[IssuedUtcSessionKey] = DateTime.UtcNow;

                return new PortalLoginPasswordPublicKey(
                    ExportSubjectPublicKeyInfoPem(publicParameters),
                    DefaultKeySizeBits);
            }
        }

        /// <summary>
        /// 中文：消费当前 Session 的一次性私钥并解密登录密码密文。
        ///
        /// English: Consumes the current Session's one-time private key and decrypts the encrypted login password.
        /// </summary>
        /// <param name="context">中文：当前 HTTP 上下文，必须带 Session。English: Current HTTP context; Session is required.</param>
        /// <param name="encryptedPassword">中文：客户端提交的 Base64 RSA 密文。English: Base64 RSA ciphertext submitted by the client.</param>
        /// <param name="password">中文：解密成功时返回当前请求内使用的明文密码。English: Plain password for this request when decryption succeeds.</param>
        /// <param name="failureCode">中文：失败分类，不包含敏感值。English: Failure category without sensitive values.</param>
        /// <param name="eventId">中文：诊断事件编号。English: Diagnostics event id.</param>
        /// <returns>中文：解密成功时为 <c>true</c>。English: <c>true</c> when decryption succeeds.</returns>
        public static bool TryDecryptSubmittedPassword(
            HttpContext context,
            string encryptedPassword,
            out string password,
            out string failureCode,
            out string eventId)
        {
            password = string.Empty;
            failureCode = string.Empty;
            eventId = string.Empty;

            if (context == null || context.Session == null)
            {
                failureCode = "NoSession";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted login password could not be decrypted because Session is unavailable.",
                    context);
                return false;
            }

            string trimmedEncryptedPassword = encryptedPassword == null ? string.Empty : encryptedPassword.Trim();
            if (trimmedEncryptedPassword.Length == 0)
            {
                failureCode = "MissingCiphertext";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted login password was missing from the submitted form.",
                    context);
                return false;
            }

            string privateKeyXml = context.Session[PrivateKeySessionKey] as string;
            object issuedUtcValue = context.Session[IssuedUtcSessionKey];
            ClearLoginPasswordKey(context);

            if (string.IsNullOrWhiteSpace(privateKeyXml))
            {
                failureCode = "MissingPrivateKey";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted login password could not be decrypted because the one-time private key was missing.",
                    context);
                return false;
            }

            if (!(issuedUtcValue is DateTime) || DateTime.UtcNow - (DateTime)issuedUtcValue > KeyLifetime)
            {
                failureCode = "ExpiredPrivateKey";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted login password could not be decrypted because the one-time private key expired.",
                    context);
                return false;
            }

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(trimmedEncryptedPassword);
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(privateKeyXml);
                    byte[] plainBytes = rsa.Decrypt(cipherBytes, false);
                    password = Encoding.UTF8.GetString(plainBytes);
                }

                return true;
            }
            catch (FormatException exception)
            {
                failureCode = "InvalidCiphertext";
                eventId = PortalDiagnostics.Error(
                    "LoginPasswordEncryption",
                    "Encrypted login password was not valid Base64.",
                    exception,
                    context);
                return false;
            }
            catch (CryptographicException exception)
            {
                failureCode = "DecryptFailed";
                eventId = PortalDiagnostics.Error(
                    "LoginPasswordEncryption",
                    "Encrypted login password RSA decryption failed.",
                    exception,
                    context);
                return false;
            }
        }

        private static void EnsureSession(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (context.Session == null)
            {
                throw new InvalidOperationException("The current request does not have a Session.");
            }
        }

        private static void ClearLoginPasswordKey(HttpContext context)
        {
            context.Session.Remove(PrivateKeySessionKey);
            context.Session.Remove(IssuedUtcSessionKey);
        }

        private static string ExportSubjectPublicKeyInfoPem(RSAParameters parameters)
        {
            byte[] publicKeyDer = EncodeSubjectPublicKeyInfo(parameters);
            string base64 = Convert.ToBase64String(publicKeyDer);
            var builder = new StringBuilder();
            builder.AppendLine("-----BEGIN PUBLIC KEY-----");

            for (int index = 0; index < base64.Length; index += 64)
            {
                int length = Math.Min(64, base64.Length - index);
                builder.AppendLine(base64.Substring(index, length));
            }

            builder.AppendLine("-----END PUBLIC KEY-----");
            return builder.ToString();
        }

        private static byte[] EncodeSubjectPublicKeyInfo(RSAParameters parameters)
        {
            byte[] rsaPublicKey = EncodeSequence(
                Concat(
                    EncodeInteger(parameters.Modulus),
                    EncodeInteger(parameters.Exponent)));

            byte[] algorithmIdentifier = EncodeSequence(
                Concat(
                    new byte[] { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 },
                    new byte[] { 0x05, 0x00 }));

            return EncodeSequence(
                Concat(
                    algorithmIdentifier,
                    EncodeBitString(rsaPublicKey)));
        }

        private static byte[] EncodeInteger(byte[] value)
        {
            byte[] normalizedValue = TrimLeadingZeroBytes(value);
            bool mustPrefixZero = normalizedValue.Length > 0 && (normalizedValue[0] & 0x80) != 0;
            byte[] integerBytes = new byte[normalizedValue.Length + (mustPrefixZero ? 1 : 0)];

            if (mustPrefixZero)
            {
                Buffer.BlockCopy(normalizedValue, 0, integerBytes, 1, normalizedValue.Length);
            }
            else
            {
                Buffer.BlockCopy(normalizedValue, 0, integerBytes, 0, normalizedValue.Length);
            }

            return EncodeTag(0x02, integerBytes);
        }

        private static byte[] EncodeBitString(byte[] value)
        {
            byte[] bitStringValue = new byte[value.Length + 1];
            Buffer.BlockCopy(value, 0, bitStringValue, 1, value.Length);
            return EncodeTag(0x03, bitStringValue);
        }

        private static byte[] EncodeSequence(byte[] value)
        {
            return EncodeTag(0x30, value);
        }

        private static byte[] EncodeTag(byte tag, byte[] value)
        {
            byte[] length = EncodeLength(value.Length);
            byte[] encoded = new byte[1 + length.Length + value.Length];
            encoded[0] = tag;
            Buffer.BlockCopy(length, 0, encoded, 1, length.Length);
            Buffer.BlockCopy(value, 0, encoded, 1 + length.Length, value.Length);
            return encoded;
        }

        private static byte[] EncodeLength(int length)
        {
            if (length < 128)
            {
                return new byte[] { (byte)length };
            }

            int tempLength = length;
            int byteCount = 0;
            while (tempLength > 0)
            {
                byteCount++;
                tempLength >>= 8;
            }

            byte[] encoded = new byte[byteCount + 1];
            encoded[0] = (byte)(0x80 | byteCount);

            for (int index = byteCount; index > 0; index--)
            {
                encoded[index] = (byte)(length & 0xFF);
                length >>= 8;
            }

            return encoded;
        }

        private static byte[] TrimLeadingZeroBytes(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return new byte[] { 0 };
            }

            int index = 0;
            while (index < value.Length - 1 && value[index] == 0)
            {
                index++;
            }

            byte[] normalized = new byte[value.Length - index];
            Buffer.BlockCopy(value, index, normalized, 0, normalized.Length);
            return normalized;
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            byte[] result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }
    }

    /// <summary>
    /// 中文：登录密码加密公钥响应模型。
    ///
    /// English: Response model for a login-password encryption public key.
    /// </summary>
    public sealed class PortalLoginPasswordPublicKey
    {
        /// <summary>
        /// 中文：创建公钥响应模型。
        ///
        /// English: Creates a public-key response model.
        /// </summary>
        /// <param name="publicKeyPem">中文：SubjectPublicKeyInfo PEM 公钥。English: SubjectPublicKeyInfo PEM public key.</param>
        /// <param name="keySizeBits">中文：RSA 密钥位数。English: RSA key size in bits.</param>
        public PortalLoginPasswordPublicKey(string publicKeyPem, int keySizeBits)
        {
            PublicKeyPem = publicKeyPem ?? string.Empty;
            KeySizeBits = keySizeBits;
        }

        /// <summary>
        /// 中文：PEM 格式公钥。
        ///
        /// English: PEM-format public key.
        /// </summary>
        public string PublicKeyPem { get; private set; }

        /// <summary>
        /// 中文：密钥位数。
        ///
        /// English: Key size in bits.
        /// </summary>
        public int KeySizeBits { get; private set; }
    }
}
