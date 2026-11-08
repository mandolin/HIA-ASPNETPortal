namespace ASPNET.StarterKit.Portal
{
    using System;
    using System.Web;

    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包审计的运行时写入包装（承载于 Web 项目，依赖 `PortalOperationAudit`）。
    ///   `summary` 应为 `PortalOnlinePackageAuditFormatter` 的产物（已确保 ≤500 且不截断）。
    ///   本类不读取隔离暂存文件、不改变装配态、不执行安装、不生成 SQL。</zh-CN>
    ///   <en>Runtime write wrapper for online capability package audits (lives in the Web project, depends on `PortalOperationAudit`).
    ///   `summary` should be the output of `PortalOnlinePackageAuditFormatter` (already guaranteed ≤500 and non-truncated).
    ///   This class reads no isolated staging file, changes no assembly mode, performs no installation, and generates no SQL.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>**工具不代写**：离线工具只生成可审阅产物，绝不调用本类；审计记录由运行期在审批通过后写入。
    ///   写入经 `PortalOperationAudit.Record`，为 best-effort（失败只记诊断、不冒泡），因此审计只用于追溯，
    ///   **不是**安装是否发生的授权或事实依据。动作与结果值在调用前由 Components 层词表校验，未知值直接拒绝写入。
    ///   **A 级硬边界**：`targetId` 只用服务端生成的隔离暂存名，不用包内声明标识、来源或 capability 作为身份/授权依据；
    ///   摘要不含一次性票据 nonce、防伪令牌、口令或连接串。</zh-CN>
    ///   <en>**Tools never write on behalf of runtime**: offline tools emit reviewable artifacts only and never call this class;
    ///   audit records are written at runtime after approval. Writes go through `PortalOperationAudit.Record` and are best-effort
    ///   (failures only log diagnostics, never bubble), so audit serves traceability and is **not** proof or authorization that
    ///   an installation occurred. Actions and outcomes are validated against the Components-layer vocabulary before the call;
    ///   unknown values are refused outright.
    ///   **A-level hard boundary**: `targetId` uses only the server-generated isolated staging name, never a package-declared id,
    ///   origin, or capability as an identity/authorization basis; summaries contain no ticket nonce, anti-forgery token, password,
    ///   or connection string.</en>
    /// </lang>
    /// </remarks>
    public static class PortalOnlinePackageAudit
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>记录一次在线能力包动作（best-effort）。动作与结果须为已登记常量，`summary` 须为格式器产物。</zh-CN>
        ///   <en>Records one online capability package action (best-effort). Action and outcome must be registered constants, and `summary` must come from the formatter.</en>
        /// </lang>
        /// </summary>
        /// <param name="action"><l zh-CN="PortalOnlinePackageAuditEvents 中的动作常量。" en="Action constant from PortalOnlinePackageAuditEvents." /></param>
        /// <param name="targetId"><l zh-CN="服务端生成的隔离暂存名。" en="Server-generated isolated staging name." /></param>
        /// <param name="summary"><l zh-CN="六要素紧凑摘要，≤500 字符。" en="Six-element compact summary, ≤500 characters." /></param>
        /// <param name="outcome"><l zh-CN="Success/Failure/Skipped；失败须如实传 Failure，不伪造成功。" en="Success/Failure/Skipped; failures must pass Failure honestly, never faked as success." /></param>
        /// <param name="context"><l zh-CN="可选 HTTP 上下文，供动作人与请求元数据的受限读取。" en="Optional HTTP context for restricted actor/request metadata reads." /></param>
        /// <returns><l zh-CN="参数合法且已提交 best-effort 写入时为 true；参数不合法而未写入时为 false。" en="True when parameters are valid and a best-effort write was submitted; false when invalid and nothing was written." /></returns>
        public static bool TryRecord(
            string action,
            string targetId,
            string summary,
            string outcome,
            HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>动作必须是封闭词表成员：拒绝自由文本进入 Action 列，也防止用装配态动作冒充在线包动作。</zh-CN>
            //   <en>The action must belong to the closed vocabulary: this keeps free text out of the Action column and stops assembly-state actions from impersonating online-package actions.</en>
            // </lang>
            if (!PortalOnlinePackageAuditFormatter.IsRegisteredAction(action))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>结果值必须符合表 CHECK 白名单；未知值提前拒绝，避免 INSERT 失败被 best-effort 静默吞掉而丢失审计。</zh-CN>
            //   <en>The outcome must satisfy the table CHECK whitelist; unknown values are refused early so a failed INSERT is not silently swallowed and the audit lost.</en>
            // </lang>
            if (!PortalOnlinePackageAuditFormatter.IsRegisteredOutcome(outcome))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>目标标识与摘要不得缺失；摘要长度以格式器上限为准，超长直接拒绝而不截断。</zh-CN>
            //   <en>Target id and summary must be present; summary length follows the formatter cap and overflow is refused rather than truncated.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(targetId) || string.IsNullOrEmpty(summary) ||
                summary.Length > PortalOnlinePackageAuditFormatter.MaxSummaryLength)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>经既有统一入口写入；relatedEventId 传 null，outcome 显式传入以保留真实失败事实。
            //   本调用是 best-effort，异常由 PortalOperationAudit 内部降级处理。</zh-CN>
            //   <en>Write through the existing unified entry point; relatedEventId is null and outcome is passed explicitly to preserve genuine failure facts.
            //   The call is best-effort, and exceptions are degraded inside PortalOperationAudit.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOnlinePackageAuditEvents.Category,
                action,
                PortalOnlinePackageAuditEvents.TargetType,
                targetId,
                summary,
                context,
                null,
                outcome);

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录上传并通过隔离校验（第一步）；六要素由格式器生成，超长时抛
        ///   <c>AuditSummaryTooLongException</c>，不静默截断。</zh-CN>
        ///   <en>Records an upload that passed isolated validation (step one); the six elements come from the formatter, and overflow throws
        ///   `AuditSummaryTooLongException` rather than truncating silently.</en>
        /// </lang>
        /// </summary>
        /// <param name="targetId"><l zh-CN="服务端生成的隔离暂存名。" en="Server-generated isolated staging name." /></param>
        /// <param name="stagingName"><l zh-CN="进入摘要 pkg 的同一暂存名。" en="The same staging name used as pkg in the summary." /></param>
        /// <param name="packageHash"><l zh-CN="隔离暂存包 SHA-256 十六进制摘要。" en="SHA-256 hex digest of the isolated staged package." /></param>
        /// <param name="actor"><l zh-CN="已认证账号名。" en="Authenticated account name." /></param>
        /// <param name="startUtc"><l zh-CN="动作起始 UTC 时间。" en="Action start UTC time." /></param>
        /// <param name="endUtc"><l zh-CN="动作结束 UTC 时间。" en="Action end UTC time." /></param>
        /// <param name="verification"><l zh-CN="校验结论短语。" en="Verification conclusion phrase." /></param>
        /// <param name="context"><l zh-CN="可选 HTTP 上下文。" en="Optional HTTP context." /></param>
        /// <returns><l zh-CN="已提交 best-effort 写入时为 true。" en="True when a best-effort write was submitted." /></returns>
        public static bool RecordUploadValidated(
            string targetId,
            string stagingName,
            string packageHash,
            string actor,
            DateTime startUtc,
            DateTime endUtc,
            string verification,
            HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>上传前没有旧哈希，hashBefore 固定为空串；六要素仍完整保留该键位。</zh-CN>
            //   <en>An upload has no prior hash, so hashBefore is a fixed empty string while the key position stays part of the six elements.</en>
            // </lang>
            string summary = PortalOnlinePackageAuditFormatter.FormatSixElementSummary(
                stagingName, string.Empty, packageHash, actor, startUtc, endUtc, verification,
                PortalOnlinePackageAuditEvents.UploadValidated);

            return TryRecord(
                PortalOnlinePackageAuditEvents.UploadValidated, targetId, summary,
                PortalOnlinePackageAuditEvents.OutcomeSuccess, context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录双步确认通过（第二步）；此时才允许运行期执行安装，审计不代替安装动作。</zh-CN>
        ///   <en>Records a passed two-step confirmation (step two); only now may runtime install, and audit never substitutes for installation.</en>
        /// </lang>
        /// </summary>
        /// <param name="targetId"><l zh-CN="服务端生成的隔离暂存名。" en="Server-generated isolated staging name." /></param>
        /// <param name="stagingName"><l zh-CN="进入摘要 pkg 的同一暂存名。" en="The same staging name used as pkg in the summary." /></param>
        /// <param name="packageHash"><l zh-CN="确认时重算的暂存包哈希（与票据绑定一致）。" en="Staged package hash recomputed at confirmation (matching the ticket binding)." /></param>
        /// <param name="actor"><l zh-CN="已认证账号名。" en="Authenticated account name." /></param>
        /// <param name="startUtc"><l zh-CN="动作起始 UTC 时间。" en="Action start UTC time." /></param>
        /// <param name="endUtc"><l zh-CN="动作结束 UTC 时间。" en="Action end UTC time." /></param>
        /// <param name="verification"><l zh-CN="校验结论短语。" en="Verification conclusion phrase." /></param>
        /// <param name="context"><l zh-CN="可选 HTTP 上下文。" en="Optional HTTP context." /></param>
        /// <returns><l zh-CN="已提交 best-effort 写入时为 true。" en="True when a best-effort write was submitted." /></returns>
        public static bool RecordInstallConfirmed(
            string targetId,
            string stagingName,
            string packageHash,
            string actor,
            DateTime startUtc,
            DateTime endUtc,
            string verification,
            HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>确认前后包内容未变，两个哈希位取同一值，明确「未替换暂存包」。</zh-CN>
            //   <en>Package content is unchanged before and after confirmation, so both hash positions carry the same value, making "no staged package substitution" explicit.</en>
            // </lang>
            string summary = PortalOnlinePackageAuditFormatter.FormatSixElementSummary(
                stagingName, packageHash, packageHash, actor, startUtc, endUtc, verification,
                PortalOnlinePackageAuditEvents.InstallConfirmed);

            return TryRecord(
                PortalOnlinePackageAuditEvents.InstallConfirmed, targetId, summary,
                PortalOnlinePackageAuditEvents.OutcomeSuccess, context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录隔离暂存包被放弃/过期清理，或安装失败的事实；`outcome` 由调用方如实传入，不伪造成功。</zh-CN>
        ///   <en>Records that an isolated staged package was discarded/purged, or that an install failed; callers pass `outcome` honestly, never faking success.</en>
        /// </lang>
        /// </summary>
        /// <param name="action"><l zh-CN="已登记动作（放弃清理或回滚演练）。" en="Registered action (discard/purge or rollback drill)." /></param>
        /// <param name="targetId"><l zh-CN="服务端生成的隔离暂存名。" en="Server-generated isolated staging name." /></param>
        /// <param name="stagingName"><l zh-CN="进入摘要 pkg 的同一暂存名。" en="The same staging name used as pkg in the summary." /></param>
        /// <param name="packageHash"><l zh-CN="暂存包哈希。" en="Staged package hash." /></param>
        /// <param name="actor"><l zh-CN="已认证账号名。" en="Authenticated account name." /></param>
        /// <param name="startUtc"><l zh-CN="动作起始 UTC 时间。" en="Action start UTC time." /></param>
        /// <param name="endUtc"><l zh-CN="动作结束 UTC 时间。" en="Action end UTC time." /></param>
        /// <param name="verification"><l zh-CN="结论短语。" en="Conclusion phrase." /></param>
        /// <param name="outcome"><l zh-CN="Success/Failure/Skipped；失败须如实传 Failure。" en="Success/Failure/Skipped; failures must pass Failure honestly." /></param>
        /// <param name="context"><l zh-CN="可选 HTTP 上下文。" en="Optional HTTP context." /></param>
        /// <returns><l zh-CN="已提交 best-effort 写入时为 true。" en="True when a best-effort write was submitted." /></returns>
        public static bool RecordFailureOrCleanup(
            string action,
            string targetId,
            string stagingName,
            string packageHash,
            string actor,
            DateTime startUtc,
            DateTime endUtc,
            string verification,
            string outcome,
            HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>不允许用本包装记录「安装已确认」动作，避免绕过专用的二次确认记录路径。</zh-CN>
            //   <en>This wrapper refuses to record "install confirmed" so the dedicated two-step confirmation path cannot be bypassed.</en>
            // </lang>
            if (string.Equals(action, PortalOnlinePackageAuditEvents.InstallConfirmed, StringComparison.Ordinal) ||
                string.Equals(action, PortalOnlinePackageAuditEvents.UploadValidated, StringComparison.Ordinal))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>动作与结果先经 Components 层词表校验，再生成六要素摘要；任一不合法即不写入。</zh-CN>
            //   <en>Validate action and outcome against the Components vocabulary before formatting; any invalid value writes nothing.</en>
            // </lang>
            if (!PortalOnlinePackageAuditFormatter.IsRegisteredAction(action) ||
                !PortalOnlinePackageAuditFormatter.IsRegisteredOutcome(outcome))
            {
                return false;
            }

            string summary = PortalOnlinePackageAuditFormatter.FormatSixElementSummary(
                stagingName, packageHash, packageHash, actor, startUtc, endUtc, verification, action);

            return TryRecord(action, targetId, summary, outcome, context);
        }
    }
}
