using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项参与人的只读投影。</zh-CN>
    ///   <en>Read-only projection of one enterprise collaboration-item participant.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemParticipantInfo
    {
        /// <summary><lang><zh-CN>参与人主键。</zh-CN><en>Participant primary key.</en></lang></summary>
        public long ParticipantId { get; set; }

        /// <summary><lang><zh-CN>所属事项标识。</zh-CN><en>Owning item identifier.</en></lang></summary>
        public long ItemId { get; set; }

        /// <summary><lang><zh-CN>参与人门户用户标识。</zh-CN><en>Participant Portal user identifier.</en></lang></summary>
        public int UserId { get; set; }

        /// <summary><lang><zh-CN>参与人用户名快照。</zh-CN><en>Participant user-name snapshot.</en></lang></summary>
        public string UserName { get; set; }

        /// <summary><lang><zh-CN>参与角色键。</zh-CN><en>Participant role key.</en></lang></summary>
        public string ParticipantRoleKey { get; set; }

        /// <summary><lang><zh-CN>加入参与人的 UTC 时间。</zh-CN><en>UTC time when the participant was added.</en></lang></summary>
        public DateTime CreatedUtc { get; set; }
    }
}
