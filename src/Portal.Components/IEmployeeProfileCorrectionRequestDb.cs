using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求业务模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for the employee-profile correction-request business module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P6.4.3 第一版只支持字段级文本更正请求和最小管理员处理状态，不直接修改员工主数据。</zh-CN>
    ///   <en>The first P6.4.3 version supports only field-level text correction requests and minimal administrator review states; it does not directly update employee master data.</en>
    /// </lang>
    /// </remarks>
    public interface IEmployeeProfileCorrectionRequestDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查更正请求表和依赖的员工绑定基础表是否可用。</zh-CN>
        ///   <en>Checks whether the correction-request table and required employee-binding foundation tables are available.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>相关表可用时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the related tables are available.</en>
        /// </l>
        /// </returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定门户用户当前可提交更正请求的员工资料。</zh-CN>
        ///   <en>Reads the employee profile for which the specified Portal user may submit a correction request.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前资料视图；缺表、未绑定或非 Active 状态时为空。</zh-CN>
        ///   <en>Current profile view, or null when schema is missing, no active binding exists, or the employee is not active.</en>
        /// </l>
        /// </returns>
        EmployeeProfileCorrectionProfileView GetCurrentProfileForUser(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定门户用户最近提交的更正请求。</zh-CN>
        ///   <en>Reads recent correction requests submitted by the specified Portal user.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>最多返回条数。</zh-CN>
        ///   <en>Maximum number of rows to return.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求列表。</zh-CN>
        ///   <en>Request list.</en>
        /// </l>
        /// </returns>
        IList<EmployeeProfileCorrectionRequestInfo> GetRecentRequestsForUser(int userId, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交一条员工资料更正请求。</zh-CN>
        ///   <en>Submits one employee-profile correction request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>提交请求。</zh-CN>
        ///   <en>Submission request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果。</zh-CN>
        ///   <en>Write result.</en>
        /// </l>
        /// </returns>
        EmployeeProfileCorrectionRequestResult SubmitRequest(EmployeeProfileCorrectionSubmitRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按状态读取后台处理列表。</zh-CN>
        ///   <en>Reads an administrator review list by status.</en>
        /// </lang>
        /// </summary>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>状态筛选；空值表示全部。</zh-CN>
        ///   <en>Status filter; empty means all statuses.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>最多返回条数。</zh-CN>
        ///   <en>Maximum number of rows to return.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求列表。</zh-CN>
        ///   <en>Request list.</en>
        /// </l>
        /// </returns>
        IList<EmployeeProfileCorrectionRequestInfo> GetAdminRequests(string status, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员更新更正请求处理状态。</zh-CN>
        ///   <en>Updates the administrator review status of a correction request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>处理请求。</zh-CN>
        ///   <en>Review request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>处理结果。</zh-CN>
        ///   <en>Review result.</en>
        /// </l>
        /// </returns>
        EmployeeProfileCorrectionRequestResult ReviewRequest(EmployeeProfileCorrectionReviewRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>撤回本人提出的、尚在处理中的更正请求。</zh-CN>
        ///   <en>Withdraws a still-pending correction request submitted by the requesting user.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>W69 新增。请求表状态受检查约束限定，因此撤回复用 <c>Closed</c> 并把提交人记为操作人、在备注留痕，不引入新状态也不需要迁移。契约按 fail-closed 处理：用户标识或请求标识非正值、请求不存在、归属不符、状态已不是处理中，以及任何运行失败，都返回失败且不改变任何行；失败说明保持低敏，不回显他人请求细节。</zh-CN>
        ///   <en>Added in W69. Because the request table constrains status values, withdrawal reuses <c>Closed</c>, records the requester as the actor, and leaves a note; no new status and no migration are needed. The contract is fail-closed: a non-positive user or request identifier, a missing request, an ownership mismatch, a status that is no longer pending, and any runtime failure all return a failure without changing any row. Failure explanations stay low-sensitivity and never echo another user's request details.</en>
        /// </lang>
        /// </remarks>
        /// <param name="userId"><l><zh-CN>当前门户用户标识。</zh-CN><en>The current Portal user identifier.</en></l></param>
        /// <param name="requestId"><l><zh-CN>更正请求标识。</zh-CN><en>The correction request identifier.</en></l></param>
        /// <param name="actorName"><l><zh-CN>操作人账号名或系统标识；空白时实现使用 <c>system</c>。</zh-CN><en>Actor account name or system identifier; implementations use <c>system</c> when blank.</en></l></param>
        /// <returns><l><zh-CN>撤回结果。</zh-CN><en>The withdrawal result.</en></l></returns>
        EmployeeProfileCorrectionRequestResult CancelOwnRequest(int userId, long requestId, string actorName);
    }
}
