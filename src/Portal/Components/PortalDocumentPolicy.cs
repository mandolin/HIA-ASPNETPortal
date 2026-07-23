using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>集中定义文档模块的上传、链接、文件名和编辑回跳安全边界。</zh-CN>
    ///   <en>Centralizes upload, link, filename, and edit-return safety boundaries for the document module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本策略只处理服务器文件上传与现有手填链接。允许扩展名设置只能收紧内置硬允许集， 不能将脚本、页面、配置或可执行文件重新放行。生成的物理文件名用于避免冲突并帮助运维识别， 不是下载授权凭据，也不替代未来的私有文件服务。</zh-CN>
    ///   <en>This policy handles server-file uploads and existing manually entered links only. The allowed-extension setting can narrow, but never expand, the built-in hard allowlist; scripts, pages, configuration files, and executables cannot be re-enabled. Generated physical filenames prevent collisions and aid operations; they are not download credentials and do not replace a future private-file service.</en>
    /// </lang>
    /// </remarks>
    public static class PortalDocumentPolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>上传目录的应用相对虚拟路径。</zh-CN>
        ///   <en>Application-relative virtual path of the upload directory.</en>
        /// </lang>
        /// </summary>
        public const string UploadVirtualDirectory = "~/uploads";

        /// <summary>
        /// <lang>
        ///   <zh-CN>基础设施当前允许的单请求最大字节数，与 Web.config/IIS 的 30 MiB 限制一致。</zh-CN>
        ///   <en>Maximum bytes currently allowed for one request by infrastructure, aligned with the 30 MiB Web.config/IIS limit.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>验证一次服务器文件上传的大小和扩展名。</zh-CN>
        ///   <en>Validates size and extension for one server-file upload.</en>
        /// </lang>
        /// </summary>
        /// <param name="postedFile">
        /// <l>
        ///   <zh-CN>浏览器提交的文件，不能为 <c>null</c>。</zh-CN>
        ///   <en>Browser-posted file; cannot be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <param name="errorMessage">
        /// <l>
        ///   <zh-CN>验证失败时面向编辑者的安全提示。</zh-CN>
        ///   <en>Safe editor-facing message when validation fails.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>文件满足当前大小和扩展名策略时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the file satisfies the current size and extension policy.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>读取当前有效的文档上传大小上限，并格式化为面向编辑者的短文本。</zh-CN>
        ///   <en>Reads the effective document-upload size limit and formats it as a short editor-facing text.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>例如 <c>10 MB</c> 的大小说明。</zh-CN>
        ///   <en>Size text such as <c>10 MB</c>.</en>
        /// </l>
        /// </returns>
        public static string GetMaximumUploadSizeDisplayText()
        {
            return FormatFileSize(GetMaximumUploadBytes());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前有效的文档上传扩展名允许清单，并格式化为稳定顺序的展示文本。</zh-CN>
        ///   <en>Reads the effective document-upload extension allowlist and formats it as stable display text.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>逗号分隔的扩展名清单。</zh-CN>
        ///   <en>Comma-separated extension allowlist.</en>
        /// </l>
        /// </returns>
        public static string GetAllowedExtensionsDisplayText()
        {
            var extensions = new List<string>(GetConfiguredAllowedExtensions());
            extensions.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", extensions.ToArray());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为已验证上传生成可读且不易冲突的物理文件名。</zh-CN>
        ///   <en>Generates a readable, collision-resistant physical filename for a validated upload.</en>
        /// </lang>
        /// </summary>
        /// <param name="originalFileName">
        /// <l>
        ///   <zh-CN>浏览器提交的原始文件名。</zh-CN>
        ///   <en>Original filename submitted by the browser.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>UTC 时间戳、随机串、净化主名和已验证扩展名组成的文件名。</zh-CN>
        ///   <en>Filename composed of a UTC timestamp, random token, sanitized stem, and validated extension.</en>
        /// </l>
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <l>
        ///   <zh-CN>文件扩展名不符合当前策略时引发。</zh-CN>
        ///   <en>Thrown when the filename extension violates the current policy.</en>
        /// </l>
        /// </exception>
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
        /// <lang>
        ///   <zh-CN>构建存储文件在门户中的应用相对 URL。</zh-CN>
        ///   <en>Builds the application-relative URL of a stored upload.</en>
        /// </lang>
        /// </summary>
        /// <param name="storageFileName">
        /// <l>
        ///   <zh-CN>由 <see cref="CreateStorageFileName"/> 生成的存储文件名。</zh-CN>
        ///   <en>Storage filename generated by <see cref="CreateStorageFileName"/>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可保存到旧文档记录的应用相对 URL。</zh-CN>
        ///   <en>Application-relative URL that can be stored in a legacy document record.</en>
        /// </l>
        /// </returns>
        public static string GetUploadVirtualPath(string storageFileName)
        {
            return UploadVirtualDirectory + "/" + Path.GetFileName(storageFileName ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验并规范化文档模块的手填浏览地址。</zh-CN>
        ///   <en>Validates and normalizes a manually entered document browse URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>编辑者输入的候选地址。</zh-CN>
        ///   <en>Candidate address entered by an editor.</en>
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
        ///   <zh-CN>成功时返回可保存的 URL；失败时为空。</zh-CN>
        ///   <en>Persistable URL when successful; otherwise empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选地址为允许的站内相对地址或 HTTP(S) 外链时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the candidate is an allowed application-relative address or HTTP(S) external link.</en>
        /// </l>
        /// </returns>
        public static bool TryNormalizeBrowseUrl(string candidate, HttpRequest request, out string normalizedUrl)
        {
            return PortalNavigationPolicy.TryNormalizeBrowseUrl(candidate, request, out normalizedUrl);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从候选 Referer 或 ViewState 回跳值中解析当前应用内安全返回地址。</zh-CN>
        ///   <en>Resolves a safe return address inside the current application from a candidate Referer or ViewState value.</en>
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
        ///   <zh-CN>当前应用内安全 URL；非法或缺失时返回门户首页。</zh-CN>
        ///   <en>Safe URL in the current application, or the Portal home page when invalid or missing.</en>
        /// </l>
        /// </returns>
        public static string GetSafeReturnUrl(HttpRequest request, string candidate)
        {
            return PortalNavigationPolicy.GetSafeReturnUrl(request, candidate);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从当前请求的 Referer 解析安全返回地址。</zh-CN>
        ///   <en>Resolves a safe return address from the current request's Referer.</en>
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
        ///   <zh-CN>当前应用内安全 URL；无有效 Referer 时返回门户首页。</zh-CN>
        ///   <en>Safe URL in the current application, or the Portal home page when no valid Referer exists.</en>
        /// </l>
        /// </returns>
        public static string GetSafeReturnUrl(HttpRequest request)
        {
            return PortalNavigationPolicy.GetSafeReturnUrl(request);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>向已验证的当前应用内返回地址重定向，并避免 <see cref="HttpResponse.End"/> 导致的线程中止。</zh-CN>
        ///   <en>Redirects to a validated current-application return URL while avoiding the thread abort caused by <see cref="HttpResponse.End"/>.</en>
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
            PortalNavigationPolicy.RedirectToSafeReturnUrl(context, candidate);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成可安全写入 Content-Disposition 响应头的历史下载文件名。</zh-CN>
        ///   <en>Produces a legacy-download filename that is safe to write to a Content-Disposition response header.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>旧文档记录中的文件 URL 或文件名。</zh-CN>
        ///   <en>File URL or filename stored by a legacy document record.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>不含路径、控制字符或引号的文件名。</zh-CN>
        ///   <en>Filename without path, control characters, or quotation marks.</en>
        /// </l>
        /// </returns>
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

        private static string FormatFileSize(int bytes)
        {
            return (bytes / 1024d / 1024d).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
        }
    }
}
