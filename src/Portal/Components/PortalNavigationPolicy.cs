using System;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>集中定义门户页面的站内回跳、普通资源链接和部署资源路径安全边界。</zh-CN>
    ///   <en>Centralizes safety boundaries for Portal return navigation, ordinary resource links, and deployed resource paths.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该策略只负责地址语义与重定向控制流，不替代模块编辑权限、文件授权或未来的细粒度资源权限。 普通浏览地址可为当前应用内地址或 HTTP(S) 外链；XML/XSL 等部署资源只能位于当前应用目录。</zh-CN>
    ///   <en>This policy covers URL semantics and redirect control flow only. It does not replace module-edit authorization, file authorization, or future granular resource permissions. Ordinary browse URLs may be current-application paths or HTTP(S) external links; deployed XML/XSL resources must remain within the current application.</en>
    /// </lang>
    /// </remarks>
    public static class PortalNavigationPolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>校验并规范化一个普通浏览地址。</zh-CN>
        ///   <en>Validates and normalizes an ordinary browse URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>编辑者输入或旧记录保存的候选地址。</zh-CN>
        ///   <en>Candidate address entered by an editor or stored in a legacy record.</en>
        /// </l>
        /// </param>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前请求，用于确认根路径仍在当前应用虚拟目录内。</zh-CN>
        ///   <en>Current request used to ensure a root path remains inside the current application virtual directory.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedUrl">
        /// <l>
        ///   <zh-CN>成功时返回可使用的地址；失败时为空。</zh-CN>
        ///   <en>Usable URL when successful; otherwise empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选地址为允许的应用内地址或 HTTP(S) 外链时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the candidate is an allowed application path or HTTP(S) external link.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>校验并规范化 XML、XSL 等受信任部署资源的应用内路径。</zh-CN>
        ///   <en>Validates and normalizes a current-application path for trusted deployed resources such as XML and XSL files.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>模块设置中的候选路径。</zh-CN>
        ///   <en>Candidate path from module settings.</en>
        /// </l>
        /// </param>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前请求，用于确认根路径仍在当前应用虚拟目录内。</zh-CN>
        ///   <en>Current request used to ensure a root path remains inside the current application virtual directory.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedPath">
        /// <l>
        ///   <zh-CN>成功时返回应用内虚拟路径；失败时为空。</zh-CN>
        ///   <en>Current-application virtual path when successful; otherwise empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>路径为当前应用内的安全部署资源路径时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the path is a safe deployed resource path inside the current application.</en>
        /// </l>
        /// </returns>
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

            // <lang>
            //   <zh-CN>兼容旧配置中的相对路径，但保存时统一为应用相对路径，避免其受当前页面目录影响。</zh-CN>
            //   <en>Accept legacy relative paths, but normalize them to application-relative paths so they do not depend on the current page directory.</en>
            // </lang>
            if (value.StartsWith("./", StringComparison.Ordinal))
            {
                value = value.Substring(2);
            }

            normalizedPath = "~/" + value;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从候选 Referer 或 ViewState 回跳值中解析当前应用内的安全返回地址。</zh-CN>
        ///   <en>Resolves a safe current-application return address from a candidate Referer or ViewState value.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求。</zh-CN>
        ///   <en>Current HTTP request.</en>
        /// </l>
        /// </param>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>候选回跳地址，可为空。</zh-CN>
        ///   <en>Candidate return address; may be empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前应用内安全地址；非法或缺失时返回门户首页。</zh-CN>
        ///   <en>Safe current-application URL, or the Portal home page when invalid or missing.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>从当前请求 Referer 解析安全返回地址。</zh-CN>
        ///   <en>Resolves a safe return address from the current request Referer.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前 HTTP 请求。</zh-CN>
        ///   <en>Current HTTP request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前应用内安全地址；没有有效 Referer 时返回门户首页。</zh-CN>
        ///   <en>Safe current-application URL, or the Portal home page when no valid Referer exists.</en>
        /// </l>
        /// </returns>
        public static string GetSafeReturnUrl(HttpRequest request)
        {
            Uri referer = request == null ? null : request.UrlReferrer;
            return GetSafeReturnUrl(request, referer == null ? null : referer.AbsoluteUri);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>向安全返回地址重定向，并避免 <see cref="HttpResponse.End"/> 造成线程中止。</zh-CN>
        ///   <en>Redirects to a safe return URL while avoiding the thread abort caused by <see cref="HttpResponse.End"/>.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>候选回跳地址。</zh-CN>
        ///   <en>Candidate return address.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>重定向到编辑拒绝页，并避免 <see cref="HttpResponse.End"/> 造成线程中止。</zh-CN>
        ///   <en>Redirects to the edit-access-denied page while avoiding the thread abort caused by <see cref="HttpResponse.End"/>.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前 HTTP 上下文。</zh-CN>
        ///   <en>Current HTTP context.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>尝试读取正整数请求参数。</zh-CN>
        ///   <en>Attempts to read a positive integer request parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始参数值。</zh-CN>
        ///   <en>Raw parameter value.</en>
        /// </l>
        /// </param>
        /// <param name="parsedValue">
        /// <l>
        ///   <zh-CN>成功时返回正整数；失败时为零。</zh-CN>
        ///   <en>Positive integer when successful; otherwise zero.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数是正整数时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is a positive integer.</en>
        /// </l>
        /// </returns>
        public static bool TryReadPositiveInt32(string value, out int parsedValue)
        {
            return int.TryParse(value, out parsedValue) && parsedValue > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试读取非负整数请求参数，用于旧数据中允许从零开始的标识，例如种子管理员角色。</zh-CN>
        ///   <en>Attempts to read a nonnegative integer request parameter for legacy identifiers that may start at zero, such as the seeded administrator role.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始参数值。</zh-CN>
        ///   <en>Raw parameter value.</en>
        /// </l>
        /// </param>
        /// <param name="parsedValue">
        /// <l>
        ///   <zh-CN>成功时返回非负整数；失败时为零。</zh-CN>
        ///   <en>Nonnegative integer when successful; otherwise zero.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数为非负整数时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is a nonnegative integer.</en>
        /// </l>
        /// </returns>
        public static bool TryReadNonNegativeInt32(string value, out int parsedValue)
        {
            return int.TryParse(value, out parsedValue) && parsedValue >= 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断候选地址是否包含会破坏 HTTP/路径解析的控制字符。</zh-CN>
        ///   <en>Determines whether a candidate address contains control characters that can disrupt HTTP or path parsing.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>已去除外层空白的候选地址。</zh-CN>
        ///   <en>Candidate address after outer whitespace has been trimmed.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>包含控制字符时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a control character is present.</en>
        /// </l>
        /// </returns>
        private static bool ContainsControlCharacter(string value)
        {
            // <lang>
            //   <zh-CN>逐字符检查候选值，生命周期只覆盖当前地址校验。</zh-CN>
            //   <en>Inspect the candidate one character at a time; the value is used only for the current URL check.</en>
            // </lang>
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断路径在解码后是否包含父目录遍历片段。</zh-CN>
        ///   <en>Determines whether a path contains a parent-directory traversal segment after decoding.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待检查的地址或路径。</zh-CN>
        ///   <en>Address or path to inspect.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>存在遍历片段或无法解码时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when traversal is present or decoding fails.</en>
        /// </l>
        /// </returns>
        private static bool HasTraversalSegment(string value)
        {
            // <lang>
            //   <zh-CN>先去除查询串和片段标识，只对路径部分做解码与遍历检查。</zh-CN>
            //   <en>Remove query and fragment portions first, then decode and inspect only the path.</en>
            // </lang>
            string path = value.Split(new[] { '?', '#' }, 2)[0];
            try
            {
                path = Uri.UnescapeDataString(path);
            }
            catch (UriFormatException)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>按统一斜杠切分路径段，避免反斜杠伪装成合法层级。</zh-CN>
            //   <en>Split on normalized slashes so a backslash cannot disguise a path hierarchy.</en>
            // </lang>
            foreach (string segment in path.Replace('\\', '/').Split('/'))
            {
                if (string.Equals(segment, "..", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范回跳地址，只接受当前应用路径或与当前请求同源的绝对地址。</zh-CN>
        ///   <en>Normalizes a return URL, accepting only a current-application path or an absolute URL with the current origin.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前请求，用于比较 scheme、host、port 和应用路径。</zh-CN>
        ///   <en>Current request used to compare scheme, host, port, and application path.</en>
        /// </l>
        /// </param>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>Referer 或 ViewState 中的候选回跳值。</zh-CN>
        ///   <en>Candidate return value from Referer or ViewState.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedUrl">
        /// <l>
        ///   <zh-CN>成功时返回站内 PathAndQuery；失败时为空。</zh-CN>
        ///   <en>In-application PathAndQuery on success; empty on failure.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选值通过回跳安全边界时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the candidate passes the return-navigation boundary.</en>
        /// </l>
        /// </returns>
        private static bool TryNormalizeReturnUrl(HttpRequest request, string candidate, out string normalizedUrl)
        {
            // <lang>
            //   <zh-CN>失败默认输出空地址，调用方随后回退到门户首页。</zh-CN>
            //   <en>Default to an empty address on failure so the caller can fall back to the Portal home page.</en>
            // </lang>
            normalizedUrl = string.Empty;
            if (request == null || string.IsNullOrWhiteSpace(candidate) || ContainsControlCharacter(candidate))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>首次请求把合法 Referer 规范为 PathAndQuery 保存到 ViewState；回发时仍须接受该站内格式。</zh-CN>
            //   <en>The first request stores a valid Referer as PathAndQuery in ViewState, which must remain valid on postback.</en>
            // </lang>
            if (candidate.StartsWith("/", StringComparison.Ordinal) &&
                !candidate.StartsWith("//", StringComparison.Ordinal) &&
                IsCurrentApplicationPath(candidate, request))
            {
                normalizedUrl = candidate;
                return true;
            }

            // <lang>
            //   <zh-CN>读取当前请求来源，作为绝对回跳地址的同源比较基准。</zh-CN>
            //   <en>Read the current request origin as the same-origin comparison baseline for absolute return URLs.</en>
            // </lang>
            Uri requestUri = request.Url;

            // <lang>
            //   <zh-CN>尝试解析候选绝对地址；解析失败或来源不一致时拒绝回跳。</zh-CN>
            //   <en>Attempt to parse the absolute candidate; reject the return when parsing fails or its origin differs.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断绝对路径在解码和遍历检查后仍位于当前应用虚拟目录。</zh-CN>
        ///   <en>Determines whether an absolute path remains inside the current application virtual directory after decoding and traversal checks.</en>
        /// </lang>
        /// </summary>
        /// <param name="absolutePath">
        /// <l>
        ///   <zh-CN>候选绝对路径。</zh-CN>
        ///   <en>Candidate absolute path.</en>
        /// </l>
        /// </param>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前请求，提供应用虚拟目录。</zh-CN>
        ///   <en>Current request providing the application virtual directory.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>路径属于当前应用且未发现遍历时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the path belongs to the current application and no traversal is found.</en>
        /// </l>
        /// </returns>
        private static bool IsCurrentApplicationPath(string absolutePath, HttpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(absolutePath) ||
                absolutePath.StartsWith("//", StringComparison.Ordinal) || absolutePath.IndexOf('\\') >= 0 ||
                HasTraversalSegment(absolutePath))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>分离查询串和片段，只用路径部分参与应用目录判断。</zh-CN>
            //   <en>Separate query and fragment portions so only the path participates in application-directory checks.</en>
            // </lang>
            string pathOnly = absolutePath.Split(new[] { '?', '#' }, 2)[0];

            // <lang>
            //   <zh-CN>解码路径用于再次检查编码后的反斜杠和遍历片段。</zh-CN>
            //   <en>Decode the path for a second check against encoded backslashes and traversal segments.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>读取当前应用虚拟目录；根应用使用斜杠并接受所有未违规路径。</zh-CN>
            //   <en>Read the current application virtual directory; the root application uses a slash and accepts any path without violations.</en>
            // </lang>
            string applicationPath = request.ApplicationPath;
            if (string.IsNullOrEmpty(applicationPath) || string.Equals(applicationPath, "/", StringComparison.Ordinal))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>去除尾部斜杠后比较应用根本身及其子路径，保持路径边界完整匹配。</zh-CN>
            //   <en>Trim the trailing slash and compare both the application root and its child paths with a complete boundary.</en>
            // </lang>
            string normalizedApplicationPath = applicationPath.TrimEnd('/');
            return string.Equals(pathOnly, normalizedApplicationPath, StringComparison.OrdinalIgnoreCase) ||
                   pathOnly.StartsWith(normalizedApplicationPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据当前应用虚拟目录构造门户桌面首页地址。</zh-CN>
        ///   <en>Builds the Portal desktop-home URL from the current application virtual directory.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>当前请求；为空时使用根应用回退地址。</zh-CN>
        ///   <en>Current request; a root-application fallback is used when it is null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前应用内的 DesktopDefault.aspx 地址。</zh-CN>
        ///   <en>An in-application DesktopDefault.aspx address.</en>
        /// </l>
        /// </returns>
        private static string GetPortalHomeUrl(HttpRequest request)
        {
            // <lang>
            //   <zh-CN>读取应用路径，生命周期仅覆盖本次首页回退地址生成。</zh-CN>
            //   <en>Read the application path for the lifetime of this home-url fallback calculation.</en>
            // </lang>
            string applicationPath = request == null ? string.Empty : request.ApplicationPath;
            if (string.IsNullOrEmpty(applicationPath) || string.Equals(applicationPath, "/", StringComparison.Ordinal))
            {
                return "/DesktopDefault.aspx";
            }

            return applicationPath.TrimEnd('/') + "/DesktopDefault.aspx";
        }
    }
}
