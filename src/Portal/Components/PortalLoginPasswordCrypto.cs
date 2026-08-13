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
            // <lang>
            //   <zh-CN>先确认请求具备 Session；私钥只能绑定当前请求上下文，不能在无 Session 的情况下生成并丢失消费边界。</zh-CN>
            //   <en>Validate Session first; the private key must be bound to the current request context and must not be generated without a consumption boundary.</en>
            // </lang>
            EnsureSession(context);

            // <lang>
            //   <zh-CN>在固定密钥强度下创建一次性 RSA 实例，并把其生命周期限制在当前 using 块。</zh-CN>
            //   <en>Create a one-time RSA instance at the fixed key strength and limit its lifetime to the current using block.</en>
            // </lang>
            using (var rsa = new RSACryptoServiceProvider(DefaultKeySizeBits))
            {
                // <lang>
                //   <zh-CN>只导出公钥参数供页面使用；私钥不进入返回模型。</zh-CN>
                //   <en>Export only public parameters for the page; the private key never enters the response model.</en>
                // </lang>
                RSAParameters publicParameters = rsa.ExportParameters(false);

                // <lang>
                //   <zh-CN>把完整私钥和签发时间放入当前 Session，供下一次口令提交一次性消费。</zh-CN>
                //   <en>Store the full private key and issue time in the current Session for one-time consumption by the next password post.</en>
                // </lang>
                context.Session[PrivateKeySessionKey] = rsa.ToXmlString(true);
                context.Session[IssuedUtcSessionKey] = DateTime.UtcNow;

                // <lang>
                //   <zh-CN>返回 SubjectPublicKeyInfo PEM 和固定位数；响应不携带私钥、Session 键或诊断数据。</zh-CN>
                //   <en>Return SubjectPublicKeyInfo PEM and the fixed key size; the response contains no private key, Session key, or diagnostic data.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>先初始化明文输出，确保失败路径不会把调用方已有内容当作本次解密结果。</zh-CN>
            //   <en>Initialize the plaintext output first so a failure path cannot expose a caller's previous value as this decryption result.</en>
            // </lang>
            password = string.Empty;

            // <lang>
            //   <zh-CN>单字段入口复用多字段消费逻辑，保证一次性私钥清理和失败分类只有一套实现。</zh-CN>
            //   <en>Reuse the multi-field consumption path so one-time-key clearing and failure classification have a single implementation.</en>
            // </lang>
            string[] passwords;
            if (!TryDecryptSubmittedPasswords(
                context,
                new[] { encryptedPassword },
                out passwords,
                out failureCode,
                out eventId))
            {
                // <lang>
                //   <zh-CN>底层已给出失败分类和低敏诊断事件；单字段入口不再重写或扩展敏感信息。</zh-CN>
                //   <en>The lower layer already supplied a failure category and low-sensitivity diagnostic event; the single-field entry does not rewrite or extend sensitive details.</en>
                // </lang>
                return false;
            }

            // <lang>
            //   <zh-CN>只从成功的多字段结果取第一项；空数组仍安全回退为空字符串。</zh-CN>
            //   <en>Read only the first item from the successful multi-field result; an empty array safely falls back to an empty string.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>初始化全部 out 参数，避免 Session、输入或解密失败时泄露上一次调用的数据。</zh-CN>
            //   <en>Initialize all out parameters so Session, input, or decryption failures cannot leak data from a prior call.</en>
            // </lang>
            passwords = new string[0];
            failureCode = string.Empty;
            eventId = string.Empty;

            // <lang>
            //   <zh-CN>Session 是一次性私钥的存储边界；上下文或 Session 缺失时不尝试读取密文。</zh-CN>
            //   <en>Session is the storage boundary for the one-time private key; when context or Session is missing, do not process ciphertext.</en>
            // </lang>
            if (context == null || context.Session == null)
            {
                // <lang>
                //   <zh-CN>失败码只表达缺少运行条件，诊断消息不包含密码或密文。</zh-CN>
                //   <en>The failure code describes only the missing runtime condition; the diagnostic message contains no password or ciphertext.</en>
                // </lang>
                failureCode = "NoSession";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission could not be decrypted because Session is unavailable.",
                    context);
                return false;
            }

            // <lang>
            //   <zh-CN>没有字段就没有可消费的密文；拒绝空数组以免误把空输入当作成功。</zh-CN>
            //   <en>With no fields there is no ciphertext to consume; reject an empty array rather than treating empty input as success.</en>
            // </lang>
            if (encryptedPasswords == null || encryptedPasswords.Length == 0)
            {
                failureCode = "MissingCiphertext";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission did not contain any ciphertext fields.",
                    context);
                return false;
            }

            // <lang>
            //   <zh-CN>为每个字段建立去除首尾空白的临时数组；原始请求数组不被改写，明文仍只在后续当前请求范围内生成。</zh-CN>
            //   <en>Build a trimmed temporary array for each field; do not mutate the request array, and create plaintext only within the later current-request scope.</en>
            // </lang>
            string[] trimmedEncryptedPasswords = new string[encryptedPasswords.Length];
            for (int index = 0; index < encryptedPasswords.Length; index++)
            {
                // <lang>
                //   <zh-CN>把 null 统一成空字符串再检查，保持字段数量和调用方顺序不变。</zh-CN>
                //   <en>Normalize null to an empty string before checking so field count and caller order remain unchanged.</en>
                // </lang>
                trimmedEncryptedPasswords[index] = encryptedPasswords[index] == null
                    ? string.Empty
                    : encryptedPasswords[index].Trim();

                if (trimmedEncryptedPasswords[index].Length == 0)
                {
                    // <lang>
                    //   <zh-CN>任一字段缺失都会拒绝整次提交，避免部分解密结果被调用方误用。</zh-CN>
                    //   <en>Reject the whole submission when any field is missing so callers cannot misuse a partial decryption result.</en>
                    // </lang>
                    failureCode = "MissingCiphertext";
                    eventId = PortalDiagnostics.Warn(
                        "LoginPasswordEncryption",
                        "Encrypted password submission was missing one or more ciphertext fields.",
                        context);
                    return false;
                }
            }

            // <lang>
            //   <zh-CN>先取出私钥和签发时间，再立即清除 Session；无论后续校验或 RSA 解密结果如何，该密钥都只能消费一次。</zh-CN>
            //   <en>Read the private key and issue time, then clear Session immediately; regardless of later validation or RSA results, the key is consumed only once.</en>
            // </lang>
            string privateKeyXml = context.Session[PrivateKeySessionKey] as string;
            object issuedUtcValue = context.Session[IssuedUtcSessionKey];
            ClearLoginPasswordKey(context);

            if (string.IsNullOrWhiteSpace(privateKeyXml))
            {
                // <lang>
                //   <zh-CN>私钥缺失表示提交无法与当前 Session 配对，按低敏失败分类结束。</zh-CN>
                //   <en>A missing private key means the post cannot be paired with the current Session; finish with a low-sensitivity failure category.</en>
                // </lang>
                failureCode = "MissingPrivateKey";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission could not be decrypted because the one-time private key was missing.",
                    context);
                return false;
            }

            // <lang>
            //   <zh-CN>只接受 DateTime 且未超过五分钟的签发记录；类型不符或过期都拒绝解密。</zh-CN>
            //   <en>Accept only a DateTime issue record within five minutes; reject decryption for a wrong type or an expired record.</en>
            // </lang>
            if (!(issuedUtcValue is DateTime) || DateTime.UtcNow - (DateTime)issuedUtcValue > KeyLifetime)
            {
                // <lang>
                //   <zh-CN>过期私钥不能回退为明文或重试同一密文，调用方只能重新获取公钥。</zh-CN>
                //   <en>An expired private key must not fall back to plaintext or retry the same ciphertext; callers must obtain a new public key.</en>
                // </lang>
                failureCode = "ExpiredPrivateKey";
                eventId = PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Encrypted password submission could not be decrypted because the one-time private key expired.",
                    context);
                return false;
            }

            try
            {
                // <lang>
                //   <zh-CN>仅在 Session、字段和时效门禁全部通过后分配明文数组，降低明文驻留范围。</zh-CN>
                //   <en>Allocate the plaintext array only after Session, field, and lifetime gates pass, minimizing plaintext residency.</en>
                // </lang>
                passwords = new string[trimmedEncryptedPasswords.Length];

                // <lang>
                //   <zh-CN>从一次性 XML 私钥恢复 RSA 解密器，生命周期限制在当前 try 块。</zh-CN>
                //   <en>Restore the RSA decryptor from the one-time XML private key and limit its lifetime to the current try block.</en>
                // </lang>
                using (var rsa = new RSACryptoServiceProvider())
                {
                    // <lang>
                    //   <zh-CN>导入私钥仅用于当前批次，不把密钥写回 Session 或任何输出。</zh-CN>
                    //   <en>Import the private key only for this batch; never write it back to Session or any output.</en>
                    // </lang>
                    rsa.FromXmlString(privateKeyXml);
                    for (int index = 0; index < trimmedEncryptedPasswords.Length; index++)
                    {
                        // <lang>
                        //   <zh-CN>按提交顺序解码每个 Base64 密文并使用既有 RSA PKCS#1 v1.5 参数解密。</zh-CN>
                        //   <en>Decode each Base64 ciphertext in submission order and decrypt it with the existing RSA PKCS#1 v1.5 setting.</en>
                        // </lang>
                        byte[] cipherBytes = Convert.FromBase64String(trimmedEncryptedPasswords[index]);

                        // <lang>
                        //   <zh-CN>明文只转换为 UTF-8 字符串并放入当前 out 数组，不记录、不持久化。</zh-CN>
                        //   <en>Convert plaintext only to a UTF-8 string in the current out array; do not log or persist it.</en>
                        // </lang>
                        byte[] plainBytes = rsa.Decrypt(cipherBytes, false);
                        passwords[index] = Encoding.UTF8.GetString(plainBytes);
                    }
                }

                return true;
            }
            catch (FormatException exception)
            {
                // <lang>
                //   <zh-CN>Base64 格式错误只映射为稳定失败码和诊断事件，异常对象交给低敏诊断层处理。</zh-CN>
                //   <en>Map invalid Base64 only to a stable failure code and diagnostic event; let the low-sensitivity diagnostics layer handle the exception object.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>RSA 解密失败不回退为明文；返回稳定分类并清除已消费的密钥。</zh-CN>
                //   <en>Do not fall back to plaintext after RSA decryption fails; return a stable category while the consumed key remains cleared.</zh-CN>
                // </lang>
                failureCode = "DecryptFailed";
                eventId = PortalDiagnostics.Error(
                    "LoginPasswordEncryption",
                    "Encrypted password submission RSA decryption failed.",
                    exception,
                    context);
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证口令加密流程所需的 HTTP 上下文和 Session；缺少任一条件时立即拒绝继续。</zh-CN>
        ///   <en>Validates the HTTP context and Session required by password encryption; aborts immediately when either is unavailable.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <l>
        ///   <zh-CN>上下文为空时引发。</zh-CN>
        ///   <en>Thrown when the context is null.</en>
        /// </l>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>请求未启用 Session 时引发。</zh-CN>
        ///   <en>Thrown when the request has no Session.</en>
        /// </l>
        /// </exception>
        private static void EnsureSession(HttpContext context)
        {
            // <lang>
            //   <zh-CN>空上下文无法提供 Session，也无法形成可诊断的当前请求边界。</zh-CN>
            //   <en>A null context cannot provide Session or establish a diagnosable current-request boundary.</en>
            // </lang>
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            // <lang>
            //   <zh-CN>无 Session 时不允许签发或消费一次性密钥，避免密钥落入不可回收的范围。</zh-CN>
            //   <en>Do not issue or consume a one-time key without Session, preventing a key from entering an unrecoverable scope.</en>
            // </lang>
            if (context.Session == null)
            {
                throw new InvalidOperationException("The current request does not have a Session.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>清除当前 Session 中的口令私钥和签发时间，使一次性密钥在本次消费后不可再次使用。</zh-CN>
        ///   <en>Clears the password private key and issue time from the current Session so the one-time key cannot be reused after consumption.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>带有当前登录口令加密 Session 的 HTTP 上下文。</zh-CN>
        ///   <en>HTTP context containing the current login-password encryption Session.</en>
        /// </l>
        /// </param>
        private static void ClearLoginPasswordKey(HttpContext context)
        {
            // <lang>
            //   <zh-CN>同时移除私钥和签发时间，避免只删一项造成旧密钥状态残留。</zh-CN>
            //   <en>Remove both the private key and issue time together so stale-key state cannot remain after deleting only one item.</en>
            // </lang>
            context.Session.Remove(PrivateKeySessionKey);
            context.Session.Remove(IssuedUtcSessionKey);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把 RSA 公钥参数导出为 SubjectPublicKeyInfo PEM 文本，供前端加密库消费。</zh-CN>
        ///   <en>Exports RSA public parameters as SubjectPublicKeyInfo PEM text for the client-side encryption library.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameters">
        /// <l>
        ///   <zh-CN>仅包含公钥部分的 RSA 参数。</zh-CN>
        ///   <en>RSA parameters containing only the public-key material.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带标准头尾和 64 字符换行的 PEM 公钥。</zh-CN>
        ///   <en>PEM public key with standard headers, footer, and 64-character line wrapping.</en>
        /// </l>
        /// </returns>
        private static string ExportSubjectPublicKeyInfoPem(RSAParameters parameters)
        {
            // <lang>
            //   <zh-CN>先得到完整 DER，再转换为 Base64；PEM 只承载公钥，不接触私钥参数。</zh-CN>
            //   <en>Build complete DER first and then convert it to Base64; PEM carries public key material only and never touches private parameters.</en>
            // </lang>
            byte[] publicKeyDer = EncodeSubjectPublicKeyInfo(parameters);

            // <lang>
            //   <zh-CN>Base64 是页面协议载体，内容只来自公钥 DER 字节。</zh-CN>
            //   <en>Base64 is the page-protocol carrier and is derived only from public-key DER bytes.</en>
            // </lang>
            string base64 = Convert.ToBase64String(publicKeyDer);

            // <lang>
            //   <zh-CN>使用 StringBuilder 组装标准 PEM 头、分行内容和尾部，避免隐式格式差异。</zh-CN>
            //   <en>Use StringBuilder to assemble the standard PEM header, wrapped content, and footer without implicit formatting differences.</en>
            // </lang>
            var builder = new StringBuilder();
            builder.AppendLine("-----BEGIN PUBLIC KEY-----");

            for (int index = 0; index < base64.Length; index += 64)
            {
                // <lang>
                //   <zh-CN>每行最多 64 个 Base64 字符，保持既有 PEM 文本兼容格式。</zh-CN>
                //   <en>Keep each line to at most 64 Base64 characters for compatibility with the existing PEM text format.</en>
                // </lang>
                int length = Math.Min(64, base64.Length - index);
                builder.AppendLine(base64.Substring(index, length));
            }

            builder.AppendLine("-----END PUBLIC KEY-----");
            return builder.ToString();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按 ASN.1 DER 规则编码 RSA SubjectPublicKeyInfo 结构。</zh-CN>
        ///   <en>Encodes the RSA SubjectPublicKeyInfo structure according to ASN.1 DER rules.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameters">
        /// <l>
        ///   <zh-CN>用于构造模数和指数的 RSA 公钥参数。</zh-CN>
        ///   <en>RSA public parameters used to construct the modulus and exponent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可放入 PEM 的 SubjectPublicKeyInfo DER 字节。</zh-CN>
        ///   <en>SubjectPublicKeyInfo DER bytes suitable for PEM encoding.</en>
        /// </l>
        /// </returns>
        private static byte[] EncodeSubjectPublicKeyInfo(RSAParameters parameters)
        {
            // <lang>
            //   <zh-CN>先编码 RSA 公钥主体：模数和指数按 SEQUENCE 顺序排列。</zh-CN>
            //   <en>Encode the RSA public-key body first, placing modulus and exponent in SEQUENCE order.</en>
            // </lang>
            byte[] rsaPublicKey = EncodeSequence(
                Concat(
                    EncodeInteger(parameters.Modulus),
                    EncodeInteger(parameters.Exponent)));

            // <lang>
            //   <zh-CN>使用 rsaEncryption OID 与空参数构造算法标识，保持 SubjectPublicKeyInfo 结构兼容。</zh-CN>
            //   <en>Build the algorithm identifier with the rsaEncryption OID and empty parameters to preserve SubjectPublicKeyInfo compatibility.</en>
            // </lang>
            byte[] algorithmIdentifier = EncodeSequence(
                Concat(
                    new byte[] { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 },
                    new byte[] { 0x05, 0x00 }));

            // <lang>
            //   <zh-CN>把算法标识和 BIT STRING 公钥主体包装为顶层 SubjectPublicKeyInfo。</zh-CN>
            //   <en>Wrap the algorithm identifier and BIT STRING public-key body as the top-level SubjectPublicKeyInfo.</en>
            // </lang>
            return EncodeSequence(
                Concat(
                    algorithmIdentifier,
                    EncodeBitString(rsaPublicKey)));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把无符号大端字节按 ASN.1 INTEGER 的非负数规则编码，必要时补前导零。</zh-CN>
        ///   <en>Encodes unsigned big-endian bytes as a non-negative ASN.1 INTEGER, adding a leading zero when required.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>RSA 模数或指数的大端字节。</zh-CN>
        ///   <en>Big-endian bytes for an RSA modulus or exponent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带 INTEGER 标签和长度的 DER 字节。</zh-CN>
        ///   <en>DER bytes with the INTEGER tag and length.</en>
        /// </l>
        /// </returns>
        private static byte[] EncodeInteger(byte[] value)
        {
            // <lang>
            //   <zh-CN>先去除冗余前导零，再判断最高位；ASN.1 INTEGER 的符号位要求非负 RSA 数补零。</zh-CN>
            //   <en>Remove redundant leading zeros, then inspect the high bit; ASN.1 INTEGER requires a zero prefix for non-negative RSA values when needed.</en>
            // </lang>
            byte[] normalizedValue = TrimLeadingZeroBytes(value);

            // <lang>
            //   <zh-CN>最高位为 1 时补零，否则直接沿用规范化内容；结果仍保持大端顺序。</zh-CN>
            //   <en>Prefix zero when the high bit is set; otherwise keep normalized content in big-endian order.</en>
            // </lang>
            bool mustPrefixZero = normalizedValue.Length > 0 && (normalizedValue[0] & 0x80) != 0;

            // <lang>
            //   <zh-CN>为可选的符号保护字节预留最终 INTEGER 内容缓冲区。</zh-CN>
            //   <en>Reserve the final INTEGER-content buffer, including the optional sign-protection byte.</en>
            // </lang>
            byte[] integerBytes = new byte[normalizedValue.Length + (mustPrefixZero ? 1 : 0)];

            if (mustPrefixZero)
            {
                // <lang>
                //   <zh-CN>从索引 1 写入原始数值，索引 0 保留为非负符号保护字节。</zh-CN>
                //   <en>Copy the numeric value at index 1 and reserve index 0 for the non-negative sign-protection byte.</en>
                // </lang>
                Buffer.BlockCopy(normalizedValue, 0, integerBytes, 1, normalizedValue.Length);
            }
            else
            {
                // <lang>
                //   <zh-CN>无需符号保护时直接复制完整规范化数值。</zh-CN>
                //   <en>Copy the complete normalized value directly when no sign-protection byte is required.</en>
                // </lang>
                Buffer.BlockCopy(normalizedValue, 0, integerBytes, 0, normalizedValue.Length);
            }

            return EncodeTag(0x02, integerBytes);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把 DER 公钥结构包装为带一个未使用位计数前导字节的 ASN.1 BIT STRING。</zh-CN>
        ///   <en>Wraps a DER public-key structure as an ASN.1 BIT STRING with the required unused-bit-count prefix.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>需要包装的 DER 内容。</zh-CN>
        ///   <en>DER content to wrap.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带 BIT STRING 标签和长度的 DER 字节。</zh-CN>
        ///   <en>DER bytes with the BIT STRING tag and length.</en>
        /// </l>
        /// </returns>
        private static byte[] EncodeBitString(byte[] value)
        {
            // <lang>
            //   <zh-CN>首字节记录未使用位数为 0，随后复制完整 RSA 公钥主体，符合 SubjectPublicKeyInfo 的 BIT STRING 约束。</zh-CN>
            //   <en>Set the unused-bit count to zero, then copy the complete RSA public-key body to satisfy the SubjectPublicKeyInfo BIT STRING constraint.</en>
            // </lang>
            byte[] bitStringValue = new byte[value.Length + 1];

            // <lang>
            //   <zh-CN>从索引 1 写入主体，保留索引 0 的未使用位计数前导字节。</zh-CN>
            //   <en>Copy the body at index 1 and retain index 0 for the unused-bit-count prefix.</en>
            // </lang>
            Buffer.BlockCopy(value, 0, bitStringValue, 1, value.Length);
            return EncodeTag(0x03, bitStringValue);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用 ASN.1 SEQUENCE 标签包装已经编码的 DER 内容。</zh-CN>
        ///   <en>Wraps already encoded DER content with the ASN.1 SEQUENCE tag.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>顺序结构内容。</zh-CN>
        ///   <en>Sequence content.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带 SEQUENCE 标签和长度的 DER 字节。</zh-CN>
        ///   <en>DER bytes with the SEQUENCE tag and length.</en>
        /// </l>
        /// </returns>
        private static byte[] EncodeSequence(byte[] value)
        {
            // <lang>
            //   <zh-CN>SEQUENCE helper 不改变内容，仅追加结构标签和 DER 长度。</zh-CN>
            //   <en>The SEQUENCE helper preserves content and adds only the structural tag and DER length.</en>
            // </lang>
            return EncodeTag(0x30, value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为 DER 值写入指定标签、长度和内容，保持 TLV 顺序不变。</zh-CN>
        ///   <en>Writes the specified DER tag, length, and content while preserving TLV ordering.</en>
        /// </lang>
        /// </summary>
        /// <param name="tag">
        /// <l>
        ///   <zh-CN>ASN.1 标签字节。</zh-CN>
        ///   <en>ASN.1 tag byte.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>标签对应的内容字节。</zh-CN>
        ///   <en>Content bytes for the tag.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>完整的 DER TLV 字节。</zh-CN>
        ///   <en>Complete DER TLV bytes.</en>
        /// </l>
        /// </returns>
        private static byte[] EncodeTag(byte tag, byte[] value)
        {
            // <lang>
            //   <zh-CN>长度字段必须先于缓冲区分配确定，以便一次分配完整 TLV。</zh-CN>
            //   <en>Determine the length field before allocating so the complete TLV can be allocated once.</en>
            // </lang>
            byte[] length = EncodeLength(value.Length);

            // <lang>
            //   <zh-CN>缓冲区布局为 tag、length、value，严格保持 DER 的 TLV 顺序。</zh-CN>
            //   <en>Lay out the buffer as tag, length, and value, preserving DER TLV order exactly.</en>
            // </lang>
            byte[] encoded = new byte[1 + length.Length + value.Length];
            encoded[0] = tag;
            Buffer.BlockCopy(length, 0, encoded, 1, length.Length);
            Buffer.BlockCopy(value, 0, encoded, 1 + length.Length, value.Length);
            return encoded;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按 DER 短格式或长格式编码内容长度。</zh-CN>
        ///   <en>Encodes a content length using the DER short or long form.</en>
        /// </lang>
        /// </summary>
        /// <param name="length">
        /// <l>
        ///   <zh-CN>待编码内容长度，不应为负数。</zh-CN>
        ///   <en>Content length to encode; it must not be negative.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>DER 长度字段字节。</zh-CN>
        ///   <en>DER length-field bytes.</en>
        /// </l>
        /// </returns>
        private static byte[] EncodeLength(int length)
        {
            // <lang>
            //   <zh-CN>短格式直接写入一个长度字节，覆盖 DER 小于 128 的内容长度。</zh-CN>
            //   <en>Use one length byte for DER content lengths below 128.</en>
            // </lang>
            if (length < 128)
            {
                return new byte[] { (byte)length };
            }

            // <lang>
            //   <zh-CN>复制原始长度用于统计长格式需要的有效字节数，不提前破坏调用方长度值。</zh-CN>
            //   <en>Copy the original length to count significant bytes for long form without consuming the caller's length value early.</en>
            // </lang>
            int tempLength = length;
            int byteCount = 0;
            while (tempLength > 0)
            {
                // <lang>
                //   <zh-CN>每右移 8 位增加一个大端长度字节。</zh-CN>
                //   <en>Each eight-bit shift accounts for one big-endian length byte.</en>
                // </lang>
                byteCount++;
                tempLength >>= 8;
            }

            // <lang>
            //   <zh-CN>首字节标记长格式，后续字节承载实际长度。</zh-CN>
            //   <en>Mark long form in the first byte and carry the actual length in following bytes.</en>
            // </lang>
            byte[] encoded = new byte[byteCount + 1];
            encoded[0] = (byte)(0x80 | byteCount);

            for (int index = byteCount; index > 0; index--)
            {
                // <lang>
                //   <zh-CN>从低位向高位填充，最终数组仍为 DER 要求的大端顺序。</zh-CN>
                //   <en>Fill from low-order positions toward high order so the final array remains DER big-endian.</en>
                // </lang>
                encoded[index] = (byte)(length & 0xFF);
                length >>= 8;
            }

            return encoded;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>去除大端整数的冗余前导零，同时为全空值保留一个零字节。</zh-CN>
        ///   <en>Removes redundant leading zeros from a big-endian integer while retaining one zero byte for null or empty input.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待规范化的大端整数。</zh-CN>
        ///   <en>Big-endian integer to normalize.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>至少包含一个字节且无冗余前导零的数组。</zh-CN>
        ///   <en>An array with at least one byte and no redundant leading zeros.</en>
        /// </l>
        /// </returns>
        private static byte[] TrimLeadingZeroBytes(byte[] value)
        {
            // <lang>
            //   <zh-CN>空值不让 ASN.1 INTEGER 变成零长度，保留一个零字节作为合法数值。</zh-CN>
            //   <en>Do not let null or empty input become a zero-length ASN.1 INTEGER; retain one zero byte as a valid value.</zh-CN>
            // </lang>
            if (value == null || value.Length == 0)
            {
                return new byte[] { 0 };
            }

            // <lang>
            //   <zh-CN>跳过冗余前导零，但始终保留最后一个字节，避免全零数值被清空。</zh-CN>
            //   <en>Skip redundant leading zeros while retaining the final byte so an all-zero value is never emptied.</zh-CN>
            // </lang>
            int index = 0;
            while (index < value.Length - 1 && value[index] == 0)
            {
                index++;
            }

            // <lang>
            //   <zh-CN>复制规范化后的尾部字节，保持 RSA 参数的大端方向。</zh-CN>
            //   <en>Copy the normalized tail bytes while preserving RSA parameter big-endian direction.</en>
            // </lang>
            byte[] normalized = new byte[value.Length - index];
            Buffer.BlockCopy(value, index, normalized, 0, normalized.Length);
            return normalized;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按参数顺序拼接两个 DER 片段，供结构编码 helper 组合字段。</zh-CN>
        ///   <en>Concatenates two DER fragments in argument order for composition by the structure-encoding helpers.</en>
        /// </lang>
        /// </summary>
        /// <param name="first">
        /// <l>
        ///   <zh-CN>前置字节片段。</zh-CN>
        ///   <en>Leading byte fragment.</en>
        /// </l>
        /// </param>
        /// <param name="second">
        /// <l>
        ///   <zh-CN>后置字节片段。</zh-CN>
        ///   <en>Trailing byte fragment.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>顺序不变的合并字节。</zh-CN>
        ///   <en>Combined bytes in the original order.</en>
        /// </l>
        /// </returns>
        private static byte[] Concat(byte[] first, byte[] second)
        {
            // <lang>
            //   <zh-CN>一次分配两个片段的总长度，避免组合 DER 结构时改变片段内容。</zh-CN>
            //   <en>Allocate the combined length once without altering either fragment while composing the DER structure.</en>
            // </lang>
            byte[] result = new byte[first.Length + second.Length];

            // <lang>
            //   <zh-CN>先写入前置片段，再从其长度位置写入后置片段，保持参数顺序。</zh-CN>
            //   <en>Copy the leading fragment first, then copy the trailing fragment at its length offset to preserve argument order.</en>
            // </lang>
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
