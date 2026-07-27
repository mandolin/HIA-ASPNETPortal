using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>抽象业务申请的列表和详情投影。</zh-CN>
    ///   <en>List and detail projection for an abstract business application.</en>
    /// </lang>
    /// </summary>
    public sealed class BusinessApplicationInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>业务申请主键。</zh-CN>
        ///   <en>Business-application primary key.</en>
        /// </lang>
        /// </summary>
        public long ApplicationId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>面向人工沟通的申请编号。</zh-CN>
        ///   <en>Human-readable application code.</en>
        /// </lang>
        /// </summary>
        public string ApplicationCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请标题。</zh-CN>
        ///   <en>Application title.</en>
        /// </lang>
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请分类键。</zh-CN>
        ///   <en>Application category key.</en>
        /// </lang>
        /// </summary>
        public string CategoryKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>低敏摘要。</zh-CN>
        ///   <en>Low-sensitivity summary.</en>
        /// </lang>
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请说明正文。</zh-CN>
        ///   <en>Application body text.</en>
        /// </lang>
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请人门户用户标识。</zh-CN>
        ///   <en>Applicant Portal user identifier.</en>
        /// </lang>
        /// </summary>
        public int ApplicantUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请人用户名快照。</zh-CN>
        ///   <en>Applicant user-name snapshot.</en>
        /// </lang>
        /// </summary>
        public string ApplicantUserName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选申请人员工标识。</zh-CN>
        ///   <en>Optional applicant employee identifier.</en>
        /// </lang>
        /// </summary>
        public int? ApplicantEmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选组织单元标识。</zh-CN>
        ///   <en>Optional organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        public int? OrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核角色权限键。</zh-CN>
        ///   <en>Review role permission key.</en>
        /// </lang>
        /// </summary>
        public string ReviewRoleKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请状态。</zh-CN>
        ///   <en>Application status.</en>
        /// </lang>
        /// </summary>
        public string ApplicationStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交 UTC 时间。</zh-CN>
        ///   <en>Submission UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime? SubmittedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近审核 UTC 时间。</zh-CN>
        ///   <en>Latest review UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime? ReviewedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近审核人门户用户标识。</zh-CN>
        ///   <en>Latest reviewer Portal user identifier.</en>
        /// </lang>
        /// </summary>
        public int? ReviewedByUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近审核意见。</zh-CN>
        ///   <en>Latest review comment.</en>
        /// </lang>
        /// </summary>
        public string ReviewComment { get; set; }
    }
}
