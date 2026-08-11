using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using ASPNET.StarterKit.Portal.Util;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 主题解析结果的来源层级。
    /// Source layer of a resolved theme result.
    /// </summary>
    public enum PortalThemeSource
    {
        /// <summary>
        /// 内置 Default 回退。
        /// Built-in Default fallback.
        /// </summary>
        Default,

        /// <summary>
        /// 部署级 appSettings。
        /// Deployment-level appSettings.
        /// </summary>
        AppSettings,

        /// <summary>
        /// 数据库运行级覆盖。
        /// Database runtime override.
        /// </summary>
        Database,

        /// <summary>
        /// 当前门户 Tab 覆盖。
        /// Current portal Tab override.
        /// </summary>
        TabOverride,

        /// <summary>
        /// 非法或不可用主题触发的安全回退。
        /// Safe fallback caused by an invalid or unavailable theme.
        /// </summary>
        Fallback
    }

    /// <summary>
    /// 当前请求的最终主题及 CSS 作用域上下文。
    /// Final theme and CSS-scope context for the current request.
    /// </summary>
    public sealed class PortalThemeContext
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建当前请求的主题上下文。</zh-CN>
        ///   <en>Creates the theme context for the current request.</en>
        /// </lang>
        /// </summary>
        internal PortalThemeContext(string themeName, PortalThemeSource source, int? tabId, string fallbackReason)
        {
            // <lang>
            //   <zh-CN>将主题名空值归一为安全 Default，保证 Web Forms 始终接收单一可用基底主题。</zh-CN>
            //   <en>Normalize a null theme name to safe Default so Web Forms always receives one usable base theme.</en>
            // </lang>
            ThemeName = themeName ?? PortalThemeResolver.DefaultThemeName;

            // <lang>
            //   <zh-CN>保存最终来源层级，供健康检查和受限诊断解释回退但不暴露配置原文。</zh-CN>
            //   <en>Store the final source layer so health checks and restricted diagnostics can explain fallback without exposing raw configuration.</en>
            // </lang>
            Source = source;

            // <lang>
            //   <zh-CN>保存可选 Tab 标识；后台、错误或无门户上下文请求保持 null。</zh-CN>
            //   <en>Store the optional tab identifier; administration, error, or no-portal-context requests remain null.</en>
            // </lang>
            TabId = tabId;

            // <lang>
            //   <zh-CN>将回退原因空值归一为空字符串，且该字段只容纳非敏感说明。</zh-CN>
            //   <en>Normalize a null fallback reason to empty; this field holds non-sensitive explanation only.</en>
            // </lang>
            FallbackReason = fallbackReason ?? string.Empty;
        }

        /// <summary>
        /// 已应用到唯一 Web Forms Page.Theme 的主题名。
        /// Theme name applied to the single Web Forms Page.Theme.
        /// </summary>
        public string ThemeName { get; private set; }

        /// <summary>
        /// 最终主题来源。
        /// Source of the final theme.
        /// </summary>
        public PortalThemeSource Source { get; private set; }

        /// <summary>
        /// 门户页面关联的 Tab；Admin 和错误页为 null。
        /// Tab associated with a portal page; null for Admin and error pages.
        /// </summary>
        public int? TabId { get; private set; }

        /// <summary>
        /// 发生安全回退时的非敏感原因。
        /// Non-sensitive reason when a safe fallback occurred.
        /// </summary>
        /// <remarks>
        /// 此字段描述影响最终全局主题选择的回退。无效 Tab 覆盖会告警并继续使用已解析的全局主题，当前不会在此字段
        /// 追加候选链细节。
        /// This field describes a fallback affecting the final global theme selection. An invalid Tab override warns and
        /// continues with the resolved global theme; the current implementation does not append candidate-chain detail here.
        /// </remarks>
        public string FallbackReason { get; private set; }
    }

    /// <summary>
    /// 解析并应用门户 Web Forms 主题。
    /// Resolves and applies the portal Web Forms theme.
    /// </summary>
    /// <remarks>
        /// 原生 Theme 只承担每页唯一基底；Tab 和模块差异通过稳定 CSS scope 表达。
        /// 主题值必须对应已部署且通过 manifest 校验的可信包，不能由查询字符串、远程 URL 或脚本决定。
        /// 解析优先级保持 Tab 覆盖、数据库运行设置、appSettings、Default；结果只在当前 HttpContext.Items 中缓存，
        /// 不构成跨请求主题缓存。
        /// Native Theme provides the one base theme per page only; tab and module variations use stable CSS
        /// scopes. Theme values must resolve to a deployed package that passes manifest validation and cannot be
        /// selected by query strings, remote URLs, or scripts. Resolution priority remains Tab override, database runtime
        /// setting, appSettings, and Default. Results are cached only in the current HttpContext.Items, not across requests.
    /// </remarks>
    public static class PortalThemeResolver
    {
        /// <summary>
        /// 用于配置当前门户主题的稳定设置键。
        /// Stable setting key used to configure the current portal theme.
        /// </summary>
        public const string ThemeNameSettingKey = PortalSettingKeys.ThemeName;

        /// <summary>
        /// 配置无效、包缺失或错误恢复时使用的安全回退主题。
        /// Safe fallback theme used for invalid configuration, missing packages, or error recovery.
        /// </summary>
        public const string DefaultThemeName = "Default";

        /// <summary>
        /// <lang>
        ///   <zh-CN>主题回退的受限诊断分类，不包含用户、路径或配置原文。</zh-CN>
        ///   <en>Restricted diagnostic category for theme fallback, without users, paths, or raw configuration.</en>
        /// </lang>
        /// </summary>
        private const string TraceCategory = "PortalTheme";

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求 Items 中保存主题上下文的稳定键，不构成跨请求缓存。</zh-CN>
        ///   <en>Stable key for the theme context in current-request Items; it does not form a cross-request cache.</en>
        /// </lang>
        /// </summary>
        private const string ThemeContextKey = "PortalThemeContext";

        /// <summary>
        /// <lang>
        ///   <zh-CN>保护进程内回退告警去重集合的同步锁。</zh-CN>
        ///   <en>Synchronization lock protecting the process-local fallback-warning de-duplication set.</en>
        /// </lang>
        /// </summary>
        private static readonly object TraceLock = new object();

        /// <summary>
        /// <lang>
        ///   <zh-CN>已记录的受限回退键集合，避免同一主题/原因重复污染诊断。</zh-CN>
        ///   <en>Set of recorded restricted fallback keys, preventing repeated diagnostics for the same theme and reason.</en>
        /// </lang>
        /// </summary>
        private static readonly HashSet<string> WarnedFallbacks = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 在页面 PreInit 阶段解析并应用唯一原生 Theme。
        /// Resolves and applies the single native Theme during page PreInit.
        /// </summary>
        /// <param name="page">接收主题的 Web Forms 页面。Web Forms page receiving the theme.</param>
        /// <returns>本请求的完整主题上下文。Complete theme context for this request.</returns>
        /// <exception cref="ArgumentNullException">页面实例为 null 时抛出。
        /// Thrown when the page instance is null.</exception>
        /// <remarks>
        /// 调用方应在 <c>PreInit</c> 调用本方法，使 Web Forms 在加载 App_Themes 资源前得到主题。它不会重新排序
        /// 页面依赖注入，也不会从 URL 接受主题覆盖。
        /// Callers should invoke this method during <c>PreInit</c> so Web Forms receives the theme before loading
        /// App_Themes resources. It neither reorders page dependency injection nor accepts theme overrides from a URL.
        /// </remarks>
        public static PortalThemeContext ApplyTheme(Page page)
        {
            if (page == null)
            {
                throw new ArgumentNullException("page");
            }

            // <lang>
            //   <zh-CN>解析当前请求主题上下文；解析只依赖既有受控来源，不接受 URL 或脚本输入。</zh-CN>
            //   <en>Resolve the current-request theme context from existing controlled sources only, never from URL or script input.</en>
            // </lang>
            PortalThemeContext themeContext = ResolveThemeContext(HttpContext.Current);
            page.Theme = themeContext.ThemeName;
            return themeContext;
        }

        /// <summary>
        /// 解析当前请求的主题上下文并在 HttpContext 中缓存结果。
        /// Resolves the current request theme context and caches the result in HttpContext.
        /// </summary>
        /// <param name="context">当前 HTTP 上下文。Current HTTP context.</param>
        /// <returns>最终主题与 CSS scope 信息。Final theme and CSS-scope information.</returns>
        /// <remarks>
        /// Admin 请求不读取 Tab 覆盖。覆盖表不可用时保留已解析全局主题；无效覆盖也不阻断请求，而是记录受限告警。
        /// Admin requests do not read Tab overrides. When the override table is unavailable, the resolved global theme
        /// remains; an invalid override also does not block the request and records a restricted warning.
        /// </remarks>
        public static PortalThemeContext ResolveThemeContext(HttpContext context)
        {
            // <lang>
            //   <zh-CN>读取本请求已缓存的主题上下文，避免同一请求在渲染过程中重复访问设置存储。</zh-CN>
            //   <en>Read the theme context already cached for this request to avoid repeated settings-store access during rendering.</en>
            // </lang>
            PortalThemeContext existing = GetCurrentThemeContext(context);
            if (existing != null)
            {
                return existing;
            }

            // <lang>
            //   <zh-CN>读取全局主题的受控运行期设置值及来源层级，后续仍需校验部署包可信度。</zh-CN>
            //   <en>Read the controlled runtime global-theme value and source layer; deployed-package trust still requires later validation.</en>
            // </lang>
            PortalRuntimeSettingValue globalSetting =
                PortalRuntimeSettings.GetEffectiveValue(PortalSettingsRegistry.ThemeName, context);

            // <lang>
            //   <zh-CN>保存全局候选校验失败的非敏感原因；成功时保持空字符串。</zh-CN>
            //   <en>Hold the non-sensitive reason when global-candidate validation fails; retain empty on success.</en>
            // </lang>
            string fallbackReason = string.Empty;

            // <lang>
            //   <zh-CN>将全局候选解析为可信部署主题，失败时得到 Default 而非未验证目录名。</zh-CN>
            //   <en>Resolve the global candidate to a trusted deployed theme, producing Default on failure rather than an unverified directory name.</en>
            // </lang>
            string themeName = ResolveTrustedThemeName(globalSetting.Value, context, out fallbackReason);

            // <lang>
            //   <zh-CN>将运行期设置来源映射为展示/健康检查使用的稳定主题来源。</zh-CN>
            //   <en>Map the runtime-setting source to the stable theme source used by display and health checks.</en>
            // </lang>
            PortalThemeSource source = ToThemeSource(globalSetting.Source, !string.IsNullOrEmpty(fallbackReason));

            // <lang>
            //   <zh-CN>初始化可选 Tab 标识；后台请求和无法建立门户上下文的请求保持 null。</zh-CN>
            //   <en>Initialize the optional tab identifier; administration requests and requests without portal context remain null.</en>
            // </lang>
            int? tabId = null;

            if (!IsAdminRequest(context))
            {
                // <lang>
                //   <zh-CN>仅门户请求尝试读取活动 Tab，避免后台页因覆盖表或 Tab 上下文出现额外依赖。</zh-CN>
                //   <en>Attempt to read the active tab only for portal requests, avoiding extra override-table or tab-context dependencies for administration pages.</en>
                // </lang>
                tabId = TryGetActiveTabId(context);
                if (tabId.HasValue)
                {
                    // <lang>
                    //   <zh-CN>读取当前 Tab 的覆盖状态；表不可用只触发告警并保持已解析全局主题。</zh-CN>
                    //   <en>Read current-tab override state; an unavailable table only triggers a warning and retains the resolved global theme.</en>
                    // </lang>
                    PortalTabThemeOverrideReadResult overrideResult = PortalTabThemeOverrides.Read(tabId.Value, context);
                    if (overrideResult.IsAvailable && overrideResult.IsFound)
                    {
                        // <lang>
                        //   <zh-CN>保存覆盖主题验证失败原因；无效覆盖不会替换全局主题或阻断请求。</zh-CN>
                        //   <en>Hold the override-theme validation failure reason; an invalid override neither replaces the global theme nor blocks the request.</en>
                        // </lang>
                        string overrideFallbackReason;

                        // <lang>
                        //   <zh-CN>把覆盖候选解析为可信部署主题，只有验证成功才允许提升来源为 TabOverride。</zh-CN>
                        //   <en>Resolve the override candidate to a trusted deployed theme and elevate the source to TabOverride only after validation succeeds.</en>
                        // </lang>
                        string overrideThemeName = ResolveTrustedThemeName(
                            overrideResult.ThemeName,
                            context,
                            out overrideFallbackReason);
                        if (string.IsNullOrEmpty(overrideFallbackReason))
                        {
                            themeName = overrideThemeName;
                            source = PortalThemeSource.TabOverride;
                        }
                    }
                    else if (!overrideResult.IsAvailable)
                    {
                        TraceFallback(
                            context,
                            "TabOverrideTable",
                            "Tab theme override table is unavailable; using the global theme.");
                    }
                }
            }

            // <lang>
            //   <zh-CN>封装本请求最终主题快照，避免后续渲染阶段重新计算候选链。</zh-CN>
            //   <en>Package the final theme snapshot for this request so later rendering does not recompute the candidate chain.</en>
            // </lang>
            var resolved = new PortalThemeContext(themeName, source, tabId, fallbackReason);
            if (context != null)
            {
                context.Items[ThemeContextKey] = resolved;
            }

            return resolved;
        }

        /// <summary>
        /// 返回当前请求已解析的主题上下文；尚未解析时返回 null。
        /// Returns the resolved theme context for this request, or null when it has not been resolved.
        /// </summary>
        /// <param name="context">当前 HTTP 上下文。Current HTTP context.</param>
        /// <returns>已缓存的主题上下文，或 null。Cached theme context, or null.</returns>
        public static PortalThemeContext GetCurrentThemeContext(HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>优先使用显式上下文，再回退当前请求；没有请求时安全返回 null。</zh-CN>
            //   <en>Prefer the explicit context and then fall back to the current request; return null safely when no request exists.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;
            return current == null ? null : current.Items[ThemeContextKey] as PortalThemeContext;
        }

        /// <summary>
        /// 解析最终主题名；保留给健康检查和旧调用点使用。
        /// Resolves the final theme name; retained for health checks and legacy call sites.
        /// </summary>
        /// <param name="context">当前 HTTP 上下文。Current HTTP context.</param>
        /// <returns>合法且已验证的主题目录名称。Valid and verified theme directory name.</returns>
        public static string ResolveThemeName(HttpContext context)
        {
            return ResolveThemeContext(context).ThemeName;
        }

        /// <summary>
        /// 获取写入 Master body 的稳定主题与 Tab CSS class。
        /// Gets stable theme and tab CSS classes written to the Master body.
        /// </summary>
        /// <param name="context">当前 HTTP 上下文。Current HTTP context.</param>
        /// <returns>仅含受控 ASCII class 的文本。Text containing controlled ASCII classes only.</returns>
        /// <remarks>
        /// 正常门户页面在 <c>PreInit</c> 已解析主题。若调用过早而无上下文缓存，本方法只返回 Default scope，
        /// 不在渲染阶段重新读取数据库或配置。
        /// Normal portal pages resolve the theme during <c>PreInit</c>. When called too early without a context cache,
        /// this method returns only the Default scope and does not reread database or configuration during rendering.
        /// </remarks>
        public static string GetCurrentCssClass(HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>读取请求缓存主题；缺失时只输出 Default scope，不在渲染阶段访问配置或数据库。</zh-CN>
            //   <en>Read the request-cached theme; when absent, emit Default scope only and never access configuration or database during rendering.</en>
            // </lang>
            PortalThemeContext themeContext = GetCurrentThemeContext(context);
            if (themeContext == null)
            {
                return "portal-theme-default";
            }

            // <lang>
            //   <zh-CN>构造仅含受控前缀和规范化片段的 body class，避免主题原文直接进入 HTML 属性。</zh-CN>
            //   <en>Build a body class using only controlled prefixes and normalized segments so raw theme text never enters an HTML attribute.</en>
            // </lang>
            var builder = new StringBuilder("portal-theme-");
            builder.Append(NormalizeCssSegment(themeContext.ThemeName));
            if (themeContext.TabId.HasValue && themeContext.TabId.Value > 0)
            {
                builder.Append(" portal-tab-");
                builder.Append(themeContext.TabId.Value);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 获取门户模块包装元素的稳定 CSS class。
        /// Gets stable CSS classes for a portal module wrapper element.
        /// </summary>
        /// <param name="moduleId">模块实例标识。Module instance identifier.</param>
        /// <param name="paneName">模块所在窗格名。Pane containing the module.</param>
        /// <param name="packageId">已验证部署包标识；Legacy 模块传空。Validated deployment package id; empty for a Legacy module.</param>
        /// <returns>模块和窗格作用域 class。Module and pane scope classes.</returns>
        public static string GetModuleCssClass(int moduleId, string paneName, string packageId = null)
        {
            // <lang>
            //   <zh-CN>构造模块作用域 class；窗格和包标识均先规范化，模块 ID 保留整数形式。</zh-CN>
            //   <en>Build module-scope classes; pane and package identifiers are normalized first while module ID remains an integer representation.</en>
            // </lang>
            var builder = new StringBuilder("portal-module portal-module-");
            builder.Append(moduleId);
            builder.Append(" portal-pane-");
            builder.Append(NormalizeCssSegment(paneName));
            if (!string.IsNullOrWhiteSpace(packageId))
            {
                builder.Append(" portal-package-");
                builder.Append(NormalizeCssSegment(packageId));
            }

            return builder.ToString();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把候选主题名解析为已验证部署主题，失败时回退 Default 并记录受限告警。</zh-CN>
        ///   <en>Resolves a candidate theme name to a validated deployed theme, falling back to Default and recording a restricted warning on failure.</en>
        /// </lang>
        /// </summary>
        private static string ResolveTrustedThemeName(
            string requestedThemeName,
            HttpContext context,
            out string fallbackReason)
        {
            // <lang>
            //   <zh-CN>把空白候选归一为 Default，否则只去除外围空白；最终可信度由主题目录校验决定。</zh-CN>
            //   <en>Normalize a blank candidate to Default and otherwise trim surrounding whitespace; theme-directory validation decides final trust.</en>
            // </lang>
            string themeName = string.IsNullOrWhiteSpace(requestedThemeName)
                ? DefaultThemeName
                : requestedThemeName.Trim();

            // <lang>
            //   <zh-CN>保存可信主题包，在成功时只返回其规范名称。</zh-CN>
            //   <en>Hold the trusted theme package and return only its canonical name on success.</en>
            // </lang>
            PortalThemePackage package;

            // <lang>
            //   <zh-CN>接收非敏感验证失败原因，用于受限告警和最终上下文而不泄露 manifest 内容。</zh-CN>
            //   <en>Receive the non-sensitive validation failure reason for restricted warning and final context without leaking manifest content.</en>
            // </lang>
            string validationReason;
            if (PortalThemeCatalog.TryGetTrustedPackage(themeName, out package, out validationReason))
            {
                fallbackReason = string.Empty;
                return package.Name;
            }

            fallbackReason = validationReason;
            TraceFallback(context, themeName, validationReason);
            return DefaultThemeName;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把运行期设置来源转换为主题来源枚举。</zh-CN>
        ///   <en>Converts runtime-setting source into the theme-source enumeration.</en>
        /// </lang>
        /// </summary>
        private static PortalThemeSource ToThemeSource(
            PortalRuntimeSettingSource settingSource,
            bool usedFallback)
        {
            if (usedFallback)
            {
                return PortalThemeSource.Fallback;
            }

            switch (settingSource)
            {
                case PortalRuntimeSettingSource.Database:
                    return PortalThemeSource.Database;
                case PortalRuntimeSettingSource.AppSettings:
                    return PortalThemeSource.AppSettings;
                default:
                    return PortalThemeSource.Default;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求是否为后台页，后台页不参与 Tab 主题覆盖。</zh-CN>
        ///   <en>Determines whether the current request is an administration page, which does not participate in tab theme overrides.</en>
        /// </lang>
        /// </summary>
        private static bool IsAdminRequest(HttpContext context)
        {
            // <lang>
            //   <zh-CN>优先采用显式上下文，再回退当前请求，以保持测试和宿主调用可预测。</zh-CN>
            //   <en>Prefer the explicit context and then the current request to keep tests and host calls predictable.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;

            // <lang>
            //   <zh-CN>读取应用相对执行路径；缺少请求时使用空文本，使判定安全地落入非后台分支。</zh-CN>
            //   <en>Read the application-relative execution path; use empty text without a request so the decision safely falls outside the administration branch.</en>
            // </lang>
            string path = current == null || current.Request == null
                ? string.Empty
                : current.Request.AppRelativeCurrentExecutionFilePath;
            return path.StartsWith("~/Admin/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从当前门户上下文读取活动 Tab 标识，无法取得时返回 null。</zh-CN>
        ///   <en>Reads the active tab identifier from the current portal context, returning null when unavailable.</en>
        /// </lang>
        /// </summary>
        private static int? TryGetActiveTabId(HttpContext context)
        {
            try
            {
                // <lang>
                //   <zh-CN>读取当前请求关联的门户设置；该操作只服务 Tab 判定，不修改上下文或主题设置。</zh-CN>
                //   <en>Read portal settings associated with the current request; this serves tab determination only and changes neither context nor theme settings.</en>
                // </lang>
                PortalSettings settings = PortalContext.GetPortalSettings(context);
                return settings.ActiveTab == null || settings.ActiveTab.TabId <= 0
                    ? (int?)null
                    : settings.ActiveTab.TabId;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把主题、pane 或包标识转换为稳定 CSS class 片段。</zh-CN>
        ///   <en>Converts a theme, pane, or package identifier into a stable CSS class segment.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeCssSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "default";
            }

            // <lang>
            //   <zh-CN>逐字符构造 ASCII 安全 class 片段，任何非允许字符统一降级为连字符。</zh-CN>
            //   <en>Build an ASCII-safe class segment character by character, degrading every disallowed character to a hyphen.</en>
            // </lang>
            var builder = new StringBuilder();

            // <lang>
            //   <zh-CN>遍历已去除外围空白的输入；循环不保留原始 Unicode 字符。</zh-CN>
            //   <en>Traverse the trimmed input; the loop does not retain raw Unicode characters.</en>
            // </lang>
            foreach (char character in value.Trim())
            {
                if ((character >= 'A' && character <= 'Z') ||
                    (character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '-' ||
                    character == '_')
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
                else
                {
                    builder.Append('-');
                }
            }

            return builder.Length == 0 ? "default" : builder.ToString();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录主题回退告警，并按请求主题和原因去重。</zh-CN>
        ///   <en>Records a theme fallback warning and de-duplicates by requested theme and reason.</en>
        /// </lang>
        /// </summary>
        private static void TraceFallback(HttpContext context, string requestedTheme, string fallbackReason)
        {
            // <lang>
            //   <zh-CN>净化并截断请求主题，供受限日志使用，避免把原始配置或攻击输入写入诊断。</zh-CN>
            //   <en>Sanitize and truncate the requested theme for restricted logging, preventing raw configuration or hostile input from entering diagnostics.</en>
            // </lang>
            string requested = PortalDiagnosticSanitizer.SanitizeAndTruncate(requestedTheme, 100);

            // <lang>
            //   <zh-CN>净化并截断回退原因，保持诊断稳定且不携带 manifest 或路径细节。</zh-CN>
            //   <en>Sanitize and truncate the fallback reason, keeping diagnostics stable without manifest or path details.</en>
            // </lang>
            string reason = PortalDiagnosticSanitizer.SanitizeAndTruncate(fallbackReason, 200);

            // <lang>
            //   <zh-CN>组合进程内去重键；它不写入 Cookie、数据库或跨请求用户状态。</zh-CN>
            //   <en>Combine the process-local de-duplication key; it writes to no cookie, database, or cross-request user state.</en>
            // </lang>
            string fallbackKey = requested + "|" + reason;
            lock (TraceLock)
            {
                if (WarnedFallbacks.Contains(fallbackKey))
                {
                    return;
                }

                WarnedFallbacks.Add(fallbackKey);
            }

            // <lang>
            //   <zh-CN>构造已净化的固定格式告警文本，供 Trace 和门户诊断复用。</zh-CN>
            //   <en>Build a sanitized fixed-format warning message shared by Trace and portal diagnostics.</en>
            // </lang>
            string warningMessage = string.Format(
                "Theme configuration fell back to {0}. Requested='{1}', Reason='{2}'.",
                DefaultThemeName,
                requested,
                reason);
            if (context != null && context.Trace != null)
            {
                context.Trace.Warn(TraceCategory, warningMessage);
            }

            PortalDiagnostics.Warn(TraceCategory, warningMessage, context);
        }
    }
}
