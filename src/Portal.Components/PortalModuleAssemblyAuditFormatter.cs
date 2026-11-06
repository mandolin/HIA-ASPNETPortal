namespace ASPNET.StarterKit.Portal
{
    using System;
    using System.Globalization;

    /// <summary>
    /// <lang>
    ///   <zh-CN>模块装配态审计载荷的六要素结构化紧凑格式器（纯函数，不依赖 Web 上下文、不读取台账、不改变装配态）。
    ///   格式与离线工具 Invoke-PortalModuleAssemblyCheck.ps1 使用同一字段顺序，确保两份产物可互相印证。目的载体是 PortalCfg_OperationAudits 的 Summary 列。</zh-CN>
    ///   <en>Structured compact formatter for the six-element module-assembly-state audit payload (pure function; no Web context, no ledger read, no assembly-mode change).
    ///   Field order matches the offline tool Invoke-PortalModuleAssemblyCheck.ps1 so the two artifacts corroborate each other. The carrier is the Summary column of PortalCfg_OperationAudits.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>超长时抛 AuditSummaryTooLongException（fail-closed），绝不静默截断导致六要素缺失；调用方须据此走审计表扩展方案。本类只负责格式化，不负责写入——写入由 Web 项目中的 PortalModuleAssemblyAudit 包装调用 PortalOperationAudit.Record。</zh-CN>
    ///   <en>On overflow it throws AuditSummaryTooLongException (fail-closed) rather than silently truncating and dropping elements; callers must take the audit-table extension path. This class only formats; writing is done by the PortalModuleAssemblyAudit wrapper in the Web project via PortalOperationAudit.Record.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleAssemblyAuditFormatter
    {
        /// <summary><lang><zh-CN>Summary 列的最大字符数（与 PortalCfg_OperationAudits 列定义一致）。</zh-CN><en>Maximum character count of the Summary column (matches PortalCfg_OperationAudits).</en></lang></summary>
        public const int MaxSummaryLength = 500;

        /// <summary>
        /// <lang>
        ///   <zh-CN>六要素结构化紧凑格式：固定键序、分号分隔、不写源文件路径、TargetId 以 packageId@version 表达；超过 MaxSummaryLength 时抛出
        ///   <c>AuditSummaryTooLongException</c>，绝不静默截断。字段顺序与离线工具完全一致。</zh-CN>
        ///   <en>Structured compact format of the six elements: fixed key order, semicolon separated, no source paths, TargetId as packageId@version;
        ///   when exceeding MaxSummaryLength it throws AuditSummaryTooLongException rather than silently truncating. Field order matches the offline tool exactly.</en>
        /// </lang>
        /// </summary>
        public static string FormatSixElementSummary(
            string packageId,
            string version,
            string hashBefore,
            string hashAfter,
            string actor,
            DateTime startUtc,
            DateTime endUtc,
            string verification,
            string direction)
        {
            // <lang>
            //   <zh-CN>TargetId 用 packageId@version；version 为空以 unknown 表达，避免装配态事实被省略而静默丢要素。</zh-CN>
            //   <en>TargetId uses packageId@version; a blank version becomes unknown so the assembly-state fact is never omitted and silently loses an element.</en>
            // </lang>
            string targetId = string.IsNullOrEmpty(version) ? (packageId + "@unknown") : (packageId + "@" + version);
            string summary = string.Format(
                CultureInfo.InvariantCulture,
                "pkg={0};hashBefore={1};hashAfter={2};actor={3};startUtc={4};endUtc={5};verification={6};direction={7}",
                targetId,
                hashBefore ?? string.Empty,
                hashAfter ?? string.Empty,
                actor ?? string.Empty,
                startUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                endUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                verification ?? string.Empty,
                direction ?? string.Empty);

            // <lang>
            //   <zh-CN>超长即失败并显式抛出，让调用方决定走审计表扩展方案；不截断、不丢弃任何要素。</zh-CN>
            //   <en>Overflow fails explicitly so the caller can choose the audit-table extension path; no element is truncated or dropped.</en>
            // </lang>
            if (summary.Length > MaxSummaryLength)
            {
                throw new AuditSummaryTooLongException(summary.Length, MaxSummaryLength);
            }

            return summary;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试格式化；超长时不抛异常而返回 success=false（summary 为 null），供需要走审计表扩展方案的调用方使用。字段顺序与 FormatSixElementSummary 一致。</zh-CN>
        ///   <en>Attempts formatting; on overflow returns success=false (summary null) instead of throwing, for callers that will take the audit-table extension path. Field order matches FormatSixElementSummary.</en>
        /// </lang>
        /// </summary>
        public static bool TryFormatSixElementSummary(
            string packageId,
            string version,
            string hashBefore,
            string hashAfter,
            string actor,
            DateTime startUtc,
            DateTime endUtc,
            string verification,
            string direction,
            out string summary)
        {
            summary = null;
            string targetId = string.IsNullOrEmpty(version) ? (packageId + "@unknown") : (packageId + "@" + version);
            string candidate = string.Format(
                CultureInfo.InvariantCulture,
                "pkg={0};hashBefore={1};hashAfter={2};actor={3};startUtc={4};endUtc={5};verification={6};direction={7}",
                targetId,
                hashBefore ?? string.Empty,
                hashAfter ?? string.Empty,
                actor ?? string.Empty,
                startUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                endUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                verification ?? string.Empty,
                direction ?? string.Empty);
            if (candidate.Length > MaxSummaryLength)
            {
                return false;
            }

            summary = candidate;
            return true;
        }
    }
}
