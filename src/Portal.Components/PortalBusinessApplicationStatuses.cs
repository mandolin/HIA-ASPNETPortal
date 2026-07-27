namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>抽象业务申请状态的稳定值。</zh-CN>
    ///   <en>Stable status values for abstract business applications.</en>
    /// </lang>
    /// </summary>
    public static class PortalBusinessApplicationStatuses
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>草稿；第一版页面暂不开放保存草稿，但作为状态模型稳定预留。</zh-CN>
        ///   <en>Draft; the first page does not expose draft saving yet, but the stable state is reserved.</en>
        /// </lang>
        /// </summary>
        public const string Draft = "Draft";

        /// <summary>
        /// <lang>
        ///   <zh-CN>已提交，等待审核。</zh-CN>
        ///   <en>Submitted and waiting for review.</en>
        /// </lang>
        /// </summary>
        public const string Submitted = "Submitted";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核中；第一版作为后续认领/处理中状态预留。</zh-CN>
        ///   <en>In review; reserved for later claim or in-progress handling.</en>
        /// </lang>
        /// </summary>
        public const string InReview = "InReview";

        /// <summary>
        /// <lang>
        ///   <zh-CN>已退回申请人补充或修改。</zh-CN>
        ///   <en>Returned to the applicant for supplement or correction.</en>
        /// </lang>
        /// </summary>
        public const string Returned = "Returned";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核通过。</zh-CN>
        ///   <en>Approved by review.</en>
        /// </lang>
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核驳回。</zh-CN>
        ///   <en>Rejected by review.</en>
        /// </lang>
        /// </summary>
        public const string Rejected = "Rejected";

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请人已撤回；第一版保留状态，撤回入口后置。</zh-CN>
        ///   <en>Withdrawn by the applicant; the first version reserves the state and defers the entry point.</en>
        /// </lang>
        /// </summary>
        public const string Withdrawn = "Withdrawn";

        /// <summary>
        /// <lang>
        ///   <zh-CN>由管理员关闭。</zh-CN>
        ///   <en>Closed by an administrator.</en>
        /// </lang>
        /// </summary>
        public const string Closed = "Closed";
    }
}
