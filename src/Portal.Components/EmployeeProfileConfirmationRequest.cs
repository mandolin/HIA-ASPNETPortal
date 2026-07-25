using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料确认写入请求。</zh-CN>
    ///   <en>Write request for confirming an employee profile.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>调用方必须先基于当前登录账号解析出门户用户标识和员工标识；本请求不承载密码、Cookie、Token 或任何身份票据内容。数据层会再次确认该用户与员工之间存在当前有效绑定，避免页面层传入过期员工标识。</zh-CN>
    ///   <en>Callers must resolve the Portal user id and employee id from the current sign-in context before creating this request. This request carries no password, cookie, token, or authentication-ticket content. The data layer rechecks the active user-to-employee binding so a stale employee id from the page is not trusted.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileConfirmationRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户标识；应来自当前认证身份解析结果，而不是浏览器提交的隐藏字段。</zh-CN>
        ///   <en>Portal user identifier; it should come from the current authenticated identity, not from a browser-posted hidden field.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识；必须与当前用户的有效绑定匹配。</zh-CN>
        ///   <en>Employee identifier; it must match the current user's active binding.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认动作发生的 UTC 时间；未设置时由数据层使用当前 UTC，避免页面服务器和数据库写入之间出现空时间。</zh-CN>
        ///   <en>UTC time of the confirmation; the data layer uses current UTC when this value is not set, avoiding an empty timestamp between page handling and database write.</en>
        /// </lang>
        /// </summary>
        public DateTime? ConfirmedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行确认的账号名称或系统标识；用于审计展示，不用于重新认证。</zh-CN>
        ///   <en>Account name or system identifier that performs the confirmation; it is for audit display, not for reauthentication.</en>
        /// </lang>
        /// </summary>
        public string ConfirmedBy { get; set; }
    }
}
