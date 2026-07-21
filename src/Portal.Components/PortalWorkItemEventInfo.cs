using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：轻量待办事件的查询投影。
    ///
    /// English: Query projection for a lightweight work-item event.
    /// </summary>
    public sealed class PortalWorkItemEventInfo
    {
        /// <summary>中文：事件标识。English: Event identifier.</summary>
        public long EventId { get; set; }

        /// <summary>中文：待办标识。English: Work-item identifier.</summary>
        public long WorkItemId { get; set; }

        /// <summary>中文：发生 UTC 时间。English: Occurrence UTC time.</summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary>中文：事件类型。English: Event type.</summary>
        public string EventType { get; set; }

        /// <summary>中文：操作者门户用户标识；未知时为空。English: Actor Portal user id; empty when unknown.</summary>
        public int? ActorUserId { get; set; }

        /// <summary>中文：操作者账号名。English: Actor account name.</summary>
        public string ActorName { get; set; }

        /// <summary>中文：原待办状态。English: Previous work-item status.</summary>
        public string FromStatus { get; set; }

        /// <summary>中文：新待办状态。English: New work-item status.</summary>
        public string ToStatus { get; set; }

        /// <summary>中文：办理备注。English: Handling comment.</summary>
        public string Comment { get; set; }
    }
}
