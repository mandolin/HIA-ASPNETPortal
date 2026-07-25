namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求写入或处理结果。</zh-CN>
    ///   <en>Result of submitting or reviewing an employee-profile correction request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象只承载低敏状态、请求标识和可展示消息；不要把员工资料快照、凭据、原始异常或 SQL 细节放入 <see cref="Message"/>。</zh-CN>
    ///   <en>This object carries only low-sensitivity status, request identity, and display-safe text; do not put employee profile snapshots, credentials, raw exceptions, or SQL details into <see cref="Message"/>.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileCorrectionRequestResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建员工资料更正请求结果。</zh-CN>
        ///   <en>Creates an employee-profile correction request result.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded">
        /// <l>
        ///   <zh-CN>数据写入或审核处理是否已经成功完成。</zh-CN>
        ///   <en>Whether the data write or review operation completed successfully.</en>
        /// </l>
        /// </param>
        /// <param name="requestId">
        /// <l>
        ///   <zh-CN>员工资料更正请求标识；失败或未创建请求时应为 0。</zh-CN>
        ///   <en>Employee-profile correction request identifier; should be 0 when the request failed or was not created.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>可直接显示给页面或写入低敏摘要日志的安全说明。</zh-CN>
        ///   <en>Display-safe text that can be shown on pages or written to low-sensitivity summary logs.</en>
        /// </l>
        /// </param>
        public EmployeeProfileCorrectionRequestResult(bool succeeded, long requestId, string message)
        {
            Succeeded = succeeded;
            RequestId = requestId;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取数据写入或审核处理是否已经成功完成。</zh-CN>
        ///   <en>Gets whether the data write or review operation completed successfully.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取员工资料更正请求标识；失败或未创建请求时为 0。</zh-CN>
        ///   <en>Gets the employee-profile correction request identifier; 0 when the request failed or was not created.</en>
        /// </lang>
        /// </summary>
        public long RequestId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取可直接显示给页面或写入低敏摘要日志的安全说明。</zh-CN>
        ///   <en>Gets display-safe text that can be shown on pages or written to low-sensitivity summary logs.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }
}
