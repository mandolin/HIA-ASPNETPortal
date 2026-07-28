namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项写入结果。</zh-CN>
    ///   <en>Write result for enterprise collaboration items.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建企业协同事项写入结果。</zh-CN>
        ///   <en>Creates an enterprise collaboration-item write result.</en>
        /// </lang>
        /// </summary>
        public CollaborationItemResult(bool succeeded, long itemId, string itemCode, string actionKey, string message)
        {
            Succeeded = succeeded;
            ItemId = itemId;
            ItemCode = itemCode ?? string.Empty;
            ActionKey = actionKey ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary><lang><zh-CN>操作是否成功。</zh-CN><en>Whether the operation succeeded.</en></lang></summary>
        public bool Succeeded { get; private set; }

        /// <summary><lang><zh-CN>协同事项主键。</zh-CN><en>Collaboration-item primary key.</en></lang></summary>
        public long ItemId { get; private set; }

        /// <summary><lang><zh-CN>协同事项编号。</zh-CN><en>Collaboration-item code.</en></lang></summary>
        public string ItemCode { get; private set; }

        /// <summary><lang><zh-CN>已执行动作键。</zh-CN><en>Applied action key.</en></lang></summary>
        public string ActionKey { get; private set; }

        /// <summary><lang><zh-CN>可展示的低敏结果说明。</zh-CN><en>Display-safe result message.</en></lang></summary>
        public string Message { get; private set; }
    }
}
