using System;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
        /// 校验进入 Web Forms <c>LoadControl</c> 的桌面模块相对路径。
        /// Validates a desktop-module relative path before it reaches Web Forms <c>LoadControl</c>.
        /// </summary>
        /// <remarks>
        /// 此校验器只允许 <c>DesktopModules/</c> 或 <c>Admin/</c> 下的站内 <c>.ascx</c> 控件，拒绝绝对路径、
        /// 外部 URL 和父目录跳转。它缩小旧动态加载机制的路径边界，不替代模块写操作、页面访问或部署信任授权。
        /// This validator permits only site-local <c>.ascx</c> controls under <c>DesktopModules/</c> or <c>Admin/</c>
        /// and rejects absolute paths, external URLs, and parent traversal. It narrows the path boundary of the legacy
        /// dynamic loader; it does not replace authorization for module writes, page access, or deployment trust.
        /// </remarks>
    public static class PortalModulePathValidator
    {
        private static readonly string[] AllowedDesktopPrefixes =
        {
            "DesktopModules/",
            "Admin/"
        };

        /// <summary>
        /// 校验并规范化桌面模块控件路径。
        /// Validates and normalizes a desktop module-control path.
        /// </summary>
        /// <param name="source">来自旧模块定义或 manifest 的原始路径。
        /// Raw path from a legacy module definition or manifest.</param>
        /// <param name="normalizedSource">成功时为不带 <c>~/</c> 前缀、使用正斜杠的站内相对路径。
        /// On success, a site-relative path without the <c>~/</c> prefix and with forward slashes.</param>
        /// <param name="errorMessage">失败时供受控管理界面或诊断使用的说明，不包含物理路径。
        /// Failure explanation for controlled administration UI or diagnostics, without physical paths.</param>
        /// <returns>路径是否满足当前动态加载边界。
        /// Whether the path meets the current dynamic-loading boundary.</returns>
        public static bool TryNormalizeDesktopSource(string source, out string normalizedSource, out string errorMessage)
        {
            normalizedSource = NormalizeSeparators(source);
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedSource))
            {
                errorMessage = "桌面模块控件路径不能为空。";
                return false;
            }

            if (normalizedSource.StartsWith("~/", StringComparison.Ordinal))
            {
                normalizedSource = normalizedSource.Substring(2);
            }

            if (normalizedSource.StartsWith("/", StringComparison.Ordinal) ||
                normalizedSource.Contains("://") ||
                normalizedSource.Split('/').Any(part => part == ".."))
            {
                errorMessage = "桌面模块控件路径必须是站点内相对路径，不能包含绝对路径、外部 URL 或上级目录。";
                return false;
            }

            if (!normalizedSource.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "桌面模块控件路径必须指向 .ascx 用户控件。";
                return false;
            }

            string candidateSource = normalizedSource;
            bool allowedPrefix = AllowedDesktopPrefixes.Any(prefix =>
                candidateSource.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (!allowedPrefix)
            {
                errorMessage = "桌面模块控件路径只能位于 DesktopModules/ 或 Admin/ 目录下。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 返回已规范化的模块路径，或在校验失败时抛出异常。
        /// Returns the normalized module path or throws when validation fails.
        /// </summary>
        /// <param name="source">待校验的原始模块路径。
        /// Raw module path to validate.</param>
        /// <returns>可传给受控动态加载流程的站内相对路径。
        /// Site-relative path that may enter the controlled dynamic-loading flow.</returns>
        /// <exception cref="InvalidOperationException">路径为空、超出允许前缀或包含不安全形式时抛出。
        /// Thrown when the path is empty, outside an allowed prefix, or contains an unsafe form.</exception>
        /// <remarks>
        /// 异常当前会保留原始输入，调用方在面向用户的输出中必须继续使用既有诊断净化策略，不能直接回显。
        /// The exception currently retains the raw input. Callers must continue using the established diagnostic
        /// sanitization policy for user-facing output and must not echo it directly.
        /// </remarks>
        public static string NormalizeDesktopSourceOrThrow(string source)
        {
            string normalizedSource;
            string errorMessage;
            if (TryNormalizeDesktopSource(source, out normalizedSource, out errorMessage))
            {
                return normalizedSource;
            }

            throw new InvalidOperationException($"{errorMessage} 当前值：{source}");
        }

        private static string NormalizeSeparators(string source)
        {
            return (source ?? string.Empty).Trim().Replace('\\', '/');
        }
    }
}
