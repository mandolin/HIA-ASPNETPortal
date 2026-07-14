using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：集中定义文档模块的上传、链接、文件名和编辑回跳安全边界。
    ///
    /// English: Centralizes upload, link, filename, and edit-return safety boundaries for the document module.
    /// </summary>
    /// <remarks>
    /// 中文：本策略只处理服务器文件上传与现有手填链接。允许扩展名设置只能收紧内置硬允许集，
    /// 不能将脚本、页面、配置或可执行文件重新放行。生成的物理文件名用于避免冲突并帮助运维识别，
    /// 不是下载授权凭据，也不替代未来的私有文件服务。
    ///
    /// English: This policy handles server-file uploads and existing manually entered links only. The allowed-extension
    /// setting can narrow, but never expand, the built-in hard allowlist; scripts, pages, configuration files, and
    /// executables cannot be re-enabled. Generated physical filenames prevent collisions and aid operations; they are
    /// not download credentials and do not replace a future private-file service.
    /// </remarks>
    public static class PortalDocumentPolicy
    {
        /// <summary>
        /// 中文：上传目录的应用相对虚拟路径。
        ///
        /// English: Application-relative virtual path of the upload directory.
        /// </summary>
        public const string UploadVirtualDirectory = "~/uploads";

        /// <summary>
        /// 中文：基础设施当前允许的单请求最大字节数，与 Web.config/IIS 的 30 MiB 限制一致。
        ///
        /// English: Maximum bytes currently allowed for one request by infrastructure, aligned with the 30 MiB Web.config/IIS limit.
        /// </summary>
        public const int InfrastructureMaximumUploadBytes = 31457280;

        private const int StorageStemMaximumLength = 48;
        private static readonly ISet<string> HardAllowedExtensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".txt",
            ".csv",
            ".json",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".ppt",
            ".pptx",
            ".zip"
        };

        /// <summary>
        /// 中文：验证一次服务器文件上传的大小和扩展名。
        ///
        /// English: Validates size and extension for one server-file upload.
        /// </summary>
        /// <param name="postedFile">中文：浏览器提交的文件，不能为 <c>null</c>。English: Browser-posted file; cannot be <c>null</c>.</param>
        /// <param name="errorMessage">中文：验证失败时面向编辑者的安全提示。English: Safe editor-facing message when validation fails.</param>
        /// <returns>中文：文件满足当前大小和扩展名策略时为 <c>true</c>。English: <c>true</c> when the file satisfies the current size and extension policy.</returns>
        public static bool TryValidateUpload(HttpPostedFile postedFile, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (postedFile == null || postedFile.ContentLength <= 0)
            {
                errorMessage = "请选择要上传的文件。";
                return false;
            }

            int maximumBytes = GetMaximumUploadBytes();
            if (postedFile.ContentLength > maximumBytes)
            {
                errorMessage = "上传文件不能超过 " + FormatFileSize(maximumBytes) + "。当前文件大小为 " +
                               FormatFileSize(postedFile.ContentLength) + "。";
                return false;
            }

            string extension = GetNormalizedExtension(postedFile.FileName);
            if (!IsExtensionAllowed(extension))
            {
                errorMessage = "该文件类型不在当前文档上传允许范围内。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 中文：为已验证上传生成可读且不易冲突的物理文件名。
        ///
        /// English: Generates a readable, collision-resistant physical filename for a validated upload.
        /// </summary>
        /// <param name="originalFileName">中文：浏览器提交的原始文件名。English: Original filename submitted by the browser.</param>
        /// <returns>中文：UTC 时间戳、随机串、净化主名和已验证扩展名组成的文件名。English: Filename composed of a UTC timestamp, random token, sanitized stem, and validated extension.</returns>
        /// <exception cref="ArgumentException">中文：文件扩展名不符合当前策略时引发。English: Thrown when the filename extension violates the current policy.</exception>
        public static string CreateStorageFileName(string originalFileName)
        {
            string sourceFileName = Path.GetFileName(originalFileName ?? string.Empty);
            string extension = GetNormalizedExtension(sourceFileName);
            if (!IsExtensionAllowed(extension))
            {
                throw new ArgumentException("The upload extension is not allowed.", "originalFileName");
            }

            string stem = SanitizeFileStem(Path.GetFileNameWithoutExtension(sourceFileName));
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss'Z'", CultureInfo.InvariantCulture);
            string randomToken = Guid.NewGuid().ToString("N").Substring(0, 12);
            return timestamp + "-" + randomToken + "-" + stem + extension;
        }

        /// <summary>
        /// 中文：构建存储文件在门户中的应用相对 URL。
        ///
        /// English: Builds the application-relative URL of a stored upload.
        /// </summary>
        /// <param name="storageFileName">中文：由 <see cref="CreateStorageFileName"/> 生成的存储文件名。English: Storage filename generated by <see cref="CreateStorageFileName"/>.</param>
        /// <returns>中文：可保存到旧文档记录的应用相对 URL。English: Application-relative URL that can be stored in a legacy document record.</returns>
        public static string GetUploadVirtualPath(string storageFileName)
        {
            return UploadVirtualDirectory + "/" + Path.GetFileName(storageFileName ?? string.Empty);
        }

        /// <summary>
        /// 中文：校验并规范化文档模块的手填浏览地址。
        ///
        /// English: Validates and normalizes a manually entered document browse URL.
        /// </summary>
        /// <param name="candidate">中文：编辑者输入的候选地址。English: Candidate address entered by an editor.</param>
        /// <param name="request">中文：当前请求，用于确认根路径仍在当前应用虚拟目录内。English: Current request used to ensure a root path remains inside the current application virtual directory.</param>
        /// <param name="normalizedUrl">中文：成功时返回可保存的 URL；失败时为空。English: Persistable URL when successful; otherwise empty.</param>
        /// <returns>中文：候选地址为允许的站内相对地址或 HTTP(S) 外链时为 <c>true</c>。English: <c>true</c> when the candidate is an allowed application-relative address or HTTP(S) external link.</returns>
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
        /// 中文：从候选 Referer 或 ViewState 回跳值中解析当前应用内安全返回地址。
        ///
        /// English: Resolves a safe return address inside the current application from a candidate Referer or ViewState value.
        /// </summary>
        /// <param name="request">中文：当前 HTTP 请求。English: Current HTTP request.</param>
        /// <param name="candidate">中文：候选回跳地址，可为空。English: Candidate return address; may be empty.</param>
        /// <returns>中文：当前应用内安全 URL；非法或缺失时返回门户首页。English: Safe URL in the current application, or the Portal home page when invalid or missing.</returns>
        public static string GetSafeReturnUrl(HttpRequest request, string candidate)
        {
            string normalized;
            if (TryNormalizeReturnUrl(request, candidate, out normalized))
            {
                return normalized;
            }

            return GetPortalHomeUrl(request);
        }

        /// <summary>
        /// 中文：从当前请求的 Referer 解析安全返回地址。
        ///
        /// English: Resolves a safe return address from the current request's Referer.
        /// </summary>
        /// <param name="request">中文：当前 HTTP 请求。English: Current HTTP request.</param>
        /// <returns>中文：当前应用内安全 URL；无有效 Referer 时返回门户首页。English: Safe URL in the current application, or the Portal home page when no valid Referer exists.</returns>
        public static string GetSafeReturnUrl(HttpRequest request)
        {
            Uri referer = request == null ? null : request.UrlReferrer;
            return GetSafeReturnUrl(request, referer == null ? null : referer.AbsoluteUri);
        }

        /// <summary>
        /// 中文：向已验证的当前应用内返回地址重定向，并避免 <see cref="HttpResponse.End"/> 导致的线程中止。
        ///
        /// English: Redirects to a validated current-application return URL while avoiding the thread abort caused by <see cref="HttpResponse.End"/>.
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
        /// 中文：生成可安全写入 Content-Disposition 响应头的历史下载文件名。
        ///
        /// English: Produces a legacy-download filename that is safe to write to a Content-Disposition response header.
        /// </summary>
        /// <param name="candidate">中文：旧文档记录中的文件 URL 或文件名。English: File URL or filename stored by a legacy document record.</param>
        /// <returns>中文：不含路径、控制字符或引号的文件名。English: Filename without path, control characters, or quotation marks.</returns>
        public static string GetSafeDownloadFileName(string candidate)
        {
            string fileName = Path.GetFileName(candidate ?? string.Empty);
            string extension = GetNormalizedExtension(fileName);
            string stem = SanitizeFileStem(Path.GetFileNameWithoutExtension(fileName));
            return stem + extension;
        }

        private static int GetMaximumUploadBytes()
        {
            int configured = PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.MaxUploadBytes);
            return configured > 0 && configured <= InfrastructureMaximumUploadBytes
                ? configured
                : 10485760;
        }

        private static bool IsExtensionAllowed(string extension)
        {
            if (string.IsNullOrEmpty(extension) || !HardAllowedExtensions.Contains(extension))
            {
                return false;
            }

            return GetConfiguredAllowedExtensions().Contains(extension);
        }

        private static ISet<string> GetConfiguredAllowedExtensions()
        {
            string configured = PortalRuntimeSettings.GetString(PortalSettingsRegistry.AllowedDocumentExtensions);
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string token in (configured ?? string.Empty).Split(','))
            {
                string normalized = NormalizeConfiguredExtension(token);
                if (!string.IsNullOrEmpty(normalized) && HardAllowedExtensions.Contains(normalized))
                {
                    extensions.Add(normalized);
                }
            }

            return extensions;
        }

        private static string NormalizeConfiguredExtension(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            return normalized.StartsWith(".", StringComparison.Ordinal)
                ? normalized.ToLowerInvariant()
                : "." + normalized.ToLowerInvariant();
        }

        private static string GetNormalizedExtension(string fileName)
        {
            string extension = Path.GetExtension(Path.GetFileName(fileName ?? string.Empty));
            return string.IsNullOrEmpty(extension) ? string.Empty : extension.ToLowerInvariant();
        }

        private static string SanitizeFileStem(string value)
        {
            string source = value ?? string.Empty;
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder();
            foreach (char character in source)
            {
                if (char.IsControl(character) || character == '"' || character == '\'' ||
                    Array.IndexOf(invalidCharacters, character) >= 0)
                {
                    builder.Append('-');
                }
                else
                {
                    builder.Append(character);
                }
            }

            string sanitized = builder.ToString().Trim(' ', '.', '-');
            if (sanitized.Length == 0)
            {
                sanitized = "document";
            }

            return sanitized.Substring(0, Math.Min(sanitized.Length, StorageStemMaximumLength));
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

        private static string FormatFileSize(int bytes)
        {
            return (bytes / 1024d / 1024d).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
        }
    }
}
