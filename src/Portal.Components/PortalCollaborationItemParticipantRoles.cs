namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项参与角色的稳定值。</zh-CN>
    ///   <en>Stable participant-role values for enterprise collaboration items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些值会进入参与人表与展示。发起人与负责人仍由主表的单值字段（<c>InitiatorUserId</c> / <c>OwnerUserId</c> / <c>OwnerRoleKey</c>）承载，不重复入参与人表；本类只提供集合维度的两个增量角色。</zh-CN>
    ///   <en>These values are persisted to the participant table and used in display. The initiator and owner remain on the fact-table scalar fields (<c>InitiatorUserId</c> / <c>OwnerUserId</c> / <c>OwnerRoleKey</c>) and are not duplicated here; this class provides only the two incremental roles of the set dimension.</en>
    /// </lang>
    /// </remarks>
    public static class PortalCollaborationItemParticipantRoles
    {
        /// <summary><lang><zh-CN>协办，参与办理事项。</zh-CN><en>Collaborator who helps handle the item.</en></lang></summary>
        public const string Collaborator = "Collaborator";

        /// <summary><lang><zh-CN>关注，仅接收通知与可见。</zh-CN><en>Watcher who receives notifications and visibility only.</en></lang></summary>
        public const string Watcher = "Watcher";
    }
}
