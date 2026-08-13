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
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一个已通过 manifest 和资源校验的模块包元数据对象。</zh-CN>
        ///   <en>Creates metadata for a module package that has passed manifest and resource validation.</en>
        /// </lang>
        /// </summary>
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
    /// <lang>
    ///   <zh-CN>一个可由页面层消费的已验证模块包 CSS 资源引用。</zh-CN>
    ///   <en>A validated module-package CSS resource reference consumable by the page layer.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象只承载 catalog 已筛选的稳定包标识与站内虚拟路径；它不重新验证 manifest、构造外部 URL、注入标记、加载控件或实施当前用户的页面授权。</zh-CN>
    ///   <en>This object carries only the stable package identifier and site-local virtual path already filtered by the catalog; it does not revalidate the manifest, construct an external URL, inject markup, load a control, or enforce the current user's page authorization.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalModuleStyleResource
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建一个已验证模块包 CSS 资源引用。</zh-CN>
        ///   <en>Creates a CSS resource reference for a validated module package.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId"><l zh-CN="catalog 传入的稳定包标识；为保持既有 DTO 回退，null 会保留为空字符串。" en="Stable package identifier supplied by the catalog; null is retained as an empty string to preserve the existing DTO fallback." /></param>
        /// <param name="virtualPath"><l zh-CN="catalog 从已验证目录和已声明 CSS 资源组成的站内虚拟路径；为保持既有 DTO 回退，null 会保留为空字符串。" en="Site-local virtual path composed by the catalog from a validated directory and declared CSS resource; null is retained as an empty string to preserve the existing DTO fallback." /></param>
        internal PortalModuleStyleResource(string packageId, string virtualPath)
        {
            // <lang>
            //   <zh-CN>稳定包标识仅用于把输出样式归属到 catalog 已解析的包；空值兼容旧调用方，且不在此处推断授权或包存在性。</zh-CN>
            //   <en>The stable package identifier only associates the output style with the package resolved by the catalog; an empty value preserves caller compatibility and does not infer authorization or package existence here.</en>
            // </lang>
            PackageId = packageId ?? string.Empty;

            // <lang>
            //   <zh-CN>虚拟路径是页面注入器随后解析的站内引用；其 manifest/路径信任已由上游建立，本 DTO 不进行第二次路径处理。</zh-CN>
            //   <en>The virtual path is a site-local reference later resolved by the page injector; its manifest and path trust were established upstream, so this DTO performs no second path transformation.</en>
            // </lang>
            VirtualPath = virtualPath ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>产出样式所属的稳定模块包标识。</zh-CN>
        ///   <en>Stable module-package identifier that owns the output style.</en>
        /// </lang>
        /// </summary>
        /// <remarks><l zh-CN="该值用于归属和诊断关联，不是当前请求的授权断言。" en="This value supports ownership and diagnostic association; it is not an authorization assertion for the current request." /></remarks>
        public string PackageId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可由当前应用虚拟目录解析的站内 CSS 路径。</zh-CN>
        ///   <en>Site-local CSS path resolvable through the current application virtual directory.</en>
        /// </lang>
        /// </summary>
        /// <remarks><l zh-CN="它来自 catalog 的受信任资源集合，不表示已经写入页面 Head。" en="It originates from the catalog's trusted resource set and does not mean it has already been written to the page Head." /></remarks>
        public string VirtualPath { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>当前模块实例经受控解析后的运行描述。</zh-CN>
    ///   <en>Runtime descriptor of the current module instance after controlled resolution.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该描述符只陈述路径、包/Profile 分类与状态解析事实；它不加载控件、不注入样式、不实施页面授权，也不证明当前用户可访问模块。</zh-CN>
    ///   <en>This descriptor states only path, package/profile classification, and state-resolution facts; it does not load a control, inject styles, enforce page authorization, or prove that the current user may access the module.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalModuleRuntimeDescriptor
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建当前模块实例的受控运行描述。</zh-CN>
        ///   <en>Creates the controlled runtime descriptor for the current module instance.</en>
        /// </lang>
        /// </summary>
        /// <param name="desktopSource"><l zh-CN="已规范化且允许继续进入加载流程的桌面控件路径。" en="Normalized desktop-control path allowed to continue into the loading flow." /></param>
        /// <param name="profilePackageId"><l zh-CN="用于 Profile gate 的稳定或 Legacy 虚拟包标识；Core 使用固定标识。" en="Stable or Legacy virtual package identifier used by the profile gate; Core uses its fixed identifier." /></param>
        /// <param name="isManagedPackage"><l zh-CN="是否匹配已验证部署 manifest 包，而不是 Legacy/Core 路径。" en="Whether it matches a validated deployment-manifest package rather than a Legacy/Core path." /></param>
        /// <param name="isEnabled"><l zh-CN="状态解析得出的启用事实；它不替代 Profile、路径或页面授权。" en="Enabled fact produced by state resolution; it does not replace profile, path, or page authorization." /></param>
        /// <param name="isStateAvailable"><l zh-CN="状态表读取是否可用；Legacy/Core 路径不依赖该表。" en="Whether the state-table read was available; Legacy/Core paths do not depend on that table." /></param>
        /// <param name="package"><l zh-CN="已验证部署包；Legacy/Core 路径保持 null。" en="Validated deployment package; Legacy/Core paths retain null." /></param>
        /// <param name="cacheIdentity"><l zh-CN="供模块缓存隔离的受控身份；不是用户身份或授权令牌。" en="Controlled identity used for module-cache isolation; it is not a user identity or authorization token." /></param>
        internal PortalModuleRuntimeDescriptor(
            string desktopSource,
            string profilePackageId,
            bool isManagedPackage,
            bool isEnabled,
            bool isStateAvailable,
            PortalModulePackage package,
            string cacheIdentity)
        {
            // <lang>
            //   <zh-CN>构造器只投影已完成的解析事实，不重新读取 manifest、状态表或 Profile；调用方已决定该实例属于 Core、Legacy 或已验证部署包。</zh-CN>
            //   <en>The constructor projects completed resolution facts only and does not reread manifest, state table, or profile; its caller has already decided whether the instance is Core, Legacy, or a validated deployment package.</en>
            // </lang>
            DesktopSource = desktopSource ?? string.Empty;
            ProfilePackageId = profilePackageId ?? string.Empty;
            IsManagedPackage = isManagedPackage;
            IsEnabled = isEnabled;
            IsStateAvailable = isStateAvailable;
            Package = package;

            // <lang>
            //   <zh-CN>缓存身份保持受控稳定文本；空值归一而非允许调用方把 null 当作可与其它模块共享缓存的身份。</zh-CN>
            //   <en>The cache identity remains controlled stable text; normalize null rather than letting callers treat it as an identity that may share cache with another module.</en>
            // </lang>
            CacheIdentity = cacheIdentity ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>已规范化且允许继续进入后续加载流程的桌面控件路径。</zh-CN>
        ///   <en>Normalized desktop-control path allowed to continue into the later loading flow.</en>
        /// </lang>
        /// </summary>
        public string DesktopSource { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>参与 Profile 判定的 package id；Core 模块使用稳定虚拟标识。</zh-CN>
        ///   <en>Package id used for Profile decisions; Core modules use a stable virtual identifier.</en>
        /// </lang>
        /// </summary>
        public string ProfilePackageId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前实例是否匹配已验证部署 manifest 包；false 可表示 Core 或已映射 Legacy。</zh-CN>
        ///   <en>Whether the current instance matches a validated deployment-manifest package; false may mean Core or mapped Legacy.</en>
        /// </lang>
        /// </summary>
        public bool IsManagedPackage { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析后的包状态是否允许后续加载；已禁用包仍可有描述符，使调用方安全跳过而非把状态与解析失败混淆。</zh-CN>
        ///   <en>Whether the resolved package state permits later loading; a disabled package can still have a descriptor so callers safely skip it instead of conflating state with resolution failure.</en>
        /// </lang>
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>受信任部署包的状态表是否可用于本次解析；不可用时保持既有兼容默认启用，而不影响 Profile/部署边界。</zh-CN>
        ///   <en>Whether the state table was available for this validated deployment-package resolution; when unavailable, retain the existing compatibility default enabled state without affecting profile or deployment boundaries.</en>
        /// </lang>
        /// </summary>
        public bool IsStateAvailable { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>已验证部署包；Core 与 Legacy 路径为 null。</zh-CN>
        ///   <en>Validated deployment package; null for Core and Legacy paths.</en>
        /// </lang>
        /// </summary>
        public PortalModulePackage Package { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用于缓存隔离的受控身份文本，组合入口、包版本和显式状态修订，不包含用户凭据或请求秘密。</zh-CN>
        ///   <en>Controlled identity text for cache isolation, combining entry, package version, and explicit state revision without user credentials or request secrets.</en>
        /// </lang>
        /// </summary>
        public string CacheIdentity { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>受信任部署模块包目录与 manifest 的受限校验器。</zh-CN>
    ///   <en>Restricted validator for trusted-deployment module-package directories and manifests.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P3.2 只发现受信任部署流程写入的 `.ascx` 包目录。它不上传、不解压、不写入、不执行 DLL 或脚本，也不自动加载外部资源。manifest 通过不表示当前 Profile 允许、状态表已启用、页面已授权或控件已加载；这些后续门禁由独立运行时路径处理。</zh-CN>
    ///   <en>P3.2 discovers only `.ascx` package directories written by a trusted deployment process. It does not upload, unzip, write, execute DLLs or scripts, or auto-load external resources. A passing manifest does not mean that the current profile allows it, the state table enables it, a page is authorized, or a control has loaded; separate runtime paths handle those later gates.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleCatalog
    {
        // <lang>
        //   <zh-CN>仅接受当前受控 manifest schema；版本不匹配时整包不会进入可信目录，不能用宽松解析猜测未来字段语义。</zh-CN>
        //   <en>Accept only the current controlled manifest schema; a version mismatch keeps the whole package out of the trusted catalog rather than using permissive parsing to guess future field semantics.</en>
        // </lang>
        private const int ManifestSchemaVersion = 1;

        // <lang>
        //   <zh-CN>模块发现只能从应用内固定虚拟根开始；manifest、目录名或请求值都不能替换此根。</zh-CN>
        //   <en>Module discovery starts only from this fixed application-local virtual root; no manifest, directory name, or request value can replace it.</en>
        // </lang>
        private const string ModuleRootVirtualPath = "~/DesktopModules";

        // <lang>
        //   <zh-CN>稳定包标识限于受控 ASCII 形式，供状态表、审计和缓存身份共同引用；显示名称不能替代它。</zh-CN>
        //   <en>The stable package identifier is limited to a controlled ASCII form shared by state rows, audit, and cache identity; a display name cannot substitute for it.</en>
        // </lang>
        private static readonly Regex PackageIdPattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_.-]{0,99}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // <lang>
        //   <zh-CN>物理包目录名使用更窄的受控形式；在组合根内路径之前先拒绝点号、分隔符和其它逃逸表达。</zh-CN>
        //   <en>Physical package-directory names use a narrower controlled form; reject dots, separators, and other escape expressions before combining a path under the root.</en>
        // </lang>
        private static readonly Regex DirectoryNamePattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_-]{0,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取所有通过受信任部署目录与 manifest 校验的模块包。</zh-CN>
        ///   <en>Gets every module package that passes trusted-deployment directory and manifest validation.</en>
        /// </lang>
        /// </summary>
        /// <returns><l zh-CN="按显示名和稳定包标识排序的只读可信包列表；目录/manifest 无效或稳定标识重复的包不会出现。" en="Read-only trusted-package list ordered by display name and stable package identifier; packages with invalid directory/manifest data or duplicate stable identifiers are absent." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法只发现与验证候选部署包，不读取模块状态表、不解析 Profile、不授予页面访问，也不加载控件或资源。</zh-CN>
        ///   <en>This method only discovers and validates candidate deployment packages; it does not read the module-state table, resolve profiles, grant page access, or load controls or resources.</en>
        /// </lang>
        /// </remarks>
        public static IList<PortalModulePackage> GetTrustedPackages()
        {
            // <lang>
            //   <zh-CN>先在内存收集逐包验证成功的候选；在遍历完成前不向调用方暴露任何部分可信集合。</zh-CN>
            //   <en>First collect per-package validation successes in memory; do not expose a partially trusted set to callers before traversal completes.</en>
            // </lang>
            var packages = new List<PortalModulePackage>();
            string rootPath = HostingEnvironment.MapPath(ModuleRootVirtualPath);
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                // <lang>
                //   <zh-CN>应用未映射模块根或根目录不存在时安全地返回空集合；不从其它磁盘位置、请求路径或默认目录继续发现。</zh-CN>
                //   <en>When the application does not map the module root or the root is absent, safely return an empty set; do not continue discovery from another disk location, request path, or default directory.</en>
                // </lang>
                return packages.AsReadOnly();
            }

            foreach (DirectoryInfo directory in new DirectoryInfo(rootPath).GetDirectories())
            {
                // <lang>
                //   <zh-CN>每个直接子目录必须独立通过目录、manifest、入口和资源检查；一个失败候选不会使其它有效包或整个门户失败。</zh-CN>
                //   <en>Each immediate child directory must independently pass directory, manifest, entry, and resource checks; one failed candidate does not fail other valid packages or the whole portal.</en>
                // </lang>
                PortalModulePackage package;
                string reason;
                if (TryReadPackage(directory.Name, out package, out reason))
                {
                    packages.Add(package);
                }
            }

            // <lang>
            //   <zh-CN>重复 PackageId 会让状态表、审计和缓存身份失去唯一归属，因此整组包都不作为可信包返回。</zh-CN>
            //   <en>A duplicate PackageId makes state rows, audit records, and cache identity ambiguous, so no package in that duplicate group is returned as trusted.</en>
            // </lang>
            return packages
                // <lang>
                //   <zh-CN>对稳定标识按不区分大小写分组后只保留唯一组；排序仅稳定目录展示和调用方枚举，不改变包的受信任结论。</zh-CN>
                //   <en>After grouping stable identifiers case-insensitively, retain only unique groups; sorting only stabilizes catalog display and caller enumeration and does not change a package's trusted conclusion.</en>
                // </lang>
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
        /// <lang>
        ///   <zh-CN>从模块实例设置解析受控桌面入口、包状态和缓存身份。</zh-CN>
        ///   <en>Resolves controlled desktop entry, package state, and cache identity from module-instance settings.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleSettings"><l zh-CN="当前已读取的模块实例设置；本方法只消费其桌面源路径。" en="Current module-instance settings already read by the caller; this method consumes only its desktop-source path." /></param>
        /// <param name="context"><l zh-CN="可选 HTTP 上下文，仅传给后续 Profile/状态解析边界。" en="Optional HTTP context passed only to later profile/state-resolution boundaries." /></param>
        /// <param name="descriptor"><l zh-CN="可进入后续流程时返回受控描述；已禁用包也返回描述符。" en="Returns a controlled descriptor when it may enter later processing; disabled packages also return a descriptor." /></param>
        /// <param name="reason"><l zh-CN="解析失败或禁用时的受控非敏感原因。" en="Controlled non-sensitive reason when resolution fails or the package is disabled." /></param>
        /// <returns><l zh-CN="路径/Profile/部署边界通过时为 true；true 不等同于已加载或当前用户已获授权。" en="True when path, profile, and deployment boundaries pass; true does not mean loaded or authorized for the current user." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>这是运行时组合入口，不重做 manifest 校验、状态表实现或页面授权。它先规范化实例路径，再委托桌面入口解析，以保持页面加载、后台挂载与其它调用方的同一解析顺序。</zh-CN>
        ///   <en>This is the runtime-composition entry and does not reimplement manifest validation, state-table implementation, or page authorization. It normalizes the instance path first and then delegates to desktop-entry resolution so page loading, admin mounting, and other callers preserve the same resolution order.</en>
        /// </lang>
        /// </remarks>
        public static bool TryResolveModule(
            ModuleSettings moduleSettings,
            HttpContext context,
            out PortalModuleRuntimeDescriptor descriptor,
            out string reason)
        {
            descriptor = null;
            reason = string.Empty;

            // <lang>
            //   <zh-CN>缺少实例设置时没有可验证的原始入口，立即失败而不猜测默认控件、包或缓存身份。</zh-CN>
            //   <en>Without instance settings there is no raw entry to validate, so fail immediately rather than guessing a default control, package, or cache identity.</en>
            // </lang>
            if (moduleSettings == null)
            {
                reason = "Module settings are unavailable.";
                return false;
            }

            string source;
            try
            {
                // <lang>
                //   <zh-CN>定义表中的原始桌面路径不是加载许可；先由共享路径校验器规范化，才能进入 Core/包/Legacy 分支。</zh-CN>
                //   <en>The raw desktop path from the definition table is not loading permission; normalize it through the shared path validator before entering Core/package/Legacy branches.</en>
                // </lang>
                source = PortalModulePathValidator.NormalizeDesktopSourceOrThrow(moduleSettings.DesktopSrc);
            }
            catch (InvalidOperationException exception)
            {
                // <lang>
                //   <zh-CN>路径校验失败只以截断净化后的原因返回；不回显原始定义、物理路径或异常堆栈。</zh-CN>
                //   <en>A path-validation failure returns only a truncated sanitized reason; do not echo the raw definition, physical path, or exception stack.</en>
                // </lang>
                reason = PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.Message, 200);
                return false;
            }

            // <lang>
            //   <zh-CN>统一委托桌面入口解析，避免模块实例入口与后台候选入口使用不同的 Profile/状态顺序。</zh-CN>
            //   <en>Delegate uniformly to desktop-entry resolution so module-instance entries and admin candidate entries do not use different profile/state ordering.</en>
            // </lang>
            return TryResolveDesktopSource(source, context, out descriptor, out reason);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按桌面入口解析模块运行描述，并应用启动期 Profile gate。</zh-CN>
        ///   <en>Resolves a module runtime descriptor by desktop entry and applies the startup Profile gate.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法供页面加载和后台新增模块共同使用，避免“运行时禁止但后台仍可新增”的不一致。它依次组合路径、Core/Legacy/受信任部署分类、Profile 与状态事实；它不加载控件、不注入样式，也不实施页面授权。</zh-CN>
        ///   <en>This method is shared by page loading and admin module creation so the system does not allow creation of entries that runtime would later block. It composes path, Core/Legacy/validated-deployment classification, profile, and state facts in sequence; it does not load a control, inject styles, or enforce page authorization.</en>
        /// </lang>
        /// </remarks>
        /// <param name="desktopSource">
        /// <l zh-CN="旧定义表或模块实例中的桌面控件路径；必须先通过共享路径规范化。" en="Desktop control path from the legacy definition table or module instance; it must first pass shared path normalization." />
        /// </param>
        /// <param name="context">
        /// <l zh-CN="仅传给 Profile/状态解析边界的可选 HTTP 上下文；它不是授权结论。" en="Optional HTTP context passed only to profile/state-resolution boundaries; it is not an authorization conclusion." />
        /// </param>
        /// <param name="descriptor">
        /// <l zh-CN="路径与 Profile/部署边界通过时的受控运行描述；已禁用包仍返回描述。" en="Controlled runtime descriptor when path and profile/deployment boundaries pass; disabled packages still return a descriptor." />
        /// </param>
        /// <param name="reason">
        /// <l zh-CN="失败或禁用时的受控非敏感原因，不含物理路径、连接或异常细节。" en="Controlled non-sensitive reason when failed or disabled, with no physical path, connection, or exception detail." />
        /// </param>
        /// <returns>
        /// <l zh-CN="入口通过路径/Profile/部署边界时为 true；true 不表示已加载或已授权。" en="True when the entry passes path/profile/deployment boundaries; true does not mean loaded or authorized." />
        /// </returns>
        public static bool TryResolveDesktopSource(
            string desktopSource,
            HttpContext context,
            out PortalModuleRuntimeDescriptor descriptor,
            out string reason)
        {
            descriptor = null;
            reason = string.Empty;

            string source;
            try
            {
                // <lang>
                //   <zh-CN>即使调用方已经规范化路径，也在公共入口重新使用同一校验器，避免不同调用点以字符串表示差异绕过受控模块分支。</zh-CN>
                //   <en>Even when a caller has normalized the path, reuse the same validator at this public entry so representation differences across call sites cannot bypass controlled module branches.</en>
                // </lang>
                source = PortalModulePathValidator.NormalizeDesktopSourceOrThrow(desktopSource);
            }
            catch (InvalidOperationException exception)
            {
                // <lang>
                //   <zh-CN>失败原因经过净化和长度限制后才交给 UI/调用方；不把原始路径或异常细节变成可探测信息。</zh-CN>
                //   <en>Sanitize and length-limit the failure reason before it reaches UI/callers; do not turn raw paths or exception detail into probeable information.</en>
                // </lang>
                reason = PortalDiagnosticSanitizer.SanitizeAndTruncate(exception.Message, 200);
                return false;
            }

            // <lang>
            //   <zh-CN>Profile 快照是当前环境允许范围的输入；它不是用户权限，并且 Core 分支不会用它拒绝恢复性基础模块。</zh-CN>
            //   <en>The profile snapshot supplies the current environment's allowed scope; it is not user permission, and the Core branch does not use it to reject recoverability-critical base modules.</en>
            // </lang>
            PortalModuleProfileSnapshot profile = PortalModuleProfileResolver.Resolve(context);
            if (PortalModuleProfileResolver.IsCoreDesktopSource(source))
            {
                // <lang>
                //   <zh-CN>Core 路径在路径已规范化后获得硬保护：它不伪装为 manifest 包、不读取状态表，缓存身份只绑定受控入口。</zh-CN>
                //   <en>After path normalization, a Core path receives hard protection: it is not disguised as a manifest package, does not read the state table, and has a cache identity bound only to the controlled entry.</en>
                // </lang>
                descriptor = new PortalModuleRuntimeDescriptor(
                    source,
                    "Core",
                    false,
                    true,
                    true,
                    null,
                    "core|" + source.ToLowerInvariant());
                return true;
            }

            // <lang>
            //   <zh-CN>仅从已验证部署包集合按已规范化入口精确匹配；这里消费前批 manifest 信任事实，不重读或放宽文件校验。</zh-CN>
            //   <en>Match only from the validated deployment-package set by normalized entry; consume the earlier manifest-trust fact here without rereading or loosening file validation.</en>
            // </lang>
            PortalModulePackage package = GetTrustedPackages().FirstOrDefault(item =>
                string.Equals(item.DesktopEntry, source, StringComparison.OrdinalIgnoreCase));
            if (package == null)
            {
                if (HasManifestCandidate(source))
                {
                    // <lang>
                    //   <zh-CN>位于带 manifest 候选目录却未进入可信集合时必须拒绝，不能回退为 Legacy；否则无效 manifest 可绕过受信任部署边界。</zh-CN>
                    //   <en>When an entry lies in a candidate directory with a manifest but did not enter the trusted set, reject it rather than falling back to Legacy; otherwise an invalid manifest could bypass the trusted-deployment boundary.</en>
                    // </lang>
                    reason = "Module package manifest is invalid or does not declare the requested entry.";
                    return false;
                }

                string legacyPackageId;
                if (!PortalModuleProfileResolver.TryGetLegacyPackageId(source, out legacyPackageId))
                {
                    // <lang>
                    //   <zh-CN>Legacy 兼容只接受内置路径到虚拟包标识的固定映射；未映射路径不会因为不是 manifest 包就自动允许。</zh-CN>
                    //   <en>Legacy compatibility accepts only fixed mappings from built-in paths to virtual package identifiers; an unmapped path is not automatically allowed merely because it is not a manifest package.</en>
                    // </lang>
                    reason = PortalModuleProfileResolver.NotAllowedReasonPrefix +
                             " Legacy module path is not mapped to a package profile.";
                    return false;
                }

                if (!profile.IsPackageAllowed(legacyPackageId))
                {
                    // <lang>
                    //   <zh-CN>Legacy 虚拟包同样必须通过当前 Profile；这里仍不判定用户角色、权限或页面访问。</zh-CN>
                    //   <en>A Legacy virtual package must also pass the current profile; this still does not decide user role, permission, or page access.</en>
                    // </lang>
                    reason = PortalModuleProfileResolver.NotAllowedReasonPrefix +
                             " Package '" + legacyPackageId + "' is not allowed by active profile '" +
                             profile.ActiveProfile + "'.";
                    return false;
                }

                // <lang>
                //   <zh-CN>Legacy 经过路径和 Profile 允许后不依赖状态表；缓存身份区分虚拟包与规范入口，但不把它称为物理部署包。</zh-CN>
                //   <en>After passing path and profile gates, Legacy does not depend on the state table; its cache identity distinguishes virtual package and normalized entry without calling it a physical deployment package.</en>
                // </lang>
                descriptor = new PortalModuleRuntimeDescriptor(
                    source,
                    legacyPackageId,
                    false,
                    true,
                    true,
                    null,
                    "legacy|" + legacyPackageId.ToLowerInvariant() + "|" + source.ToLowerInvariant());
                return true;
            }

            if (!profile.IsPackageAllowed(package.PackageId))
            {
                // <lang>
                //   <zh-CN>已验证部署包先受 Profile 环境边界限制，再考虑状态表；显式启用状态不能越过未允许的 Profile。</zh-CN>
                //   <en>A validated deployment package is constrained by the profile environment boundary before considering the state table; an explicit enabled state cannot bypass a disallowed profile.</en>
                // </lang>
                reason = PortalModuleProfileResolver.NotAllowedReasonPrefix +
                         " Package '" + package.PackageId + "' is not allowed by active profile '" +
                         profile.ActiveProfile + "'.";
                return false;
            }

            // <lang>
            //   <zh-CN>仅在 Profile 已允许的受信任部署包上读取状态。读取不可用或无状态快照时按 ADR 既有兼容规则默认启用；这不改变 manifest/Profile 结论。</zh-CN>
            //   <en>Read state only for a profile-allowed validated deployment package. When the read is unavailable or lacks a snapshot, default enabled under the existing ADR compatibility rule; this does not change manifest or profile conclusions.</en>
            // </lang>
            PortalModulePackageStateReadResult stateResult = PortalModulePackageStates.Read(package.PackageId, context);
            bool isEnabled = stateResult.IsAvailable && stateResult.State != null
                ? stateResult.State.IsEnabled
                : true;

            // <lang>
            //   <zh-CN>缓存修订只采用可用显式状态行的 UTC tick；缺行与不可用均使用稳定 default，避免把失败详情或不受控时间进入缓存键。</zh-CN>
            //   <en>Cache revision uses UTC ticks only from an available explicit state row; both missing and unavailable states use stable default, preventing failure detail or uncontrolled time from entering the cache key.</en>
            // </lang>
            string stateRevision = stateResult.IsAvailable && stateResult.State != null && stateResult.State.IsConfigured
                ? stateResult.State.UpdatedUtc.Ticks.ToString()
                : "default";
            descriptor = new PortalModuleRuntimeDescriptor(
                source,
                package.PackageId,
                true,
                isEnabled,
                stateResult.IsAvailable,
                package,
                "package|" + package.PackageId.ToLowerInvariant() + "|" + package.Version + "|" + stateRevision + "|" + source.ToLowerInvariant());
            if (!isEnabled)
            {
                // <lang>
                //   <zh-CN>显式禁用不是解析失败：保留描述符与受控原因，使页面/样式消费方可以跳过后续加载并给出稳定诊断。</zh-CN>
                //   <en>Explicit disablement is not resolution failure: retain descriptor and controlled reason so page/style consumers can skip later loading and provide stable diagnostics.</en>
                // </lang>
                reason = "Module package is disabled.";
            }

            // <lang>
            //   <zh-CN>true 只表示解析链已产生受控描述；调用方仍须检查 IsEnabled 并在各自页面边界实施加载与授权。</zh-CN>
            //   <en>True means only that the resolution chain produced a controlled descriptor; callers still must check IsEnabled and enforce loading and authorization at their own page boundary.</en>
            // </lang>
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前前台门户 Tab 中已启用受信任模块包的 CSS 候选。</zh-CN>
        ///   <en>Gets CSS candidates for enabled trusted module packages in the current front-end portal Tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><l zh-CN="可选 HTTP 上下文；未提供时使用当前上下文，二者均不可用时返回空只读集合。" en="Optional HTTP context; the current context is used when omitted, and an empty read-only collection is returned when neither is available." /></param>
        /// <returns><l zh-CN="按活动 Tab 顺序保留首次出现项、大小写无关去重的站内 CSS 候选只读集合；后台请求、无活动 Tab 或解析期间受控异常可能返回空集合，受控异常发生前已累计的候选会被保留。" en="Read-only site-local CSS candidates that retain first occurrence in active-Tab order and are de-duplicated case-insensitively; administration requests, no active Tab, or a controlled resolution exception can return an empty collection, while candidates accumulated before a controlled exception are retained." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法消费已完成路径、Profile 和状态 gate 的运行描述符，只输出受信任且已启用包的 manifest CSS 资源候选。它不重新校验 manifest、不注入 link 标记、不加载控件、不实施导航或页面授权；实际 Head 注入由 Master Page 的独立边界负责。</zh-CN>
        ///   <en>This method consumes runtime descriptors that have already passed path, Profile, and state gates, and outputs candidates only for manifest CSS resources of trusted enabled packages. It does not revalidate manifests, inject link markup, load controls, enforce navigation, or enforce page authorization; actual Head injection belongs to the Master Page's separate boundary.</en>
        /// </lang>
        /// </remarks>
        public static IList<PortalModuleStyleResource> GetActiveStyleResources(HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>保持按 Tab 顺序的可变候选容器，直到所有受控过滤完成后才作为只读集合交给调用方。</zh-CN>
            //   <en>Keep a mutable candidate container in Tab order until all controlled filtering completes, then expose it to the caller as a read-only collection.</en>
            // </lang>
            var resources = new List<PortalModuleStyleResource>();

            // <lang>
            //   <zh-CN>优先使用显式上下文以支持受控调用；仅在其缺失时回退到当前 HTTP 上下文，不持久化请求对象。</zh-CN>
            //   <en>Prefer the explicit context for controlled callers; fall back to the current HTTP context only when it is absent, without retaining a request object.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;

            // <lang>
            //   <zh-CN>没有前台请求上下文或请求位于后台时不产生样式候选；后台排除只控制资源输出，不是后台页授权判定。</zh-CN>
            //   <en>Produce no style candidates when no front-end request context exists or the request is in administration; the administration exclusion controls resource output only and is not an authorization decision for the administration page.</en>
            // </lang>
            if (current == null || IsAdminRequest(current))
            {
                // <lang>
                //   <zh-CN>空集合仍以只读表面返回，避免调用方把无上下文或后台情况当作可修改的资源列表。</zh-CN>
                //   <en>Return the empty collection through the same read-only surface so callers do not treat the no-context or administration case as a mutable resource list.</en>
                // </lang>
                return resources.AsReadOnly();
            }

            try
            {
                // <lang>
                //   <zh-CN>门户设置提供当前活动 Tab 的配置模块顺序；该读取不替代每个模块后续的受控解析。</zh-CN>
                //   <en>Portal settings provide the configured module order for the current active Tab; this lookup does not replace controlled resolution of each module below.</en>
                // </lang>
                PortalSettings settings = PortalContext.GetPortalSettings(current);

                // <lang>
                //   <zh-CN>未解析到门户设置或活动 Tab 时没有可消费的模块集合，因此保持无副作用的空输出。</zh-CN>
                //   <en>When portal settings or the active Tab cannot be resolved, there is no module set to consume, so retain an empty output without side effects.</en>
                // </lang>
                if (settings == null || settings.ActiveTab == null)
                {
                    return resources.AsReadOnly();
                }

                // <lang>
                //   <zh-CN>路径集合按大小写无关方式记录已输出的站内 CSS 路径，避免同一资源因重复模块配置或大小写差异被重复注入。</zh-CN>
                //   <en>The path set records emitted site-local CSS paths case-insensitively, preventing duplicate injection when module configuration repeats a resource or differs only by casing.</en>
                // </lang>
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // <lang>
                //   <zh-CN>按照活动 Tab 已配置的模块顺序消费描述符；第一个合格路径决定最终输出中的稳定相对顺序。</zh-CN>
                //   <en>Consume descriptors in the active Tab's configured module order; the first qualifying path determines the stable relative order in the final output.</en>
                // </lang>
                foreach (ModuleSettings module in settings.ActiveTab.Modules)
                {
                    // <lang>
                    //   <zh-CN>描述符承载已受控解析的包、Profile 和状态事实；此消费方不自行拼接或猜测模块来源。</zh-CN>
                    //   <en>The descriptor carries controlled package, Profile, and state facts; this consumer does not compose or guess a module source itself.</en>
                    // </lang>
                    PortalModuleRuntimeDescriptor descriptor;

                    // <lang>
                    //   <zh-CN>解析原因仅满足共享解析 API 的输出契约；样式候选不向页面泄露该诊断文本。</zh-CN>
                    //   <en>The resolution reason only satisfies the shared resolver API's output contract; style candidates do not disclose that diagnostic text to the page.</en>
                    // </lang>
                    string reason;

                    // <lang>
                    //   <zh-CN>仅消费成功解析、属于受管理部署包且显式启用的描述符；Core/Legacy、未解析或禁用模块都不获得此模块包 CSS 输出。</zh-CN>
                    //   <en>Consume only descriptors that resolve successfully, belong to a managed deployment package, and are explicitly enabled; Core, Legacy, unresolved, and disabled modules receive no module-package CSS output here.</en>
                    // </lang>
                    if (!TryResolveModule(module, current, out descriptor, out reason) ||
                        !descriptor.IsManagedPackage || !descriptor.IsEnabled)
                    {
                        continue;
                    }

                    // <lang>
                    //   <zh-CN>资源集合已在 manifest 信任边界验证为包内声明项；本层只选择 CSS 后缀，不接受脚本或外部资源。</zh-CN>
                    //   <en>The resource collection was validated as declared in-package items at the manifest trust boundary; this layer selects only the CSS suffix and accepts neither scripts nor external resources.</en>
                    // </lang>
                    foreach (string resource in descriptor.Package.Resources.Where(item =>
                        item.EndsWith(".css", StringComparison.OrdinalIgnoreCase)))
                    {
                        // <lang>
                        //   <zh-CN>从受信任目录名和已验证资源相对路径组成应用内虚拟路径；不把请求输入拼入路径，也不形成外部 URL。</zh-CN>
                        //   <en>Compose an application-local virtual path from the trusted directory name and validated resource-relative path; do not concatenate request input or form an external URL.</en>
                        // </lang>
                        string virtualPath = "~/DesktopModules/" + descriptor.Package.DirectoryName + "/" + resource;

                        // <lang>
                        //   <zh-CN>仅在该路径尚未输出时创建 DTO，借此保留首次出现的包归属和活动 Tab 顺序。</zh-CN>
                        //   <en>Create the DTO only when this path has not yet been output, preserving the first occurrence's package ownership and active-Tab order.</en>
                        // </lang>
                        if (paths.Add(virtualPath))
                        {
                            resources.Add(new PortalModuleStyleResource(descriptor.Package.PackageId, virtualPath));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>将解析故障限制在诊断通道而不向页面抛出内部详情；已在异常前累计的候选保持可用，调用方仍只会看到只读集合。</zh-CN>
                //   <en>Confine a resolution failure to the diagnostic channel without throwing internal detail to the page; candidates accumulated before the exception remain available and the caller still sees only a read-only collection.</en>
                // </lang>
                PortalDiagnostics.Error("ModulePackage.Styles", "Resolving active module package styles failed.", exception, current);
            }

            // <lang>
            //   <zh-CN>封存最终候选以避免页面层在注入前修改 catalog 已完成的过滤和去重结果。</zh-CN>
            //   <en>Seal the final candidates so the page layer cannot modify the catalog's completed filtering and de-duplication result before injection.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>按目录名读取并验证一个受信任部署模块包。</zh-CN>
        ///   <en>Reads and validates one trusted-deployment module package by directory name.</en>
        /// </lang>
        /// </summary>
        /// <param name="directoryName"><l zh-CN="`DesktopModules` 直接子目录的候选名称；必须先通过受控目录名规则。" en="Candidate name of an immediate `DesktopModules` child directory; it must first pass the controlled directory-name rule." /></param>
        /// <param name="package"><l zh-CN="成功时返回不可变受信任包快照；失败时保持 null。" en="Returns an immutable trusted-package snapshot when successful; it remains null on failure." /></param>
        /// <param name="reason"><l zh-CN="不含物理路径、原始 manifest 或异常详情的受控失败原因。" en="Controlled failure reason that contains no physical path, raw manifest, or exception detail." /></param>
        /// <returns><l zh-CN="目录、manifest、入口和资源均通过当前受控校验时为 true。" en="True when the directory, manifest, entry, and resources all pass current controlled validation." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该私有读取链只验证既有部署内容，不安装、修复或修改任何文件；成功也不代替 Profile、状态表、路由或页面授权。</zh-CN>
        ///   <en>This private read chain validates existing deployment content only and never installs, repairs, or modifies files; success also does not replace profile, state-table, routing, or page-authorization gates.</en>
        /// </lang>
        /// </remarks>
        private static bool TryReadPackage(string directoryName, out PortalModulePackage package, out string reason)
        {
            package = null;
            reason = string.Empty;

            // <lang>
            //   <zh-CN>目录名在任何文件系统组合前必须满足受控规则；拒绝的名称不应触发根外探测、异常文本回显或路径修复尝试。</zh-CN>
            //   <en>The directory name must satisfy the controlled rule before any filesystem combination; a rejected name must not trigger outside-root probing, exception-text echoing, or path-repair attempts.</en>
            // </lang>
            if (!DirectoryNamePattern.IsMatch(directoryName ?? string.Empty))
            {
                reason = "Module package directory name is invalid.";
                return false;
            }

            // <lang>
            //   <zh-CN>再次从固定应用根解析物理位置，避免调用方传入的目录名隐式绑定到当前工作目录或任意文件系统根。</zh-CN>
            //   <en>Resolve the physical location again from the fixed application root so a caller-supplied directory name cannot implicitly bind to the current working directory or an arbitrary filesystem root.</en>
            // </lang>
            string rootPath = HostingEnvironment.MapPath(ModuleRootVirtualPath);
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                reason = "Module root is unavailable.";
                return false;
            }

            string packagePath;
            try
            {
                // <lang>
                //   <zh-CN>根内路径 helper 规范化并检查候选仍在根下；目录名正则是第一层，完整路径比较是防御性第二层。</zh-CN>
                //   <en>The in-root path helper normalizes and checks that the candidate remains below the root; the directory-name regex is the first layer and the full-path comparison is the defensive second layer.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>目录或固定 manifest 缺失时不把候选降级为 Legacy，也不依据其它文件推断可信元数据。</zh-CN>
                //   <en>When the directory or fixed manifest is missing, do not downgrade this candidate to Legacy or infer trusted metadata from other files.</en>
                // </lang>
                reason = "module.json is missing.";
                return false;
            }

            try
            {
                // <lang>
                //   <zh-CN>只按 UTF-8 读取固定 manifest 文件并解析为 JSON 对象；解析后仍须通过 schema、字段、路径和禁止能力校验，JSON 可解析本身不足以建立信任。</zh-CN>
                //   <en>Read only the fixed manifest file as UTF-8 and parse it as a JSON object; parsed JSON still must pass schema, field, path, and prohibited-capability validation, because parseability alone does not establish trust.</en>
                // </lang>
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

                // <lang>
                //   <zh-CN>稳定包标识、入口路径和包目录三者必须一致：路径校验器先消除表示差异，再要求入口仍位于当前包目录，不能借合格 ID 访问其它包或根外控件。</zh-CN>
                //   <en>Stable package identifier, entry path, and package directory must agree: the path validator first removes representation differences and then the entry must remain in the current package directory, so a valid ID cannot reach another package or an outside-root control.</en>
                // </lang>
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
                    // <lang>
                    //   <zh-CN>通过文本路径校验仍不足够；入口文件必须实际存在于已验证包根内，避免把陈旧 manifest 或空占位当作可加载控件。</zh-CN>
                    //   <en>Passing textual path validation is still insufficient; the entry file must actually exist beneath the validated package root so stale manifests or empty placeholders are not treated as loadable controls.</en>
                    // </lang>
                    reason = "Module manifest desktopEntry does not exist.";
                    return false;
                }

                // <lang>
                //   <zh-CN>当前部署模型只允许受控 ASCX 入口和站内静态资源；显式拒绝脚本、外部 URL、程序集和包 URL，不能因 manifest 声明而改变执行或下载能力。</zh-CN>
                //   <en>The current deployment model permits only controlled ASCX entries and site-local static resources; explicitly reject scripts, external URLs, assemblies, and package URLs because a manifest declaration cannot change execution or download capability.</en>
                // </lang>
                if (manifest["script"] != null || manifest["scripts"] != null ||
                    manifest["externalUrl"] != null || manifest["externalUrls"] != null ||
                    manifest["assembly"] != null || manifest["assemblies"] != null || manifest["packageUrl"] != null)
                {
                    reason = "Module manifest declares a prohibited script, external URL, or assembly.";
                    return false;
                }

                // <lang>
                //   <zh-CN>资源列表先完整验证后才形成不可变包快照；单个越界、缺失或禁止扩展资源会拒绝整包，而不是悄悄丢弃声明。</zh-CN>
                //   <en>Validate the resource list completely before forming the immutable package snapshot; one escaping, missing, or prohibited-extension resource rejects the whole package instead of silently dropping the declaration.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>预期的 I/O、权限、JSON 或路径/字段校验失败统一收敛为受控原因；不暴露物理路径、manifest 内容或异常细节，且不把失败包加入可信集合。</zh-CN>
                //   <en>Expected I/O, authorization, JSON, or path/field-validation failures converge to a controlled reason; do not expose physical paths, manifest content, or exception detail, and never add the failed package to the trusted set.</en>
                // </lang>
                reason = "Module manifest is invalid.";
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证模块 manifest schemaVersion 是否为当前支持版本。</zh-CN>
        ///   <en>Validates whether the module manifest schemaVersion is currently supported.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest"><l zh-CN="已解析但尚未被信任的 manifest 对象。" en="Parsed but not yet trusted manifest object." /></param>
        /// <returns><l zh-CN="仅当 schemaVersion 是当前受控整数版本时为 true。" en="True only when schemaVersion is the current controlled integer version." /></returns>
        private static bool IsSchemaVersionSupported(JObject manifest)
        {
            // <lang>
            //   <zh-CN>版本字段必须存在、是 JSON 整数且精确等于当前版本；缺失、字符串数值或未来版本均不能通过宽松兼容解释。</zh-CN>
            //   <en>The version field must exist, be a JSON integer, and equal the current version exactly; missing values, numeric strings, and future versions cannot pass through permissive compatibility interpretation.</en>
            // </lang>
            JToken token = manifest["schemaVersion"];
            return token != null && token.Type == JTokenType.Integer && token.Value<int>() == ManifestSchemaVersion;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取必填字符串 manifest 属性。</zh-CN>
        ///   <en>Reads a required string manifest property.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest"><l zh-CN="已解析但尚未被信任的 manifest 对象。" en="Parsed but not yet trusted manifest object." /></param>
        /// <param name="propertyName"><l zh-CN="代码定义的固定属性名，不来自请求或 manifest 值。" en="Code-defined fixed property name, not a request or manifest value." /></param>
        /// <param name="maximumLength"><l zh-CN="接受前的最大字符长度。" en="Maximum character length before acceptance." /></param>
        /// <returns><l zh-CN="修剪后的非空字符串。" en="Trimmed non-empty string." /></returns>
        /// <exception cref="InvalidOperationException"><l zh-CN="属性缺失、为 null、非字符串、超长或修剪后为空时引发，由受控包读取回退处理。" en="Raised when the property is missing, null, non-string, overlong, or empty after trimming; the controlled package-read fallback handles it." /></exception>
        private static string ReadRequiredString(JObject manifest, string propertyName, int maximumLength)
        {
            // <lang>
            //   <zh-CN>复用可选字段的类型、修剪和长度规则，再把空白提升为必填契约失败；不以默认值掩盖部署 manifest 缺字段。</zh-CN>
            //   <en>Reuse optional-field type, trimming, and length rules, then elevate blank text to a required-contract failure; do not hide a missing deployment-manifest field with a default value.</en>
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
        ///   <zh-CN>读取可选字符串 manifest 属性，并执行类型和长度检查。</zh-CN>
        ///   <en>Reads an optional string manifest property with type and length checks.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest"><l zh-CN="已解析但尚未被信任的 manifest 对象。" en="Parsed but not yet trusted manifest object." /></param>
        /// <param name="propertyName"><l zh-CN="代码定义的固定属性名，不来自请求或 manifest 值。" en="Code-defined fixed property name, not a request or manifest value." /></param>
        /// <param name="maximumLength"><l zh-CN="接受前的最大字符长度。" en="Maximum character length before acceptance." /></param>
        /// <returns><l zh-CN="缺失/null 时为稳定空文本，否则为通过类型、修剪和长度检查的文本。" en="Stable empty text when missing/null; otherwise text that passed type, trimming, and length checks." /></returns>
        /// <exception cref="InvalidOperationException"><l zh-CN="属性不是字符串或超过固定长度时引发，由受控包读取回退处理。" en="Raised when the property is not a string or exceeds the fixed length; the controlled package-read fallback handles it." /></exception>
        private static string ReadOptionalString(JObject manifest, string propertyName, int maximumLength)
        {
            // <lang>
            //   <zh-CN>缺失与 JSON null 是唯一允许的空值形态；对象、数组、数值和布尔值不会被字符串化，以避免歧义 manifest 表示进入后续路径或版本判断。</zh-CN>
            //   <en>Missing and JSON null are the only permitted empty forms; objects, arrays, numbers, and Booleans are not stringified, preventing ambiguous manifest representations from reaching later path or version decisions.</en>
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
            //   <zh-CN>先修剪再按字符数限制，确保目录展示和后续受控比较使用稳定文本，同时不对值进行 URL、路径或版本语义解释。</zh-CN>
            //   <en>Trim before applying the character limit so catalog display and later controlled comparison use stable text; this does not interpret URL, path, or version semantics in the value.</en>
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
        ///   <zh-CN>读取并验证模块包声明的站内静态资源。</zh-CN>
        ///   <en>Reads and validates site-local static resources declared by a module package.</en>
        /// </lang>
        /// </summary>
        /// <param name="manifest"><l zh-CN="已通过前置 schema/字段检查、但资源声明仍待验证的 manifest。" en="Manifest that passed prior schema/field checks but whose resource declarations still require validation." /></param>
        /// <param name="packagePath"><l zh-CN="已验证包目录的物理根路径。" en="Physical root path of the validated package directory." /></param>
        /// <returns><l zh-CN="保持 manifest 顺序的只读包内资源相对路径列表。" en="Read-only list of package-relative resource paths preserving manifest order." /></returns>
        /// <exception cref="InvalidOperationException"><l zh-CN="资源数组、条目、路径、物理文件或扩展名不满足当前部署契约时引发。" en="Raised when the resource array, item, path, physical file, or extension violates the current deployment contract." /></exception>
        private static IList<string> ReadAndValidateResources(JObject manifest, string packagePath)
        {
            // <lang>
            //   <zh-CN>资源必须显式为 JSON 数组；缺失不等同于空数组，避免未声明资源边界的包被隐式接受。</zh-CN>
            //   <en>Resources must be an explicit JSON array; absence is not equivalent to an empty array, avoiding implicit acceptance of a package with no declared resource boundary.</en>
            // </lang>
            JArray resources = manifest["resources"] as JArray;
            if (resources == null)
            {
                throw new InvalidOperationException("Module resources are missing.");
            }

            var validatedResources = new List<string>();
            foreach (JToken token in resources)
            {
                // <lang>
                //   <zh-CN>每项必须是文本并标准化为正斜杠相对路径；不允许把 JSON 其它类型或平台分隔符的歧义直接带入物理路径组合。</zh-CN>
                //   <en>Each item must be text and normalizes to a forward-slash relative path; do not carry other JSON types or platform-separator ambiguity directly into physical-path combination.</en>
                // </lang>
                if (token.Type != JTokenType.String)
                {
                    throw new InvalidOperationException("Module resource is invalid.");
                }

                string resource = token.Value<string>().Trim().Replace('\\', '/');
                if (!IsValidResourcePath(resource))
                {
                    throw new InvalidOperationException("Module resource path is not allowed.");
                }

                // <lang>
                //   <zh-CN>路径文本通过后仍在包根内解析并要求文件存在且扩展名受限；既不下载外部资源，也不接受可执行或任意静态文件类型。</zh-CN>
                //   <en>After the path text passes, resolve it under the package root and require an existing file with an allowed extension; never download an external resource or accept executable or arbitrary static file types.</en>
                // </lang>
                string physicalPath = GetChildPath(packagePath, resource.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(physicalPath) || !IsAllowedResourceExtension(resource))
                {
                    throw new InvalidOperationException("Module resource is unavailable or uses a prohibited extension.");
                }

                validatedResources.Add(resource);
            }

            return validatedResources.AsReadOnly();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证资源路径为包内相对路径，且不包含外部 URL 或目录逃逸。</zh-CN>
        ///   <en>Validates that a resource path is package-relative and contains no external URL or directory escape.</en>
        /// </lang>
        /// </summary>
        /// <param name="resource"><l zh-CN="已修剪并以正斜杠表示的 manifest 资源相对路径。" en="Trimmed manifest resource-relative path expressed with forward slashes." /></param>
        /// <returns><l zh-CN="路径是非空包内相对路径且不包含 URL 或点段逃逸时为 true。" en="True when the path is a non-empty package-relative path without a URL or dot-segment escape." /></returns>
        private static bool IsValidResourcePath(string resource)
        {
            // <lang>
            //   <zh-CN>先拒绝绝对路径、协议和网络路径前缀；这些形式即使最终映射到本机也不属于 manifest 可声明的包内资源。</zh-CN>
            //   <en>First reject absolute paths, protocol prefixes, and network-path prefixes; even if they ultimately map locally, these forms are not package-local resources that a manifest may declare.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(resource) || resource.StartsWith("/", StringComparison.Ordinal) ||
                resource.IndexOf("://", StringComparison.Ordinal) >= 0 || resource.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>逐段拒绝空段、当前目录和上级目录，防止混合分隔符归一化后出现目录逃逸或不稳定同义路径。</zh-CN>
            //   <en>Reject empty, current-directory, and parent-directory segments one by one to prevent directory escape or unstable equivalent paths after mixed-separator normalization.</en>
            // </lang>
            string[] segments = resource.Split('/');
            return segments.All(segment => !string.IsNullOrWhiteSpace(segment) && segment != "." && segment != "..");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断资源扩展名是否属于当前模块包允许的 CSS 或图片类型。</zh-CN>
        ///   <en>Determines whether the resource extension is an allowed CSS or image type for module packages.</en>
        /// </lang>
        /// </summary>
        /// <param name="resource"><l zh-CN="已验证为包内相对路径的资源文本。" en="Resource text already validated as package-relative." /></param>
        /// <returns><l zh-CN="扩展名属于当前 CSS 或图片 allowlist 时为 true。" en="True when the extension belongs to the current CSS or image allowlist." /></returns>
        private static bool IsAllowedResourceExtension(string resource)
        {
            // <lang>
            //   <zh-CN>allowlist 是当前静态资源能力边界，不按 MIME 猜测、不接受无扩展名，也不把新扩展名作为向后兼容自动放行。</zh-CN>
            //   <en>The allowlist is the current static-resource capability boundary: do not infer from MIME, accept extensionless files, or automatically allow new extensions as backward compatibility.</en>
            // </lang>
            string extension = Path.GetExtension(resource);
            return string.Equals(extension, ".css", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断一个旧模块路径是否位于带 manifest 的候选包目录下。</zh-CN>
        ///   <en>Determines whether a legacy module path sits under a candidate package directory with a manifest.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求是否为后台页面，以避免后台维护页加载前台模块样式。</zh-CN>
        ///   <en>Determines whether the current request is an administration page so front-end module styles are not loaded there.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><l zh-CN="已由样式候选入口确认非空的当前 HTTP 上下文。" en="Current HTTP context already confirmed non-null by the style-candidate entry point." /></param>
        /// <returns><l zh-CN="当应用相对执行路径以 ~/Admin/ 开头时为 true；该结果仅控制样式候选输出，不授予或拒绝后台访问。" en="True when the app-relative execution path starts with ~/Admin/; the result controls style-candidate output only and neither grants nor denies administration access." /></returns>
        private static bool IsAdminRequest(HttpContext context)
        {
            // <lang>
            //   <zh-CN>仅提取当前执行文件的应用相对路径；Request 不存在时使用空字符串，使该轻量边界保持为前台样式输出判定而非路由或授权解析。</zh-CN>
            //   <en>Read only the app-relative path of the current execution file; use an empty string when Request is absent so this lightweight boundary remains a front-end style-output decision rather than routing or authorization parsing.</en>
            // </lang>
            string path = context.Request == null ? string.Empty : context.Request.AppRelativeCurrentExecutionFilePath;

            // <lang>
            //   <zh-CN>以大小写无关的固定应用路径前缀排除后台页面；不规范化、重写或验证 URL，保留宿主既有请求路径语义。</zh-CN>
            //   <en>Exclude administration pages using a case-insensitive fixed app-path prefix; do not normalize, rewrite, or validate the URL, preserving the host's existing request-path semantics.</en>
            // </lang>
            return path.StartsWith("~/Admin/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在根目录内解析子路径，并拒绝目录逃逸。</zh-CN>
        ///   <en>Resolves a child path inside a root directory and rejects directory escape.</en>
        /// </lang>
        /// </summary>
        /// <param name="rootPath"><l zh-CN="经调用方选择的受控物理根路径。" en="Controlled physical root path selected by the caller." /></param>
        /// <param name="childPath"><l zh-CN="待解析的受控子路径；调用方仍须在此 helper 前执行自己的格式规则。" en="Controlled child path to resolve; callers still must apply their own format rules before this helper." /></param>
        /// <returns><l zh-CN="规范化后仍位于根目录下的物理子路径。" en="Normalized physical child path that remains below the root directory." /></returns>
        /// <exception cref="InvalidOperationException"><l zh-CN="组合并规范化后路径不在根目录下时引发。" en="Raised when the combined and normalized path is not below the root directory." /></exception>
        private static string GetChildPath(string rootPath, string childPath)
        {
            // <lang>
            //   <zh-CN>两端先用完整路径规范化，再以带分隔符的根前缀比较，避免仅比较文本前缀时把相邻目录误判为根内。</zh-CN>
            //   <en>Normalize both sides to full paths and compare with a separator-terminated root prefix, avoiding a plain textual-prefix check that could mistake a sibling directory for one inside the root.</en>
            // </lang>
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
