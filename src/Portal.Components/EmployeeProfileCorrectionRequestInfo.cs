using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料更正请求的列表和详情投影。
    ///
    /// English: List and detail projection for an employee-profile correction request.
    /// </summary>
    /// <remarks>
    /// 中文：此对象只承载低敏字段级更正信息，不承载附件、身份证号、手机号、薪资或其它高敏个人资料。
    ///
    /// English: This object carries only low-sensitivity field-level correction information and no attachments,
    /// government ids, mobile phone numbers, compensation data, or other high-sensitivity personal data.
    /// </remarks>
    public sealed class EmployeeProfileCorrectionRequestInfo
    {
        /// <summary>中文：请求标识。English: Request identifier.</summary>
        public long RequestId { get; set; }

        /// <summary>中文：员工标识。English: Employee identifier.</summary>
        public int EmployeeId { get; set; }

        /// <summary>中文：员工号。English: Employee code.</summary>
        public string EmployeeCode { get; set; }

        /// <summary>中文：员工显示名。English: Employee display name.</summary>
        public string EmployeeDisplayName { get; set; }

        /// <summary>中文：门户用户标识。English: Portal user identifier.</summary>
        public int UserId { get; set; }

        /// <summary>中文：门户用户名。English: Portal user name.</summary>
        public string UserName { get; set; }

        /// <summary>中文：提交时使用的员工绑定标识。English: Employee-binding identifier used at submission time.</summary>
        public int BindingId { get; set; }

        /// <summary>中文：提交 UTC 时间。English: Submission UTC time.</summary>
        public DateTime SubmittedUtc { get; set; }

        /// <summary>中文：提交者。English: Submitter.</summary>
        public string SubmittedBy { get; set; }

        /// <summary>中文：请求更正的字段名。English: Field requested for correction.</summary>
        public string FieldName { get; set; }

        /// <summary>中文：提交时的当前值快照。English: Current-value snapshot captured at submission time.</summary>
        public string CurrentValueSnapshot { get; set; }

        /// <summary>中文：建议更正值。English: Proposed corrected value.</summary>
        public string ProposedValue { get; set; }

        /// <summary>中文：员工说明。English: Employee note.</summary>
        public string RequestNote { get; set; }

        /// <summary>中文：请求状态。English: Request status.</summary>
        public string RequestStatus { get; set; }

        /// <summary>中文：处理 UTC 时间。English: Review UTC time.</summary>
        public DateTime? ReviewedUtc { get; set; }

        /// <summary>中文：处理人。English: Reviewer.</summary>
        public string ReviewedBy { get; set; }

        /// <summary>中文：管理员处理说明。English: Administrator review note.</summary>
        public string ReviewNote { get; set; }
    }
}
