using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料确认写入请求。
    ///
    /// English: Write request for confirming an employee profile.
    /// </summary>
    /// <remarks>
    /// 中文：调用方必须先基于当前登录账号解析出门户用户标识和员工标识；本请求不承载密码、Cookie、
    /// Token 或任何身份票据内容。
    ///
    /// English: Callers must resolve the Portal user id and employee id from the current sign-in context before
    /// creating this request. This request carries no password, cookie, token, or authentication-ticket content.
    /// </remarks>
    public sealed class EmployeeProfileConfirmationRequest
    {
        /// <summary>
        /// 中文：门户用户标识。
        ///
        /// English: Portal user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 中文：员工标识。
        ///
        /// English: Employee identifier.
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// 中文：确认动作发生的 UTC 时间；未设置时由数据层使用当前 UTC。
        ///
        /// English: UTC time of the confirmation; the data layer uses current UTC when this value is not set.
        /// </summary>
        public DateTime? ConfirmedUtc { get; set; }

        /// <summary>
        /// 中文：执行确认的账号名称或系统标识。
        ///
        /// English: Account name or system identifier that performs the confirmation.
        /// </summary>
        public string ConfirmedBy { get; set; }
    }
}
