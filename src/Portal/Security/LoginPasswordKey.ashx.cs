using System;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace ASPNET.StarterKit.Portal.Security
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>登录密码前端加密使用的一次性公钥下发 handler。</zh-CN>
    ///   <en>One-time public-key handler used by client-side login-password encryption.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>handler 只返回公钥，私钥保存在当前 Session 中并由登录提交消费；响应禁止缓存，也不会返回密码、私钥或会话内部状态。失败时只输出诊断事件编号，便于管理员追查而不暴露异常细节。</zh-CN>
    ///   <en>The handler returns only the public key; the private key is kept in the current Session and consumed by the login post. Responses are never cached and never include passwords, private keys, or internal session state. On failure, it outputs only a diagnostic event identifier so administrators can investigate without exposing exception details.</en>
    /// </lang>
    /// </remarks>
    public sealed class LoginPasswordKey : IHttpHandler, IRequiresSessionState
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>handler 不可复用，因为它依赖当前请求 Session。</zh-CN>
        ///   <en>The handler is not reusable because it depends on the current request Session.</en>
        /// </lang>
        /// </summary>
        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成一次性 RSA 公钥并写入纯文本响应。</zh-CN>
        ///   <en>Generates a one-time RSA public key and writes it to a plain-text response.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        public void ProcessRequest(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            context.Response.ContentType = "text/plain";
            context.Response.ContentEncoding = Encoding.UTF8;
            // <lang>
            //   <zh-CN>公钥是一次性登录材料，不能被浏览器、代理或共享缓存复用。</zh-CN>
            //   <en>The public key is one-time login material and must not be reused from browser, proxy, or shared caches.</en>
            // </lang>
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();

            try
            {
                // <lang>
                //   <zh-CN>加密材料的生成和 Session 写入集中在密码提交加密服务中，handler 只负责传输公开部分。</zh-CN>
                //   <en>Encryption-material creation and Session writes remain centralized in the password-submission crypto service; this handler only transports the public portion.</en>
                // </lang>
                PortalLoginPasswordPublicKey publicKey = PortalPasswordSubmissionCrypto.IssuePasswordSubmissionKey(context);
                context.Response.Write(publicKey.PublicKeyPem);
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>下发失败属于登录安全链路异常，记录完整诊断但响应只给事件编号。</zh-CN>
                //   <en>Issuing failure is an exception in the login security chain, so record full diagnostics while returning only the event identifier.</en>
                // </lang>
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
