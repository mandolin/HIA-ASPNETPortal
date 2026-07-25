using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>管理员处理员工资料更正请求的参数。</zh-CN>
    ///   <en>Parameters for an administrator to review an employee-profile correction request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象只承载审核动作输入，不自行判断权限。调用方必须先确认当前用户具备资料更正审核权限，并把允许的目标状态限制在业务状态目录内。</zh-CN>
    ///   <en>This object only carries review-action input and does not decide authorization. Callers must first confirm that the current user may review profile corrections and must restrict the target status to the business status catalog.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileCorrectionReviewRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>待审核的员工资料更正请求标识。</zh-CN>
        ///   <en>The employee-profile correction request identifier to review.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>数据层会按此标识定位请求，并在审核前再次检查当前记录状态是否允许流转。</zh-CN>
        ///   <en>The data layer locates the request by this identifier and checks again that the current record status may transition before applying the review.</en>
        /// </lang>
        /// </remarks>
        public long RequestId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核后的目标状态。</zh-CN>
        ///   <en>The target status after review.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>应使用 <see cref="EmployeeProfileCorrectionRequestStatuses"/> 中的稳定值；任意字符串不应直接透传到数据库。</zh-CN>
        ///   <en>Use stable values from <see cref="EmployeeProfileCorrectionRequestStatuses"/>; arbitrary strings should not be passed directly to the database.</en>
        /// </lang>
        /// </remarks>
        public string RequestStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核说明。</zh-CN>
        ///   <en>The review note.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该说明可能展示给管理员或写入审计摘要，调用方应避免填入密码、证件号、完整联系方式等敏感内容。</zh-CN>
        ///   <en>This note may be shown to administrators or written into audit summaries, so callers should avoid passwords, identity numbers, full contact information, and other sensitive content.</en>
        /// </lang>
        /// </remarks>
        public string ReviewNote { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理 UTC 时间；为空时由数据层使用当前 UTC。</zh-CN>
        ///   <en>Review UTC time; the data layer uses current UTC when empty.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>允许为空是为了让数据层以统一 UTC 来源落库，避免页面层和数据库层时间不一致。</zh-CN>
        ///   <en>It may be empty so the data layer can persist a single UTC source of truth and avoid disagreement between the page layer and database layer.</en>
        /// </lang>
        /// </remarks>
        public DateTime? ReviewedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理人账号名或系统标识。</zh-CN>
        ///   <en>The reviewer account name or system identifier.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该字段用于审计和历史记录，不作为授权依据；权限必须来自当前请求身份和后台权限判断。</zh-CN>
        ///   <en>This field is for audit and history only, not authorization; authorization must come from the current request identity and administration permission checks.</en>
        /// </lang>
        /// </remarks>
        public string ReviewedBy { get; set; }
    }
}
