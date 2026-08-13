using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>更新业务对象对应待办状态的可变跨层参数。</zh-CN>
    ///   <en>Mutable cross-layer parameters used to update the work-item state for a business object.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本请求只携带待办处理声明，不直接改变业务对象本身状态，也不验证状态迁移、事件类型、身份或权限。调用方应先完成领域动作与授权，再把受控稳定值和低敏办理信息传入待办数据层；当前数据层复制并归一化字段，不修改调用方实例。</zh-CN>
    ///   <en>This request only carries a work-item handling assertion. It neither changes the business object's own state nor validates state transitions, event types, identity, or authorization. Callers should complete and authorize the domain action first, then pass controlled stable values and low-sensitivity handling information to the work-item data layer. The current data layer copies and normalizes fields without mutating the caller instance.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemCompletionRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>必填的稳定业务对象类型；例如员工资料更正请求。当前数据层裁剪至 80 个字符，该值只用于定位未完成待办。</zh-CN>
        ///   <en>Required stable business-object kind, such as an employee-profile correction request. The current data layer trims it to 80 characters, and the value is only used to locate unfinished work items.</en>
        /// </lang>
        /// </summary>
        public string BusinessKind { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>必填业务对象标识；以字符串保存并由当前数据层裁剪至 80 个字符，便于不同业务对象共用同一待办表。标识本身不证明对象存在或调用方可访问。</zh-CN>
        ///   <en>Required business-object identifier stored as text and trimmed to 80 characters by the current data layer so different business objects can share one work-item table. The identifier itself proves neither object existence nor caller access.</en>
        /// </lang>
        /// </summary>
        public string BusinessId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>必填事件类型；用于写入待办事件流水，不替代目标状态。当前数据层只裁剪至 40 个字符，不验证是否来自 <see cref="PortalWorkItemEventTypes"/>。</zh-CN>
        ///   <en>Required event type written to the work-item event stream; it does not replace the target status. The current data layer only trims it to 40 characters and does not validate it against <see cref="PortalWorkItemEventTypes"/>.</en>
        /// </lang>
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>必填目标待办状态；应来自 <see cref="PortalWorkItemStatuses"/> 的稳定值。当前数据层只裁剪至 20 个字符，不执行状态机或终态检查。</zh-CN>
        ///   <en>Required target work-item status that should come from stable values in <see cref="PortalWorkItemStatuses"/>. The current data layer only trims it to 20 characters and enforces neither a state machine nor terminal-state rules.</en>
        /// </lang>
        /// </summary>
        public string TargetStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作者门户用户标识；系统任务或旧入口无法解析时为空，非正数也会被归一化为空。该声明用于事件追踪，不证明请求身份。</zh-CN>
        ///   <en>Actor Portal user identifier; null for system tasks or legacy entry points that cannot resolve one, with non-positive values also normalized to null. The assertion is for event tracing and does not prove request identity.</en>
        /// </lang>
        /// </summary>
        public int? ActorUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作者账号名或系统标识；空白时使用 <c>system</c>，当前数据层裁剪至 100 个字符。它用于审计展示，不作为身份或授权依据。</zh-CN>
        ///   <en>Actor account name or system identifier. Blank values become <c>system</c>, and the current data layer trims the value to 100 characters. It is for audit display and is not an identity or authorization basis.</en>
        /// </lang>
        /// </summary>
        public string ActorName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选低敏纯文本办理备注；当前数据层裁剪至 1000 个字符。不得放入口令、令牌或敏感业务正文，展示方仍须按输出上下文编码。</zh-CN>
        ///   <en>Optional low-sensitivity plain-text handling comment, trimmed to 1,000 characters by the current data layer. It must not contain passwords, tokens, or sensitive domain content, and renderers must still encode it for the output context.</en>
        /// </lang>
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选事件 UTC 时间；为空时数据层使用 <see cref="DateTime.UtcNow"/>。数据层不转换时区、不验证 <see cref="DateTime.Kind"/>，也不保证该时间晚于已有事件。</zh-CN>
        ///   <en>Optional event UTC time; the data layer uses <see cref="DateTime.UtcNow"/> when absent. The data layer neither converts time zones nor validates <see cref="DateTime.Kind"/>, and it does not guarantee that the value follows existing events.</en>
        /// </lang>
        /// </summary>
        public DateTime? OccurredUtc { get; set; }
    }
}
