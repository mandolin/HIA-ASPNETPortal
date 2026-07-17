namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料更正请求写入或处理结果。
    ///
    /// English: Result of submitting or reviewing an employee-profile correction request.
    /// </summary>
    public sealed class EmployeeProfileCorrectionRequestResult
    {
        /// <summary>
        /// 中文：创建员工资料更正请求结果。
        ///
        /// English: Creates an employee-profile correction request result.
        /// </summary>
        /// <param name="succeeded">中文：是否成功。English: Whether the operation succeeded.</param>
        /// <param name="requestId">中文：请求标识。English: Request identifier.</param>
        /// <param name="message">中文：可展示的安全说明。English: Display-safe message.</param>
        public EmployeeProfileCorrectionRequestResult(bool succeeded, long requestId, string message)
        {
            Succeeded = succeeded;
            RequestId = requestId;
            Message = message ?? string.Empty;
        }

        /// <summary>中文：是否成功。English: Whether the operation succeeded.</summary>
        public bool Succeeded { get; private set; }

        /// <summary>中文：请求标识。English: Request identifier.</summary>
        public long RequestId { get; private set; }

        /// <summary>中文：可展示的安全说明。English: Display-safe message.</summary>
        public string Message { get; private set; }
    }
}
