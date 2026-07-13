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
        internal PortalThemeContext(string themeName, PortalThemeSource source, int? tabId, string fallbackReason)
        {
            ThemeName = themeName ?? PortalThemeResolver.DefaultThemeName;
            Source = source;
            TabId = tabId;
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
        public string FallbackReason { get; private set; }
    }

    /// <summary>
    /// 解析并应用门户 Web Forms 主题。
    /// Resolves and applies the portal Web Forms theme.
    /// </summary>
    /// <remarks>
    /// 原生 Theme 只承担每页唯一基底；Tab 和后续模块差异通过稳定 CSS scope 表达。
    /// 主题值必须对应已部署且通过 manifest 校验的可信包，不能由查询字符串、远程 URL 或脚本决定。
    /// Native Theme provides the one base theme per page only; tab and future module variations use stable CSS
    /// scopes. Theme values must resolve to a deployed package that passes manifest validation and cannot be
    /// selected by query strings, remote URLs, or scripts.
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

        private const string TraceCategory = "PortalTheme";
        private const string ThemeContextKey = "PortalThemeContext";

        private static readonly object TraceLock = new object();
        private static readonly HashSet<string> WarnedFallbacks = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 在页面 PreInit 阶段解析并应用唯一原生 Theme。
        /// Resolves and applies the single native Theme during page PreInit.
        /// </summary>
        /// <param name="page">接收主题的 Web Forms 页面。Web Forms page receiving the theme.</param>
        /// <returns>本请求的完整主题上下文。Complete theme context for this request.</returns>
        public static PortalThemeContext ApplyTheme(Page page)
        {
            if (page == null)
            {
                throw new ArgumentNullException("page");
            }

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
        public static PortalThemeContext ResolveThemeContext(HttpContext context)
        {
            PortalThemeContext existing = GetCurrentThemeContext(context);
            if (existing != null)
            {
                return existing;
            }

            PortalRuntimeSettingValue globalSetting =
                PortalRuntimeSettings.GetEffectiveValue(PortalSettingsRegistry.ThemeName, context);
            string fallbackReason = string.Empty;
            string themeName = ResolveTrustedThemeName(globalSetting.Value, context, out fallbackReason);
            PortalThemeSource source = ToThemeSource(globalSetting.Source, !string.IsNullOrEmpty(fallbackReason));
            int? tabId = null;

            if (!IsAdminRequest(context))
            {
                tabId = TryGetActiveTabId(context);
                if (tabId.HasValue)
                {
                    PortalTabThemeOverrideReadResult overrideResult = PortalTabThemeOverrides.Read(tabId.Value, context);
                    if (overrideResult.IsAvailable && overrideResult.IsFound)
                    {
                        string overrideFallbackReason;
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
        public static string GetCurrentCssClass(HttpContext context = null)
        {
            PortalThemeContext themeContext = GetCurrentThemeContext(context);
            if (themeContext == null)
            {
                return "portal-theme-default";
            }

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

        private static string ResolveTrustedThemeName(
            string requestedThemeName,
            HttpContext context,
            out string fallbackReason)
        {
            string themeName = string.IsNullOrWhiteSpace(requestedThemeName)
                ? DefaultThemeName
                : requestedThemeName.Trim();
            PortalThemePackage package;
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

        private static bool IsAdminRequest(HttpContext context)
        {
            HttpContext current = context ?? HttpContext.Current;
            string path = current == null || current.Request == null
                ? string.Empty
                : current.Request.AppRelativeCurrentExecutionFilePath;
            return path.StartsWith("~/Admin/", StringComparison.OrdinalIgnoreCase);
        }

        private static int? TryGetActiveTabId(HttpContext context)
        {
            try
            {
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

        private static string NormalizeCssSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "default";
            }

            var builder = new StringBuilder();
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

        private static void TraceFallback(HttpContext context, string requestedTheme, string fallbackReason)
        {
            string requested = PortalDiagnosticSanitizer.SanitizeAndTruncate(requestedTheme, 100);
            string reason = PortalDiagnosticSanitizer.SanitizeAndTruncate(fallbackReason, 200);
            string fallbackKey = requested + "|" + reason;
            lock (TraceLock)
            {
                if (WarnedFallbacks.Contains(fallbackKey))
                {
                    return;
                }

                WarnedFallbacks.Add(fallbackKey);
            }

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
