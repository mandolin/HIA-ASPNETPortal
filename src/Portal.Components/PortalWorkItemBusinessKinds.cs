namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量审批工作项支持的稳定业务对象类型。</zh-CN>
    ///   <en>Stable business-object kinds supported by lightweight work items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些字符串会写入审批工作项表、审计记录和诊断日志，用来把通用工作项关联回具体业务对象。新增业务对象类型时应只追加新常量，并同步迁移脚本、权限说明和业务模块文档。</zh-CN>
    ///   <en>These strings are persisted in work-item tables, audit records, and diagnostic logs so generic work items can be linked back to concrete business objects. New business-object kinds should be added as new constants and mirrored in migrations, permission notes, and module documentation.</en>
    /// </lang>
    /// </remarks>
    public static class PortalWorkItemBusinessKinds
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料更正请求业务对象。</zh-CN>
        ///   <en>Employee-profile correction request business object.</en>
        /// </lang>
        /// </summary>
        public const string EmployeeProfileCorrectionRequest = "EmployeeProfileCorrectionRequest";

        /// <summary>
        /// <lang>
        ///   <zh-CN>抽象业务申请业务对象。</zh-CN>
        ///   <en>Abstract business-application business object.</en>
        /// </lang>
        /// </summary>
        public const string BusinessApplication = "BusinessApplication";

        /// <summary>
        /// <lang>
        ///   <zh-CN>企业协同事项业务对象。</zh-CN>
        ///   <en>Enterprise collaboration-item business object.</en>
        /// </lang>
        /// </summary>
        public const string CollaborationItem = "CollaborationItem";
    }
}
