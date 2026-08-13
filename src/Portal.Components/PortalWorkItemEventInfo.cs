using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办事件的可变查询投影。</zh-CN>
    ///   <en>Mutable query projection for a lightweight work-item event.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型面向后台列表、审计辅助和排错展示，只描述某次查询看到的历史事件，不自动反映并发写入。授权判断仍应使用当前用户、角色和业务对象状态，而不是反向依赖历史投影；字符串未按 HTML 上下文编码，展示方必须授权并编码。</zh-CN>
    ///   <en>This type is intended for administration lists, audit assistance, and troubleshooting displays. It only describes historical events observed by one query and does not automatically reflect concurrent writes. Authorization decisions must still use the current user, roles, and business-object state rather than historical projections. Strings are not HTML-context encoded, so renderers must authorize and encode them.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemEventInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化事件标识；只用于排序和关联，不证明事件可见性。</zh-CN>
        ///   <en>Persisted event identifier used only for ordering and correlation; it proves no event visibility.</en>
        /// </lang>
        /// </summary>
        public long EventId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>关联待办标识；调用方仍须授权对应待办和业务对象。</zh-CN>
        ///   <en>Associated work-item identifier; callers must still authorize the work item and its business object.</en>
        /// </lang>
        /// </summary>
        public long WorkItemId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化事件发生 UTC 时间；DTO 不验证时区种类或与其他事件的先后关系。</zh-CN>
        ///   <en>Persisted UTC occurrence time; the DTO validates neither time-zone kind nor chronology relative to other events.</en>
        /// </lang>
        /// </summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化事件分类；通常来自 <see cref="PortalWorkItemEventTypes"/>，但投影不验证稳定值，也不表示当前状态。</zh-CN>
        ///   <en>Persisted event classification, normally from <see cref="PortalWorkItemEventTypes"/>. The projection does not validate stable values, and the value does not represent current state.</en>
        /// </lang>
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件记录声明的操作者门户用户标识；未知时为空。它是历史审计字段，不证明当前请求身份。</zh-CN>
        ///   <en>Actor Portal user identifier asserted by the event record; null when unknown. It is a historical audit field and does not prove current-request identity.</en>
        /// </lang>
        /// </summary>
        public int? ActorUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作者账号名或系统标识的历史显示值；可能与当前账号状态不同，不得用于授权。</zh-CN>
        ///   <en>Historical display value for the actor account name or system identifier. It may differ from current account state and must not be used for authorization.</en>
        /// </lang>
        /// </summary>
        public string ActorName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件记录的可选原待办状态；它是历史快照，不保证仍为当前状态。</zh-CN>
        ///   <en>Optional previous work-item status recorded by the event; it is a historical snapshot and is not guaranteed to remain current.</en>
        /// </lang>
        /// </summary>
        public string FromStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件记录的可选目标待办状态；它描述该次事件，不替代当前待办或业务对象状态读取。</zh-CN>
        ///   <en>Optional target work-item status recorded by the event. It describes that event and does not replace reading current work-item or domain state.</en>
        /// </lang>
        /// </summary>
        public string ToStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选低敏纯文本办理备注；投影不净化或编码，展示方不得直接拼接到 HTML、日志结构或脚本。</zh-CN>
        ///   <en>Optional low-sensitivity plain-text handling comment. The projection neither sanitizes nor encodes it, so renderers must not concatenate it directly into HTML, structured logs, or scripts.</en>
        /// </lang>
        /// </summary>
        public string Comment { get; set; }
    }
}
