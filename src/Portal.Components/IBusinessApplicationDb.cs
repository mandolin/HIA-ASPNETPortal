using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>抽象业务申请和轻量 Workflow 事实的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for abstract business applications and lightweight workflow facts.</en>
    /// </lang>
    /// </summary>
    public interface IBusinessApplicationDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查申请表和流程事件表是否已部署。</zh-CN>
        ///   <en>Checks whether the application and workflow-event tables are deployed.</en>
        /// </lang>
        /// </summary>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交一条抽象业务申请。</zh-CN>
        ///   <en>Submits one abstract business application.</en>
        /// </lang>
        /// </summary>
        BusinessApplicationResult SubmitApplication(BusinessApplicationSubmitRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户最近提交的申请。</zh-CN>
        ///   <en>Reads recent applications submitted by the current user.</en>
        /// </lang>
        /// </summary>
        IList<BusinessApplicationInfo> GetRecentApplicationsForUser(int applicantUserId, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台申请列表。</zh-CN>
        ///   <en>Reads the administration application list.</en>
        /// </lang>
        /// </summary>
        IList<BusinessApplicationInfo> GetAdminApplications(string status, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行管理员审核动作。</zh-CN>
        ///   <en>Applies an administrator review action.</en>
        /// </lang>
        /// </summary>
        BusinessApplicationResult ReviewApplication(BusinessApplicationReviewRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请人重提被退回的业务申请，把它送回待审核窗口。</zh-CN>
        ///   <en>Lets the applicant resubmit a returned business application, sending it back into the review window.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>W71 新增。这是**申请人自助动作**，与 <see cref="ReviewApplication"/> 的审核职责分开：只有提交人本人可重提，且只从 <c>Returned</c> 重提到 <c>Submitted</c>；归属与状态条件都写在同一个 UPDATE 语句里，避免读取与写入之间的状态漂移（TOCTOU）。实现还必须同时清空审核时间与审核人，因为表的检查约束要求 <c>Submitted</c> 状态下这两列为空。契约按 fail-closed 处理：标识非正值、申请不存在、归属不符、状态不允许或读取失败，一律返回失败且不改变任何行。</zh-CN>
        ///   <en>Added in W71. This is an applicant self-service action kept apart from the review responsibility of <see cref="ReviewApplication"/>: only the original applicant may resubmit, and only from <c>Returned</c> to <c>Submitted</c>. Both the ownership and status conditions live in a single UPDATE statement to prevent state drift between reading and writing (TOCTOU). An implementation must also clear the review time and reviewer columns because a table check constraint requires both to be null in the <c>Submitted</c> state. The contract is fail-closed: a non-positive identifier, a missing application, an ownership mismatch, a disallowed status, or a read failure all return a failure without changing any row.</en>
        /// </lang>
        /// </remarks>
        /// <param name="applicationId"><l><zh-CN>业务申请标识。</zh-CN><en>The business application identifier.</en></l></param>
        /// <param name="applicantUserId"><l><zh-CN>提交人门户用户标识；必须与该申请的提交人一致。</zh-CN><en>The applicant's Portal user identifier; it must match the application's applicant.</en></l></param>
        /// <param name="actorName"><l><zh-CN>操作人账号名或系统标识；空白时实现使用 <c>system</c>。</zh-CN><en>Actor account name or system identifier; implementations use <c>system</c> when blank.</en></l></param>
        /// <returns><l><zh-CN>重提结果。</zh-CN><en>The resubmission result.</en></l></returns>
        BusinessApplicationResult ResubmitApplication(long applicationId, int applicantUserId, string actorName);
    }
}
