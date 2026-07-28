using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>经事项参与者范围过滤后的协同事项事件投影。</zh-CN>
    ///   <en>Collaboration-item event projection filtered to the item participant scope.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemEventInfo
    {
        /// <summary><lang><zh-CN>事件标识。</zh-CN><en>Event identifier.</en></lang></summary>
        public long EventId { get; set; }

        /// <summary><lang><zh-CN>所属事项标识。</zh-CN><en>Owning item identifier.</en></lang></summary>
        public long ItemId { get; set; }

        /// <summary><lang><zh-CN>事件类型。</zh-CN><en>Event type.</en></lang></summary>
        public string EventType { get; set; }

        /// <summary><lang><zh-CN>流程动作键；评论事件为空。</zh-CN><en>Workflow action key; empty for comment events.</en></lang></summary>
        public string ActionKey { get; set; }

        /// <summary><lang><zh-CN>可见范围。</zh-CN><en>Visibility scope.</en></lang></summary>
        public string VisibilityScope { get; set; }

        /// <summary><lang><zh-CN>作者门户用户标识。</zh-CN><en>Author Portal user identifier.</en></lang></summary>
        public int? ActorUserId { get; set; }

        /// <summary><lang><zh-CN>服务端确认的作者快照。</zh-CN><en>Server-confirmed author snapshot.</en></lang></summary>
        public string ActorName { get; set; }

        /// <summary><lang><zh-CN>发生 UTC 时间。</zh-CN><en>Occurrence UTC time.</en></lang></summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary><lang><zh-CN>动作前状态；评论事件为空。</zh-CN><en>Status before action; empty for comments.</en></lang></summary>
        public string FromStatus { get; set; }

        /// <summary><lang><zh-CN>动作后状态；评论事件为空。</zh-CN><en>Status after action; empty for comments.</en></lang></summary>
        public string ToStatus { get; set; }

        /// <summary><lang><zh-CN>已按纯文本边界保存的评论或办理意见。</zh-CN><en>Comment or handling note stored under the plain-text boundary.</en></lang></summary>
        public string Comment { get; set; }
    }
}
