namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求的稳定状态值。</zh-CN>
    ///   <en>Stable status values for employee-profile correction requests.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些字符串会进入资料更正表、后台筛选、审计事件和工作项摘要，属于持久化契约；新增或改名状态时必须同步数据库脚本、后台页面、审计说明和验收清单。本类只表达门户当前的最小状态流，不替代完整工作流引擎。</zh-CN>
    ///   <en>These strings are written to correction tables, admin filters, audit events, and work-item summaries, so they are a persistence contract; adding or renaming statuses must be synchronized with database scripts, admin pages, audit documentation, and acceptance checks. This class expresses only the portal's current minimal status flow and does not replace a full workflow engine.</en>
    /// </lang>
    /// </remarks>
    public static class EmployeeProfileCorrectionRequestStatuses
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>员工已提交更正请求，管理员尚未处理；这是资料更正请求进入后台队列时的起始状态。</zh-CN>
        ///   <en>The employee has submitted a correction request and administrators have not processed it yet; this is the starting state when the request enters the admin queue.</en>
        /// </lang>
        /// </summary>
        public const string Submitted = "Submitted";

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员已查看请求并记录处理意见；该状态只说明请求已被人工复核，不必然表示员工主档已发生变更。</zh-CN>
        ///   <en>An administrator has reviewed the request and recorded a handling note; this status only means the request was manually reviewed and does not necessarily mean the employee master record changed.</en>
        /// </lang>
        /// </summary>
        public const string Reviewed = "Reviewed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>请求已完结且无需继续处理；列表和审计仍保留该记录作为历史证据。</zh-CN>
        ///   <en>The request has been completed with no further processing required; lists and audits still keep the record as historical evidence.</en>
        /// </lang>
        /// </summary>
        public const string Closed = "Closed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>请求被管理员拒绝；调用方应保留拒绝原因和原始请求内容，便于员工沟通和审计追溯。</zh-CN>
        ///   <en>The request was rejected by an administrator; callers should keep the rejection reason and original request content for employee communication and audit traceability.</en>
        /// </lang>
        /// </summary>
        public const string Rejected = "Rejected";
    }
}
