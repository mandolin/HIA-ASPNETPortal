using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：完成或取消业务对象对应待办的参数。
    ///
    /// English: Parameters used to complete or cancel the work item for a business object.
    /// </summary>
    public sealed class PortalWorkItemCompletionRequest
    {
        /// <summary>中文：业务对象类型。English: Business-object kind.</summary>
        public string BusinessKind { get; set; }

        /// <summary>中文：业务对象标识。English: Business-object identifier.</summary>
        public string BusinessId { get; set; }

        /// <summary>中文：事件类型。English: Event type.</summary>
        public string EventType { get; set; }

        /// <summary>中文：目标待办状态。English: Target work-item status.</summary>
        public string TargetStatus { get; set; }

        /// <summary>中文：操作者门户用户标识；未知时为空。English: Actor Portal user id; empty when unknown.</summary>
        public int? ActorUserId { get; set; }

        /// <summary>中文：操作者账号名或系统标识。English: Actor account name or system identifier.</summary>
        public string ActorName { get; set; }

        /// <summary>中文：可选办理备注。English: Optional handling comment.</summary>
        public string Comment { get; set; }

        /// <summary>中文：事件 UTC 时间；为空时数据层使用当前 UTC。English: Event UTC time; data layer uses current UTC when empty.</summary>
        public DateTime? OccurredUtc { get; set; }
    }
}
