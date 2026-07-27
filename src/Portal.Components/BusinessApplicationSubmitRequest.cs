using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>提交抽象业务申请的参数。</zh-CN>
    ///   <en>Parameters for submitting an abstract business application.</en>
    /// </lang>
    /// </summary>
    public sealed class BusinessApplicationSubmitRequest
    {
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
        ///   <zh-CN>纯文本申请说明。</zh-CN>
        ///   <en>Plain-text application body.</en>
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
        ///   <zh-CN>申请人可选员工标识。</zh-CN>
        ///   <en>Optional applicant employee identifier.</en>
        /// </lang>
        /// </summary>
        public int? ApplicantEmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请人可选组织单元标识。</zh-CN>
        ///   <en>Optional applicant organization-unit identifier.</en>
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
        ///   <zh-CN>提交 UTC 时间；为空时数据层使用当前 UTC。</zh-CN>
        ///   <en>Submission UTC time; the data layer uses current UTC when empty.</en>
        /// </lang>
        /// </summary>
        public DateTime? SubmittedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交人账号名。</zh-CN>
        ///   <en>Submitter account name.</en>
        /// </lang>
        /// </summary>
        public string SubmittedBy { get; set; }
    }
}
