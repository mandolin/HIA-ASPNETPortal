using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求的提交参数。</zh-CN>
    ///   <en>Submission parameters for an employee-profile correction request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>调用方必须先解析当前登录账号对应的门户用户和 Active 员工绑定；本请求不承载身份票据或凭据。字段名和值仍会在数据层归一化并按白名单校验。</zh-CN>
    ///   <en>Callers must resolve the current sign-in to a Portal user and active employee binding first; this request carries no authentication tickets or credentials. Field names and values are still normalized and checked against the allow-list in the data layer.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileCorrectionSubmitRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户标识；应由当前认证上下文解析得到。</zh-CN>
        ///   <en>Portal user identifier resolved from the current authenticated context.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识；必须与当前有效绑定属于同一员工。</zh-CN>
        ///   <en>Employee identifier; it must belong to the same employee as the current active binding.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工绑定标识；用于防止只凭员工标识提交更正。</zh-CN>
        ///   <en>Employee-binding identifier used to prevent submitting a correction by employee id alone.</en>
        /// </lang>
        /// </summary>
        public int BindingId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>请求更正的字段名；数据层只接受当前白名单字段。</zh-CN>
        ///   <en>Field requested for correction; the data layer accepts only the current allow-listed fields.</en>
        /// </lang>
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>建议更正值；数据层会裁剪长度，但页面层仍应先做用户体验层校验。</zh-CN>
        ///   <en>Proposed corrected value; the data layer truncates to the storage limit, while the page should still validate for user experience.</en>
        /// </lang>
        /// </summary>
        public string ProposedValue { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工说明；可为空，保存前会归一化为空数据库值。</zh-CN>
        ///   <en>Employee note; it may be empty and is normalized to a database null before saving.</en>
        /// </lang>
        /// </summary>
        public string RequestNote { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交 UTC 时间；为空时由数据层使用当前 UTC。</zh-CN>
        ///   <en>Submission UTC time; the data layer uses current UTC when empty.</en>
        /// </lang>
        /// </summary>
        public DateTime? SubmittedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交者账号名或系统标识；用于审计展示，不替代 `UserId` 授权判断。</zh-CN>
        ///   <en>Submitter account name or system identifier; it is for audit display and does not replace `UserId` authorization checks.</en>
        /// </lang>
        /// </summary>
        public string SubmittedBy { get; set; }
    }
}
