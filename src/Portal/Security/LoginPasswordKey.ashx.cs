using System;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace ASPNET.StarterKit.Portal.Security
{
    /// <summary>
    /// 中文：登录密码前端加密使用的一次性公钥下发 handler。
    ///
    /// English: One-time public-key handler used by client-side login-password encryption.
    /// </summary>
    /// <remarks>
    /// 中文：handler 只返回公钥，私钥保存在当前 Session 中并由登录提交消费；响应禁止缓存。
    ///
    /// English: The handler returns only the public key; the private key is kept in the current Session and
    /// consumed by the login post. Responses are never cached.
    /// </remarks>
    public sealed class LoginPasswordKey : IHttpHandler, IRequiresSessionState
    {
        /// <summary>
        /// 中文：handler 不可复用，因为它依赖当前请求 Session。
        ///
        /// English: The handler is not reusable because it depends on the current request Session.
        /// </summary>
        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        /// 中文：生成一次性 RSA 公钥并写入纯文本响应。
        ///
        /// English: Generates a one-time RSA public key and writes it to a plain-text response.
        /// </summary>
        /// <param name="context">中文：当前 HTTP 上下文。English: Current HTTP context.</param>
        public void ProcessRequest(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            context.Response.ContentType = "text/plain";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();

            try
            {
                PortalLoginPasswordPublicKey publicKey = PortalLoginPasswordCrypto.IssueLoginPasswordKey(context);
                context.Response.Write(publicKey.PublicKeyPem);
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "LoginPasswordEncryption",
                    "Failed to issue login password public key.",
                    exception,
                    context);

                context.Response.StatusCode = 500;
                context.Response.Write("ERROR:" + eventId);
            }
        }
    }
}
