using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// Resolves and applies the configured WebForms theme for portal pages.
    /// 解析并应用门户页面的 WebForms 主题配置。
    /// </summary>
    /// <remarks>
    /// P1.2 keeps the native ASP.NET Theme mechanism and only selects a validated theme name.
    /// P1.2 保留 ASP.NET 原生 Theme 机制，只负责选择通过校验的主题名称。
    /// </remarks>
    public static class PortalThemeResolver
    {
        /// <summary>
        /// AppSettings key used to configure the active portal theme.
        /// 用于配置当前门户主题的 appSettings 键名。
        /// </summary>
        public const string ThemeNameSettingKey = PortalSettingKeys.ThemeName;

        /// <summary>
        /// Safe fallback theme used when the configured theme is empty, invalid, or missing.
        /// 当配置为空、非法或目录缺失时使用的安全回退主题。
        /// </summary>
        public const string DefaultThemeName = "Default";

        private const string TraceCategory = "PortalTheme";

        private static readonly object TraceLock = new object();

        private static readonly HashSet<string> WarnedFallbacks = new HashSet<string>(StringComparer.Ordinal);

        private static readonly Regex ThemeNamePattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_-]{0,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Applies the resolved theme to the current page during PreInit.
        /// 在页面 PreInit 阶段将解析出的主题应用到当前页面。
        /// </summary>
        /// <param name="page">The WebForms page receiving the theme. 要应用主题的 WebForms 页面。</param>
        /// <returns>The final theme name applied to the page. 最终应用到页面的主题名称。</returns>
        public static string ApplyTheme(Page page)
        {
            if (page == null)
            {
                throw new ArgumentNullException("page");
            }

            string themeName = ResolveThemeName(HttpContext.Current);
            page.Theme = themeName;
            return themeName;
        }

        /// <summary>
        /// Resolves the configured theme name and falls back to <see cref="DefaultThemeName"/> when needed.
        /// 解析配置的主题名称；必要时回退到 <see cref="DefaultThemeName"/>。
        /// </summary>
        /// <param name="context">Current HTTP context used for trace output. 用于输出 Trace 的当前 HTTP 上下文。</param>
        /// <returns>A valid theme directory name. 合法的主题目录名称。</returns>
        public static string ResolveThemeName(HttpContext context)
        {
            string configuredTheme = PortalRuntimeSettings.GetString(PortalSettingsRegistry.ThemeName);
            string themeName = string.IsNullOrWhiteSpace(configuredTheme)
                ? DefaultThemeName
                : configuredTheme.Trim();

            string fallbackReason = null;

            if (!ThemeNamePattern.IsMatch(themeName))
            {
                fallbackReason = "主题名包含非法字符。 Theme name contains invalid characters.";
            }
            else if (!ThemeDirectoryExists(themeName))
            {
                fallbackReason = "主题目录不存在。 Theme directory does not exist.";
            }

            if (fallbackReason == null)
            {
                return themeName;
            }

            TraceFallback(context, configuredTheme, fallbackReason);
            return DefaultThemeName;
        }

        private static bool ThemeDirectoryExists(string themeName)
        {
            string physicalPath = HostingEnvironment.MapPath("~/App_Themes/" + themeName);
            return !string.IsNullOrEmpty(physicalPath) && Directory.Exists(physicalPath);
        }

        private static void TraceFallback(HttpContext context, string configuredTheme, string fallbackReason)
        {
            string requestedTheme = string.IsNullOrWhiteSpace(configuredTheme) ? "(empty)" : configuredTheme;
            string fallbackKey = requestedTheme + "|" + fallbackReason;
            lock (TraceLock)
            {
                // 同一非法值/原因在应用生命周期内只记录一次，避免错误配置时刷屏。
                // Log the same invalid value/reason only once per application lifetime.
                if (WarnedFallbacks.Contains(fallbackKey))
                {
                    return;
                }

                WarnedFallbacks.Add(fallbackKey);
            }

            string warningMessage = string.Format(
                "主题配置已回退到 {0}。 Theme configuration fell back to {0}. Requested='{1}', Reason='{2}'.",
                DefaultThemeName,
                requestedTheme,
                fallbackReason);

            if (context != null && context.Trace != null)
            {
                context.Trace.Warn(TraceCategory, warningMessage);
            }

            PortalDiagnostics.Warn(TraceCategory, warningMessage, context);
        }
    }
}
