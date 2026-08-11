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
    /// <lang>
    ///   <zh-CN>已验证的受信任部署主题包元数据。</zh-CN>
    ///   <en>Metadata for a validated trusted-deployment theme package.</en>
    /// </lang>
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
        /// <l>
        ///   <zh-CN>与物理目录一致的稳定主题名。</zh-CN>
        ///   <en>Stable theme name matching the physical directory.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>后台选择器展示名称。</zh-CN>
        ///   <en>Display name for admin selectors.</en>
        /// </l>
        /// </param>
        /// <param name="version">
        /// <l>
        ///   <zh-CN>主题包声明版本。</zh-CN>
        ///   <en>Version declared by the theme package.</en>
        /// </l>
        /// </param>
        /// <param name="minimumPortalVersion">
        /// <l>
        ///   <zh-CN>主题包声明的最低门户版本，可为空。</zh-CN>
        ///   <en>Minimum portal version declared by the package, if any.</en>
        /// </l>
        /// </param>
        /// <param name="inheritsDefault">
        /// <l>
        ///   <zh-CN>是否声明继承 Default 主题。</zh-CN>
        ///   <en>Whether the package declares inheritance from the Default theme.</en>
        /// </l>
        /// </param>
        /// <param name="resources">
        /// <l>
        ///   <zh-CN>已校验的站内本地资源路径集合。</zh-CN>
        ///   <en>Validated site-local resource path collection.</en>
        /// </l>
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
        /// <lang>
        ///   <zh-CN>与部署目录一致的稳定主题名。</zh-CN>
        ///   <en>Stable theme name matching the deployment directory.</en>
        /// </lang>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>供后台选择器展示的名称。</zh-CN>
        ///   <en>Name displayed by the admin selector.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>主题包版本。</zh-CN>
        ///   <en>Theme package version.</en>
        /// </lang>
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>主题声明的最低门户版本。</zh-CN>
        ///   <en>Minimum portal version declared by the theme.</en>
        /// </lang>
        /// </summary>
        public string MinimumPortalVersion { get; private set; }

        /// <summary>
        /// 是否在 CSS 中继承 Default 主题。
        /// Whether the CSS inherits the Default theme.
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>当前仅是部署包声明性元数据。主题 CSS 必须自行显式表达继承关系；catalog 和 Master Page 不会据此自动加载或注入 Default 主题资源。</zh-CN>
        ///   <en>This is declarative deployment-package metadata only. Theme CSS must explicitly express inheritance itself; the catalog and Master Page do not automatically load or inject Default theme resources from this value.</en>
        /// </lang>
        /// </remarks>
        public bool InheritsDefault { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>已声明且位于主题目录内的本地资源。</zh-CN>
        ///   <en>Declared local resources located inside the theme directory.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此列表用于部署包校验和工具链，不是 Master Page 的通用资源注入协议。Web Forms 原生 Theme 仍按 App_Themes 机制处理主题文件。</zh-CN>
        ///   <en>This list is for deployment-package validation and tooling; it is not a general Master Page resource injection protocol. Native Web Forms Theme handling still processes theme files through App_Themes.</en>
        /// </lang>
        /// </remarks>
        public IList<string> Resources { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>主题部署包目录和 manifest 校验器。</zh-CN>
    ///   <en>Theme deployment-package directory and manifest validator.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P3.1 只发现由受信任部署流程写入的目录。此类不上传、不解压、不编辑，也不自动加载远程 URL 或 JavaScript；可信主题包来源、签名、许可、版本与回滚由独立机制覆盖。</zh-CN>
    ///   <en>P3.1 discovers directories written by a trusted deployment process only. It does not upload, unzip, or edit packages, and it never auto-loads remote URLs or JavaScript. A separate trusted-package mechanism covers provenance, signatures, licenses, versions, and rollback.</en>
    /// </lang>
        /// </remarks>
    public static class PortalThemeCatalog
    {
        // <lang>
        //   <zh-CN>当前主题 manifest 的唯一受支持 schema 版本；未知版本 fail-closed，不按旧结构猜测。</zh-CN>
        //   <en>Only supported schema version for the current theme manifest; unknown versions fail closed rather than being guessed as an old shape.</en>
        // </lang>
        private const int ManifestSchemaVersion = 1;

        // <lang>
        //   <zh-CN>主题名同时作为目录名和 CSS class 片段，限制为稳定 ASCII 标识符以阻断路径/标记结构。</zh-CN>
        //   <en>Theme names serve as both directory names and CSS-class fragments, so they are restricted to stable ASCII identifiers to block path/markup structure.</en>
        // </lang>
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
            // <lang>
            //   <zh-CN>收集本次扫描中通过最小信任契约的包；失败包不会阻断其他包或默认主题回退。</zh-CN>
            //   <en>Collect packages that pass the minimal trust contract for this scan; one failed package does not block other packages or the default-theme fallback.</en>
            // </lang>
            var packages = new List<PortalThemePackage>();
            // <lang>
            //   <zh-CN>只映射应用内 App_Themes 根目录；根目录不可用时返回空只读集合，不暴露物理路径。</zh-CN>
            //   <en>Map only the in-application App_Themes root; return an empty read-only collection when unavailable without exposing a physical path.</en>
            // </lang>
            string rootPath = HostingEnvironment.MapPath("~/App_Themes");
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return packages.AsReadOnly();
            }

            foreach (DirectoryInfo directory in new DirectoryInfo(rootPath).GetDirectories())
            {
                // <lang>
                //   <zh-CN>逐目录执行独立验证；reason 仅用于诊断边界，本入口不把失败原因写入返回集合。</zh-CN>
                //   <en>Validate each directory independently; reason is kept within the diagnostic boundary and is not emitted in the returned collection.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>先清空 out 结果，确保任一失败分支不会泄露上一次调用的包或原因。</zh-CN>
            //   <en>Clear out results first so every failure branch cannot leak a package or reason from a previous call.</en>
            // </lang>
            package = null;
            reason = string.Empty;

            if (!IsValidThemeName(themeName))
            {
                reason = "Theme name contains invalid characters.";
                return false;
            }

            // <lang>
            //   <zh-CN>主题根目录是后续物理路径检查的信任边界，目录缺失时保持不可用而非回退到任意路径。</zh-CN>
            //   <en>The theme root is the trust boundary for later physical-path checks; when unavailable, remain unavailable instead of falling back to an arbitrary path.</en>
            // </lang>
            string rootPath = HostingEnvironment.MapPath("~/App_Themes");
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                reason = "Theme root is unavailable.";
                return false;
            }

            // <lang>
            //   <zh-CN>通过根目录内子路径 helper 解析包目录；越界异常转换为不含路径的安全原因。</zh-CN>
            //   <en>Resolve the package directory through the root-contained child-path helper; convert escape exceptions into a path-free safe reason.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>固定 manifest 和 Default.css 入口，只有两者都存在才进入内容解析。</zh-CN>
            //   <en>Use fixed manifest and Default.css entry points; parse content only when both exist.</en>
            // </lang>
            string manifestPath = Path.Combine(packagePath, "theme.json");
            string defaultCssPath = Path.Combine(packagePath, "Default.css");
            if (!File.Exists(manifestPath) || !File.Exists(defaultCssPath))
            {
                reason = "theme.json or Default.css is missing.";
                return false;
            }

            try
            {
                // <lang>
                //   <zh-CN>以 UTF-8 读取低敏 manifest；解析和契约失败统一转换为无路径的无效包结果。</zh-CN>
                //   <en>Read the low-sensitivity manifest as UTF-8; parsing or contract failures become a path-free invalid-package result.</en>
                // </lang>
                JObject manifest = JObject.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                if (!IsSchemaVersionSupported(manifest))
                {
                    reason = "Theme manifest schemaVersion is unsupported.";
                    return false;
                }

                // <lang>
                //   <zh-CN>manifest 名称必须与已经验证的目录名精确一致，防止包投影与物理目录错配。</zh-CN>
                //   <en>The manifest name must exactly match the already validated directory name so the package projection cannot diverge from its physical directory.</en>
                // </lang>
                string manifestName = ReadRequiredString(manifest, "name", 64);
                if (!string.Equals(themeName, manifestName, StringComparison.Ordinal))
                {
                    reason = "Theme manifest name does not match its directory.";
                    return false;
                }

                // <lang>
                //   <zh-CN>读取展示、版本、兼容性和资源投影；这些值仅用于已验证包的低敏元数据，不改变主题加载授权。</zh-CN>
                //   <en>Read display, version, compatibility, and resource projections; these values are low-sensitivity metadata for a validated package and do not grant theme-loading authorization.</en>
                // </lang>
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
        /// <lang>
        ///   <zh-CN>判断主题名是否可安全作为部署目录和 CSS class 片段。</zh-CN>
        ///   <en>Determines whether a theme name is safe as a deployment directory and CSS-class segment.</en>
        /// </lang>
        /// </summary>
        /// <param name="themeName">待校验主题名。Theme name to validate.</param>
        /// <returns>主题名是否满足稳定 ASCII 契约。Whether the name meets the stable ASCII contract.</returns>
        public static bool IsValidThemeName(string themeName)
        {
            // <lang>
            //   <zh-CN>先拒绝空白，再用文化无关正则校验稳定 ASCII 形状；trim 只用于验证，不改写调用方值。</zh-CN>
            //   <en>Reject blank input first, then validate a stable ASCII shape with the culture-invariant regex; trimming is validation-only and does not rewrite caller input.</en>
            // </lang>
            return !string.IsNullOrWhiteSpace(themeName) && ThemeNamePattern.IsMatch(themeName.Trim());
        }

        /// <summary>
        /// <lang>
        /// <zh-CN>检查 manifest schema 是否为当前加载器支持的最小版本。</zh-CN>
        /// <en>Checks whether the manifest schema is the minimal version supported by the current loader.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest">
        /// <l>
        ///   <zh-CN>已解析的主题 manifest。</zh-CN>
        ///   <en>Parsed theme manifest.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>schemaVersion 是否精确匹配当前受支持版本。</zh-CN>
        ///   <en>Whether schemaVersion exactly matches the currently supported version.</en>
        /// </l>
        /// </returns>
        private static bool IsSchemaVersionSupported(JObject manifest)
        {
            // <lang>
            //   <zh-CN>只读取 schemaVersion 节点并要求整数精确匹配；缺失、null 或其它类型均拒绝。</zh-CN>
            //   <en>Read only the schemaVersion node and require an exact integer match; missing, null, or other types are rejected.</en>
            // </lang>
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
        /// <l>
        ///   <zh-CN>已解析的主题 manifest。</zh-CN>
        ///   <en>Parsed theme manifest.</en>
        /// </l>
        /// </param>
        /// <param name="propertyName">
        /// <l>
        ///   <zh-CN>需要读取的属性名。</zh-CN>
        ///   <en>Property name to read.</en>
        /// </l>
        /// </param>
        /// <param name="maximumLength">
        /// <l>
        ///   <zh-CN>允许的最大字符数。</zh-CN>
        ///   <en>Maximum allowed character count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>去除首尾空白后的必填字段值。</zh-CN>
        ///   <en>Required field value after trimming surrounding whitespace.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>字段缺失、为空、类型不符或超过长度限制时抛出。</zh-CN>
        ///   <en>Thrown when the field is missing, blank, incorrectly typed, or too long.</en>
        /// </l>
        /// </exception>
        private static string ReadRequiredString(JObject manifest, string propertyName, int maximumLength)
        {
            // <lang>
            //   <zh-CN>复用可选读取器取得已完成类型、trim 和长度校验的值，再将空值提升为必填字段错误。</zh-CN>
            //   <en>Reuse the optional reader for type, trimming, and length validation, then promote an empty value to a required-field error.</en>
            // </lang>
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
        /// <l>
        ///   <zh-CN>已解析的主题 manifest。</zh-CN>
        ///   <en>Parsed theme manifest.</en>
        /// </l>
        /// </param>
        /// <param name="propertyName">
        /// <l>
        ///   <zh-CN>需要读取的属性名。</zh-CN>
        ///   <en>Property name to read.</en>
        /// </l>
        /// </param>
        /// <param name="maximumLength">
        /// <l>
        ///   <zh-CN>允许的最大字符数。</zh-CN>
        ///   <en>Maximum allowed character count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>字段不存在时返回空字符串，否则返回 trim 后的值。</zh-CN>
        ///   <en>Empty string when the field is absent; otherwise the trimmed value.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>字段类型不符或超过长度限制时抛出。</zh-CN>
        ///   <en>Thrown when the field type is invalid or the value is too long.</en>
        /// </l>
        /// </exception>
        private static string ReadOptionalString(JObject manifest, string propertyName, int maximumLength)
        {
            // <lang>
            //   <zh-CN>按属性名读取原始 JSON token；缺失或 null 明确回退为空字符串，不伪造默认业务值。</zh-CN>
            //   <en>Read the raw JSON token by property name; missing or null values explicitly fall back to an empty string without fabricating a business default.</en>
            // </lang>
            JToken token = manifest[propertyName];
            if (token == null || token.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            if (token.Type != JTokenType.String)
            {
                throw new InvalidOperationException("Manifest string value is invalid.");
            }

            // <lang>
            //   <zh-CN>类型确认后去除首尾空白，再按 manifest 字段上限拒绝过长值。</zh-CN>
            //   <en>After confirming the type, trim surrounding whitespace and reject values beyond the manifest field limit.</en>
            // </lang>
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
        /// <l>
        ///   <zh-CN>已解析的主题 manifest。</zh-CN>
        ///   <en>Parsed theme manifest.</en>
        /// </l>
        /// </param>
        /// <param name="propertyName">
        /// <l>
        ///   <zh-CN>需要读取的属性名。</zh-CN>
        ///   <en>Property name to read.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>读取到的布尔值；未声明时为 false。</zh-CN>
        ///   <en>The declared Boolean value; false when not declared.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>字段存在但不是布尔类型时抛出。</zh-CN>
        ///   <en>Thrown when the field exists but is not Boolean.</en>
        /// </l>
        /// </exception>
        private static bool ReadOptionalBoolean(JObject manifest, string propertyName)
        {
            // <lang>
            //   <zh-CN>缺失布尔值采用 false 这一声明性回退；存在但类型不符仍然阻止包通过。</zh-CN>
            //   <en>Use false as the declarative fallback for a missing Boolean; an existing value with the wrong type still blocks the package.</en>
            // </lang>
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
        /// <l>
        ///   <zh-CN>已解析的主题 manifest。</zh-CN>
        ///   <en>Parsed theme manifest.</en>
        /// </l>
        /// </param>
        /// <param name="packagePath">
        /// <l>
        ///   <zh-CN>主题包物理根目录。</zh-CN>
        ///   <en>Physical root directory of the theme package.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已通过路径、存在性和类型限制的只读资源集合。</zh-CN>
        ///   <en>Read-only resource collection that passed path, existence, and type restrictions.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>资源清单缺失、路径越界、文件不存在、声明脚本或缺少 Default.css 时抛出。</zh-CN>
        ///   <en>Thrown when resources are missing, escape the package root, do not exist, declare scripts, or omit Default.css.</en>
        /// </l>
        /// </exception>
        private static IList<string> ReadAndValidateResources(JObject manifest, string packagePath)
        {
            // <lang>
            //   <zh-CN>资源必须以非空 JSON 数组声明；缺失数组不是“无资源”成功状态。</zh-CN>
            //   <en>Resources must be declared as a nonempty JSON array; a missing array is not a successful “no resources” state.</en>
            // </lang>
            JArray resources = manifest["resources"] as JArray;
            if (resources == null || resources.Count == 0)
            {
                throw new InvalidOperationException("Theme resources are missing.");
            }

            // <lang>
            //   <zh-CN>只累积已通过路径、存在性、脚本排除和 Default.css 契约的资源相对路径。</zh-CN>
            //   <en>Accumulate only resource-relative paths that pass path, existence, script exclusion, and Default.css contract checks.</en>
            // </lang>
            var validatedResources = new List<string>();
            // <lang>
            //   <zh-CN>Default.css 是主题包最小样式入口，循环结束前必须被显式声明。</zh-CN>
            //   <en>Default.css is the package's minimum style entry point and must be explicitly declared before the loop completes.</en>
            // </lang>
            bool containsDefaultCss = false;
            foreach (JToken token in resources)
            {
                if (token.Type != JTokenType.String)
                {
                    throw new InvalidOperationException("Theme resource is invalid.");
                }

                // <lang>
                //   <zh-CN>把资源声明规范为斜杠相对路径；后续白名单和物理映射均基于该规范值。</zh-CN>
                //   <en>Normalize the resource declaration to a slash-separated relative path; later allowlist and physical mapping use this normalized value.</en>
                // </lang>
                string resource = token.Value<string>().Trim().Replace('\\', '/');
                if (!IsValidResourcePath(resource))
                {
                    throw new InvalidOperationException("Theme resource path is not allowed.");
                }

                // <lang>
                //   <zh-CN>把已通过相对路径检查的资源映射回包根目录；GetChildPath 再次执行规范化前缀边界检查。</zh-CN>
                //   <en>Map the validated relative resource under the package root; GetChildPath performs the normalized-prefix boundary check again.</en>
                // </lang>
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
        /// <l>
        ///   <zh-CN>manifest 中声明的资源相对路径。</zh-CN>
        ///   <en>Resource relative path declared in the manifest.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>路径是否可继续映射到主题包根目录下。</zh-CN>
        ///   <en>Whether the path may continue to be mapped under the theme package root.</en>
        /// </l>
        /// </returns>
        private static bool IsValidResourcePath(string resource)
        {
            // <lang>
            //   <zh-CN>先拒绝空值、绝对路径、协议 URL 和协议相对路径，避免资源声明离开主题根目录语义。</zh-CN>
            //   <en>Reject blank, absolute, protocol, and protocol-relative paths first so resource declarations cannot leave the theme-root semantics.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(resource) || resource.StartsWith("/", StringComparison.Ordinal) ||
                resource.IndexOf("://", StringComparison.Ordinal) >= 0 || resource.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>逐段拒绝空段、当前目录和父目录片段，阻断显式遍历路径。</zh-CN>
            //   <en>Reject empty, current-directory, and parent-directory segments to block explicit traversal paths.</en>
            // </lang>
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
        /// <l>
        ///   <zh-CN>允许访问的物理根目录。</zh-CN>
        ///   <en>Allowed physical root directory.</en>
        /// </l>
        /// </param>
        /// <param name="childPath">
        /// <l>
        ///   <zh-CN>待映射的子路径。</zh-CN>
        ///   <en>Child path to map.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>确认位于根目录下的规范化物理路径。</zh-CN>
        ///   <en>Normalized physical path confirmed to remain under the root directory.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>子路径逃逸根目录时抛出。</zh-CN>
        ///   <en>Thrown when the child path escapes the root directory.</en>
        /// </l>
        /// </exception>
        private static string GetChildPath(string rootPath, string childPath)
        {
            // <lang>
            //   <zh-CN>先规范化根目录并去除末尾分隔符，保证后续前缀比较只接受根目录的真实子路径。</zh-CN>
            //   <en>Normalize the root and remove its trailing separator first so the later prefix check accepts only real descendants.</en>
            // </lang>
            string normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            // <lang>
            //   <zh-CN>组合并规范化候选路径；任何路径解析后的逃逸都在返回前拒绝。</zh-CN>
            //   <en>Combine and normalize the candidate path; any escape revealed by path resolution is rejected before return.</en>
            // </lang>
            string candidate = Path.GetFullPath(Path.Combine(normalizedRoot, childPath));
            // <lang>
            //   <zh-CN>使用带分隔符的根前缀，避免把同前缀但非子目录的路径误判为合法子路径。</zh-CN>
            //   <en>Use a separator-terminated root prefix so a sibling path sharing the text prefix is not mistaken for a child.</en>
            // </lang>
            string rootWithSeparator = normalizedRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path escapes its root.");
            }

            return candidate;
        }
    }
}
