using System;
using System.Text.RegularExpressions;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>诊断与审计输出的统一敏感信息净化工具。</zh-CN>
    ///   <en>Shared sensitive-data sanitizer for diagnostics and audit output.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该工具遵循“宁可少记、不回退原文”的 fail-closed 原则，但不是唯一安全边界；调用者仍不得传入 Cookie、请求正文、表单值或认证票据。</zh-CN>
    ///   <en>This helper follows a fail-closed rule of omitting rather than falling back to raw text, but it is not the only security boundary; callers must still avoid cookies, bodies, form values, and authentication tickets.</en>
    /// </lang>
    /// </remarks>
    internal static class PortalDiagnosticSanitizer
    {
        // <lang>
        //   <zh-CN>这些文化无关模式按连接串、数据源、整行敏感头和值赋值的顺序遮蔽常见秘密；它们是纵深防御，不代表调用方可以记录任意原文。</zh-CN>
        //   <en>These culture-invariant patterns redact common secrets in connection-string, data-source, full sensitive-line, and assignment order; they are defense in depth and do not permit callers to log arbitrary raw text.</en>
        // </lang>
        private static readonly Regex ConnectionStringPattern = new Regex(
            @"(?<key>connection\s*string|connectionstring)\s*(?<separator>[:=])\s*(?<value>""[^""]*""|'[^']*'|[^\r\n]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // <lang>
        //   <zh-CN>覆盖未使用 connection string 键名、但仍包含 data source/server 的连接串片段。</zh-CN>
        //   <en>Redacts connection-string fragments that omit a connection-string key but still contain data source/server values.</en>
        // </lang>
        private static readonly Regex DataSourceConnectionPattern = new Regex(
            @"(?:data\s*source|server)\s*=\s*[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // <lang>
        //   <zh-CN>遮蔽整行凭据/认证头，优先于紧凑赋值规则以避免留下同一行的其它敏感尾部。</zh-CN>
        //   <en>Redacts full credential/authentication lines before compact assignments so no sensitive tail remains on the same line.</en>
        // </lang>
        private static readonly Regex SensitiveLinePattern = new Regex(
            @"(?<key>password|pwd|token|authorization|cookie|set-cookie|api(?:[_\s-]?key)?|secret)\s*(?<separator>[:=])\s*[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // <lang>
        //   <zh-CN>覆盖分号、逗号或空白分隔的紧凑敏感键值，作为整行规则之后的兜底。</zh-CN>
        //   <en>Redacts compact sensitive assignments delimited by semicolons, commas, or whitespace as the fallback after full-line rules.</en>
        // </lang>
        private static readonly Regex SensitiveAssignmentPattern = new Regex(
            @"(?<key>password|pwd|token|authorization|cookie|api(?:[_\s-]?key)?|secret)\s*(?<separator>[:=])\s*(?<value>""[^""]*""|'[^']*'|[^;,\s\r\n]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// <lang>
        ///   <zh-CN>净化可能包含敏感键值的文本。</zh-CN>
        ///   <en>Sanitizes text that may contain sensitive key-value data.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可能包含连接串、凭据或认证字段的候选文本。</zh-CN>
        ///   <en>Candidate text that may contain connection strings, credentials, or authentication fields.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已遮蔽已识别敏感值的文本；净化失败时返回固定占位，绝不返回原文。</zh-CN>
        ///   <en>Text with recognized sensitive values redacted, or a fixed placeholder on sanitization failure; never raw input.</en>
        /// </l>
        /// </returns>
        public static string Sanitize(string value)
        {
            // <lang>
            //   <zh-CN>空输入归一为空字符串，避免向调用方传播 null 或把“没有可记录内容”解释为原文回退。</zh-CN>
            //   <en>Normalize empty input to an empty string so null does not propagate and “nothing to log” cannot be interpreted as a raw-text fallback.</en>
            // </lang>
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            try
            {
                // <lang>
                //   <zh-CN>先遮蔽连接串和数据源，再覆盖可能携带多个字段的认证/Cookie 整行，最后处理紧凑赋值；顺序保留既有净化覆盖面。</zh-CN>
                //   <en>Redact connection strings and data sources first, then full authorization/Cookie lines that may carry multiple fields, and finally compact assignments; the order preserves established sanitization coverage.</en>
                // </lang>
                string sanitized = ConnectionStringPattern.Replace(value, ReplaceAssignment);
                sanitized = DataSourceConnectionPattern.Replace(sanitized, "[REDACTED_CONNECTION_STRING]");
                sanitized = SensitiveLinePattern.Replace(sanitized, ReplaceAssignment);
                sanitized = SensitiveAssignmentPattern.Replace(sanitized, ReplaceAssignment);
                return sanitized;
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>正则净化失败时 fail-closed 返回固定占位，绝不以异常处理为由回退记录原文。</zh-CN>
                //   <en>If regex sanitization fails, return a fixed fail-closed placeholder and never use error handling to fall back to raw text.</en>
                // </lang>
                return "[SANITIZATION_FAILED]";
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>净化并截断文本，避免日志或审计字段被无限放大。</zh-CN>
        ///   <en>Sanitizes and truncates text to prevent unbounded log or audit fields.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可能包含敏感键值且需要受控输出长度的候选文本。</zh-CN>
        ///   <en>Candidate text that may contain sensitive key-value data and needs a controlled output length.</en>
        /// </l>
        /// </param>
        /// <param name="maximumLength">
        /// <l>
        ///   <zh-CN>输出最大字符数；非正值返回空字符串。</zh-CN>
        ///   <en>Maximum output character count; a non-positive value returns an empty string.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>先净化后截断的文本；截断时尾部包含省略号。</zh-CN>
        ///   <en>Text sanitized before truncation, with an ellipsis when truncation occurs.</en>
        /// </l>
        /// </returns>
        public static string SanitizeAndTruncate(string value, int maximumLength)
        {
            // <lang>
            //   <zh-CN>始终先净化再判断长度；非正上限明确返回空字符串，而不是泄露未截断输入。</zh-CN>
            //   <en>Always sanitize before evaluating length; a non-positive cap explicitly returns an empty string rather than exposing untruncated input.</en>
            // </lang>
            string sanitized = Sanitize(value);
            if (maximumLength <= 0 || sanitized.Length <= maximumLength)
            {
                return maximumLength <= 0 ? string.Empty : sanitized;
            }

            // <lang>
            //   <zh-CN>省略号计入固定上限，保留“内容被截断”的事实而不让结果超过调用方字段预算。</zh-CN>
            //   <en>The ellipsis counts toward the fixed cap, preserving the fact of truncation without exceeding the caller's field budget.</en>
            // </lang>
            return sanitized.Substring(0, Math.Max(0, maximumLength - 3)) + "...";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用固定占位替换正则匹配到的敏感赋值，同时保留键名和分隔符。</zh-CN>
        ///   <en>Replaces a regex-matched sensitive assignment with a fixed placeholder while retaining its key and separator.</en>
        /// </lang>
        /// </summary>
        /// <param name="match">
        /// <l>
        ///   <zh-CN>包含 <c>key</c>、<c>separator</c> 和可选 <c>value</c> 捕获组的匹配结果。</zh-CN>
        ///   <en>Match result containing the <c>key</c>, <c>separator</c>, and optional <c>value</c> capture groups.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>保留结构但不含原始敏感值的文本片段。</zh-CN>
        ///   <en>A structurally recognizable fragment that contains no original sensitive value.</en>
        /// </l>
        /// </returns>
        private static string ReplaceAssignment(Match match)
        {
            // <lang>
            //   <zh-CN>保留匹配键和分隔符以维持可诊断结构，但无条件替换整个观察到的值。</zh-CN>
            //   <en>Preserve the matched key and separator for diagnostic structure while unconditionally replacing the entire observed value.</en>
            // </lang>
            return match.Groups["key"].Value + match.Groups["separator"].Value + "[REDACTED]";
        }
    }
}
