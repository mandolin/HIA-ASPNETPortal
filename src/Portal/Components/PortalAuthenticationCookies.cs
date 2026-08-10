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
        // <lang>
        //   <zh-CN>角色票据 UserData 的固定分隔符；它不允许外部请求改写编码结构。</zh-CN>
        //   <en>Fixed separator for role-ticket UserData; external requests cannot rewrite the encoding structure.</en>
        // </lang>
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
            // <lang>
            //   <zh-CN>先以空数组建立失败输出，确保 Cookie 缺失、过期、解密失败或版本不匹配时不泄露旧角色状态。</zh-CN>
            //   <en>Initialize an empty failure output so missing, expired, undecryptable, or version-mismatched cookies never leak stale roles.</en>
            // </lang>
            roles = new string[0];

            // <lang>
            //   <zh-CN>只读取固定角色 Cookie 的密文文本，不把请求其它 Cookie 或原始票据内容带入解析。</zh-CN>
            //   <en>Read only the fixed role-cookie ciphertext and bring no other request cookies or raw ticket content into parsing.</en>
            // </lang>
            string encryptedTicket = request?.Cookies[RolesCookieName]?.Value;
            if (string.IsNullOrWhiteSpace(encryptedTicket))
            {
                return false;
            }

            try
            {
                // <lang>
                //   <zh-CN>解密 Forms Authentication 票据并立即检查存在性和过期状态，失败即拒绝角色恢复。</zh-CN>
                //   <en>Decrypt the Forms Authentication ticket and immediately check presence and expiration, rejecting role recovery on failure.</en>
                // </lang>
                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(encryptedTicket);
                if (ticket == null || ticket.Expired)
                {
                    return false;
                }

                // <lang>
                //   <zh-CN>承接角色票据内的安全版本和角色片段，仅在当前读取调用中短暂使用。</zh-CN>
                //   <en>Receive the ticket security version and role segment only for the current read call.</en>
                // </lang>
                long securityVersion;

                // <lang>
                //   <zh-CN>保存未解析角色数据片段；它必须先通过版本匹配和角色解析器才能进入输出数组。</zh-CN>
                //   <en>Keep the unparsed role-data segment; it must pass version matching and the role parser before entering output.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>记录角色票据签发时间，与主身份票据使用同一 Forms Authentication timeout。</zh-CN>
            //   <en>Record the role-ticket issuance time using the same Forms Authentication timeout as the main identity ticket.</en>
            // </lang>
            DateTime issuedAt = DateTime.Now;

            // <lang>
            //   <zh-CN>计算角色票据过期边界，持久 Cookie 只复用该时间而不自行延长。</zh-CN>
            //   <en>Compute the role-ticket expiration boundary; a persistent cookie reuses it without extending it.</en>
            // </lang>
            DateTime expiresAt = issuedAt.Add(FormsAuthentication.Timeout);

            // <lang>
            //   <zh-CN>将安全版本和角色集合编码为兼容 UserData；角色列表由受控解析器规范化。</zh-CN>
            //   <en>Encode security version and roles into compatible UserData; the controlled role parser owns normalization.</en>
            // </lang>
            string roleData = BuildRoleData(securityVersion, roles);

            // <lang>
            //   <zh-CN>创建加密角色票据，生命周期和持久性由调用方显式选择。</zh-CN>
            //   <en>Create the encrypted role ticket with lifetime and persistence explicitly selected by the caller.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>封装加密票据为角色 Cookie，并复用 Path 规则；不把角色文本直接写入 Cookie value。</zh-CN>
            //   <en>Wrap the encrypted ticket as the role cookie and reuse the Path rule; never write role text directly to the cookie value.</en>
            // </lang>
            var cookie = new HttpCookie(RolesCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Path = GetCookiePath(request)
            };

            if (isPersistent)
            {
                // <lang>
                //   <zh-CN>仅持久登录写入过期时间；会话登录保持浏览器会话 Cookie 语义。</zh-CN>
                //   <en>Write an expiration only for persistent sign-in; session sign-in retains browser-session cookie semantics.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据当前请求解析角色 Cookie 的应用程序路径；根站点和虚拟目录必须与写入、失效操作使用同一规则。</zh-CN>
        ///   <en>Resolves the application path for the role cookie; root sites and virtual directories must use the same rule for writing and expiration.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求，可为 <c>null</c>。</zh-CN>
        ///   <en>Current HTTP request; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可用于 Cookie Path 的非空路径；无法确定时返回根路径 <c>/</c>。</zh-CN>
        ///   <en>A non-empty Cookie Path; returns the root path <c>/</c> when it cannot be determined.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把安全版本和角色集合编码为角色票据的 UserData；编码格式必须与解析端保持兼容。</zh-CN>
        ///   <en>Encodes the security version and role collection as role-ticket UserData; the format must remain compatible with the parser.</en>
        /// </lang>
        /// </summary>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>当前用户的安全版本，用于使旧角色票据失效。</zh-CN>
        ///   <en>Current user security version, used to invalidate stale role tickets.</en>
        /// </l>
        /// </param>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>要写入票据的角色集合。</zh-CN>
        ///   <en>Role collection to store in the ticket.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>包含版本分隔符和规范化角色数据的 UserData 字符串。</zh-CN>
        ///   <en>UserData string containing the version separator and normalized role data.</en>
        /// </l>
        /// </returns>
        private static string BuildRoleData(long securityVersion, string[] roles)
        {
            return PortalAuthenticationService.FormatSecurityVersion(securityVersion) +
                   RolesDataSeparator +
                   PortalRoleParser.Join(roles);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析角色票据 UserData 中的安全版本和角色片段；格式不完整或版本无法解析时拒绝使用该票据。</zh-CN>
        ///   <en>Parses the security version and role segment from role-ticket UserData; rejects the ticket when its format is incomplete or the version is invalid.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待解析的票据 UserData。</zh-CN>
        ///   <en>Ticket UserData to parse.</en>
        /// </l>
        /// </param>
        /// <param name="securityVersion">
        /// <l>
        ///   <zh-CN>成功时返回票据携带的安全版本；失败时为 <c>0</c>。</zh-CN>
        ///   <en>Ticket security version on success; <c>0</c> on failure.</en>
        /// </l>
        /// </param>
        /// <param name="roleData">
        /// <l>
        ///   <zh-CN>成功时返回角色数据片段；失败时为空字符串。</zh-CN>
        ///   <en>Role-data segment on success; an empty string on failure.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>UserData 同时包含有效安全版本和角色分隔符时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when UserData contains a valid security version and role separator.</en>
        /// </l>
        /// </returns>
        private static bool TryParseRoleData(string value, out long securityVersion, out string roleData)
        {
            securityVersion = 0;
            roleData = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>查找固定分隔符位置，拒绝缺少角色片段边界的票据数据。</zh-CN>
            //   <en>Find the fixed separator and reject ticket data without a role-segment boundary.</en>
            // </lang>
            int separatorIndex = value.IndexOf(RolesDataSeparator, StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>提取分隔符前的安全版本文本，交给主认证服务按固定格式校验。</zh-CN>
            //   <en>Extract the security-version text before the separator and validate it through the main authentication service.</en>
            // </lang>
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
