using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 已验证的受信任部署模块包元数据。
    /// Metadata for a validated trusted-deployment module package.
    /// </summary>
    public sealed class PortalModulePackage
    {
        internal PortalModulePackage(
            string directoryName,
            string packageId,
            string displayName,
            string version,
            string minimumPortalVersion,
            string desktopEntry,
            IList<string> resources)
        {
            DirectoryName = directoryName ?? string.Empty;
            PackageId = packageId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            MinimumPortalVersion = minimumPortalVersion ?? string.Empty;
            DesktopEntry = desktopEntry ?? string.Empty;
            Resources = new List<string>(resources ?? new List<string>()).AsReadOnly();
        }

        /// <summary>
        /// `DesktopModules` 下的受控目录名。
        /// Controlled directory name under `DesktopModules`.
        /// </summary>
        public string DirectoryName { get; private set; }

        /// <summary>
        /// 不随显示名改变的稳定包标识。
        /// Stable package identifier independent from its display name.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// 供管理员目录展示的名称。
        /// Name displayed by the administrator catalog.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// 模块包版本。
        /// Module package version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// 模块包声明的最低门户版本。
        /// Minimum portal version declared by the module package.
        /// </summary>
        public string MinimumPortalVersion { get; private set; }

        /// <summary>
        /// 通过校验的桌面用户控件入口。
        /// Validated desktop user-control entry point.
        /// </summary>
        public string DesktopEntry { get; private set; }

        /// <summary>
        /// 已声明且位于包目录内的静态资源。
        /// Declared static resources located inside the package directory.
        /// </summary>
        public IList<string> Resources { get; private set; }
    }

    /// <summary>
    /// 一个已验证模块包 CSS 资源的虚拟路径。
    /// Virtual path for one validated module-package CSS resource.
    /// </summary>
    public sealed class PortalModuleStyleResource
    {
        internal PortalModuleStyleResource(string packageId, string virtualPath)
        {
            PackageId = packageId ?? string.Empty;
            VirtualPath = virtualPath ?? string.Empty;
        }

        /// <summary>
        /// 样式所属模块包标识。
        /// Module package identifier owning the style.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// 可通过当前应用虚拟目录解析的站内路径。
        /// Site-local path resolvable through the current application virtual directory.
        /// </summary>
        public string VirtualPath { get; private set; }
    }

    /// <summary>
    /// 当前模块实例的受控运行描述。
    /// Controlled runtime descriptor for the current module instance.
    /// </summary>
    public sealed class PortalModuleRuntimeDescriptor
    {
        internal PortalModuleRuntimeDescriptor(
            string desktopSource,
            bool isManagedPackage,
            bool isEnabled,
            bool isStateAvailable,
            PortalModulePackage package,
            string cacheIdentity)
        {
            DesktopSource = desktopSource ?? string.Empty;
            IsManagedPackage = isManagedPackage;
            IsEnabled = isEnabled;
            IsStateAvailable = isStateAvailable;
            Package = package;
            CacheIdentity = cacheIdentity ?? string.Empty;
        }

        /// <summary>
        /// 已规范化且允许加载的桌面控件路径。
        /// Normalized desktop-control path allowed to load.
        /// </summary>
        public string DesktopSource { get; private set; }

        /// <summary>
        /// 当前实例是否匹配已验证的部署模块包。
        /// Whether the instance matches a validated deployment module package.
        /// </summary>
        public bool IsManagedPackage { get; private set; }

        /// <summary>
        /// 当前包状态是否允许加载。
        /// Whether the current package state permits loading.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// 状态表是否可用于本次解析。
        /// Whether the state table was available for this resolution.
        /// </summary>
        public bool IsStateAvailable { get; private set; }

        /// <summary>
        /// 已验证包；Legacy 模块时为 null。
        /// Validated package; null for a Legacy module.
        /// </summary>
        public PortalModulePackage Package { get; private set; }

        /// <summary>
        /// 用于缓存隔离的受控身份文本。
        /// Controlled identity text used for cache isolation.
        /// </summary>
        public string CacheIdentity { get; private set; }
    }

    /// <summary>
    /// 模块部署目录和 `module.json` 校验器。
    /// Module deployment-directory and `module.json` validator.
    /// </summary>
    /// <remarks>
    /// P3.2 只发现受信任部署流程写入的 `.ascx` 包目录。它不上传、不解压、不执行 DLL 或脚本，
    /// 也不自动加载外部资源。未来动态能力必须先接入统一可信部署包机制。
    /// P3.2 discovers only `.ascx` package directories written by a trusted deployment process. It does not upload,
    /// unzip, execute DLLs or scripts, and never auto-loads external resources. Future dynamic capabilities must
    /// first join a unified trusted deployment-package mechanism.
    /// </remarks>
    public static class PortalModuleCatalog
    {
        private const int ManifestSchemaVersion = 1;
        private const string ModuleRootVirtualPath = "~/DesktopModules";

        private static readonly Regex PackageIdPattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_.-]{0,99}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex DirectoryNamePattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_-]{0,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// 获取所有已部署并通过 manifest 校验的模块包。
        /// Gets every deployed module package that passes manifest validation.
        /// </summary>
        /// <returns>按显示名排序的只读模块包列表。Read-only module package list ordered by display name.</returns>
        public static IList<PortalModulePackage> GetTrustedPackages()
        {
            var packages = new List<PortalModulePackage>();
            string rootPath = HostingEnvironment.MapPath(ModuleRootVirtualPath);
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return packages.AsReadOnly();
            }

            foreach (DirectoryInfo directory in new DirectoryInfo(rootPath).GetDirectories())
            {
                PortalModulePackage package;
                string reason;
                if (TryReadPackage(directory.Name, out package, out reason))
                {
                    packages.Add(package);
                }
            }

            // 重复 PackageId 会让状态表、审计和缓存身份失去唯一归属，因此整组包都不作为可信包返回。
            // A duplicate PackageId makes state rows, audit records, and cache identity ambiguous, so no package in
            // that duplicate group is returned as trusted.
            return packages
                .GroupBy(item => item.PackageId, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() == 1)
                .Select(group => group.Single())
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.PackageId, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 按稳定包标识读取一个已验证部署模块包。
        /// Reads one validated deployment module package by stable package identifier.
        /// </summary>
        /// <param name="packageId">模块包标识。Module package identifier.</param>
        /// <param name="package">成功时返回已验证模块包。Validated module package when successful.</param>
        /// <param name="reason">失败时返回不含物理路径的原因。Failure reason without physical paths.</param>
        /// <returns>是否找到并验证该模块包。Whether the module package was found and validated.</returns>
        public static bool TryGetTrustedPackage(
            string packageId,
            out PortalModulePackage package,
            out string reason)
        {
            package = null;
            reason = string.Empty;
            if (!IsValidPackageId(packageId))
            {
                reason = "Module package identifier is invalid.";
                return false;
            }

            package = GetTrustedPackages().FirstOrDefault(item =>
                string.Equals(item.PackageId, packageId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (package == null)
            {
                reason = "Module package is not deployed or its manifest is invalid.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 解析模块实例的部署包、启用状态和缓存身份。
        /// Resolves a module instance deployment package, enabled state, and cache identity.
        /// </summary>
        /// <param name="moduleSettings">当前模块实例设置。Current module instance settings.</param>
        /// <param name="context">当前 HTTP 上下文。Current HTTP context.</param>
        /// <param name="descriptor">成功时返回受控运行描述。Controlled runtime descriptor when successful.</param>
        /// <param name="reason">失败或禁用时的非敏感原因。Non-sensitive reason when failed or disabled.</param>
        /// <returns>模块入口是否允许进入加载流程。Whether the module entry may enter the loading flow.</returns>
        public static bool TryResolveModule(
            ModuleSettings moduleSettings,
            HttpContext context,
            out PortalModuleRuntimeDescriptor descriptor,
            out string reason)
        {
            descriptor = null;
            reason = string.Empty;
            if (moduleSettings == null)
            {
                reason = "Module settings are unavailable.";
                return false;
            }

            string source;
            try
            {
                source = PortalModulePathValidator.NormalizeDesktopSourceOrThrow(moduleSettings.DesktopSrc);
            }
            catch (InvalidOperationException exception)
            {
                reason = PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.Message, 200);
                return false;
            }

            PortalModulePackage package = GetTrustedPackages().FirstOrDefault(item =>
                string.Equals(item.DesktopEntry, source, StringComparison.OrdinalIgnoreCase));
            if (package == null)
            {
                if (HasManifestCandidate(source))
                {
                    reason = "Module package manifest is invalid or does not declare the requested entry.";
                    return false;
                }

                descriptor = new PortalModuleRuntimeDescriptor(
                    source,
                    false,
                    true,
                    true,
                    null,
                    "legacy|" + source.ToLowerInvariant());
                return true;
            }

            PortalModulePackageStateReadResult stateResult = PortalModulePackageStates.Read(package.PackageId, context);
            bool isEnabled = stateResult.IsAvailable && stateResult.State != null
                ? stateResult.State.IsEnabled
                : true;
            string stateRevision = stateResult.IsAvailable && stateResult.State != null && stateResult.State.IsConfigured
                ? stateResult.State.UpdatedUtc.Ticks.ToString()
                : "default";
            descriptor = new PortalModuleRuntimeDescriptor(
                source,
                true,
                isEnabled,
                stateResult.IsAvailable,
                package,
                "package|" + package.PackageId.ToLowerInvariant() + "|" + package.Version + "|" + stateRevision + "|" + source.ToLowerInvariant());
            if (!isEnabled)
            {
                reason = "Module package is disabled.";
            }

            return true;
        }

        /// <summary>
        /// 获取当前门户 Tab 中已启用模块包的 CSS 资源。
        /// Gets CSS resources for enabled module packages in the current portal Tab.
        /// </summary>
        /// <param name="context">当前 HTTP 上下文。Current HTTP context.</param>
        /// <returns>已去重且可通过虚拟目录解析的 CSS 资源。De-duplicated CSS resources resolvable through the virtual directory.</returns>
        public static IList<PortalModuleStyleResource> GetActiveStyleResources(HttpContext context = null)
        {
            var resources = new List<PortalModuleStyleResource>();
            HttpContext current = context ?? HttpContext.Current;
            if (current == null || IsAdminRequest(current))
            {
                return resources.AsReadOnly();
            }

            try
            {
                PortalSettings settings = PortalContext.GetPortalSettings(current);
                if (settings == null || settings.ActiveTab == null)
                {
                    return resources.AsReadOnly();
                }

                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ModuleSettings module in settings.ActiveTab.Modules)
                {
                    PortalModuleRuntimeDescriptor descriptor;
                    string reason;
                    if (!TryResolveModule(module, current, out descriptor, out reason) ||
                        !descriptor.IsManagedPackage || !descriptor.IsEnabled)
                    {
                        continue;
                    }

                    foreach (string resource in descriptor.Package.Resources.Where(item =>
                        item.EndsWith(".css", StringComparison.OrdinalIgnoreCase)))
                    {
                        string virtualPath = "~/DesktopModules/" + descriptor.Package.DirectoryName + "/" + resource;
                        if (paths.Add(virtualPath))
                        {
                            resources.Add(new PortalModuleStyleResource(descriptor.Package.PackageId, virtualPath));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                PortalDiagnostics.Error("ModulePackage.Styles", "Resolving active module package styles failed.", exception, current);
            }

            return resources.AsReadOnly();
        }

        /// <summary>
        /// 判断包标识是否满足稳定 ASCII 契约。
        /// Determines whether a package identifier meets the stable ASCII contract.
        /// </summary>
        /// <param name="packageId">待校验包标识。Package identifier to validate.</param>
        /// <returns>包标识是否可安全用于状态表和缓存键。Whether the identifier is safe for the state table and cache keys.</returns>
        public static bool IsValidPackageId(string packageId)
        {
            return !string.IsNullOrWhiteSpace(packageId) && PackageIdPattern.IsMatch(packageId.Trim());
        }

        private static bool TryReadPackage(string directoryName, out PortalModulePackage package, out string reason)
        {
            package = null;
            reason = string.Empty;
            if (!DirectoryNamePattern.IsMatch(directoryName ?? string.Empty))
            {
                reason = "Module package directory name is invalid.";
                return false;
            }

            string rootPath = HostingEnvironment.MapPath(ModuleRootVirtualPath);
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                reason = "Module root is unavailable.";
                return false;
            }

            string packagePath;
            try
            {
                packagePath = GetChildPath(rootPath, directoryName);
            }
            catch (InvalidOperationException)
            {
                reason = "Module package directory is outside the allowed root.";
                return false;
            }

            string manifestPath = Path.Combine(packagePath, "module.json");
            if (!Directory.Exists(packagePath) || !File.Exists(manifestPath))
            {
                reason = "module.json is missing.";
                return false;
            }

            try
            {
                JObject manifest = JObject.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                if (!IsSchemaVersionSupported(manifest))
                {
                    reason = "Module manifest schemaVersion is unsupported.";
                    return false;
                }

                string packageId = ReadRequiredString(manifest, "packageId", 100);
                string displayName = ReadRequiredString(manifest, "displayName", 100);
                string version = ReadRequiredString(manifest, "version", 64);
                string minimumPortalVersion = ReadOptionalString(manifest, "minimumPortalVersion", 64);
                string desktopEntry = ReadRequiredString(manifest, "desktopEntry", 250);
                string normalizedEntry;
                string validationError;
                if (!IsValidPackageId(packageId) ||
                    !PortalModulePathValidator.TryNormalizeDesktopSource(desktopEntry, out normalizedEntry, out validationError) ||
                    !normalizedEntry.StartsWith("DesktopModules/" + directoryName + "/", StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Module manifest packageId or desktopEntry is invalid.";
                    return false;
                }

                string relativeEntry = normalizedEntry.Substring(("DesktopModules/" + directoryName + "/").Length);
                if (!File.Exists(GetChildPath(packagePath, relativeEntry.Replace('/', Path.DirectorySeparatorChar))))
                {
                    reason = "Module manifest desktopEntry does not exist.";
                    return false;
                }

                if (manifest["script"] != null || manifest["scripts"] != null ||
                    manifest["externalUrl"] != null || manifest["externalUrls"] != null ||
                    manifest["assembly"] != null || manifest["assemblies"] != null || manifest["packageUrl"] != null)
                {
                    reason = "Module manifest declares a prohibited script, external URL, or assembly.";
                    return false;
                }

                IList<string> resources = ReadAndValidateResources(manifest, packagePath);
                package = new PortalModulePackage(
                    directoryName,
                    packageId,
                    displayName,
                    version,
                    minimumPortalVersion,
                    normalizedEntry,
                    resources);
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is Newtonsoft.Json.JsonException ||
                exception is InvalidOperationException)
            {
                reason = "Module manifest is invalid.";
                return false;
            }
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

        private static IList<string> ReadAndValidateResources(JObject manifest, string packagePath)
        {
            JArray resources = manifest["resources"] as JArray;
            if (resources == null)
            {
                throw new InvalidOperationException("Module resources are missing.");
            }

            var validatedResources = new List<string>();
            foreach (JToken token in resources)
            {
                if (token.Type != JTokenType.String)
                {
                    throw new InvalidOperationException("Module resource is invalid.");
                }

                string resource = token.Value<string>().Trim().Replace('\\', '/');
                if (!IsValidResourcePath(resource))
                {
                    throw new InvalidOperationException("Module resource path is not allowed.");
                }

                string physicalPath = GetChildPath(packagePath, resource.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(physicalPath) || !IsAllowedResourceExtension(resource))
                {
                    throw new InvalidOperationException("Module resource is unavailable or uses a prohibited extension.");
                }

                validatedResources.Add(resource);
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

        private static bool IsAllowedResourceExtension(string resource)
        {
            string extension = Path.GetExtension(resource);
            return string.Equals(extension, ".css", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasManifestCandidate(string source)
        {
            string[] segments = source.Split('/');
            if (segments.Length < 3 || !string.Equals(segments[0], "DesktopModules", StringComparison.OrdinalIgnoreCase) ||
                !DirectoryNamePattern.IsMatch(segments[1]))
            {
                return false;
            }

            string rootPath = HostingEnvironment.MapPath(ModuleRootVirtualPath);
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }

            try
            {
                return File.Exists(Path.Combine(GetChildPath(rootPath, segments[1]), "module.json"));
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        private static bool IsAdminRequest(HttpContext context)
        {
            string path = context.Request == null ? string.Empty : context.Request.AppRelativeCurrentExecutionFilePath;
            return path.StartsWith("~/Admin/", StringComparison.OrdinalIgnoreCase);
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
