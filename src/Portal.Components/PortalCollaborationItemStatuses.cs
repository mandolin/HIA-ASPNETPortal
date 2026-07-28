namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项状态的稳定值。</zh-CN>
    ///   <en>Stable status values for enterprise collaboration items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些值会进入数据库、待办、审计和文档。P19 旧业务申请中的 <c>InReview</c>、<c>Approved</c>、<c>Withdrawn</c> 仅作为旧样板兼容值保留，不替代本泛化对象的状态术语。</zh-CN>
    ///   <en>These values are persisted to the database, work items, audits, and documentation. The legacy P19 business-application values <c>InReview</c>, <c>Approved</c>, and <c>Withdrawn</c> remain compatibility terms only and do not replace this generalized object's status vocabulary.</en>
    /// </lang>
    /// </remarks>
    public static class PortalCollaborationItemStatuses
    {
        /// <summary><lang><zh-CN>草稿，尚未提交给处理人。</zh-CN><en>Draft and not submitted to handlers yet.</en></lang></summary>
        public const string Draft = "Draft";

        /// <summary><lang><zh-CN>已提交，等待负责人或处理角色处理。</zh-CN><en>Submitted and waiting for an owner or handling role.</en></lang></summary>
        public const string Submitted = "Submitted";

        /// <summary><lang><zh-CN>处理中。</zh-CN><en>In progress.</en></lang></summary>
        public const string InProgress = "InProgress";

        /// <summary><lang><zh-CN>已退回发起人补充。</zh-CN><en>Returned to the initiator for supplement.</en></lang></summary>
        public const string Returned = "Returned";

        /// <summary><lang><zh-CN>已完成。</zh-CN><en>Completed.</en></lang></summary>
        public const string Completed = "Completed";

        /// <summary><lang><zh-CN>已驳回。</zh-CN><en>Rejected.</en></lang></summary>
        public const string Rejected = "Rejected";

        /// <summary><lang><zh-CN>已取消。</zh-CN><en>Cancelled.</en></lang></summary>
        public const string Cancelled = "Cancelled";

        /// <summary><lang><zh-CN>已关闭或归档。</zh-CN><en>Closed or archived.</en></lang></summary>
        public const string Closed = "Closed";
    }
}
