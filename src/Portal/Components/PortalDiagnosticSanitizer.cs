using System;
using System.Text.RegularExpressions;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 诊断与审计输出的统一敏感信息净化工具。
    /// Shared sensitive-data sanitizer for diagnostics and audit output.
    /// </summary>
    /// <remarks>
    /// 该工具以“宁可少记、不回退原文”为原则。它不是安全边界的唯一来源，
    /// 调用者仍不得传入 Cookie、请求正文、表单值或认证票据。
    /// This helper follows a fail-closed logging rule: omit rather than fall back to raw text.
    /// It is not the sole security boundary; callers must still avoid cookies, bodies, form values,
    /// and authentication tickets.
    /// </remarks>
    internal static class PortalDiagnosticSanitizer
    {
        private static readonly Regex ConnectionStringPattern = new Regex(
            @"(?<key>connection\s*string|connectionstring)\s*(?<separator>[:=])\s*(?<value>""[^""]*""|'[^']*'|[^\r\n]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex DataSourceConnectionPattern = new Regex(
            @"(?:data\s*source|server)\s*=\s*[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex SensitiveLinePattern = new Regex(
            @"(?<key>password|pwd|token|authorization|cookie|set-cookie|api(?:[_\s-]?key)?|secret)\s*(?<separator>[:=])\s*[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex SensitiveAssignmentPattern = new Regex(
            @"(?<key>password|pwd|token|authorization|cookie|api(?:[_\s-]?key)?|secret)\s*(?<separator>[:=])\s*(?<value>""[^""]*""|'[^']*'|[^;,\s\r\n]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// 净化可能包含敏感键值的文本。
        /// Sanitizes text that may contain sensitive key-value data.
        /// </summary>
        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            try
            {
                string sanitized = ConnectionStringPattern.Replace(value, ReplaceAssignment);
                sanitized = DataSourceConnectionPattern.Replace(sanitized, "[REDACTED_CONNECTION_STRING]");
                // 认证头与 Cookie 可能含空格、分号或多个字段，因此对该行剩余内容整体遮蔽。
                // Authorization and cookies may contain spaces, semicolons, or multiple values, so redact the rest of the line.
                sanitized = SensitiveLinePattern.Replace(sanitized, ReplaceAssignment);
                sanitized = SensitiveAssignmentPattern.Replace(sanitized, ReplaceAssignment);
                return sanitized;
            }
            catch (Exception)
            {
                // 净化失败时绝不回退到原文。
                // Never fall back to raw text when sanitization fails.
                return "[SANITIZATION_FAILED]";
            }
        }

        /// <summary>
        /// 净化并截断文本，避免日志或审计字段被无限放大。
        /// Sanitizes and truncates text to prevent unbounded log or audit fields.
        /// </summary>
        public static string SanitizeAndTruncate(string value, int maximumLength)
        {
            string sanitized = Sanitize(value);
            if (maximumLength <= 0 || sanitized.Length <= maximumLength)
            {
                return maximumLength <= 0 ? string.Empty : sanitized;
            }

            return sanitized.Substring(0, Math.Max(0, maximumLength - 3)) + "...";
        }

        private static string ReplaceAssignment(Match match)
        {
            return match.Groups["key"].Value + match.Groups["separator"].Value + "[REDACTED]";
        }
    }
}
