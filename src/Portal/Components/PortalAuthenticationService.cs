using System;
using System.Web;
using System.Web.Security;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户 Forms Authentication 身份票据门面。
    ///
    /// English: Forms Authentication ticket facade for the Portal.
    /// </summary>
    /// <remarks>
    /// 中文：P5.2 起身份票据的 <c>UserData</c> 只保存用户安全版本，不保存角色、密码材料或业务资料。
    /// 角色列表仍由 <see cref="PortalAuthenticationCookies"/> 单独加密保存，并使用相同安全版本校验。
    ///
    /// English: Starting with P5.2, the authentication ticket <c>UserData</c> stores only the user security version,
    /// never roles, password material, or business profile data. Role lists remain in the separately encrypted
    /// <see cref="PortalAuthenticationCookies"/> cookie and are validated with the same security version.
    /// </remarks>
    public static class PortalAuthenticationService
    {
        private const string SecurityVersionPrefix = "sv:";

        /// <summary>
        /// 中文：签发带安全版本的 Forms Authentication 身份票据。
        ///
        /// English: Issues a Forms Authentication ticket with the security version.
        /// </summary>
        /// <param name="response">中文：当前 HTTP 响应。English: Current HTTP response.</param>
        /// <param name="request">中文：当前 HTTP 请求，用于解析 Cookie Path。English: Current HTTP request used to resolve the cookie path.</param>
        /// <param name="userName">中文：认证用户名称。English: Authenticated user name.</param>
        /// <param name="securityVersion">中文：用户当前安全版本。English: Current user security version.</param>
        /// <param name="isPersistent">中文：是否写为持久 Cookie。English: Whether to write a persistent cookie.</param>
        public static void SignIn(
            HttpResponse response,
            HttpRequest request,
            string userName,
            long securityVersion,
            bool isPersistent)
        {
            DateTime issuedAt = DateTime.Now;
            DateTime expiresAt = issuedAt.Add(FormsAuthentication.Timeout);
            var ticket = new FormsAuthenticationTicket(
                1,
                userName,
                issuedAt,
                expiresAt,
                isPersistent,
                FormatSecurityVersion(securityVersion));

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
        /// 中文：注销当前身份并清理角色 Cookie。
        ///
        /// English: Signs out the current identity and clears the role cookie.
        /// </summary>
        /// <param name="response">中文：当前 HTTP 响应。English: Current HTTP response.</param>
        /// <param name="request">中文：当前 HTTP 请求，用于解析 Cookie Path。English: Current HTTP request used to resolve the cookie path.</param>
        public static void SignOut(HttpResponse response, HttpRequest request)
        {
            FormsAuthentication.SignOut();
            ExpireAuthenticationCookie(response, request);
            PortalAuthenticationCookies.ExpireRolesCookie(response, request);
        }

        /// <summary>
        /// 中文：从 Forms Authentication 身份中读取安全版本。
        ///
        /// English: Reads the security version from a Forms Authentication identity.
        /// </summary>
        /// <param name="identity">中文：Forms 身份。English: Forms identity.</param>
        /// <param name="securityVersion">中文：读取到的安全版本。English: Parsed security version.</param>
        /// <returns>中文：成功解析安全版本时为 <c>true</c>。English: <c>true</c> when the security version was parsed successfully.</returns>
        public static bool TryReadSecurityVersion(FormsIdentity identity, out long securityVersion)
        {
            securityVersion = 0;
            return identity != null &&
                   identity.Ticket != null &&
                   !identity.Ticket.Expired &&
                   TryParseSecurityVersion(identity.Ticket.UserData, out securityVersion);
        }

        /// <summary>
        /// 中文：将安全版本格式化为票据 UserData。
        ///
        /// English: Formats a security version for ticket UserData.
        /// </summary>
        /// <param name="securityVersion">中文：安全版本。English: Security version.</param>
        /// <returns>中文：稳定的票据数据文本。English: Stable ticket-data text.</returns>
        public static string FormatSecurityVersion(long securityVersion)
        {
            return SecurityVersionPrefix + Math.Max(0, securityVersion);
        }

        /// <summary>
        /// 中文：解析票据 UserData 中的安全版本。
        ///
        /// English: Parses the security version from ticket UserData.
        /// </summary>
        /// <param name="value">中文：票据数据文本。English: Ticket-data text.</param>
        /// <param name="securityVersion">中文：解析出的安全版本。English: Parsed security version.</param>
        /// <returns>中文：格式有效时为 <c>true</c>。English: <c>true</c> when the format is valid.</returns>
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

        private static string GetCookiePath(HttpRequest request)
        {
            string applicationPath = request == null ? null : request.ApplicationPath;
            if (string.IsNullOrWhiteSpace(applicationPath) || applicationPath == "/")
            {
                return "/";
            }

            return applicationPath.TrimEnd('/');
        }

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
