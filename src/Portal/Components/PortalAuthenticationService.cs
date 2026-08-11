using System;
using System.Web;
using System.Web.Security;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户 Forms Authentication 身份票据门面。</zh-CN>
    ///   <en>Forms Authentication ticket facade for the Portal.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P5.2 起身份票据的 <c>UserData</c> 只保存用户安全版本，不保存角色、密码材料或业务资料。 角色列表仍由 <see cref="PortalAuthenticationCookies"/> 单独加密保存，并使用相同安全版本校验。</zh-CN>
    ///   <en>Starting with P5.2, the authentication ticket <c>UserData</c> stores only the user security version, never roles, password material, or business profile data. Role lists remain in the separately encrypted <see cref="PortalAuthenticationCookies"/> cookie and are validated with the same security version.</en>
    /// </lang>
    /// </remarks>
    public static class PortalAuthenticationService
    {
        // <lang>
        //   <zh-CN>安全版本前缀是票据 UserData 的固定受控格式，不承载角色、秘密或业务资料。</zh-CN>
        //   <en>The security-version prefix is the fixed controlled UserData format and carries no roles, secrets, or business data.</en>
        // </lang>
        private const string SecurityVersionPrefix = "sv:";

        /// <summary>
        /// <lang>
        ///   <zh-CN>签发带安全版本的 Forms Authentication 身份票据。</zh-CN>
        ///   <en>Issues a Forms Authentication ticket with the security version.</en>
        /// </lang>
        /// </summary>
        /// <param name="response">
        /// <l>
        ///   <zh-CN>当前 HTTP 响应。</zh-CN>
        ///   <en>Current HTTP response.</en>
        /// </l>
        /// </param>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求，用于解析 Cookie Path。</zh-CN>
        ///   <en>Current HTTP request used to resolve the cookie path.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>认证用户名称。</zh-CN>
        ///   <en>Authenticated user name.</en>
        /// </l>
        /// </param>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>用户当前安全版本。</zh-CN>
        ///   <en>Current user security version.</en>
        /// </l>
        /// </param>
        /// <param name="isPersistent">
        /// <l>
        ///   <zh-CN>是否写为持久 Cookie。</zh-CN>
        ///   <en>Whether to write a persistent cookie.</en>
        /// </l>
        /// </param>
        public static void SignIn(
            HttpResponse response,
            HttpRequest request,
            string userName,
            long securityVersion,
            bool isPersistent)
        {
            // <lang>
            //   <zh-CN>记录本次票据签发的本地时间起点，后续过期边界与 Forms Authentication timeout 保持一致。</zh-CN>
            //   <en>Record the local issuance start so the later expiration boundary matches the Forms Authentication timeout.</en>
            // </lang>
            DateTime issuedAt = DateTime.Now;

            // <lang>
            //   <zh-CN>计算票据过期时间；持久 Cookie 仅在调用方选择持久登录时复用该边界。</zh-CN>
            //   <en>Compute the ticket expiration; a persistent cookie reuses this boundary only when persistent sign-in is selected.</en>
            // </lang>
            DateTime expiresAt = issuedAt.Add(FormsAuthentication.Timeout);

            // <lang>
            //   <zh-CN>创建只包含安全版本 UserData 的 Forms Authentication 票据，角色和业务资料不进入主票据。</zh-CN>
            //   <en>Create a Forms Authentication ticket whose UserData contains only the security version; roles and business data stay out of the main ticket.</en>
            // </lang>
            var ticket = new FormsAuthenticationTicket(
                1,
                userName,
                issuedAt,
                expiresAt,
                isPersistent,
                FormatSecurityVersion(securityVersion));

            // <lang>
            //   <zh-CN>创建 HttpOnly 身份 Cookie，并复用虚拟目录 Path 规则；Secure/SameSite 仍由部署策略负责。</zh-CN>
            //   <en>Create the HttpOnly identity cookie using the virtual-directory Path rule; deployment policy still owns Secure/SameSite.</en>
            // </lang>
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Path = GetCookiePath(request)
            };

            if (isPersistent)
            {
                cookie.Expires = expiresAt;
            }

            response.Cookies.Add(cookie);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>注销当前身份并清理角色 Cookie。</zh-CN>
        ///   <en>Signs out the current identity and clears the role cookie.</en>
        /// </lang>
        /// </summary>
        /// <param name="response">
        /// <l>
        ///   <zh-CN>当前 HTTP 响应。</zh-CN>
        ///   <en>Current HTTP response.</en>
        /// </l>
        /// </param>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求，用于解析 Cookie Path。</zh-CN>
        ///   <en>Current HTTP request used to resolve the cookie path.</en>
        /// </l>
        /// </param>
        public static void SignOut(HttpResponse response, HttpRequest request)
        {
            FormsAuthentication.SignOut();
            ExpireAuthenticationCookie(response, request);
            PortalAuthenticationCookies.ExpireRolesCookie(response, request);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从 Forms Authentication 身份中读取安全版本。</zh-CN>
        ///   <en>Reads the security version from a Forms Authentication identity.</en>
        /// </lang>
        /// </summary>
        /// <param name="identity">
        /// <l>
        ///   <zh-CN>Forms 身份。</zh-CN>
        ///   <en>Forms identity.</en>
        /// </l>
        /// </param>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>读取到的安全版本。</zh-CN>
        ///   <en>Parsed security version.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功解析安全版本时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the security version was parsed successfully.</en>
        /// </l>
        /// </returns>
        public static bool TryReadSecurityVersion(FormsIdentity identity, out long securityVersion)
        {
            securityVersion = 0;
            return identity != null &&
                   identity.Ticket != null &&
                   !identity.Ticket.Expired &&
                   TryParseSecurityVersion(identity.Ticket.UserData, out securityVersion);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将安全版本格式化为票据 UserData。</zh-CN>
        ///   <en>Formats a security version for ticket UserData.</en>
        /// </lang>
        /// </summary>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>安全版本。</zh-CN>
        ///   <en>Security version.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>稳定的票据数据文本。</zh-CN>
        ///   <en>Stable ticket-data text.</en>
        /// </l>
        /// </returns>
        public static string FormatSecurityVersion(long securityVersion)
        {
            return SecurityVersionPrefix + Math.Max(0, securityVersion);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析票据 UserData 中的安全版本。</zh-CN>
        ///   <en>Parses the security version from ticket UserData.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>票据数据文本。</zh-CN>
        ///   <en>Ticket-data text.</en>
        /// </l>
        /// </param>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>解析出的安全版本。</zh-CN>
        ///   <en>Parsed security version.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>格式有效时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the format is valid.</en>
        /// </l>
        /// </returns>
        public static bool TryParseSecurityVersion(string value, out long securityVersion)
        {
            securityVersion = 0;
            if (string.IsNullOrWhiteSpace(value) ||
                !value.StartsWith(SecurityVersionPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            return long.TryParse(value.Substring(SecurityVersionPrefix.Length), out securityVersion) &&
                   securityVersion >= 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析 Forms Authentication Cookie 使用的应用程序路径。</zh-CN>
        ///   <en>Resolves the application path used by the Forms Authentication cookie.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        ///   <l>
        ///     <zh-CN>当前 HTTP 请求，可为 null。</zh-CN>
        ///     <en>Current HTTP request; may be null.</en>
        ///   </l>
        /// </param>
        /// <returns>
        ///   <l>
        ///     <zh-CN>非空 Cookie Path，无法确定时为根路径。</zh-CN>
        ///     <en>Non-empty cookie path, or root path when it cannot be determined.</en>
        ///   </l>
        /// </returns>
        private static string GetCookiePath(HttpRequest request)
        {
            // <lang>
            //   <zh-CN>提取应用程序路径并在当前 helper 内使用；不接受请求参数作为额外路径片段。</zh-CN>
            //   <en>Extract the application path for this helper only and accept no request parameter as an additional path fragment.</en>
            // </lang>
            string applicationPath = request == null ? null : request.ApplicationPath;
            if (string.IsNullOrWhiteSpace(applicationPath) || applicationPath == "/")
            {
                return "/";
            }

            return applicationPath.TrimEnd('/');
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按与签入相同的 Path 规则让主身份 Cookie 立即过期。</zh-CN>
        ///   <en>Immediately expires the main identity cookie using the same Path rule as sign-in.</en>
        /// </lang>
        /// </summary>
        /// <param name="response">
        ///   <l>
        ///     <zh-CN>当前 HTTP 响应。</zh-CN>
        ///     <en>Current HTTP response.</en>
        ///   </l>
        /// </param>
        /// <param name="request">
        ///   <l>
        ///     <zh-CN>用于解析 Cookie Path 的当前请求。</zh-CN>
        ///     <en>Current request used to resolve the cookie path.</en>
        ///   </l>
        /// </param>
        private static void ExpireAuthenticationCookie(HttpResponse response, HttpRequest request)
        {
            response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, string.Empty)
            {
                Expires = DateTime.Now.AddDays(-1),
                HttpOnly = true,
                Path = GetCookiePath(request)
            });
        }
    }
}
