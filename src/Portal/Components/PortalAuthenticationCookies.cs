using System;
using System.Web;
using System.Web.Security;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>统一处理门户角色 Cookie，保持票据到期、Cookie 到期和虚拟目录 Path 规则一致。</zh-CN>
    ///   <en>Centralizes Portal role-cookie handling so ticket expiration, cookie expiration, and virtual-directory Path rules remain consistent.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>Cookie 使用 Forms Authentication 加密票据和 <c>HttpOnly</c>，但当前未设置 <c>Secure</c> 或 <c>SameSite</c>。这些属性须在 HTTPS 部署策略与 IE9+ 兼容边界明确后通过独立配置设计，不能在此直接强制。 角色变更不会主动撤销已签发 Cookie，通常在票据到期、登出或读取失败后才从数据库重建。</zh-CN>
    ///   <en>The cookie uses a Forms Authentication encrypted ticket and <c>HttpOnly</c>, but currently sets neither <c>Secure</c> nor <c>SameSite</c>. Those attributes require a separate configuration design after HTTPS deployment policy and IE9+ compatibility boundaries are settled, and must not be forced here directly. Role changes do not proactively revoke issued cookies; roles are normally rebuilt from the database only after ticket expiration, sign-out, or a read failure.</en>
    /// </lang>
    /// </remarks>
    public static class PortalAuthenticationCookies
    {
        private const string RolesDataSeparator = "\nroles:";

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户保存当前用户角色列表的 Cookie 名称。</zh-CN>
        ///   <en>Cookie name used by the legacy Portal to store the current user's role list.</en>
        /// </lang>
        /// </summary>
        public const string RolesCookieName = "portalroles";

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试从角色 Cookie 读取角色；缺失、过期、解密失败或安全版本不匹配时返回 <c>false</c>，调用方应从数据库重新加载。</zh-CN>
        ///   <en>Attempts to read roles from the role cookie; returns <c>false</c> when missing, expired, undecryptable, or security-version mismatched so the caller reloads from the database.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP request; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <param name="expectedSecurityVersion">
        /// <l>
        ///   <zh-CN>主身份票据和数据库确认的安全版本。</zh-CN>
        ///   <en>Security version confirmed by the main auth ticket and database.</en>
        /// </l>
        /// </param>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>成功时返回规范化角色数组；失败时为空数组。</zh-CN>
        ///   <en>Normalized role array on success; otherwise an empty array.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功读取未过期加密票据时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when an unexpired encrypted ticket is read successfully.</en>
        /// </l>
        /// </returns>
        public static bool TryReadRoles(HttpRequest request, long expectedSecurityVersion, out string[] roles)
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

                long securityVersion;
                string roleData;
                if (!TryParseRoleData(ticket.UserData, out securityVersion, out roleData) ||
                    securityVersion != expectedSecurityVersion)
                {
                    return false;
                }

                roles = PortalRoleParser.Parse(roleData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>写入角色 Cookie；非持久登录保持会话 Cookie，持久登录才写入过期时间。</zh-CN>
        ///   <en>Writes the role cookie; non-persistent sign-in keeps a session cookie, while persistent sign-in writes an expiration time.</en>
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
        ///   <zh-CN>当前 HTTP 请求，用于解析虚拟目录 Cookie Path。</zh-CN>
        ///   <en>Current HTTP request used to resolve the virtual-directory cookie Path.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>认证票据中的用户登录名称。</zh-CN>
        ///   <en>User sign-in name stored in the authentication ticket.</en>
        /// </l>
        /// </param>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>当前用户安全版本。</zh-CN>
        ///   <en>Current user security version.</en>
        /// </l>
        /// </param>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>要写入票据的角色集合。</zh-CN>
        ///   <en>Role collection to write into the ticket.</en>
        /// </l>
        /// </param>
        /// <param name="isPersistent">
        /// <l>
        ///   <zh-CN>是否写为持久 Cookie。</zh-CN>
        ///   <en>Whether to write a persistent cookie.</en>
        /// </l>
        /// </param>
        public static void WriteRolesCookie(
            HttpResponse response,
            HttpRequest request,
            string userName,
            long securityVersion,
            string[] roles,
            bool isPersistent)
        {
            // <lang>
            //   <zh-CN>Forms Authentication 票据和持久 Cookie 使用同一 timeout，避免二者产生不同过期边界。</zh-CN>
            //   <en>Use the same timeout for the Forms Authentication ticket and persistent cookie to avoid divergent expiration boundaries.</en>
            // </lang>
            DateTime issuedAt = DateTime.Now;
            DateTime expiresAt = issuedAt.Add(FormsAuthentication.Timeout);
            string roleData = BuildRoleData(securityVersion, roles);

            var ticket = new FormsAuthenticationTicket(
                1,
                userName,
                issuedAt,
                expiresAt,
                isPersistent,
                roleData);

            // <lang>
            //   <zh-CN>当前保持 HttpOnly 与虚拟目录 Path；Secure/SameSite 由后续部署安全策略统一配置。</zh-CN>
            //   <en>Keep HttpOnly and the virtual-directory Path for now; Secure/SameSite are configured by later deployment-security policy.</en>
            // </lang>
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
        /// <lang>
        ///   <zh-CN>让当前请求路径下的角色 Cookie 立即失效；Path 必须与写入时一致。</zh-CN>
        ///   <en>Immediately expires the role cookie for the current request path; Path must match the write path.</en>
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
        ///   <zh-CN>当前 HTTP 请求，用于解析虚拟目录 Cookie Path。</zh-CN>
        ///   <en>Current HTTP request used to resolve the virtual-directory cookie Path.</en>
        /// </l>
        /// </param>
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
            // <lang>
            //   <zh-CN>根站点使用 /，虚拟目录去除末尾 / 后作为 Cookie Path。</zh-CN>
            //   <en>Root sites use /; virtual directories use the application path without a trailing / as the cookie Path.</en>
            // </lang>
            string applicationPath = request?.ApplicationPath;
            if (string.IsNullOrWhiteSpace(applicationPath) || applicationPath == "/")
            {
                return "/";
            }

            return applicationPath.TrimEnd('/');
        }

        private static string BuildRoleData(long securityVersion, string[] roles)
        {
            return PortalAuthenticationService.FormatSecurityVersion(securityVersion) +
                   RolesDataSeparator +
                   PortalRoleParser.Join(roles);
        }

        private static bool TryParseRoleData(string value, out long securityVersion, out string roleData)
        {
            securityVersion = 0;
            roleData = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            int separatorIndex = value.IndexOf(RolesDataSeparator, StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return false;
            }

            string versionData = value.Substring(0, separatorIndex);
            if (!PortalAuthenticationService.TryParseSecurityVersion(versionData, out securityVersion))
            {
                return false;
            }

            roleData = value.Substring(separatorIndex + RolesDataSeparator.Length);
            return true;
        }
    }
}
