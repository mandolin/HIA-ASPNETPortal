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
        public bool InheritsDefault { get; private set; }

        /// <summary>
        /// 已声明且位于主题目录内的本地资源。
        /// Declared local resources located inside the theme directory.
        /// </summary>
        public IList<string> Resources { get; private set; }
    }

    /// <summary>
    /// 主题部署包目录和 manifest 校验器。
    /// Theme deployment-package directory and manifest validator.
    /// </summary>
    /// <remarks>
    /// P3.1 只发现由受信任部署流程写入的目录。此类不上传、不解压、不编辑，也不自动加载
    /// 远程 URL 或 JavaScript；后续可信主题包机制会单独覆盖来源、签名、许可、版本与回滚。
    /// P3.1 discovers directories written by a trusted deployment process only. It does not upload, unzip,
    /// or edit packages, and it never auto-loads remote URLs or JavaScript. A future trusted-package mechanism
    /// will separately cover provenance, signatures, licenses, versions, and rollback.
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

        private static bool IsSchemaVersionSupported(JObject manifest)
        {
            JToken token = manifest["schemaVersion"];
            return token != null && token.Type == JTokenType.Integer && token.Value<int>() == ManifestSchemaVersion;
        }

        private static string ReadRequiredString(JObject manifest, string propertyName, int maximumLength)
        {
            string value = ReadOptionalString(manifest, propertyName, maximumLength);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Required manifest value is missing.");
            }

            return value;
        }

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
