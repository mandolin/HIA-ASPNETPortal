using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：管理员处理员工资料更正请求的参数。
    ///
    /// English: Parameters for an administrator to review an employee-profile correction request.
    /// </summary>
    public sealed class EmployeeProfileCorrectionReviewRequest
    {
        /// <summary>中文：请求标识。English: Request identifier.</summary>
        public long RequestId { get; set; }

        /// <summary>中文：目标状态。English: Target status.</summary>
        public string RequestStatus { get; set; }

        /// <summary>中文：处理说明。English: Review note.</summary>
        public string ReviewNote { get; set; }

        /// <summary>中文：处理 UTC 时间；为空时由数据层使用当前 UTC。English: Review UTC time; the data layer uses current UTC when empty.</summary>
        public DateTime? ReviewedUtc { get; set; }

        /// <summary>中文：处理人账号名或系统标识。English: Reviewer account name or system identifier.</summary>
        public string ReviewedBy { get; set; }
    }
}
