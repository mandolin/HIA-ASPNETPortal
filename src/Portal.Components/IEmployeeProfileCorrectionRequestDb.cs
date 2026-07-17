using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料更正请求业务模块的数据访问契约。
    ///
    /// English: Data-access contract for the employee-profile correction-request business module.
    /// </summary>
    /// <remarks>
    /// 中文：P6.4.3 第一版只支持字段级文本更正请求和最小管理员处理状态，不直接修改员工主数据。
    ///
    /// English: The first P6.4.3 version supports only field-level text correction requests and minimal
    /// administrator review states; it does not directly update employee master data.
    /// </remarks>
    public interface IEmployeeProfileCorrectionRequestDb
    {
        /// <summary>
        /// 中文：检查更正请求表和依赖的员工绑定基础表是否可用。
        ///
        /// English: Checks whether the correction-request table and required employee-binding foundation tables are available.
        /// </summary>
        /// <returns>中文：相关表可用时为 <c>true</c>。English: <c>true</c> when the related tables are available.</returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// 中文：读取指定门户用户当前可提交更正请求的员工资料。
        ///
        /// English: Reads the employee profile for which the specified Portal user may submit a correction request.
        /// </summary>
        /// <param name="userId">中文：门户用户标识。English: Portal user identifier.</param>
        /// <returns>中文：当前资料视图；缺表、未绑定或非 Active 状态时为空。English: Current profile view, or null when schema is missing, no active binding exists, or the employee is not active.</returns>
        EmployeeProfileCorrectionProfileView GetCurrentProfileForUser(int userId);

        /// <summary>
        /// 中文：读取指定门户用户最近提交的更正请求。
        ///
        /// English: Reads recent correction requests submitted by the specified Portal user.
        /// </summary>
        /// <param name="userId">中文：门户用户标识。English: Portal user identifier.</param>
        /// <param name="take">中文：最多返回条数。English: Maximum number of rows to return.</param>
        /// <returns>中文：请求列表。English: Request list.</returns>
        IList<EmployeeProfileCorrectionRequestInfo> GetRecentRequestsForUser(int userId, int take);

        /// <summary>
        /// 中文：提交一条员工资料更正请求。
        ///
        /// English: Submits one employee-profile correction request.
        /// </summary>
        /// <param name="request">中文：提交请求。English: Submission request.</param>
        /// <returns>中文：写入结果。English: Write result.</returns>
        EmployeeProfileCorrectionRequestResult SubmitRequest(EmployeeProfileCorrectionSubmitRequest request);

        /// <summary>
        /// 中文：按状态读取后台处理列表。
        ///
        /// English: Reads an administrator review list by status.
        /// </summary>
        /// <param name="status">中文：状态筛选；空值表示全部。English: Status filter; empty means all statuses.</param>
        /// <param name="take">中文：最多返回条数。English: Maximum number of rows to return.</param>
        /// <returns>中文：请求列表。English: Request list.</returns>
        IList<EmployeeProfileCorrectionRequestInfo> GetAdminRequests(string status, int take);

        /// <summary>
        /// 中文：管理员更新更正请求处理状态。
        ///
        /// English: Updates the administrator review status of a correction request.
        /// </summary>
        /// <param name="request">中文：处理请求。English: Review request.</param>
        /// <returns>中文：处理结果。English: Review result.</returns>
        EmployeeProfileCorrectionRequestResult ReviewRequest(EmployeeProfileCorrectionReviewRequest request);
    }
}
