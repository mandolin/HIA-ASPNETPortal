namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang><zh-CN>基础业务模块的"存库词汇"单一字面源：Leave/Expense 在库中持久化为这些值（Pending/Approved/Rejected/Returned/Closed）。规范展示词经 BusinessApplicationLifecycle.NormalizeToCanonical 映射（Pending→Submitted 等）。本类只承载存库常量、消除按模块重复的字面量；展示语义见 PortalBusinessApplicationStatuses。</zh-CN><en>Single source of the stored vocabulary for basic-business modules: Leave/Expense persist these values (Pending/Approved/Rejected/Returned/Closed) in the database; canonical display words are mapped via BusinessApplicationLifecycle.NormalizeToCanonical (Pending→Submitted etc.). This class carries only the stored constants to remove per-module duplicated literals; display semantics live in PortalBusinessApplicationStatuses.</en></lang></summary>
    public static class BasicBusinessApplicationStatuses
    {
        /// <summary><lang><zh-CN>待审批（存库值）。</zh-CN><en>Pending review (stored value).</en></lang></summary>
        public const string Pending = "Pending";

        /// <summary><lang><zh-CN>已批准（存库值）。</zh-CN><en>Approved (stored value).</en></lang></summary>
        public const string Approved = "Approved";

        /// <summary><lang><zh-CN>已拒绝（存库值）。</zh-CN><en>Rejected (stored value).</en></lang></summary>
        public const string Rejected = "Rejected";

        /// <summary><lang><zh-CN>已退回（存库值，等待申请人修订后重新提交）。</zh-CN><en>Returned (stored value, awaiting applicant revision and resubmission).</en></lang></summary>
        public const string Returned = "Returned";

        /// <summary><lang><zh-CN>已关闭（存库值）。</zh-CN><en>Closed (stored value).</en></lang></summary>
        public const string Closed = "Closed";
    }
}
