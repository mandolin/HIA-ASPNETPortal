using System;
using System.Web;
using System.Web.Security;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：统一处理门户角色 Cookie，保持票据到期、Cookie 到期和虚拟目录 Path 规则一致。
    ///
    /// English: Centralizes Portal role-cookie handling so ticket expiration, cookie expiration, and virtual-directory Path rules remain consistent.
    /// </summary>
    /// <remarks>
    /// 中文：Cookie 使用 Forms Authentication 加密票据和 <c>HttpOnly</c>，但当前未设置 <c>Secure</c> 或
    /// <c>SameSite</c>。这些属性须在 HTTPS 部署策略与 IE9+ 兼容边界明确后通过独立配置设计，不能在此直接强制。
    /// 角色变更不会主动撤销已签发 Cookie，通常在票据到期、登出或读取失败后才从数据库重建。
    ///
    /// English: The cookie uses a Forms Authentication encrypted ticket and <c>HttpOnly</c>, but currently sets
    /// neither <c>Secure</c> nor <c>SameSite</c>. Those attributes require a separate configuration design after
    /// HTTPS deployment policy and IE9+ compatibility boundaries are settled, and must not be forced here directly.
    /// Role changes do not proactively revoke issued cookies; roles are normally rebuilt from the database only after
    /// ticket expiration, sign-out, or a read failure.
    /// </remarks>
    public static class PortalAuthenticationCookies
    {
        /// <summary>
        /// 中文：旧门户保存当前用户角色列表的 Cookie 名称。
        ///
        /// English: Cookie name used by the legacy Portal to store the current user's role list.
        /// </summary>
        public const string RolesCookieName = "portalroles";

        /// <summary>
        /// 中文：尝试从角色 Cookie 读取角色；缺失、过期或解密失败时返回 <c>false</c>，调用方应从数据库重新加载。
        ///
        /// English: Attempts to read roles from the role cookie; returns <c>false</c> when missing, expired, or undecryptable so the caller reloads from the database.
        /// </summary>
        /// <param name="request">中文：当前 HTTP 请求，可为 <c>null</c>。English: Current HTTP request; may be <c>null</c>.</param>
        /// <param name="roles">中文：成功时返回规范化角色数组；失败时为空数组。English: Normalized role array on success; otherwise an empty array.</param>
        /// <returns>中文：成功读取未过期加密票据时为 <c>true</c>。English: <c>true</c> when an unexpired encrypted ticket is read successfully.</returns>
        public static bool TryReadRoles(HttpRequest request, out string[] roles)
        {
            roles = new string[0];

            string encryptedTicket = request?.Cookies[RolesCookieName]?.Value;
            if (string.IsNullOrWhiteSpace(encryptedTicket))
            {
                return false;
            }

            try
            {
                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(encryptedTicket);
                if (ticket == null || ticket.Expired)
                {
                    return false;
                }

                roles = PortalRoleParser.Parse(ticket.UserData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 中文：写入角色 Cookie；非持久登录保持会话 Cookie，持久登录才写入过期时间。
        ///
        /// English: Writes the role cookie; non-persistent sign-in keeps a session cookie, while persistent sign-in writes an expiration time.
        /// </summary>
        /// <param name="response">中文：当前 HTTP 响应。English: Current HTTP response.</param>
        /// <param name="request">中文：当前 HTTP 请求，用于解析虚拟目录 Cookie Path。English: Current HTTP request used to resolve the virtual-directory cookie Path.</param>
        /// <param name="userName">中文：认证票据中的用户登录名称。English: User sign-in name stored in the authentication ticket.</param>
        /// <param name="roles">中文：要写入票据的角色集合。English: Role collection to write into the ticket.</param>
        /// <param name="isPersistent">中文：是否写为持久 Cookie。English: Whether to write a persistent cookie.</param>
        public static void WriteRolesCookie(HttpResponse response, HttpRequest request, string userName, string[] roles, bool isPersistent)
        {
            // 中文：Forms Authentication 票据和持久 Cookie 使用同一 timeout，避免二者产生不同过期边界。
            // English: Use the same timeout for the Forms Authentication ticket and persistent cookie to avoid divergent expiration boundaries.
            DateTime issuedAt = DateTime.Now;
            DateTime expiresAt = issuedAt.Add(FormsAuthentication.Timeout);
            string roleData = PortalRoleParser.Join(roles);

            var ticket = new FormsAuthenticationTicket(
                1,
                userName,
                issuedAt,
                expiresAt,
                isPersistent,
                roleData);

            // 中文：当前保持 HttpOnly 与虚拟目录 Path；Secure/SameSite 由后续部署安全策略统一配置。
            // English: Keep HttpOnly and the virtual-directory Path for now; Secure/SameSite are configured by later deployment-security policy.
            var cookie = new HttpCookie(RolesCookieName, FormsAuthentication.Encrypt(ticket))
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
        /// 中文：让当前请求路径下的角色 Cookie 立即失效；Path 必须与写入时一致。
        ///
        /// English: Immediately expires the role cookie for the current request path; Path must match the write path.
        /// </summary>
        /// <param name="response">中文：当前 HTTP 响应。English: Current HTTP response.</param>
        /// <param name="request">中文：当前 HTTP 请求，用于解析虚拟目录 Cookie Path。English: Current HTTP request used to resolve the virtual-directory cookie Path.</param>
        public static void ExpireRolesCookie(HttpResponse response, HttpRequest request)
        {
            response.Cookies.Add(new HttpCookie(RolesCookieName, string.Empty)
            {
                Expires = DateTime.Now.AddDays(-1),
                HttpOnly = true,
                Path = GetCookiePath(request)
            });
        }

        private static string GetCookiePath(HttpRequest request)
        {
            // 中文：根站点使用 /，虚拟目录去除末尾 / 后作为 Cookie Path。
            // English: Root sites use /; virtual directories use the application path without a trailing / as the cookie Path.
            string applicationPath = request?.ApplicationPath;
            if (string.IsNullOrWhiteSpace(applicationPath) || applicationPath == "/")
            {
                return "/";
            }

            return applicationPath.TrimEnd('/');
        }
    }
}
