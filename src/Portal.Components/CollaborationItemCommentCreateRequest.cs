using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>创建企业协同事项纯文本评论的受限输入。</zh-CN>
    ///   <en>Restricted input for creating a plain-text enterprise collaboration-item comment.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>调用方只能提供事项、正文、请求范围和当前身份线索；数据层会重新解析作者并拒绝改变状态、待办、期限或最近流程意见的输入。</zh-CN>
    ///   <en>Callers provide only the item, body, requested scope, and current-identity hints; the data layer re-resolves the author and rejects inputs that would change state, work items, due dates, or the latest workflow comment.</en>
    /// </lang>
    /// </remarks>
    public sealed class CollaborationItemCommentCreateRequest
    {
        /// <summary><lang><zh-CN>被评论的协同事项标识。</zh-CN><en>Collaboration item being commented on.</en></lang></summary>
        public long ItemId { get; set; }

        /// <summary><lang><zh-CN>最大一千字符的低敏纯文本评论。</zh-CN><en>Low-sensitivity plain-text comment of at most one thousand characters.</en></lang></summary>
        public string Comment { get; set; }

        /// <summary><lang><zh-CN>请求的可见范围，必须来自 <see cref="PortalCollaborationItemVisibilityScopes"/>。</zh-CN><en>Requested visibility scope, which must come from <see cref="PortalCollaborationItemVisibilityScopes"/>.</en></lang></summary>
        public string VisibilityScope { get; set; }

        /// <summary><lang><zh-CN>当前认证用户标识，服务端会重新确认该用户与作者快照。</zh-CN><en>Current authenticated user id, which the service rechecks against the author snapshot.</en></lang></summary>
        public int ActorUserId { get; set; }

        /// <summary><lang><zh-CN>当前认证用户名提示；不得作为作者身份的唯一依据。</zh-CN><en>Current authenticated user-name hint; it must not be the sole author-identity basis.</en></lang></summary>
        public string ActorName { get; set; }

        /// <summary><lang><zh-CN>发生 UTC 时间；为空时由服务端使用当前 UTC。</zh-CN><en>Occurrence UTC time; the service uses current UTC when empty.</en></lang></summary>
        public DateTime? OccurredUtc { get; set; }
    }
}
