using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang>
    ///   <zh-CN>通用业务申请审批生命周期门面：在已有的单一事实源（PortalBusinessApplicationStatuses / PortalWorkflowActions / PortalBusinessApplicationTransitions）之上，吸收各 BasicBusiness 模块的历史状态词汇，提供「归一化 → 校验 → 判定」的统一入口，消除 Expense/Leave 等模块重复实现的状态常量、Resubmit 方法与本地化孪生方法（W-anp-P294 Q1–Q6 裁定）。</zh-CN>
    ///   <en>Unified business-application approval-lifecycle facade. On top of the existing single source of truth (PortalBusinessApplicationStatuses / PortalWorkflowActions / PortalBusinessApplicationTransitions), it absorbs each BasicBusiness module's legacy status vocabulary and exposes one "normalize → validate → decide" entry point, removing the duplicated status constants, resubmit methods and localized twin methods that Expense/Leave etc. reimplemented (W-anp-P294 Q1–Q6 ruling).</en>
    /// </lang></summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类不重造状态机：迁移判定全部委托 PortalBusinessApplicationTransitions（其 Resubmit 已是 Returned→Submitted 且不在后台审核谓词内，与 Q2「Returned 可重提、Rejected 终态」一致）。本类只补齐「历史模块词汇 → 规范词」的归一化与便捷判定；LocalizeStatus 因依赖 App_GlobalResources 留在 Web 层（PortalStatusLocalizer）调用本类。归一化层实现 Q1「统一词汇」而不改写存储字符串，保留既有数据兼容性。</zh-CN>
    ///   <en>This class does not reinvent the state machine: all transition checks delegate to PortalBusinessApplicationTransitions (whose Resubmit is already Returned→Submitted and excluded from the review predicate, matching Q2 "Returned resubmittable, Rejected terminal"). This class only adds legacy-vocabulary normalization and convenience decisions; LocalizeStatus stays in the web layer (PortalStatusLocalizer) because it depends on App_GlobalResources. The normalization layer realizes Q1 "unified vocabulary" without rewriting stored strings, preserving existing data-store compatibility.</en>
    /// </lang>
    /// </remarks>
    public static class BusinessApplicationLifecycle
    {
        /// <summary><lang><zh-CN>历史模块状态值到规范词的归一化映射。仅登记与规范词字符串不同的历史值，其余原样透传（保留既有数据存储兼容性，不做破坏性改写）。</zh-CN><en>Legacy module status values mapped to the canonical vocabulary. Only legacy values whose string differs from the canonical is registered; the rest pass through (preserves existing data-store compatibility without destructive rewrite).</en></lang></summary>
        private static readonly Dictionary<string, string> LegacyToCanonical =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Pending", PortalBusinessApplicationStatuses.Submitted },
                { "Reviewed", PortalBusinessApplicationStatuses.InReview }
            };

        /// <summary><lang><zh-CN>把任意模块存储态归一化为规范词（Pending→Submitted、Reviewed→InReview；其余原样）。</zh-CN><en>Normalizes any module's stored status to the canonical vocabulary (Pending→Submitted, Reviewed→InReview; others unchanged).</en></lang></summary>
        /// <param name="status"><l><zh-CN>原始状态值（可能为历史模块词汇）。</zh-CN><en>Raw status value (may be a legacy module vocabulary).</en></l></param>
        /// <returns><l><zh-CN>规范词；入参为空返回 <c>null</c>。</zh-CN><en>The canonical status; <c>null</c> when input is null.</en></l></returns>
        public static string NormalizeToCanonical(string status)
        {
            if (status == null)
            {
                return null;
            }

            return LegacyToCanonical.TryGetValue(status, out string canonical) ? canonical : status;
        }

        /// <summary><lang><zh-CN>判定给定状态是否可重新提交。仅 Returned（退回）可重提；Rejected 为终态不可重提（Q2 裁定，与 PortalBusinessApplicationTransitions.Resubmit 来源集合一致）。</zh-CN><en>Whether the given status is resubmittable. Only Returned is resubmittable; Rejected is terminal (Q2 ruling, matching the PortalBusinessApplicationTransitions.Resubmit source set).</en></lang></summary>
        /// <param name="status"><l><zh-CN>原始状态值（会先归一化）。</zh-CN><en>Raw status value (normalized first).</en></l></param>
        /// <returns><l><zh-CN>可重提为 <c>true</c>。</zh-CN><en><c>true</c> when resubmittable.</en></l></returns>
        public static bool IsResubmittable(string status)
        {
            return PortalBusinessApplicationTransitions.IsLegalTransition(
                NormalizeToCanonical(status), PortalWorkflowActions.Resubmit);
        }

        /// <summary><lang><zh-CN>判定从 from 到 to 是否为合法迁移（fail-closed）。使用单一事实源 PortalBusinessApplicationTransitions；同态视为非法迁移。</zh-CN><en>Whether from→to is a legal transition (fail-closed). Delegates to the single source of truth PortalBusinessApplicationTransitions; same-state is not a transition.</en></lang></summary>
        /// <param name="fromStatus"><l><zh-CN>当前状态（会先归一化）。</zh-CN><en>Current status (normalized first).</en></l></param>
        /// <param name="toStatus"><l><zh-CN>目标状态（会先归一化）。</zh-CN><en>Target status (normalized first).</en></l></param>
        /// <returns><l><zh-CN>合法为 <c>true</c>。</zh-CN><en><c>true</c> when legal.</en></l></returns>
        public static bool CanTransition(string fromStatus, string toStatus)
        {
            string from = NormalizeToCanonical(fromStatus);
            string to = NormalizeToCanonical(toStatus);
            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                return false;
            }

            foreach (PortalBusinessApplicationTransition transition in PortalBusinessApplicationTransitions.All)
            {
                if (!string.Equals(transition.ToStatus, to, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string allowed in transition.FromStatuses)
                {
                    if (string.Equals(allowed, from, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
