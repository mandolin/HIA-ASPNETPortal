namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>协同事项评论写入结果。</zh-CN>
    ///   <en>Result of a collaboration-item comment write.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemCommentResult
    {
        /// <summary><lang><zh-CN>初始化评论写入结果。</zh-CN><en>Initializes a comment-write result.</en></lang></summary>
        public CollaborationItemCommentResult(bool succeeded, long itemId, long eventId, string message)
        {
            Succeeded = succeeded;
            ItemId = itemId;
            EventId = eventId;
            Message = message ?? string.Empty;
        }

        /// <summary><lang><zh-CN>写入是否成功。</zh-CN><en>Whether the write succeeded.</en></lang></summary>
        public bool Succeeded { get; private set; }

        /// <summary><lang><zh-CN>目标事项标识。</zh-CN><en>Target item identifier.</en></lang></summary>
        public long ItemId { get; private set; }

        /// <summary><lang><zh-CN>成功时的评论事件标识。</zh-CN><en>Comment event identifier on success.</en></lang></summary>
        public long EventId { get; private set; }

        /// <summary><lang><zh-CN>可安全展示的结果消息。</zh-CN><en>Display-safe result message.</en></lang></summary>
        public string Message { get; private set; }
    }
}
