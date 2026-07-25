using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using Newtonsoft.Json.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 已验证的受信任部署主题包元数据。
    /// Metadata for a validated trusted-deployment theme package.
    /// </summary>
    public sealed class PortalThemePackage
    {
        /// <summary>
        /// <lang>
        /// <zh-CN>创建一个已通过目录、manifest 和资源校验的主题包投影。</zh-CN>
        /// <en>Creates a theme-package projection that has passed directory, manifest, and resource validation.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l zh-CN="与物理目录一致的稳定主题名。" en="Stable theme name matching the physical directory." />
        /// </param>
        /// <param name="displayName">
        /// <l zh-CN="后台选择器展示名称。" en="Display name for admin selectors." />
        /// </param>
        /// <param name="version">
        /// <l zh-CN="主题包声明版本。" en="Version declared by the theme package." />
        /// </param>
        /// <param name="minimumPortalVersion">
        /// <l zh-CN="主题包声明的最低门户版本，可为空。" en="Minimum portal version declared by the package, if any." />
        /// </param>
        /// <param name="inheritsDefault">
        /// <l zh-CN="是否声明继承 Default 主题。" en="Whether the package declares inheritance from the Default theme." />
        /// </param>
        /// <param name="resources">
        /// <l zh-CN="已校验的站内本地资源路径集合。" en="Validated site-local resource path collection." />
        /// </param>
        internal PortalThemePackage(
            string name,
            string displayName,
            string version,
            string minimumPortalVersion,
            bool inheritsDefault,
            IList<string> resources)
        {
            Name = name ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            MinimumPortalVersion = minimumPortalVersion ?? string.Empty;
            InheritsDefault = inheritsDefault;
            Resources = new List<string>(resources ?? new List<string>()).AsReadOnly();
        }

        /// <summary>
        /// 与部署目录一致的稳定主题名。
        /// Stable theme name matching the deployment directory.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 供后台选择器展示的名称。
        /// Name displayed by the admin selector.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// 主题包版本。
        /// Theme package version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// 主题声明的最低门户版本。
        /// Minimum portal version declared by the theme.
        /// </summary>
        public string MinimumPortalVersion { get; private set; }

        /// <summary>
        /// 是否在 CSS 中继承 Default 主题。
        /// Whether the CSS inherits the Default theme.
        /// </summary>
        /// <remarks>
        /// 当前仅是部署包声明性元数据。主题 CSS 必须自行显式表达继承关系；catalog 和 Master Page 不会据此
        /// 自动加载或注入 Default 主题资源。
        /// This is declarative deployment-package metadata only. Theme CSS must explicitly express inheritance itself;
        /// the catalog and Master Page do not automatically load or inject Default theme resources from this value.
        /// </remarks>
        public bool InheritsDefault { get; private set; }

        /// <summary>
        /// 已声明且位于主题目录内的本地资源。
        /// Declared local resources located inside the theme directory.
        /// </summary>
        /// <remarks>
        /// 此列表用于部署包校验和工具链，不是 Master Page 的通用资源注入协议。Web Forms 原生 Theme 仍按
        /// App_Themes 机制处理主题文件。
        /// This list is for deployment-package validation and tooling; it is not a general Master Page resource
        /// injection protocol. Native Web Forms Theme handling still processes theme files through App_Themes.
        /// </remarks>
        public IList<string> Resources { get; private set; }
    }

    /// <summary>
    /// 主题部署包目录和 manifest 校验器。
    /// Theme deployment-package directory and manifest validator.
    /// </summary>
    /// <remarks>
    /// P3.1 只发现由受信任部署流程写入的目录。此类不上传、不解压、不编辑，也不自动加载
    /// 远程 URL 或 JavaScript；可信主题包来源、签名、许可、版本与回滚由独立机制覆盖。
    /// P3.1 discovers directories written by a trusted deployment process only. It does not upload, unzip,
    /// or edit packages, and it never auto-loads remote URLs or JavaScript. A separate trusted-package mechanism
    /// covers provenance, signatures, licenses, versions, and rollback.
        /// </remarks>
    public static class PortalThemeCatalog
    {
        private const int ManifestSchemaVersion = 1;

        private static readonly Regex ThemeNamePattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_-]{0,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// 获取所有已部署并通过 manifest 校验的主题包。
        /// Gets every deployed theme package that passes manifest validation.
        /// </summary>
        /// <returns>按显示名排序的只读主题包列表。Read-only theme package list ordered by display name.</returns>
        /// <remarks>
        /// 无法读取主题根目录或单个包校验失败时会跳过对应包，不阻断其他合格主题或页面默认回退。
        /// When the theme root cannot be read or one package fails validation, the corresponding package is skipped;
        /// it does not block other valid themes or the page default fallback.
        /// </remarks>
        public static IList<PortalThemePackage> GetTrustedPackages()
        {
            var packages = new List<PortalThemePackage>();
            string rootPath = HostingEnvironment.MapPath("~/App_Themes");
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return packages.AsReadOnly();
            }

            foreach (DirectoryInfo directory in new DirectoryInfo(rootPath).GetDirectories())
            {
                PortalThemePackage package;
                string reason;
                if (TryGetTrustedPackage(directory.Name, out package, out reason))
                {
                    packages.Add(package);
                }
            }

            return packages
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 验证并读取一个已部署主题包。
        /// Validates and reads one deployed theme package.
        /// </summary>
        /// <param name="themeName">目录和 manifest 中应一致的主题名。Theme name expected in both directory and manifest.</param>
        /// <param name="package">成功时返回已验证的主题包。Validated theme package when successful.</param>
        /// <param name="reason">失败时返回不含物理路径的原因。Failure reason without physical paths.</param>
        /// <returns>主题包是否可被当前门户安全选择。Whether the package can be safely selected by this portal.</returns>
        /// <remarks>
        /// 成功只表示目录、manifest 和本地资源满足当前最小契约；它不证明主题视觉质量、许可、签名或部署来源。
        /// Success means only that the directory, manifest, and local resources meet the current minimal contract; it
        /// does not prove visual quality, license, signature, or deployment provenance.
        /// </remarks>
        public static bool TryGetTrustedPackage(
            string themeName,
            out PortalThemePackage package,
            out string reason)
        {
            package = null;
            reason = string.Empty;

            if (!IsValidThemeName(themeName))
            {
                reason = "Theme name contains invalid characters.";
                return false;
            }

            string rootPath = HostingEnvironment.MapPath("~/App_Themes");
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                reason = "Theme root is unavailable.";
                return false;
            }

            string packagePath;
            try
            {
                packagePath = GetChildPath(rootPath, themeName);
            }
            catch (InvalidOperationException)
            {
                reason = "Theme directory is outside the allowed root.";
                return false;
            }

            if (!Directory.Exists(packagePath))
            {
                reason = "Theme directory does not exist.";
                return false;
            }

            string manifestPath = Path.Combine(packagePath, "theme.json");
            string defaultCssPath = Path.Combine(packagePath, "Default.css");
            if (!File.Exists(manifestPath) || !File.Exists(defaultCssPath))
            {
                reason = "theme.json or Default.css is missing.";
                return false;
            }

            try
            {
                JObject manifest = JObject.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                if (!IsSchemaVersionSupported(manifest))
                {
                    reason = "Theme manifest schemaVersion is unsupported.";
                    return false;
                }

                string manifestName = ReadRequiredString(manifest, "name", 64);
                if (!string.Equals(themeName, manifestName, StringComparison.Ordinal))
                {
                    reason = "Theme manifest name does not match its directory.";
                    return false;
                }

                string displayName = ReadRequiredString(manifest, "displayName", 100);
                string version = ReadRequiredString(manifest, "version", 64);
                string minimumPortalVersion = ReadOptionalString(manifest, "minimumPortalVersion", 64);
                bool inheritsDefault = ReadOptionalBoolean(manifest, "inheritsDefault");
                IList<string> resources = ReadAndValidateResources(manifest, packagePath);

                if (manifest["script"] != null || manifest["scripts"] != null ||
                    manifest["externalUrl"] != null || manifest["externalUrls"] != null)
                {
                    reason = "Theme manifest declares a prohibited script or external URL.";
                    return false;
                }

                package = new PortalThemePackage(
                    manifestName,
                    displayName,
                    version,
                    minimumPortalVersion,
                    inheritsDefault,
                    resources);
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is Newtonsoft.Json.JsonException ||
                exception is InvalidOperationException)
            {
                reason = "Theme manifest is invalid.";
                return false;
            }
        }

        /// <summary>
        /// 判断主题名是否可安全作为部署目录和 CSS class 片段。
        /// Determines whether a theme name is safe as a deployment directory and CSS-class segment.
        /// </summary>
        /// <param name="themeName">待校验主题名。Theme name to validate.</param>
        /// <returns>主题名是否满足稳定 ASCII 契约。Whether the name meets the stable ASCII contract.</returns>
        public static bool IsValidThemeName(string themeName)
        {
            return !string.IsNullOrWhiteSpace(themeName) && ThemeNamePattern.IsMatch(themeName.Trim());
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>检查 manifest schema 是否为当前加载器支持的最小版本。</zh-CN>
        /// <en>Checks whether the manifest schema is the minimal version supported by the current loader.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest">
        /// <l zh-CN="已解析的主题 manifest。" en="Parsed theme manifest." />
        /// </param>
        /// <returns>
        /// <l zh-CN="schemaVersion 是否精确匹配当前受支持版本。" en="Whether schemaVersion exactly matches the currently supported version." />
        /// </returns>
        private static bool IsSchemaVersionSupported(JObject manifest)
        {
            JToken token = manifest["schemaVersion"];
            return token != null && token.Type == JTokenType.Integer && token.Value<int>() == ManifestSchemaVersion;
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>读取必填字符串字段，并复用可选字符串读取器完成类型和长度校验。</zh-CN>
        /// <en>Reads a required string field and reuses the optional-string reader for type and length validation.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest">
        /// <l zh-CN="已解析的主题 manifest。" en="Parsed theme manifest." />
        /// </param>
        /// <param name="propertyName">
        /// <l zh-CN="需要读取的属性名。" en="Property name to read." />
        /// </param>
        /// <param name="maximumLength">
        /// <l zh-CN="允许的最大字符数。" en="Maximum allowed character count." />
        /// </param>
        /// <returns>
        /// <l zh-CN="去除首尾空白后的必填字段值。" en="Required field value after trimming surrounding whitespace." />
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l zh-CN="字段缺失、为空、类型不符或超过长度限制时抛出。" en="Thrown when the field is missing, blank, incorrectly typed, or too long." />
        /// </exception>
        private static string ReadRequiredString(JObject manifest, string propertyName, int maximumLength)
        {
            string value = ReadOptionalString(manifest, propertyName, maximumLength);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Required manifest value is missing.");
            }

            return value;
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>读取可选字符串字段，并把 manifest 侧的类型错误转为统一校验异常。</zh-CN>
        /// <en>Reads an optional string field and converts manifest-side type errors into a consistent validation exception.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest">
        /// <l zh-CN="已解析的主题 manifest。" en="Parsed theme manifest." />
        /// </param>
        /// <param name="propertyName">
        /// <l zh-CN="需要读取的属性名。" en="Property name to read." />
        /// </param>
        /// <param name="maximumLength">
        /// <l zh-CN="允许的最大字符数。" en="Maximum allowed character count." />
        /// </param>
        /// <returns>
        /// <l zh-CN="字段不存在时返回空字符串，否则返回 trim 后的值。" en="Empty string when the field is absent; otherwise the trimmed value." />
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l zh-CN="字段类型不符或超过长度限制时抛出。" en="Thrown when the field type is invalid or the value is too long." />
        /// </exception>
        private static string ReadOptionalString(JObject manifest, string propertyName, int maximumLength)
        {
            JToken token = manifest[propertyName];
            if (token == null || token.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            if (token.Type != JTokenType.String)
            {
                throw new InvalidOperationException("Manifest string value is invalid.");
            }

            string value = token.Value<string>().Trim();
            if (value.Length > maximumLength)
            {
                throw new InvalidOperationException("Manifest string value is too long.");
            }

            return value;
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>读取可选布尔字段；缺失时按 false 处理，类型错误时阻止包进入目录。</zh-CN>
        /// <en>Reads an optional Boolean field; missing values become false, while type errors block catalog entry.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest">
        /// <l zh-CN="已解析的主题 manifest。" en="Parsed theme manifest." />
        /// </param>
        /// <param name="propertyName">
        /// <l zh-CN="需要读取的属性名。" en="Property name to read." />
        /// </param>
        /// <returns>
        /// <l zh-CN="读取到的布尔值；未声明时为 false。" en="The declared Boolean value; false when not declared." />
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l zh-CN="字段存在但不是布尔类型时抛出。" en="Thrown when the field exists but is not Boolean." />
        /// </exception>
        private static bool ReadOptionalBoolean(JObject manifest, string propertyName)
        {
            JToken token = manifest[propertyName];
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }

            if (token.Type != JTokenType.Boolean)
            {
                throw new InvalidOperationException("Manifest boolean value is invalid.");
            }

            return token.Value<bool>();
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>读取并校验主题声明的站内资源清单。</zh-CN>
        /// <en>Reads and validates the site-local resource list declared by a theme.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest">
        /// <l zh-CN="已解析的主题 manifest。" en="Parsed theme manifest." />
        /// </param>
        /// <param name="packagePath">
        /// <l zh-CN="主题包物理根目录。" en="Physical root directory of the theme package." />
        /// </param>
        /// <returns>
        /// <l zh-CN="已通过路径、存在性和类型限制的只读资源集合。" en="Read-only resource collection that passed path, existence, and type restrictions." />
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l zh-CN="资源清单缺失、路径越界、文件不存在、声明脚本或缺少 Default.css 时抛出。" en="Thrown when resources are missing, escape the package root, do not exist, declare scripts, or omit Default.css." />
        /// </exception>
        private static IList<string> ReadAndValidateResources(JObject manifest, string packagePath)
        {
            JArray resources = manifest["resources"] as JArray;
            if (resources == null || resources.Count == 0)
            {
                throw new InvalidOperationException("Theme resources are missing.");
            }

            var validatedResources = new List<string>();
            bool containsDefaultCss = false;
            foreach (JToken token in resources)
            {
                if (token.Type != JTokenType.String)
                {
                    throw new InvalidOperationException("Theme resource is invalid.");
                }

                string resource = token.Value<string>().Trim().Replace('\\', '/');
                if (!IsValidResourcePath(resource))
                {
                    throw new InvalidOperationException("Theme resource path is not allowed.");
                }

                string physicalResourcePath = GetChildPath(packagePath, resource.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(physicalResourcePath))
                {
                    throw new InvalidOperationException("Theme resource does not exist.");
                }

                if (resource.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Theme JavaScript is not supported.");
                }

                if (string.Equals(resource, "Default.css", StringComparison.OrdinalIgnoreCase))
                {
                    containsDefaultCss = true;
                }

                validatedResources.Add(resource);
            }

            if (!containsDefaultCss)
            {
                throw new InvalidOperationException("Default.css must be declared as a theme resource.");
            }

            return validatedResources.AsReadOnly();
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>执行资源路径的轻量白名单校验，拒绝绝对路径、外链和目录穿越片段。</zh-CN>
        /// <en>Applies lightweight allow-list validation to reject absolute paths, external URLs, and traversal segments.</en>
        /// </lang>
        /// </summary>
        /// <param name="resource">
        /// <l zh-CN="manifest 中声明的资源相对路径。" en="Resource relative path declared in the manifest." />
        /// </param>
        /// <returns>
        /// <l zh-CN="路径是否可继续映射到主题包根目录下。" en="Whether the path may continue to be mapped under the theme package root." />
        /// </returns>
        private static bool IsValidResourcePath(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource) || resource.StartsWith("/", StringComparison.Ordinal) ||
                resource.IndexOf("://", StringComparison.Ordinal) >= 0 || resource.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            string[] segments = resource.Split('/');
            return segments.All(segment => !string.IsNullOrWhiteSpace(segment) && segment != "." && segment != "..");
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>在根目录内合成子路径，并通过规范化后的前缀检查阻止路径逃逸。</zh-CN>
        /// <en>Combines a child path under a root directory and blocks path escape through normalized-prefix checks.</en>
        /// </lang>
        /// </summary>
        /// <param name="rootPath">
        /// <l zh-CN="允许访问的物理根目录。" en="Allowed physical root directory." />
        /// </param>
        /// <param name="childPath">
        /// <l zh-CN="待映射的子路径。" en="Child path to map." />
        /// </param>
        /// <returns>
        /// <l zh-CN="确认位于根目录下的规范化物理路径。" en="Normalized physical path confirmed to remain under the root directory." />
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l zh-CN="子路径逃逸根目录时抛出。" en="Thrown when the child path escapes the root directory." />
        /// </exception>
        private static string GetChildPath(string rootPath, string childPath)
        {
            string normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string candidate = Path.GetFullPath(Path.Combine(normalizedRoot, childPath));
            string rootWithSeparator = normalizedRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path escapes its root.");
            }

            return candidate;
        }
    }
}
