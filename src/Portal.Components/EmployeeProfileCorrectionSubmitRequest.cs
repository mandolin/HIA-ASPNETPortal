using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料更正请求的提交参数。
    ///
    /// English: Submission parameters for an employee-profile correction request.
    /// </summary>
    /// <remarks>
    /// 中文：调用方必须先解析当前登录账号对应的门户用户和 Active 员工绑定；本请求不承载身份票据或凭据。
    ///
    /// English: Callers must resolve the current sign-in to a Portal user and active employee binding first; this
    /// request carries no authentication tickets or credentials.
    /// </remarks>
    public sealed class EmployeeProfileCorrectionSubmitRequest
    {
        /// <summary>中文：门户用户标识。English: Portal user identifier.</summary>
        public int UserId { get; set; }

        /// <summary>中文：员工标识。English: Employee identifier.</summary>
        public int EmployeeId { get; set; }

        /// <summary>中文：员工绑定标识。English: Employee-binding identifier.</summary>
        public int BindingId { get; set; }

        /// <summary>中文：请求更正的字段名。English: Field requested for correction.</summary>
        public string FieldName { get; set; }

        /// <summary>中文：建议更正值。English: Proposed corrected value.</summary>
        public string ProposedValue { get; set; }

        /// <summary>中文：员工说明。English: Employee note.</summary>
        public string RequestNote { get; set; }

        /// <summary>中文：提交 UTC 时间；为空时由数据层使用当前 UTC。English: Submission UTC time; the data layer uses current UTC when empty.</summary>
        public DateTime? SubmittedUtc { get; set; }

        /// <summary>中文：提交者账号名或系统标识。English: Submitter account name or system identifier.</summary>
        public string SubmittedBy { get; set; }
    }
}
