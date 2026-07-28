namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项事件可见范围的稳定值。</zh-CN>
    ///   <en>Stable visibility-scope values for enterprise collaboration-item events.</en>
    /// </lang>
    /// </summary>
    public static class PortalCollaborationItemVisibilityScopes
    {
        /// <summary><lang><zh-CN>事项发起人、合法处理主体和管理员可见。</zh-CN><en>Visible to the initiator, legitimate handlers, and administrators.</en></lang></summary>
        public const string ItemParticipants = "ItemParticipants";

        /// <summary><lang><zh-CN>仅协同事项管理员可见。</zh-CN><en>Visible only to collaboration-item administrators.</en></lang></summary>
        public const string Administrators = "Administrators";
    }
}
