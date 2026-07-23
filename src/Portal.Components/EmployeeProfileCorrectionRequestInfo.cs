using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求的列表和详情投影。</zh-CN>
    ///   <en>List and detail projection for an employee-profile correction request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此对象只承载低敏字段级更正信息，不承载附件、身份证号、手机号、薪资或其它高敏个人资料。</zh-CN>
    ///   <en>This object carries only low-sensitivity field-level correction information and no attachments, government ids, mobile phone numbers, compensation data, or other high-sensitivity personal data.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileCorrectionRequestInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>请求标识。</zh-CN>
        ///   <en>Request identifier.</en>
        /// </lang>
        /// </summary>
        public long RequestId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号。</zh-CN>
        ///   <en>Employee code.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工显示名。</zh-CN>
        ///   <en>Employee display name.</en>
        /// </lang>
        /// </summary>
        public string EmployeeDisplayName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户名。</zh-CN>
        ///   <en>Portal user name.</en>
        /// </lang>
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交时使用的员工绑定标识。</zh-CN>
        ///   <en>Employee-binding identifier used at submission time.</en>
        /// </lang>
        /// </summary>
        public int BindingId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交 UTC 时间。</zh-CN>
        ///   <en>Submission UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime SubmittedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交者。</zh-CN>
        ///   <en>Submitter.</en>
        /// </lang>
        /// </summary>
        public string SubmittedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>请求更正的字段名。</zh-CN>
        ///   <en>Field requested for correction.</en>
        /// </lang>
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交时的当前值快照。</zh-CN>
        ///   <en>Current-value snapshot captured at submission time.</en>
        /// </lang>
        /// </summary>
        public string CurrentValueSnapshot { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>建议更正值。</zh-CN>
        ///   <en>Proposed corrected value.</en>
        /// </lang>
        /// </summary>
        public string ProposedValue { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工说明。</zh-CN>
        ///   <en>Employee note.</en>
        /// </lang>
        /// </summary>
        public string RequestNote { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>请求状态。</zh-CN>
        ///   <en>Request status.</en>
        /// </lang>
        /// </summary>
        public string RequestStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理 UTC 时间。</zh-CN>
        ///   <en>Review UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime? ReviewedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理人。</zh-CN>
        ///   <en>Reviewer.</en>
        /// </lang>
        /// </summary>
        public string ReviewedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员处理说明。</zh-CN>
        ///   <en>Administrator review note.</en>
        /// </lang>
        /// </summary>
        public string ReviewNote { get; set; }
    }
}
