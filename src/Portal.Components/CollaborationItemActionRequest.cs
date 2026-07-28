using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>处理企业协同事项状态动作的参数。</zh-CN>
    ///   <en>Parameters for applying a state action to an enterprise collaboration item.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemActionRequest
    {
        /// <summary><lang><zh-CN>待处理事项标识。</zh-CN><en>Item identifier to process.</en></lang></summary>
        public long ItemId { get; set; }

        /// <summary><lang><zh-CN>动作键，应来自 <see cref="PortalCollaborationItemActions"/>。</zh-CN><en>Action key, expected to come from <see cref="PortalCollaborationItemActions"/>.</en></lang></summary>
        public string ActionKey { get; set; }

        /// <summary><lang><zh-CN>办理意见。</zh-CN><en>Handling comment.</en></lang></summary>
        public string Comment { get; set; }

        /// <summary><lang><zh-CN>动作人门户用户标识。</zh-CN><en>Actor Portal user identifier.</en></lang></summary>
        public int? ActorUserId { get; set; }

        /// <summary><lang><zh-CN>动作人账号名。</zh-CN><en>Actor account name.</en></lang></summary>
        public string ActorName { get; set; }

        /// <summary><lang><zh-CN>动作 UTC 时间；为空时数据层使用当前 UTC。</zh-CN><en>Action UTC time; the data layer uses current UTC when empty.</en></lang></summary>
        public DateTime? OccurredUtc { get; set; }
    }
}
