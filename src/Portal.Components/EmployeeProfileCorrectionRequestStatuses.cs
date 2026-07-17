namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料更正请求的稳定状态值。
    ///
    /// English: Stable status values for employee-profile correction requests.
    /// </summary>
    /// <remarks>
    /// 中文：P6.4.3 第一版只表达最小状态流，不引入复杂审批引擎。
    ///
    /// English: The first P6.4.3 version expresses only a minimal status flow and does not introduce a complex workflow engine.
    /// </remarks>
    public static class EmployeeProfileCorrectionRequestStatuses
    {
        /// <summary>中文：员工已提交，等待管理员查看。English: Submitted by the employee and awaiting administrator review.</summary>
        public const string Submitted = "Submitted";

        /// <summary>中文：管理员已查看并记录处理意见。English: Reviewed by an administrator with a handling note.</summary>
        public const string Reviewed = "Reviewed";

        /// <summary>中文：请求已关闭。English: The request has been closed.</summary>
        public const string Closed = "Closed";

        /// <summary>中文：请求已拒绝。English: The request has been rejected.</summary>
        public const string Rejected = "Rejected";
    }
}
