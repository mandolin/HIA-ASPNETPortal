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

        // <lang>
        //   <zh-CN>生成物理文件名时允许的净化主名长度；时间戳和随机串仍在此上限之外独立保留。</zh-CN>
        //   <en>Maximum sanitized-stem length for generated physical filenames; the timestamp and random token remain independent of this cap.</en>
        // </lang>
        private const int StorageStemMaximumLength = 48;

        // <lang>
        //   <zh-CN>服务器上传的硬允许扩展名集合；配置只能从该集合收紧，不能重新允许脚本、页面或可执行类型。</zh-CN>
        //   <en>Hard allowlist for server-upload extensions; configuration may narrow this set but cannot re-enable script, page, or executable types.</en>
        // </lang>
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

            // <lang>
            //   <zh-CN>从受控运行设置读取并封顶本次上传的字节预算；该值不代表底层 IIS/请求限制可被绕过。</zh-CN>
            //   <en>Read and cap the byte budget from controlled runtime settings; this value does not bypass lower-level IIS/request limits.</en>
            // </lang>
            int maximumBytes = GetMaximumUploadBytes();
            if (postedFile.ContentLength > maximumBytes)
            {
                errorMessage = "上传文件不能超过 " + FormatFileSize(maximumBytes) + "。当前文件大小为 " +
                               FormatFileSize(postedFile.ContentLength) + "。";
                return false;
            }

            // <lang>
            //   <zh-CN>只从文件名提取小写扩展名，再由硬允许集和配置允许集共同决定类型。</zh-CN>
            //   <en>Extract only a lowercase extension from the filename, then require both the hard and configured allowlists.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>复制有效扩展名集合后排序，避免展示顺序受配置输入或 HashSet 枚举顺序影响。</zh-CN>
            //   <en>Copy the effective extension set before sorting so display order is independent of configuration input or HashSet enumeration order.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>先去除目录部分，避免原始路径进入生成文件名或影响后续扩展名判断。</zh-CN>
            //   <en>Strip directory components first so an original path cannot enter the generated filename or influence extension validation.</en>
            // </lang>
            string sourceFileName = Path.GetFileName(originalFileName ?? string.Empty);
            // <lang>
            //   <zh-CN>扩展名必须先通过同一策略，生成器不为不允许类型提供旁路。</zh-CN>
            //   <en>The extension must pass the same policy before generation; the generator provides no bypass for disallowed types.</en>
            // </lang>
            string extension = GetNormalizedExtension(sourceFileName);
            if (!IsExtensionAllowed(extension))
            {
                throw new ArgumentException("The upload extension is not allowed.", "originalFileName");
            }

            // <lang>
            //   <zh-CN>主名只用于可读性，经过控制字符、路径字符和长度净化，不承担授权或保密作用。</zh-CN>
            //   <en>The stem is for readability only; it is sanitized for control/path characters and length and carries no authorization or secrecy.</en>
            // </lang>
            string stem = SanitizeFileStem(Path.GetFileNameWithoutExtension(sourceFileName));
            // <lang>
            //   <zh-CN>使用 UTC 固定格式生成可按时间观察的低敏文件名片段，避免本地时区歧义。</zh-CN>
            //   <en>Use a fixed UTC format for a low-sensitivity, time-observable filename segment without local-time ambiguity.</en>
            // </lang>
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss'Z'", CultureInfo.InvariantCulture);
            // <lang>
            //   <zh-CN>随机串用于降低同秒同主名冲突；它不是下载令牌，不写入权限或审计语义。</zh-CN>
            //   <en>The random token reduces same-second/name collisions; it is not a download token and carries no permission or audit semantics.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>仅保留文件名部分，阻断历史记录中的目录片段进入 Content-Disposition。</zh-CN>
            //   <en>Keep only the filename component so directory fragments from legacy records cannot enter Content-Disposition.</en>
            // </lang>
            string fileName = Path.GetFileName(candidate ?? string.Empty);
            // <lang>
            //   <zh-CN>扩展名和主名分别规范化，返回值不包含路径、控制字符或引号。</zh-CN>
            //   <en>Normalize extension and stem separately so the result contains no path, control characters, or quotation marks.</en>
            // </lang>
            string extension = GetNormalizedExtension(fileName);
            string stem = SanitizeFileStem(Path.GetFileNameWithoutExtension(fileName));
            return stem + extension;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取运行设置中的上传上限，并将其限制在基础设施硬上限内。</zh-CN>
        ///   <en>Reads the configured upload limit and constrains it to the infrastructure hard ceiling.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>有效正数上限，配置缺失或越界时回退到 10 MiB。</zh-CN>
        ///   <en>Effective positive limit, falling back to 10 MiB when configuration is absent or out of range.</en>
        /// </l>
        /// </returns>
        private static int GetMaximumUploadBytes()
        {
            // <lang>
            //   <zh-CN>运行设置只提供可收紧的覆盖值；无效值不能扩大基础设施允许范围。</zh-CN>
            //   <en>Runtime settings provide only a narrowing override; invalid values cannot expand the infrastructure allowance.</en>
            // </lang>
            int configured = PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.MaxUploadBytes);
            return configured > 0 && configured <= InfrastructureMaximumUploadBytes
                ? configured
                : 10485760;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认扩展名同时位于硬允许集和当前配置允许集。</zh-CN>
        ///   <en>Confirms that an extension belongs to both the hard and currently configured allowlists.</en>
        /// </lang>
        /// </summary>
        /// <param name="extension">
        /// <l>
        ///   <zh-CN>已经规范化为小写并带点的扩展名。</zh-CN>
        ///   <en>Extension normalized to lowercase with a leading dot.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>扩展名可进入上传或文件名生成流程时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the extension may enter upload or filename-generation flow.</en>
        /// </l>
        /// </returns>
        private static bool IsExtensionAllowed(string extension)
        {
            // <lang>
            //   <zh-CN>先拒绝空值和硬集合之外的类型，再读取可收紧的运行配置。</zh-CN>
            //   <en>Reject empty or hard-allowlist-external types first, then apply the narrowing runtime configuration.</en>
            // </lang>
            if (string.IsNullOrEmpty(extension) || !HardAllowedExtensions.Contains(extension))
            {
                return false;
            }

            return GetConfiguredAllowedExtensions().Contains(extension);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并规范化配置扩展名，只保留硬允许集合中的值。</zh-CN>
        ///   <en>Reads and normalizes configured extensions, retaining only values in the hard allowlist.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>不区分大小写的有效扩展名集合。</zh-CN>
        ///   <en>Case-insensitive set of effective extensions.</en>
        /// </l>
        /// </returns>
        private static ISet<string> GetConfiguredAllowedExtensions()
        {
            // <lang>
            //   <zh-CN>读取低敏设置原文；缺失按空配置处理，不把缺失解释为放开全部类型。</zh-CN>
            //   <en>Read the low-sensitivity setting text; treat missing input as empty rather than opening every type.</en>
            // </lang>
            string configured = PortalRuntimeSettings.GetString(PortalSettingsRegistry.AllowedDocumentExtensions);
            // <lang>
            //   <zh-CN>使用不区分大小写集合去重，并为后续 Contains 保持稳定比较语义。</zh-CN>
            //   <en>Use a case-insensitive set for deduplication and stable comparison semantics for later Contains checks.</en>
            // </lang>
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string token in (configured ?? string.Empty).Split(','))
            {
                // <lang>
                //   <zh-CN>每个配置 token 先规范为带点小写扩展名，只有硬允许类型才可加入有效集合。</zh-CN>
                //   <en>Normalize each configuration token to a dotted lowercase extension, adding it only when it is hard-allowed.</en>
                // </lang>
                string normalized = NormalizeConfiguredExtension(token);
                if (!string.IsNullOrEmpty(normalized) && HardAllowedExtensions.Contains(normalized))
                {
                    extensions.Add(normalized);
                }
            }

            return extensions;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将配置中的单个扩展名规范为带点的小写值。</zh-CN>
        ///   <en>Normalizes one configured extension to a dotted lowercase value.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>配置 token，可缺少前导点或为空。</zh-CN>
        ///   <en>Configuration token, which may omit the leading dot or be empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>规范扩展名，空 token 返回空字符串。</zh-CN>
        ///   <en>Normalized extension, or an empty string for an empty token.</en>
        /// </l>
        /// </returns>
        private static string NormalizeConfiguredExtension(string value)
        {
            // <lang>
            //   <zh-CN>先去除两端空白；空 token 不被补点，避免生成虚假的扩展名。</zh-CN>
            //   <en>Trim surrounding whitespace first; do not add a dot to an empty token or create a fictitious extension.</en>
            // </lang>
            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            return normalized.StartsWith(".", StringComparison.Ordinal)
                ? normalized.ToLowerInvariant()
                : "." + normalized.ToLowerInvariant();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从文件名安全提取小写扩展名，不保留目录部分。</zh-CN>
        ///   <en>Safely extracts a lowercase extension from a filename without retaining directory components.</en>
        /// </lang>
        /// </summary>
        /// <param name="fileName">
        /// <l>
        ///   <zh-CN>候选文件名，可为空。</zh-CN>
        ///   <en>Candidate filename; may be empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带点的小写扩展名，缺失时为空字符串。</zh-CN>
        ///   <en>Dotted lowercase extension, or an empty string when absent.</en>
        /// </l>
        /// </returns>
        private static string GetNormalizedExtension(string fileName)
        {
            // <lang>
            //   <zh-CN>Path.GetFileName 先建立文件名边界，随后只将非空扩展名转为不区分大小写的稳定形式。</zh-CN>
            //   <en>Establish the filename boundary with Path.GetFileName, then convert only a nonempty extension to a stable case-insensitive form.</en>
            // </lang>
            string extension = Path.GetExtension(Path.GetFileName(fileName ?? string.Empty));
            return string.IsNullOrEmpty(extension) ? string.Empty : extension.ToLowerInvariant();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>净化文件主名中的控制字符、引号和平台非法字符，并限制存储长度。</zh-CN>
        ///   <en>Sanitizes control characters, quotes, and platform-invalid characters from a filename stem and limits its storage length.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>候选文件主名，可为空。</zh-CN>
        ///   <en>Candidate filename stem; may be empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可读、非空且不超过主名上限的文件主名。</zh-CN>
        ///   <en>Readable, nonempty stem no longer than the stem limit.</en>
        /// </l>
        /// </returns>
        private static string SanitizeFileStem(string value)
        {
            // <lang>
            //   <zh-CN>将 null 视为空主名，并固定当前平台非法字符集合作为替换依据。</zh-CN>
            //   <en>Treat null as an empty stem and use the current platform's invalid-character set for replacement.</en>
            // </lang>
            string source = value ?? string.Empty;
            // <lang>
            //   <zh-CN>逐字符替换控制码、引号和平台非法字符，避免主名进入响应头或物理路径时形成结构。</zh-CN>
            //   <en>Replace control codes, quotes, and platform-invalid characters one by one so the stem cannot form response-header or physical-path structure.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>去除边界空格、点和替换横线；完全为空时使用低敏固定名称。</zh-CN>
            //   <en>Trim boundary spaces, dots, and replacement hyphens; use a fixed low-sensitivity name when nothing remains.</en>
            // </lang>
            string sanitized = builder.ToString().Trim(' ', '.', '-');
            if (sanitized.Length == 0)
            {
                sanitized = "document";
            }

            return sanitized.Substring(0, Math.Min(sanitized.Length, StorageStemMaximumLength));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将字节数格式化为不依赖区域设置的 MB 展示文本。</zh-CN>
        ///   <en>Formats a byte count as culture-invariant MB display text.</en>
        /// </lang>
        /// </summary>
        /// <param name="bytes">
        /// <l>
        ///   <zh-CN>待展示的字节数。</zh-CN>
        ///   <en>Byte count to display.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最多两位小数的 MB 文本。</zh-CN>
        ///   <en>MB text with at most two decimal places.</en>
        /// </l>
        /// </returns>
        private static string FormatFileSize(int bytes)
        {
            return (bytes / 1024d / 1024d).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
        }
    }
}
