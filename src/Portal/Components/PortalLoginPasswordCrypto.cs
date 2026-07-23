using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>登录/口令提交前端加密的服务端 RSA 支撑工具。</zh-CN>
    ///   <en>Server-side RSA support for client-side login/password-submission encryption.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P10.3 第一版固定使用 2048 位一次性密钥；后续会按客户端浏览器环境选择加密强度。 私钥只保存在当前 Session 中，并在口令提交时一次性消费，不写入日志、数据库或页面。</zh-CN>
    ///   <en>The first P10.3 version uses a fixed 2048-bit one-time key; later work will select encryption strength by client browser capability. The private key stays only in the current Session and is consumed once by the password post. It is never written to logs, the database, or the page.</en>
    /// </lang>
    /// </remarks>
    public static class PortalLoginPasswordCrypto
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前默认 RSA 密钥位数。</zh-CN>
        ///   <en>Current default RSA key size.</en>
        /// </lang>
        /// </summary>
        public const int DefaultKeySizeBits = 2048;

        private const string PrivateKeySessionKey = "Portal.Security.LoginPassword.PrivateKeyXml";
        private const string IssuedUtcSessionKey = "Portal.Security.LoginPassword.IssuedUtc";
        private static readonly TimeSpan KeyLifetime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取登录密码提交是否必须使用前端加密。</zh-CN>
        ///   <en>Reads whether login-password submission must use client-side encryption.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>必须使用加密提交时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when encrypted submission is required.</en>
        /// </l>
        /// </returns>
        public static bool IsEncryptedSubmissionRequired()
        {
            return PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.RequireEncryptedLoginPassword);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为当前 Session 签发一个登录密码一次性公钥。</zh-CN>
        ///   <en>Issues a one-time login-password public key for the current Session.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文，必须带 Session。</zh-CN>
        ///   <en>Current HTTP context; Session is required.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>PEM 公钥和密钥位数。</zh-CN>
        ///   <en>PEM public key and key size.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN><paramref name="context"/> 为 <c>null</c> 时引发。</zh-CN>
        ///   <en>Thrown when <paramref name="context"/> is <c>null</c>.</en>
        /// </l>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>当前请求没有 Session 时引发。</zh-CN>
        ///   <en>Thrown when the current request has no Session.</en>
        /// </l>
        /// </exception>
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
        /// <lang>
        ///   <zh-CN>消费当前 Session 的一次性私钥并解密登录密码密文。</zh-CN>
        ///   <en>Consumes the current Session's one-time private key and decrypts the encrypted login password.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文，必须带 Session。</zh-CN>
        ///   <en>Current HTTP context; Session is required.</en>
        /// </l>
        /// </param>
        /// <param name="encryptedPassword">
        /// <l>
        ///   <zh-CN>客户端提交的 Base64 RSA 密文。</zh-CN>
        ///   <en>Base64 RSA ciphertext submitted by the client.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>解密成功时返回当前请求内使用的明文密码。</zh-CN>
        ///   <en>Plain password for this request when decryption succeeds.</en>
        /// </l>
        /// </param>
        /// <param name="failureCode">
        /// <l>
        ///   <zh-CN>失败分类，不包含敏感值。</zh-CN>
        ///   <en>Failure category without sensitive values.</en>
        /// </l>
        /// </param>
        /// <param name="eventId">
        /// <l>
        ///   <zh-CN>诊断事件编号。</zh-CN>
        ///   <en>Diagnostics event id.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>解密成功时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when decryption succeeds.</en>
        /// </l>
        /// </returns>
        public static bool TryDecryptSubmittedPassword(
            HttpContext context,
            string encryptedPassword,
            out string password,
            out string failureCode,
            out string eventId)
        {
            password = string.Empty;
            string[] passwords;
            if (!TryDecryptSubmittedPasswords(
                context,
                new[] { encryptedPassword },
                out passwords,
                out failureCode,
                out eventId))
            {
                return false;
            }

            password = passwords.Length > 0 ? passwords[0] : string.Empty;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>消费当前 Session 的一次性私钥并解密同一表单中的多个口令密文字段。</zh-CN>
        ///   <en>Consumes the current Session's one-time private key and decrypts multiple password ciphertext fields from one form.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>注册、改密和管理员重置密码通常包含密码与确认密码两个字段；它们必须共用同一把一次性私钥， 并在一次调用中完成解密，避免第一个字段解密后清空私钥导致第二个字段失败。</zh-CN>
        ///   <en>Registration, change-password, and administrator password-reset forms usually contain password and confirmation fields. They must share the same one-time private key and decrypt in one call so the first field does not clear the key before the second field is processed.</en>
        /// </lang>
        /// </remarks>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文，必须带 Session。</zh-CN>
        ///   <en>Current HTTP context; Session is required.</en>
        /// </l>
        /// </param>
        /// <param name="encryptedPasswords">
        /// <l>
        ///   <zh-CN>客户端提交的一组 Base64 RSA 密文。</zh-CN>
        ///   <en>Base64 RSA ciphertext values submitted by the client.</en>
        /// </l>
        /// </param>
        /// <param name="passwords">
        /// <l>
        ///   <zh-CN>解密成功时返回当前请求内使用的明文口令数组。</zh-CN>
        ///   <en>Plain password values for this request when decryption succeeds.</en>
        /// </l>
        /// </param>
        /// <param name="failureCode">
        /// <l>
        ///   <zh-CN>失败分类，不包含敏感值。</zh-CN>
        ///   <en>Failure category without sensitive values.</en>
        /// </l>
        /// </param>
        /// <param name="eventId">
        /// <l>
        ///   <zh-CN>诊断事件编号。</zh-CN>
        ///   <en>Diagnostics event id.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>全部字段解密成功时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when all fields decrypt successfully.</en>
        /// </l>
        /// </returns>
        public static bool TryDecryptSubmittedPasswords(
            HttpContext context,
            string[] encryptedPasswords,
            out string[] passwords,
            out string failureCode,
            out string eventId)
        {
            passwords = new string[0];
            failureCode = string.Empty;
            eventId = string.Empty;

            if (context == null || context.Session == null)
            {
                failureCode = "NoSession";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission could not be decrypted because Session is unavailable.",
                    context);
                return false;
            }

            if (encryptedPasswords == null || encryptedPasswords.Length == 0)
            {
                failureCode = "MissingCiphertext";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission did not contain any ciphertext fields.",
                    context);
                return false;
            }

            string[] trimmedEncryptedPasswords = new string[encryptedPasswords.Length];
            for (int index = 0; index < encryptedPasswords.Length; index++)
            {
                trimmedEncryptedPasswords[index] = encryptedPasswords[index] == null
                    ? string.Empty
                    : encryptedPasswords[index].Trim();

                if (trimmedEncryptedPasswords[index].Length == 0)
                {
                    failureCode = "MissingCiphertext";
                    eventId = PortalDiagnostics.Warn(
                        "LoginPasswordEncryption",
                        "Encrypted password submission was missing one or more ciphertext fields.",
                        context);
                    return false;
                }
            }

            string privateKeyXml = context.Session[PrivateKeySessionKey] as string;
            object issuedUtcValue = context.Session[IssuedUtcSessionKey];
            ClearLoginPasswordKey(context);

            if (string.IsNullOrWhiteSpace(privateKeyXml))
            {
                failureCode = "MissingPrivateKey";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission could not be decrypted because the one-time private key was missing.",
                    context);
                return false;
            }

            if (!(issuedUtcValue is DateTime) || DateTime.UtcNow - (DateTime)issuedUtcValue > KeyLifetime)
            {
                failureCode = "ExpiredPrivateKey";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission could not be decrypted because the one-time private key expired.",
                    context);
                return false;
            }

            try
            {
                passwords = new string[trimmedEncryptedPasswords.Length];
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(privateKeyXml);
                    for (int index = 0; index < trimmedEncryptedPasswords.Length; index++)
                    {
                        byte[] cipherBytes = Convert.FromBase64String(trimmedEncryptedPasswords[index]);
                        byte[] plainBytes = rsa.Decrypt(cipherBytes, false);
                        passwords[index] = Encoding.UTF8.GetString(plainBytes);
                    }
                }

                return true;
            }
            catch (FormatException exception)
            {
                failureCode = "InvalidCiphertext";
                eventId = PortalDiagnostics.Error(
                    "LoginPasswordEncryption",
                    "Encrypted password submission was not valid Base64.",
                    exception,
                    context);
                return false;
            }
            catch (CryptographicException exception)
            {
                failureCode = "DecryptFailed";
                eventId = PortalDiagnostics.Error(
                    "LoginPasswordEncryption",
                    "Encrypted password submission RSA decryption failed.",
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
    /// <lang>
    ///   <zh-CN>登录密码加密公钥响应模型。</zh-CN>
    ///   <en>Response model for a login-password encryption public key.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalLoginPasswordPublicKey
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建公钥响应模型。</zh-CN>
        ///   <en>Creates a public-key response model.</en>
        /// </lang>
        /// </summary>
        /// <param name="publicKeyPem">
        /// <l>
        ///   <zh-CN>SubjectPublicKeyInfo PEM 公钥。</zh-CN>
        ///   <en>SubjectPublicKeyInfo PEM public key.</en>
        /// </l>
        /// </param>
        /// <param name="keySizeBits">
        /// <l>
        ///   <zh-CN>RSA 密钥位数。</zh-CN>
        ///   <en>RSA key size in bits.</en>
        /// </l>
        /// </param>
        public PortalLoginPasswordPublicKey(string publicKeyPem, int keySizeBits)
        {
            PublicKeyPem = publicKeyPem ?? string.Empty;
            KeySizeBits = keySizeBits;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>PEM 格式公钥。</zh-CN>
        ///   <en>PEM-format public key.</en>
        /// </lang>
        /// </summary>
        public string PublicKeyPem { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>密钥位数。</zh-CN>
        ///   <en>Key size in bits.</en>
        /// </lang>
        /// </summary>
        public int KeySizeBits { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>面向页面层的通用口令提交加密 facade。</zh-CN>
    ///   <en>Generic password-submission encryption facade for page code.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>保留 <see cref="PortalLoginPasswordCrypto"/> 作为第一批登录实现的兼容入口；新页面应依赖本 facade， 让注册、改密、管理员重置密码等入口共享同一提交安全语义。</zh-CN>
    ///   <en><see cref="PortalLoginPasswordCrypto"/> remains as the compatibility entry from the first login implementation; new pages should depend on this facade so registration, change-password, and administrator reset flows share one submission-security contract.</en>
    /// </lang>
    /// </remarks>
    public static class PortalPasswordSubmissionCrypto
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取口令提交是否必须使用前端加密。</zh-CN>
        ///   <en>Reads whether password submission must use client-side encryption.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>必须加密提交时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when encrypted submission is required.</en>
        /// </l>
        /// </returns>
        public static bool IsEncryptedSubmissionRequired()
        {
            return PortalLoginPasswordCrypto.IsEncryptedSubmissionRequired();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为当前 Session 签发一个一次性口令提交公钥。</zh-CN>
        ///   <en>Issues a one-time password-submission public key for the current Session.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>PEM 公钥和密钥位数。</zh-CN>
        ///   <en>PEM public key and key size.</en>
        /// </l>
        /// </returns>
        public static PortalLoginPasswordPublicKey IssuePasswordSubmissionKey(HttpContext context)
        {
            return PortalLoginPasswordCrypto.IssueLoginPasswordKey(context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解密单个口令提交密文字段。</zh-CN>
        ///   <en>Decrypts one encrypted password-submission field.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <param name="encryptedPassword">
        /// <l>
        ///   <zh-CN>Base64 RSA 密文。</zh-CN>
        ///   <en>Base64 RSA ciphertext.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>解密后的当前请求内明文。</zh-CN>
        ///   <en>Decrypted plain value for the current request.</en>
        /// </l>
        /// </param>
        /// <param name="failureCode">
        /// <l>
        ///   <zh-CN>失败分类。</zh-CN>
        ///   <en>Failure category.</en>
        /// </l>
        /// </param>
        /// <param name="eventId">
        /// <l>
        ///   <zh-CN>诊断事件编号。</zh-CN>
        ///   <en>Diagnostics event id.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>解密成功时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when decryption succeeds.</en>
        /// </l>
        /// </returns>
        public static bool TryDecryptSubmittedPassword(
            HttpContext context,
            string encryptedPassword,
            out string password,
            out string failureCode,
            out string eventId)
        {
            return PortalLoginPasswordCrypto.TryDecryptSubmittedPassword(
                context,
                encryptedPassword,
                out password,
                out failureCode,
                out eventId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解密同一口令表单中的多个密文字段。</zh-CN>
        ///   <en>Decrypts multiple encrypted fields from one password form.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <param name="encryptedPasswords">
        /// <l>
        ///   <zh-CN>同一次提交中的密文字段。</zh-CN>
        ///   <en>Ciphertext fields in the same submission.</en>
        /// </l>
        /// </param>
        /// <param name="passwords">
        /// <l>
        ///   <zh-CN>解密后的当前请求内明文数组。</zh-CN>
        ///   <en>Decrypted plain values for the current request.</en>
        /// </l>
        /// </param>
        /// <param name="failureCode">
        /// <l>
        ///   <zh-CN>失败分类。</zh-CN>
        ///   <en>Failure category.</en>
        /// </l>
        /// </param>
        /// <param name="eventId">
        /// <l>
        ///   <zh-CN>诊断事件编号。</zh-CN>
        ///   <en>Diagnostics event id.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>全部解密成功时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when all fields decrypt successfully.</en>
        /// </l>
        /// </returns>
        public static bool TryDecryptSubmittedPasswords(
            HttpContext context,
            string[] encryptedPasswords,
            out string[] passwords,
            out string failureCode,
            out string eventId)
        {
            return PortalLoginPasswordCrypto.TryDecryptSubmittedPasswords(
                context,
                encryptedPasswords,
                out passwords,
                out failureCode,
                out eventId);
        }
    }
}
