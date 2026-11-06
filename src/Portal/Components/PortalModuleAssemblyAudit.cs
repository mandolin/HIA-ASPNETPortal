namespace ASPNET.StarterKit.Portal
{
    using System;
    using System.Web;

    /// <summary>
    /// <lang>
    ///   <zh-CN>模块装配态审计的运行时写入包装（承载于 Web 项目，依赖 PortalOperationAudit）。summary 应为
    ///   PortalModuleAssemblyAuditFormatter 的产物（已确保 ≤500 且不截断）。本类不读取台账、不改变装配态、不触发编译。</zh-CN>
    ///   <en>Runtime write wrapper for module assembly-state audits (lives in the Web project, depends on PortalOperationAudit). summary should be
    ///   the output of PortalModuleAssemblyAuditFormatter (already guaranteed ≤500 and non-truncated). This class reads no ledger, changes no
    ///   assembly mode, and triggers no compilation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>写入经由 PortalOperationAudit.Record，本身为 best-effort（失败只记诊断、不冒泡）。审计只用于追溯，装配态的唯一事实源仍是台账（定稿 §二）。
    ///   与离线工具 Invoke-PortalModuleAssemblyCheck.ps1 的关系：工具生成可审阅 SQL 产物而不连库；本包装是「审批通过后由运行时记录」的同等能力入口。</zh-CN>
    ///   <en>Writes go through PortalOperationAudit.Record, which is best-effort (failures only log diagnostics, never bubble). Audit is only for traceability;
    ///   the ledger remains the single source of truth for assembly mode (ruling §2). Relationship with the offline tool Invoke-PortalModuleAssemblyCheck.ps1:
    ///   the tool generates a reviewable SQL artifact without connecting to a database; this wrapper is the equivalent runtime entry point for "record after approval".</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleAssemblyAudit
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>记录一次装配态离线校验（best-effort）。summary 应为 PortalModuleAssemblyAuditFormatter 的产物。</zh-CN>
        ///   <en>Records one offline assembly-state check (best-effort). summary should be the output of PortalModuleAssemblyAuditFormatter.</en>
        /// </lang>
        /// </summary>
        public static void RecordCheck(string targetId, string summary, string actor, HttpContext context = null)
        {
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.ModuleAssemblyCategory,
                PortalOperationAuditEvents.ModuleAssemblyStateCheck,
                PortalOperationAuditEvents.ModuleAssemblyStateTargetType,
                targetId,
                summary,
                context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录一次装配态回滚演练（best-effort）。summary 应为 PortalModuleAssemblyAuditFormatter 的产物；演练结论只证明快照可还原为零残留，不表示真实回滚发生。</zh-CN>
        ///   <en>Records one assembly-state rollback drill (best-effort). summary should be the output of PortalModuleAssemblyAuditFormatter; the drill conclusion only proves the snapshot restores to zero residue, never that a real rollback occurred.</en>
        /// </lang>
        /// </summary>
        public static void RecordRollbackDrill(string targetId, string summary, string actor, HttpContext context = null)
        {
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.ModuleAssemblyCategory,
                PortalOperationAuditEvents.ModuleAssemblyRollbackDrill,
                PortalOperationAuditEvents.ModuleAssemblyStateTargetType,
                targetId,
                summary,
                context);
        }
    }
}
