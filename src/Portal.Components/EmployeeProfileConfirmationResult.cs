namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料确认写入结果。
    ///
    /// English: Result of an employee-profile confirmation write.
    /// </summary>
    public sealed class EmployeeProfileConfirmationResult
    {
        /// <summary>
        /// 中文：创建员工资料确认写入结果。
        ///
        /// English: Creates an employee-profile confirmation write result.
        /// </summary>
        /// <param name="succeeded">中文：是否成功。English: Whether the write succeeded.</param>
        /// <param name="confirmationId">中文：成功写入的确认记录标识。English: Identifier of the inserted confirmation record.</param>
        /// <param name="message">中文：可展示的结果说明。English: Display-safe result message.</param>
        public EmployeeProfileConfirmationResult(bool succeeded, long confirmationId, string message)
        {
            Succeeded = succeeded;
            ConfirmationId = confirmationId;
            Message = message ?? string.Empty;
        }

        /// <summary>中文：是否成功。English: Whether the write succeeded.</summary>
        public bool Succeeded { get; private set; }

        /// <summary>中文：确认记录标识。English: Confirmation-record identifier.</summary>
        public long ConfirmationId { get; private set; }

        /// <summary>中文：可展示的结果说明。English: Display-safe result message.</summary>
        public string Message { get; private set; }
    }
}
