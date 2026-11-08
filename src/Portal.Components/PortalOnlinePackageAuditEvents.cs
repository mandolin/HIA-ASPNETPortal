namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包审计的稳定事件词表：分类、动作、目标类型与结果值。写入统一经既有
    ///   `PortalOperationAudit.Record` 落入 `PortalCfg_OperationAudits`，因此本类只定义键名，不实施写入、不连接数据库。</zh-CN>
    ///   <en>Stable audit event vocabulary for online capability packages: category, actions, target type, and outcome values.
    ///   Writes go through the existing `PortalOperationAudit.Record` into `PortalCfg_OperationAudits`, so this class only
    ///   defines key names; it performs no write and connects to no database.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>放在 `Portal.Components` 而非 Web 项目，是为了让 Web 层、门禁与单测共享**同一份**常量定义，
    ///   避免两份字符串漂移；既有 `PortalOperationAuditEvents` 仍在 Web 项目维护其历史词表，两类互不覆盖。
    ///   **工具不代写**：离线工具只生成可审阅产物，不调用本类写入；审计记录由运行期在审批通过后完成。
    ///   **A 级硬边界**：TargetId 只用服务端生成的隔离暂存名，不用包内声明的包标识、来源或 capability 作为授权或身份依据。</zh-CN>
    ///   <en>It lives in `Portal.Components` rather than the Web project so the Web layer, gates, and unit tests share **one**
    ///   definition and cannot drift across two copies of strings; the existing `PortalOperationAuditEvents` keeps its historical
    ///   vocabulary in the Web project and neither class overrides the other.
    ///   **Tools never write on behalf of runtime**: offline tools only emit reviewable artifacts and never write through this class;
    ///   audit records are produced at runtime after approval.
    ///   **A-level hard boundary**: TargetId uses only the server-generated isolated staging name, never a package-declared id,
    ///   origin, or capability as an authorization or identity basis.</en>
    /// </lang>
    /// </remarks>
    public static class PortalOnlinePackageAuditEvents
    {
        /// <summary><lang><zh-CN>在线能力包分类；与 ModuleAssembly、BusinessModule 等既有分类区分。</zh-CN><en>Online capability package category; distinct from existing categories such as ModuleAssembly and BusinessModule.</en></lang></summary>
        public const string Category = "OnlinePackage";

        /// <summary><lang><zh-CN>上传并通过隔离校验（第一步），记录六要素但不表示已安装。</zh-CN><en>Upload passed isolated validation (step one); records the six elements but never means installed.</en></lang></summary>
        public const string UploadValidated = "OnlinePackageUploadValidated";

        /// <summary><lang><zh-CN>双步确认通过（第二步），此时才可由运行期执行安装。</zh-CN><en>Two-step confirmation passed (step two); only now may runtime perform installation.</en></lang></summary>
        public const string InstallConfirmed = "OnlinePackageInstallConfirmed";

        /// <summary><lang><zh-CN>隔离暂存包被放弃或过期清理；不表示安装发生。</zh-CN><en>An isolated staged package was discarded or purged on expiry; does not mean installation occurred.</en></lang></summary>
        public const string StagedPackageDiscarded = "OnlinePackageStagedDiscarded";

        /// <summary><lang><zh-CN>安装/上传失败后的回滚演练结论；演练不表示真实回滚发生。</zh-CN><en>Rollback-drill conclusion after an install/upload failure; a drill never means a real rollback occurred.</en></lang></summary>
        public const string RollbackDrill = "OnlinePackageRollbackDrill";

        /// <summary><lang><zh-CN>审计目标类型：隔离暂存区内的一个在线能力包。</zh-CN><en>Audit target type: one online capability package in the isolated staging area.</en></lang></summary>
        public const string TargetType = "OnlinePackageStaging";

        /// <summary><lang><zh-CN>成功；与 PortalCfg_OperationAudits 的 Outcome 白名单一致。</zh-CN><en>Success; matches the Outcome whitelist of PortalCfg_OperationAudits.</en></lang></summary>
        public const string OutcomeSuccess = "Success";

        /// <summary><lang><zh-CN>失败；校验拒绝、二次鉴权拒绝或安装失败时如实记录，不伪造成功。</zh-CN><en>Failure; recorded honestly when validation, re-authorization, or installation fails, never faked as success.</en></lang></summary>
        public const string OutcomeFailure = "Failure";

        /// <summary><lang><zh-CN>跳过；动作因开关关闭或前置条件未满足而未执行。</zh-CN><en>Skipped; the action did not run because a switch was off or a precondition was unmet.</en></lang></summary>
        public const string OutcomeSkipped = "Skipped";
    }
}
