namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项事件类型的稳定值。</zh-CN>
    ///   <en>Stable event-type values for enterprise collaboration items.</en>
    /// </lang>
    /// </summary>
    public static class PortalCollaborationItemEventTypes
    {
        /// <summary><lang><zh-CN>会改变事项状态的流程动作事实。</zh-CN><en>Workflow action fact that changes item state.</en></lang></summary>
        public const string WorkflowAction = "WorkflowAction";

        /// <summary><lang><zh-CN>不改变事项状态的纯文本评论事实。</zh-CN><en>Plain-text comment fact that does not change item state.</en></lang></summary>
        public const string Comment = "Comment";
    }
}
