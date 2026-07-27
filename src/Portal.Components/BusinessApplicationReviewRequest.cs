using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>审核抽象业务申请的参数。</zh-CN>
    ///   <en>Parameters for reviewing an abstract business application.</en>
    /// </lang>
    /// </summary>
    public sealed class BusinessApplicationReviewRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>待处理申请标识。</zh-CN>
        ///   <en>Application identifier to process.</en>
        /// </lang>
        /// </summary>
        public long ApplicationId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>流程动作键，应来自 <see cref="PortalWorkflowActions"/>。</zh-CN>
        ///   <en>Workflow action key, expected to come from <see cref="PortalWorkflowActions"/>.</en>
        /// </lang>
        /// </summary>
        public string ActionKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核意见。</zh-CN>
        ///   <en>Review comment.</en>
        /// </lang>
        /// </summary>
        public string ReviewComment { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核人门户用户标识。</zh-CN>
        ///   <en>Reviewer Portal user identifier.</en>
        /// </lang>
        /// </summary>
        public int? ReviewedByUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核人账号名。</zh-CN>
        ///   <en>Reviewer account name.</en>
        /// </lang>
        /// </summary>
        public string ReviewedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核 UTC 时间；为空时数据层使用当前 UTC。</zh-CN>
        ///   <en>Review UTC time; the data layer uses current UTC when empty.</en>
        /// </lang>
        /// </summary>
        public DateTime? ReviewedUtc { get; set; }
    }
}
