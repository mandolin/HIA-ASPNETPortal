using System;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>校验进入 Web Forms <c>LoadControl</c> 的桌面模块相对路径。</zh-CN>
    ///   <en>Validates a desktop-module relative path before it reaches Web Forms <c>LoadControl</c>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此校验器只允许 <c>DesktopModules/</c> 或 <c>Admin/</c> 下的站内 <c>.ascx</c> 控件，拒绝绝对路径、外部 URL 和父目录跳转。它缩小旧动态加载机制的路径边界，不替代模块写操作、页面访问或部署信任授权。</zh-CN>
    ///   <en>This validator permits only site-local <c>.ascx</c> controls under <c>DesktopModules/</c> or <c>Admin/</c> and rejects absolute paths, external URLs, and parent traversal. It narrows the path boundary of the legacy dynamic loader; it does not replace authorization for module writes, page access, or deployment trust.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModulePathValidator
    {
        // <lang>
        //   <zh-CN>固定允许的站内模块目录前缀；它限制动态加载范围，不是部署信任或访问授权。</zh-CN>
        //   <en>Fixed allowed site-local module prefixes; they constrain dynamic loading and are not deployment trust or access authorization.</en>
        // </lang>
        private static readonly string[] AllowedDesktopPrefixes =
        {
            "DesktopModules/",
            "Admin/"
        };

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验并规范化桌面模块控件路径。</zh-CN>
        ///   <en>Validates and normalizes a desktop module-control path.</en>
        /// </lang>
        /// </summary>
        /// <param name="source">
        ///   <lang><zh-CN>来自旧模块定义或 manifest 的原始路径。</zh-CN><en>Raw path from a legacy module definition or manifest.</en></lang>
        /// </param>
        /// <param name="normalizedSource">
        ///   <lang><zh-CN>成功时为不带 <c>~/</c> 前缀、使用正斜杠的站内相对路径。</zh-CN><en>On success, a site-relative path without the <c>~/</c> prefix and with forward slashes.</en></lang>
        /// </param>
        /// <param name="errorMessage">
        ///   <lang><zh-CN>失败时供受控管理界面或诊断使用的说明，不包含物理路径。</zh-CN><en>Failure explanation for controlled administration UI or diagnostics, without physical paths.</en></lang>
        /// </param>
        /// <returns><lang><zh-CN>路径是否满足当前动态加载边界。</zh-CN><en>Whether the path meets the current dynamic-loading boundary.</en></lang></returns>
        public static bool TryNormalizeDesktopSource(string source, out string normalizedSource, out string errorMessage)
        {
            // <lang>
            //   <zh-CN>先统一分隔符并去除首尾空白，使后续规则只处理稳定的站内路径形状。</zh-CN>
            //   <en>Normalizes separators and trims surrounding whitespace first so later rules process one stable site-path shape.</en>
            // </lang>
            normalizedSource = NormalizeSeparators(source);

            // <lang>
            //   <zh-CN>错误说明只供受控调用方展示或诊断，保持为空直到某个拒绝分支提供固定低敏原因。</zh-CN>
            //   <en>Keeps the diagnostic explanation empty until a rejection branch supplies a fixed low-sensitivity reason for controlled callers.</en>
            // </lang>
            errorMessage = string.Empty;

            // <lang>
            //   <zh-CN>空路径没有可验证的控件目标，直接拒绝而不继续解析。</zh-CN>
            //   <en>An empty path has no verifiable control target and is rejected before further parsing.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(normalizedSource))
            {
                errorMessage = "桌面模块控件路径不能为空。";
                return false;
            }

            // <lang>
            //   <zh-CN>兼容旧 Web Forms 的站内根前缀，但只移除固定的 <c>~/</c>，不展开物理路径。</zh-CN>
            //   <en>Supports the legacy Web Forms site-root prefix by removing only fixed <c>~/</c> text without resolving a physical path.</en>
            // </lang>
            if (normalizedSource.StartsWith("~/", StringComparison.Ordinal))
            {
                normalizedSource = normalizedSource.Substring(2);
            }

            // <lang>
            //   <zh-CN>拒绝绝对路径、外部协议和父目录跳转，确保候选仍处于站内相对路径边界。</zh-CN>
            //   <en>Rejects absolute paths, external schemes, and parent traversal so the candidate remains site-relative.</en>
            // </lang>
            if (normalizedSource.StartsWith("/", StringComparison.Ordinal) ||
                normalizedSource.Contains("://") ||
                normalizedSource.Split('/').Any(part => part == ".."))
            {
                errorMessage = "桌面模块控件路径必须是站点内相对路径，不能包含绝对路径、外部 URL 或上级目录。";
                return false;
            }

            // <lang>
            //   <zh-CN>动态加载只接受用户控件后缀，避免把其它文件类型送入 <c>LoadControl</c> 路径。</zh-CN>
            //   <en>Dynamic loading accepts only the user-control suffix so other file types cannot enter the <c>LoadControl</c> path.</en>
            // </lang>
            if (!normalizedSource.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "桌面模块控件路径必须指向 .ascx 用户控件。";
                return false;
            }

            // <lang>
            //   <zh-CN>保留已规范化候选供固定目录前缀检查，避免后续分支重新解释原始输入。</zh-CN>
            //   <en>Keeps the normalized candidate for the fixed-prefix check so later branches do not reinterpret raw input.</en>
            // </lang>
            string candidateSource = normalizedSource;
            // <lang>
            //   <zh-CN>只有 DesktopModules/ 或 Admin/ 下的控件才进入受控动态加载边界。</zh-CN>
            //   <en>Only controls under DesktopModules/ or Admin/ enter the controlled dynamic-loading boundary.</en>
            // </lang>
            bool allowedPrefix = AllowedDesktopPrefixes.Any(prefix =>
                candidateSource.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (!allowedPrefix)
            {
                errorMessage = "桌面模块控件路径只能位于 DesktopModules/ 或 Admin/ 目录下。";
                return false;
            }

            // <lang>
            //   <zh-CN>所有路径规则通过后返回成功；此结果仍不代表调用方已完成页面或部署授权。</zh-CN>
            //   <en>Returns success only after all path rules pass; the result still does not represent page or deployment authorization.</en>
            // </lang>
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回已规范化的模块路径，或在校验失败时抛出异常。</zh-CN>
        ///   <en>Returns the normalized module path or throws when validation fails.</en>
        /// </lang>
        /// </summary>
        /// <param name="source"><lang><zh-CN>待校验的原始模块路径。</zh-CN><en>Raw module path to validate.</en></lang></param>
        /// <returns><lang><zh-CN>可传给受控动态加载流程的站内相对路径。</zh-CN><en>Site-relative path that may enter the controlled dynamic-loading flow.</en></lang></returns>
        /// <exception cref="InvalidOperationException"><lang><zh-CN>路径为空、超出允许前缀或包含不安全形式时抛出。</zh-CN><en>Thrown when the path is empty, outside an allowed prefix, or contains an unsafe form.</en></lang></exception>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>异常当前会保留原始输入，调用方在面向用户的输出中必须继续使用既有诊断净化策略，不能直接回显。</zh-CN>
        ///   <en>The exception currently retains the raw input. Callers must continue using the established diagnostic sanitization policy for user-facing output and must not echo it directly.</en>
        /// </lang>
        /// </remarks>
        public static string NormalizeDesktopSourceOrThrow(string source)
        {
            // <lang>
            //   <zh-CN>保留两个 out 结果，使成功路径返回规范化值，失败路径只使用固定低敏错误说明。</zh-CN>
            //   <en>Keeps both out results so success returns the normalized value while failure uses only the fixed low-sensitivity explanation.</en>
            // </lang>
            string normalizedSource;
            string errorMessage;

            // <lang>
            //   <zh-CN>复用同一校验入口，避免抛异常版本与布尔版本出现不同的路径安全规则。</zh-CN>
            //   <en>Reuses the same validation entry point so the throwing and Boolean APIs cannot diverge in path-safety rules.</en>
            // </lang>
            if (TryNormalizeDesktopSource(source, out normalizedSource, out errorMessage))
            {
                return normalizedSource;
            }

            // <lang>
            //   <zh-CN>异常保留既有诊断文本和原始值兼容行为；面向用户的调用方必须继续净化输出。</zh-CN>
            //   <en>Preserves the existing diagnostic text and raw-value compatibility behavior; user-facing callers must continue sanitizing output.</en>
            // </lang>
            throw new InvalidOperationException($"{errorMessage} 当前值：{source}");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空值转换为空字符串、修剪空白并统一为正斜杠。</zh-CN>
        ///   <en>Converts null to an empty string, trims whitespace, and normalizes separators to forward slashes.</en>
        /// </lang>
        /// </summary>
        /// <param name="source"><lang><zh-CN>待规范化的原始路径。</zh-CN><en>Raw path to normalize.</en></lang></param>
        /// <returns><lang><zh-CN>供路径规则继续检查的稳定文本。</zh-CN><en>Stable text for subsequent path-rule checks.</en></lang></returns>
        private static string NormalizeSeparators(string source)
        {
            // <lang>
            //   <zh-CN>只做文本级规范化，不解析 URI、物理路径或外部资源。</zh-CN>
            //   <en>Performs text-only normalization without resolving a URI, physical path, or external resource.</en>
            // </lang>
            return (source ?? string.Empty).Trim().Replace('\\', '/');
        }
    }
}
