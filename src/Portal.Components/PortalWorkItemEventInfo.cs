using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办事件的查询投影。</zh-CN>
    ///   <en>Query projection for a lightweight work-item event.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型面向后台列表、审计辅助和排错展示，只描述已经发生的事件；授权判断仍应使用当前用户、角色和业务对象状态，而不是反向依赖历史事件投影。</zh-CN>
    ///   <en>This type is intended for administration lists, audit assistance, and troubleshooting displays. It describes events that already happened; authorization decisions should still use the current user, roles, and business-object state instead of depending on historical event projections.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemEventInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>事件标识。</zh-CN>
        ///   <en>Event identifier.</en>
        /// </lang>
        /// </summary>
        public long EventId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办标识。</zh-CN>
        ///   <en>Work-item identifier.</en>
        /// </lang>
        /// </summary>
        public long WorkItemId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>发生 UTC 时间。</zh-CN>
        ///   <en>Occurrence UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件类型。</zh-CN>
        ///   <en>Event type.</en>
        /// </lang>
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作者门户用户标识；未知时为空。</zh-CN>
        ///   <en>Actor Portal user id; empty when unknown.</en>
        /// </lang>
        /// </summary>
        public int? ActorUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作者账号名。</zh-CN>
        ///   <en>Actor account name.</en>
        /// </lang>
        /// </summary>
        public string ActorName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>原待办状态。</zh-CN>
        ///   <en>Previous work-item status.</en>
        /// </lang>
        /// </summary>
        public string FromStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新待办状态。</zh-CN>
        ///   <en>New work-item status.</en>
        /// </lang>
        /// </summary>
        public string ToStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>办理备注。</zh-CN>
        ///   <en>Handling comment.</en>
        /// </lang>
        /// </summary>
        public string Comment { get; set; }
    }
}
