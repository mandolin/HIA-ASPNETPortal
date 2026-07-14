using System;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：集中定义门户页面的站内回跳、普通资源链接和部署资源路径安全边界。
    ///
    /// English: Centralizes safety boundaries for Portal return navigation, ordinary resource links, and deployed resource paths.
    /// </summary>
    /// <remarks>
    /// 中文：该策略只负责地址语义与重定向控制流，不替代模块编辑权限、文件授权或未来的细粒度资源权限。
    /// 普通浏览地址可为当前应用内地址或 HTTP(S) 外链；XML/XSL 等部署资源只能位于当前应用目录。
    ///
    /// English: This policy covers URL semantics and redirect control flow only. It does not replace module-edit
    /// authorization, file authorization, or future granular resource permissions. Ordinary browse URLs may be
    /// current-application paths or HTTP(S) external links; deployed XML/XSL resources must remain within the current application.
    /// </remarks>
    public static class PortalNavigationPolicy
    {
        /// <summary>
        /// 中文：校验并规范化一个普通浏览地址。
        ///
        /// English: Validates and normalizes an ordinary browse URL.
        /// </summary>
        /// <param name="candidate">中文：编辑者输入或旧记录保存的候选地址。English: Candidate address entered by an editor or stored in a legacy record.</param>
        /// <param name="request">中文：当前请求，用于确认根路径仍在当前应用虚拟目录内。English: Current request used to ensure a root path remains inside the current application virtual directory.</param>
        /// <param name="normalizedUrl">中文：成功时返回可使用的地址；失败时为空。English: Usable URL when successful; otherwise empty.</param>
        /// <returns>中文：候选地址为允许的应用内地址或 HTTP(S) 外链时为 <c>true</c>。English: <c>true</c> when the candidate is an allowed application path or HTTP(S) external link.</returns>
        public static bool TryNormalizeBrowseUrl(string candidate, HttpRequest request, out string normalizedUrl)
        {
            normalizedUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            string value = candidate.Trim();
            if (value.Length > 2048 || ContainsControlCharacter(value) ||
                value.StartsWith("//", StringComparison.Ordinal) || value.StartsWith("\\\\", StringComparison.Ordinal))
            {
                return false;
            }

            Uri absoluteUri;
            if (Uri.TryCreate(value, UriKind.Absolute, out absoluteUri))
            {
                if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedUrl = absoluteUri.AbsoluteUri;
                    return true;
                }

                return false;
            }

            if (value.IndexOf('\\') >= 0 || HasTraversalSegment(value))
            {
                return false;
            }

            if (value.StartsWith("~/", StringComparison.Ordinal))
            {
                normalizedUrl = value;
                return true;
            }

            if (value.StartsWith("/", StringComparison.Ordinal))
            {
                if (!IsCurrentApplicationPath(value, request))
                {
                    return false;
                }

                normalizedUrl = value;
                return true;
            }

            if (value.StartsWith("~", StringComparison.Ordinal))
            {
                return false;
            }

            normalizedUrl = value;
            return true;
        }

        /// <summary>
        /// 中文：校验并规范化 XML、XSL 等受信任部署资源的应用内路径。
        ///
        /// English: Validates and normalizes a current-application path for trusted deployed resources such as XML and XSL files.
        /// </summary>
        /// <param name="candidate">中文：模块设置中的候选路径。English: Candidate path from module settings.</param>
        /// <param name="request">中文：当前请求，用于确认根路径仍在当前应用虚拟目录内。English: Current request used to ensure a root path remains inside the current application virtual directory.</param>
        /// <param name="normalizedPath">中文：成功时返回应用内虚拟路径；失败时为空。English: Current-application virtual path when successful; otherwise empty.</param>
        /// <returns>中文：路径为当前应用内的安全部署资源路径时为 <c>true</c>。English: <c>true</c> when the path is a safe deployed resource path inside the current application.</returns>
        public static bool TryNormalizeTrustedDeploymentResourcePath(string candidate, HttpRequest request, out string normalizedPath)
        {
            normalizedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            string value = candidate.Trim();
            if (value.Length > 2048 || ContainsControlCharacter(value) || value.IndexOf('?') >= 0 || value.IndexOf('#') >= 0 ||
                value.StartsWith("//", StringComparison.Ordinal) || value.StartsWith("\\\\", StringComparison.Ordinal) ||
                value.IndexOf('\\') >= 0 || HasTraversalSegment(value))
            {
                return false;
            }

            Uri absoluteUri;
            if (Uri.TryCreate(value, UriKind.Absolute, out absoluteUri))
            {
                return false;
            }

            if (value.StartsWith("~/", StringComparison.Ordinal))
            {
                normalizedPath = value;
                return true;
            }

            if (value.StartsWith("/", StringComparison.Ordinal))
            {
                if (!IsCurrentApplicationPath(value, request))
                {
                    return false;
                }

                normalizedPath = value;
                return true;
            }

            if (value.StartsWith("~", StringComparison.Ordinal))
            {
                return false;
            }

            // 中文：兼容旧配置中的相对路径，但保存时统一为应用相对路径，避免其受当前页面目录影响。
            // English: Accept legacy relative paths, but normalize them to application-relative paths so they do not depend on the current page directory.
            if (value.StartsWith("./", StringComparison.Ordinal))
            {
                value = value.Substring(2);
            }

            normalizedPath = "~/" + value;
            return true;
        }

        /// <summary>
        /// 中文：从候选 Referer 或 ViewState 回跳值中解析当前应用内的安全返回地址。
        ///
        /// English: Resolves a safe current-application return address from a candidate Referer or ViewState value.
        /// </summary>
        /// <param name="request">中文：当前 HTTP 请求。English: Current HTTP request.</param>
        /// <param name="candidate">中文：候选回跳地址，可为空。English: Candidate return address; may be empty.</param>
        /// <returns>中文：当前应用内安全地址；非法或缺失时返回门户首页。English: Safe current-application URL, or the Portal home page when invalid or missing.</returns>
        public static string GetSafeReturnUrl(HttpRequest request, string candidate)
        {
            string normalizedUrl;
            if (TryNormalizeReturnUrl(request, candidate, out normalizedUrl))
            {
                return normalizedUrl;
            }

            return GetPortalHomeUrl(request);
        }

        /// <summary>
        /// 中文：从当前请求 Referer 解析安全返回地址。
        ///
        /// English: Resolves a safe return address from the current request Referer.
        /// </summary>
        /// <param name="request">中文：当前 HTTP 请求。English: Current HTTP request.</param>
        /// <returns>中文：当前应用内安全地址；没有有效 Referer 时返回门户首页。English: Safe current-application URL, or the Portal home page when no valid Referer exists.</returns>
        public static string GetSafeReturnUrl(HttpRequest request)
        {
            Uri referer = request == null ? null : request.UrlReferrer;
            return GetSafeReturnUrl(request, referer == null ? null : referer.AbsoluteUri);
        }

        /// <summary>
        /// 中文：向安全返回地址重定向，并避免 <see cref="HttpResponse.End"/> 造成线程中止。
        ///
        /// English: Redirects to a safe return URL while avoiding the thread abort caused by <see cref="HttpResponse.End"/>.
        /// </summary>
        /// <param name="context">中文：当前 HTTP 上下文。English: Current HTTP context.</param>
        /// <param name="candidate">中文：候选回跳地址。English: Candidate return address.</param>
        public static void RedirectToSafeReturnUrl(HttpContext context, string candidate)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            context.Response.Redirect(GetSafeReturnUrl(context.Request, candidate), false);
            context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// 中文：重定向到编辑拒绝页，并避免 <see cref="HttpResponse.End"/> 造成线程中止。
        ///
        /// English: Redirects to the edit-access-denied page while avoiding the thread abort caused by <see cref="HttpResponse.End"/>.
        /// </summary>
        /// <param name="context">中文：当前 HTTP 上下文。English: Current HTTP context.</param>
        public static void RedirectToEditAccessDenied(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            context.Response.Redirect("~/Admin/EditAccessDenied.aspx", false);
            context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// 中文：尝试读取正整数请求参数。
        ///
        /// English: Attempts to read a positive integer request parameter.
        /// </summary>
        /// <param name="value">中文：原始参数值。English: Raw parameter value.</param>
        /// <param name="parsedValue">中文：成功时返回正整数；失败时为零。English: Positive integer when successful; otherwise zero.</param>
        /// <returns>中文：参数是正整数时为 <c>true</c>。English: <c>true</c> when the parameter is a positive integer.</returns>
        public static bool TryReadPositiveInt32(string value, out int parsedValue)
        {
            return int.TryParse(value, out parsedValue) && parsedValue > 0;
        }

        /// <summary>
        /// 中文：尝试读取非负整数请求参数，用于旧数据中允许从零开始的标识，例如种子管理员角色。
        ///
        /// English: Attempts to read a nonnegative integer request parameter for legacy identifiers that may start at
        /// zero, such as the seeded administrator role.
        /// </summary>
        /// <param name="value">中文：原始参数值。English: Raw parameter value.</param>
        /// <param name="parsedValue">中文：成功时返回非负整数；失败时为零。English: Nonnegative integer when successful; otherwise zero.</param>
        /// <returns>中文：参数为非负整数时为 <c>true</c>。English: <c>true</c> when the parameter is a nonnegative integer.</returns>
        public static bool TryReadNonNegativeInt32(string value, out int parsedValue)
        {
            return int.TryParse(value, out parsedValue) && parsedValue >= 0;
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTraversalSegment(string value)
        {
            string path = value.Split(new[] { '?', '#' }, 2)[0];
            try
            {
                path = Uri.UnescapeDataString(path);
            }
            catch (UriFormatException)
            {
                return true;
            }

            foreach (string segment in path.Replace('\\', '/').Split('/'))
            {
                if (string.Equals(segment, "..", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryNormalizeReturnUrl(HttpRequest request, string candidate, out string normalizedUrl)
        {
            normalizedUrl = string.Empty;
            if (request == null || string.IsNullOrWhiteSpace(candidate) || ContainsControlCharacter(candidate))
            {
                return false;
            }

            // 中文：首次请求把合法 Referer 规范为 PathAndQuery 保存到 ViewState；回发时仍须接受该站内格式。
            // English: The first request stores a valid Referer as PathAndQuery in ViewState, which must remain valid on postback.
            if (candidate.StartsWith("/", StringComparison.Ordinal) &&
                !candidate.StartsWith("//", StringComparison.Ordinal) &&
                IsCurrentApplicationPath(candidate, request))
            {
                normalizedUrl = candidate;
                return true;
            }

            Uri requestUri = request.Url;
            Uri candidateUri;
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out candidateUri) || requestUri == null ||
                !string.Equals(candidateUri.Scheme, requestUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(candidateUri.Host, requestUri.Host, StringComparison.OrdinalIgnoreCase) ||
                candidateUri.Port != requestUri.Port ||
                !IsCurrentApplicationPath(candidateUri.AbsolutePath, request))
            {
                return false;
            }

            normalizedUrl = candidateUri.PathAndQuery;
            return true;
        }

        private static bool IsCurrentApplicationPath(string absolutePath, HttpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(absolutePath) ||
                absolutePath.StartsWith("//", StringComparison.Ordinal) || absolutePath.IndexOf('\\') >= 0 ||
                HasTraversalSegment(absolutePath))
            {
                return false;
            }

            string pathOnly = absolutePath.Split(new[] { '?', '#' }, 2)[0];
            string decodedPath;
            try
            {
                decodedPath = Uri.UnescapeDataString(pathOnly);
            }
            catch (UriFormatException)
            {
                return false;
            }

            if (decodedPath.StartsWith("//", StringComparison.Ordinal) || decodedPath.IndexOf('\\') >= 0 ||
                HasTraversalSegment(decodedPath))
            {
                return false;
            }

            string applicationPath = request.ApplicationPath;
            if (string.IsNullOrEmpty(applicationPath) || string.Equals(applicationPath, "/", StringComparison.Ordinal))
            {
                return true;
            }

            string normalizedApplicationPath = applicationPath.TrimEnd('/');
            return string.Equals(pathOnly, normalizedApplicationPath, StringComparison.OrdinalIgnoreCase) ||
                   pathOnly.StartsWith(normalizedApplicationPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetPortalHomeUrl(HttpRequest request)
        {
            string applicationPath = request == null ? string.Empty : request.ApplicationPath;
            if (string.IsNullOrEmpty(applicationPath) || string.Equals(applicationPath, "/", StringComparison.Ordinal))
            {
                return "/DesktopDefault.aspx";
            }

            return applicationPath.TrimEnd('/') + "/DesktopDefault.aspx";
        }
    }
}
