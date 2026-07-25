namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料确认写入结果。</zh-CN>
    ///   <en>Result of an employee-profile confirmation write.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该结果对象只承载资料确认写入的低敏结果，不包含员工资料快照、操作者凭据或异常细节，便于直接传递给页面提示层。</zh-CN>
    ///   <en>This result object carries only low-sensitivity outcome data for a profile-confirmation write. It does not contain profile snapshots, actor credentials or exception details, so it can be passed directly to page messaging.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileConfirmationResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建员工资料确认写入结果。</zh-CN>
        ///   <en>Creates an employee-profile confirmation write result.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded">
        /// <l>
        ///   <zh-CN>数据层是否成功写入确认记录。</zh-CN>
        ///   <en>Whether the data layer successfully inserted the confirmation record.</en>
        /// </l>
        /// </param>
        /// <param name="confirmationId">
        /// <l>
        ///   <zh-CN>成功写入的确认记录标识；失败时为 0。</zh-CN>
        ///   <en>Identifier of the inserted confirmation record, or 0 when the write failed.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>可展示的低敏结果说明。</zh-CN>
        ///   <en>Display-safe low-sensitivity result message.</en>
        /// </l>
        /// </param>
        public EmployeeProfileConfirmationResult(bool succeeded, long confirmationId, string message)
        {
            Succeeded = succeeded;
            ConfirmationId = confirmationId;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据层是否成功写入确认记录。</zh-CN>
        ///   <en>Whether the data layer successfully inserted the confirmation record.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认记录标识；失败时为 0。</zh-CN>
        ///   <en>Confirmation-record identifier, or 0 when the write failed.</en>
        /// </lang>
        /// </summary>
        public long ConfirmationId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可展示的低敏结果说明。</zh-CN>
        ///   <en>Display-safe low-sensitivity result message.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }
}
