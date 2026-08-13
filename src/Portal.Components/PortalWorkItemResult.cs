namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办写入的低敏结果快照。</zh-CN>
    ///   <en>Low-sensitivity result snapshot of a lightweight work-item write.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该结果把输入验证、架构不可用、未命中和运行异常统一收敛为成功标识与可展示消息；它不携带异常、SQL 或授权事实。调用方必须检查 <see cref="Succeeded"/>，不能仅根据消息或待办标识推断业务动作结果。</zh-CN>
    ///   <en>The result collapses input validation, unavailable schema, no-match outcomes, and runtime failures into a success flag and display-safe message; it carries no exception, SQL, or authorization facts. Callers must inspect <see cref="Succeeded"/> and must not infer the domain-action result from the message or identifier alone.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建不可由外部调用方修改的轻量待办写入结果。</zh-CN>
        ///   <en>Creates a lightweight work-item write result that external callers cannot modify.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded">
        /// <l>
        ///   <zh-CN>数据层是否完成所请求的待办操作；不表示业务对象写入或授权成功。</zh-CN>
        ///   <en>Whether the data layer completed the requested work-item operation; it does not indicate successful domain persistence or authorization.</en>
        /// </l>
        /// </param>
        /// <param name="workItemId">
        /// <l>
        ///   <zh-CN>成功时的待办标识；失败结果通常传入 0，构造函数不校验标识与成功标志的一致性。</zh-CN>
        ///   <en>Work-item identifier on success. Failure results normally supply zero, but the constructor does not validate consistency between the identifier and success flag.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>由生产方提供的低敏说明；空引用会转换为空字符串。页面输出时仍须按上下文编码。</zh-CN>
        ///   <en>Low-sensitivity message supplied by the producer; a null reference becomes an empty string. Pages must still encode it for the output context.</en>
        /// </l>
        /// </param>
        public PortalWorkItemResult(bool succeeded, long workItemId, string message)
        {
            // <lang>
            //   <zh-CN>一次性固定三项结果事实；只对消息做 null 归一化，不隐式修正调用方传入的成功标志或标识。</zh-CN>
            //   <en>Capture the three result facts together; only normalize a null message and do not silently repair the caller-supplied success flag or identifier.</en>
            // </lang>
            Succeeded = succeeded;
            WorkItemId = workItemId;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办数据操作是否成功；它不表示业务对象事务或授权成功。</zh-CN>
        ///   <en>Whether the work-item data operation succeeded; it does not indicate successful domain transaction or authorization.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>成功时新建、复用或更新的待办标识；失败时通常为 0。</zh-CN>
        ///   <en>Identifier of the work item created, reused, or updated on success; normally zero on failure.</en>
        /// </lang>
        /// </summary>
        public long WorkItemId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非 null 的低敏结果说明；不能用于错误分类、授权或程序分支协议，页面仍须编码。</zh-CN>
        ///   <en>Non-null low-sensitivity result message. It is not a protocol for error classification, authorization, or program branching, and pages must still encode it.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }
}
