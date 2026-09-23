namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>添加企业协同事项参与人的参数。</zh-CN>
    ///   <en>Parameters for adding an enterprise collaboration-item participant.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemParticipantCreateRequest
    {
        /// <summary><lang><zh-CN>所属事项标识。</zh-CN><en>Owning item identifier.</en></lang></summary>
        public long ItemId { get; set; }

        /// <summary><lang><zh-CN>参与人门户用户标识。</zh-CN><en>Participant Portal user identifier.</en></lang></summary>
        public int UserId { get; set; }

        /// <summary><lang><zh-CN>参与角色键，应来自 <see cref="PortalCollaborationItemParticipantRoles"/>。</zh-CN><en>Participant role key, expected to come from <see cref="PortalCollaborationItemParticipantRoles"/>.</en></lang></summary>
        public string ParticipantRoleKey { get; set; }

        /// <summary><lang><zh-CN>操作者门户用户标识，用于服务端授权。</zh-CN><en>Actor Portal user identifier used for server-side authorization.</en></lang></summary>
        public int ActorUserId { get; set; }
    }
}
