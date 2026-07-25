using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>完成或取消业务对象对应待办的参数。</zh-CN>
    ///   <en>Parameters used to complete or cancel the work item for a business object.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本请求描述待办任务的处理动作，不直接改变业务对象本身状态。调用方应先完成业务对象权限判断，再把低敏办理信息传入待办数据层。</zh-CN>
    ///   <en>This request describes a work-item handling action and does not directly change the business object's own state. Callers should finish business-object authorization first, then pass low-sensitivity handling information to the work-item data layer.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemCompletionRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象类型；例如员工资料更正请求。</zh-CN>
        ///   <en>Business-object kind, such as an employee-profile correction request.</en>
        /// </lang>
        /// </summary>
        public string BusinessKind { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象标识；以字符串保存，便于不同业务对象共用同一待办表。</zh-CN>
        ///   <en>Business-object identifier stored as text so different business objects can share the same work-item table.</en>
        /// </lang>
        /// </summary>
        public string BusinessId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件类型；用于写入待办事件流水，不替代目标状态。</zh-CN>
        ///   <en>Event type written to the work-item event stream; it does not replace the target status.</en>
        /// </lang>
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>目标待办状态；应来自 <see cref="PortalWorkItemStatuses"/> 的稳定值。</zh-CN>
        ///   <en>Target work-item status; it should come from stable values in <see cref="PortalWorkItemStatuses"/>.</en>
        /// </lang>
        /// </summary>
        public string TargetStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作者门户用户标识；系统任务或旧入口无法解析时为空。</zh-CN>
        ///   <en>Actor Portal user id; null when a system task or legacy entry point cannot resolve one.</en>
        /// </lang>
        /// </summary>
        public int? ActorUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作者账号名或系统标识；用于审计展示，不作为授权依据。</zh-CN>
        ///   <en>Actor account name or system identifier; it is for audit display and is not an authorization basis.</en>
        /// </lang>
        /// </summary>
        public string ActorName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选办理备注；数据层会按字段容量归一化。</zh-CN>
        ///   <en>Optional handling comment; the data layer normalizes it to the storage capacity.</en>
        /// </lang>
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件 UTC 时间；为空时数据层使用当前 UTC。</zh-CN>
        ///   <en>Event UTC time; the data layer uses current UTC when empty.</en>
        /// </lang>
        /// </summary>
        public DateTime? OccurredUtc { get; set; }
    }
}
