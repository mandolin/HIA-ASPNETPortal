namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量 Workflow 动作键的稳定值。</zh-CN>
    ///   <en>Stable action keys for the lightweight workflow backbone.</en>
    /// </lang>
    /// </summary>
    public static class PortalWorkflowActions
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建草稿。</zh-CN>
        ///   <en>Create a draft.</en>
        /// </lang>
        /// </summary>
        public const string CreateDraft = "CreateDraft";

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交申请。</zh-CN>
        ///   <en>Submit an application.</en>
        /// </lang>
        /// </summary>
        public const string Submit = "Submit";

        /// <summary>
        /// <lang>
        ///   <zh-CN>认领待处理事项。</zh-CN>
        ///   <en>Claim a pending item.</en>
        /// </lang>
        /// </summary>
        public const string Claim = "Claim";

        /// <summary>
        /// <lang>
        ///   <zh-CN>批准申请。</zh-CN>
        ///   <en>Approve an application.</en>
        /// </lang>
        /// </summary>
        public const string Approve = "Approve";

        /// <summary>
        /// <lang>
        ///   <zh-CN>退回申请。</zh-CN>
        ///   <en>Return an application.</en>
        /// </lang>
        /// </summary>
        public const string Return = "Return";

        /// <summary>
        /// <lang>
        ///   <zh-CN>驳回申请。</zh-CN>
        ///   <en>Reject an application.</en>
        /// </lang>
        /// </summary>
        public const string Reject = "Reject";

        /// <summary>
        /// <lang>
        ///   <zh-CN>撤回申请。</zh-CN>
        ///   <en>Withdraw an application.</en>
        /// </lang>
        /// </summary>
        public const string Withdraw = "Withdraw";

        /// <summary>
        /// <lang>
        ///   <zh-CN>关闭申请。</zh-CN>
        ///   <en>Close an application.</en>
        /// </lang>
        /// </summary>
        public const string Close = "Close";

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请人自助重提：把被退回的业务申请重新送回待审核窗口。W71 新增，来源状态仅 `Returned`，且不走后台审核路径。</zh-CN>
        ///   <en>Applicant self-service resubmission: sends a returned business application back into the review window. Added in W71; its only source status is `Returned` and it does not travel the administration review path.</en>
        /// </lang>
        /// </summary>
        public const string Resubmit = "Resubmit";
    }
}
