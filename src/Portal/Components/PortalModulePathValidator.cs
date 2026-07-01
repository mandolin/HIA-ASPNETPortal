using System;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 校验模块定义中的桌面控件路径。它只保护当前旧机制的 LoadControl 边界，不替代后续权限体系。
    /// </summary>
    public static class PortalModulePathValidator
    {
        private static readonly string[] AllowedDesktopPrefixes =
        {
            "DesktopModules/",
            "Admin/"
        };

        /// <summary>
        /// 校验并规范化桌面模块控件路径。
        /// </summary>
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
        /// 校验失败时抛出明确异常，供运行期动态加载模块前使用。
        /// </summary>
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
