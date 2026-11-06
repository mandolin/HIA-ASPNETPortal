namespace ASPNET.StarterKit.Portal
{
    using System;

    /// <summary>
    /// <lang>
    ///   <zh-CN>装配态审计摘要超过最大长度时抛出；用于阻断「静默截断导致六要素缺失」，迫使调用方改用审计表扩展方案（独立明细表）而非丢弃任何要素。</zh-CN>
    ///   <en>Thrown when an assembly-state audit summary exceeds the maximum length; it blocks silent truncation that would drop six-element data and forces the caller to take the audit-table extension path (a separate detail table) instead of dropping any element.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>定稿 §三.2 要求六要素不得静默截断：当结构化紧凑格式超过 Summary 的 500 字符上限时，抛此异常比依赖数据库截断更诚实——调用方据此知道必须走扩展表，而不是收到一条缺要素的记录。</zh-CN>
    ///   <en>Ruling §3.2 forbids silently truncating the six elements: when the compact format exceeds the Summary 500-character limit, throwing is more honest than relying on database truncation, because it tells the caller to take the extension-table path rather than accept a record missing elements.</en>
    /// </lang>
    /// </remarks>
    public class AuditSummaryTooLongException : Exception
    {
        /// <summary><lang><zh-CN>实际长度。</zh-CN><en>Actual length.</en></lang></summary>
        public int ActualLength { get; private set; }

        /// <summary><lang><zh-CN>允许的最大长度。</zh-CN><en>Allowed maximum length.</en></lang></summary>
        public int MaxLength { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建异常并携带实际与最大长度。</zh-CN>
        ///   <en>Creates the exception carrying the actual and maximum lengths.</en>
        /// </lang>
        /// </summary>
        public AuditSummaryTooLongException(int actualLength, int maxLength)
            : base("Assembly-state audit summary length " + actualLength.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   " exceeds maximum " + maxLength.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   "; use the audit-table extension path instead of truncating.")
        {
            ActualLength = actualLength;
            MaxLength = maxLength;
        }
    }
}
