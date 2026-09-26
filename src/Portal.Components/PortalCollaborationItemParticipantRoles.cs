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
    ///   <zh-CN>这些值会进入参与人表与展示。发起人与负责人仍由主表的单值字段（<c>InitiatorUserId</c> / <c>OwnerUserId</c> / <c>OwnerRoleKey</c>）承载，不重复入参与人表；本类只提供集合维度的两个增量角色。两个角色都纳入事项的可见性与参与人集合；"通知"不由本角色本身发出——按项目"待办即通知"定位，通知（如需呈现）由工作项（待办）投影承担，本角色不构成独立通知通道。</zh-CN>
    ///   <en>These values are persisted to the participant table and used in display. The initiator and owner remain on the fact-table scalar fields (<c>InitiatorUserId</c> / <c>OwnerUserId</c> / <c>OwnerRoleKey</c>) and are not duplicated here; this class provides only the two incremental roles of the set dimension. Both roles are included in the item's visibility and participant set; neither role emits notifications by itself—per the project's "work item is the notification" positioning, any notification (if surfaced) is carried by the work-item projection, and these roles are not independent notification channels.</en>
    /// </lang>
    /// </remarks>
    public static class PortalCollaborationItemParticipantRoles
    {
        /// <summary><lang><zh-CN>协办，参与办理事项，并被纳入可见性与参与人集合。</zh-CN><en>Collaborator who helps handle the item and is included in visibility and the participant set.</en></lang></summary>
        public const string Collaborator = "Collaborator";

        /// <summary><lang><zh-CN>关注，被纳入可见性与参与人集合；通知（如需呈现）由工作项（待办）投影承担，本角色不构成独立通知通道。</zh-CN><en>Watcher who is included in visibility and the participant set; any notification (if surfaced) is carried by the work-item projection, and this role is not an independent notification channel.</en></lang></summary>
        public const string Watcher = "Watcher";
    }
}
