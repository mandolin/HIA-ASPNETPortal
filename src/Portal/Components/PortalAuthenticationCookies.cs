using System;
using System.Web;
using System.Web.Security;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 统一处理门户角色 Cookie，避免票据过期、Cookie 过期和虚拟目录 Path 规则彼此不一致。
    /// </summary>
    public static class PortalAuthenticationCookies
    {
        /// <summary>
        /// 旧门户用于保存当前用户角色列表的 Cookie 名称。
        /// </summary>
        public const string RolesCookieName = "portalroles";

        /// <summary>
        /// 尝试从角色 Cookie 中读取角色；缺失、过期或解密失败时返回 false，由调用方重新从数据库加载。
        /// </summary>
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
        /// 写入角色 Cookie。非持久登录时保持会话 Cookie；持久登录时才写 Expires。
        /// </summary>
        public static void WriteRolesCookie(HttpResponse response, HttpRequest request, string userName, string[] roles, bool isPersistent)
        {
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
        /// 让角色 Cookie 立即失效。Path 必须和写入时一致，否则虚拟目录下可能删不干净。
        /// </summary>
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
            string applicationPath = request?.ApplicationPath;
            if (string.IsNullOrWhiteSpace(applicationPath) || applicationPath == "/")
            {
                return "/";
            }

            return applicationPath.TrimEnd('/');
        }
    }
}
