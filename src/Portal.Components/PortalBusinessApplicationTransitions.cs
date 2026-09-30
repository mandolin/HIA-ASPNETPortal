using System;
using System.Collections.Generic;
using System.Text;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>业务申请的一条合法迁移：动作、目标状态、合法来源状态集合，以及该动作是否走后台审核路径。</zh-CN>
    ///   <en>One legal business-application transition: the action, its target status, the set of legal source statuses, and whether the action travels the administration review path.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型只是数据行，不含判断逻辑；判断集中在 <see cref="PortalBusinessApplicationTransitions"/>，便于单测与派生 SQL 守卫。`ViaReviewPath` 用于把"审核类动作"与"申请人自助动作"分开——只有前者参与后台审核 SQL 的状态谓词，避免自助动作被后台路径意外触发。</zh-CN>
    ///   <en>This type is only a data row and holds no judgement logic; the judgements live in <see cref="PortalBusinessApplicationTransitions"/> so they can be unit tested and used to derive the SQL guard. `ViaReviewPath` separates review actions from applicant self-service actions: only the former participate in the administration review SQL predicate, so a self-service action cannot be triggered accidentally through the review path.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalBusinessApplicationTransition
    {
        /// <summary><lang><zh-CN>动作键，取自 <see cref="PortalWorkflowActions"/> 的稳定常量。</zh-CN><en>The action key, taken from the stable constants in <see cref="PortalWorkflowActions"/>.</en></lang></summary>
        public string ActionKey { get; private set; }

        /// <summary><lang><zh-CN>该动作的唯一目标状态。</zh-CN><en>The single target status of the action.</en></lang></summary>
        public string ToStatus { get; private set; }

        /// <summary><lang><zh-CN>允许发起该动作的来源状态集合。</zh-CN><en>The set of source statuses from which the action may be started.</en></lang></summary>
        public IList<string> FromStatuses { get; private set; }

        /// <summary><lang><zh-CN>该动作是否走后台审核路径（决定是否进入审核 SQL 状态谓词）。</zh-CN><en>Whether the action travels the administration review path, which decides whether it enters the review SQL status predicate.</en></lang></summary>
        public bool ViaReviewPath { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化一条迁移声明；来源集合会被复制为只读列表。</zh-CN>
        ///   <en>Initializes a transition declaration; the source set is copied into a read-only list.</en>
        /// </lang>
        /// </summary>
        /// <param name="actionKey"><l><zh-CN>动作键。</zh-CN><en>The action key.</en></l></param>
        /// <param name="toStatus"><l><zh-CN>目标状态。</zh-CN><en>The target status.</en></l></param>
        /// <param name="fromStatuses"><l><zh-CN>合法来源状态集合。</zh-CN><en>The legal source statuses.</en></l></param>
        /// <param name="viaReviewPath"><l><zh-CN>是否走后台审核路径。</zh-CN><en>Whether it travels the administration review path.</en></l></param>
        public PortalBusinessApplicationTransition(string actionKey, string toStatus, string[] fromStatuses, bool viaReviewPath)
        {
            ActionKey = actionKey;
            ToStatus = toStatus;
            FromStatuses = new List<string>(fromStatuses ?? new string[0]).AsReadOnly();
            ViaReviewPath = viaReviewPath;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>业务申请状态机的**单一事实源**：动作到目标状态、合法来源状态，以及后台审核 SQL 状态谓词的生成。</zh-CN>
    ///   <en>The single source of truth for the business-application state machine: action-to-target mapping, legal source statuses, and generation of the administration review SQL status predicate.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>与协同事项的 <see cref="PortalCollaborationItemTransitions"/> 同构（`W64` 已建立该判据）。本表刻意**保留既有语义**：Approve / Return / Reject / Close 四个审核动作的合法来源都是 `Submitted` / `InReview`，与显式化之前的硬编码窗口完全一致；显式化不是改语义，调整 Close 的合法来源属独立决策。`Resubmit` 为申请人自助动作，来源为 `Returned`，目标为 `Submitted`，且**不进入**后台审核谓词。</zh-CN>
    ///   <en>Structurally identical to <see cref="PortalCollaborationItemTransitions"/> for collaboration items, following the criterion established in W64. This table deliberately preserves existing semantics: the four review actions Approve / Return / Reject / Close all accept `Submitted` / `InReview`, exactly matching the hard-coded window that preceded this explicitization. Making the table explicit does not change semantics, and adjusting the legal sources of Close is a separate decision. `Resubmit` is an applicant self-service action from `Returned` to `Submitted` that deliberately stays out of the administration review predicate.</en>
    /// </lang>
    /// </remarks>
    public static class PortalBusinessApplicationTransitions
    {
        /// <summary><lang><zh-CN>审核类动作的合法来源：处于待审核窗口的两个状态。</zh-CN><en>Legal sources for review actions: the two statuses inside the review window.</en></lang></summary>
        private static readonly string[] ReviewSources =
        {
            PortalBusinessApplicationStatuses.Submitted,
            PortalBusinessApplicationStatuses.InReview
        };

        /// <summary><lang><zh-CN>申请人自助重提的合法来源：仅被退回的申请。</zh-CN><en>The legal source for applicant self-service resubmission: only returned applications.</en></lang></summary>
        private static readonly string[] ResubmitSources =
        {
            PortalBusinessApplicationStatuses.Returned
        };

        /// <summary><lang><zh-CN>业务申请的全部合法迁移声明。</zh-CN><en>All legal business-application transition declarations.</en></lang></summary>
        public static readonly IList<PortalBusinessApplicationTransition> All =
            new List<PortalBusinessApplicationTransition>
            {
                new PortalBusinessApplicationTransition(PortalWorkflowActions.Approve, PortalBusinessApplicationStatuses.Approved, ReviewSources, true),
                new PortalBusinessApplicationTransition(PortalWorkflowActions.Return, PortalBusinessApplicationStatuses.Returned, ReviewSources, true),
                new PortalBusinessApplicationTransition(PortalWorkflowActions.Reject, PortalBusinessApplicationStatuses.Rejected, ReviewSources, true),
                new PortalBusinessApplicationTransition(PortalWorkflowActions.Close, PortalBusinessApplicationStatuses.Closed, ReviewSources, true),
                new PortalBusinessApplicationTransition(PortalWorkflowActions.Resubmit, PortalBusinessApplicationStatuses.Submitted, ResubmitSources, false)
            }.AsReadOnly();

        /// <summary>
        /// <lang>
        ///   <zh-CN>取动作的唯一目标状态。</zh-CN>
        ///   <en>Gets the single target status of an action.</en>
        /// </lang>
        /// </summary>
        /// <param name="actionKey"><l><zh-CN>动作键；空白或未知动作返回 <c>false</c>。</zh-CN><en>The action key; blank or unknown actions return <c>false</c>.</en></l></param>
        /// <param name="toStatus"><l><zh-CN>命中的目标状态；未命中为 <c>null</c>。</zh-CN><en>The matched target status, or <c>null</c> when unmatched.</en></l></param>
        /// <returns><l><zh-CN>命中为 <c>true</c>。</zh-CN><en><c>true</c> when matched.</en></l></returns>
        public static bool TryGetTargetStatus(string actionKey, out string toStatus)
        {
            toStatus = null;
            foreach (PortalBusinessApplicationTransition transition in All)
            {
                if (string.Equals(transition.ActionKey, actionKey, StringComparison.Ordinal))
                {
                    toStatus = transition.ToStatus;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断从给定来源状态发起该动作是否合法（fail-closed：任一入参未知即不合法）。</zh-CN>
        ///   <en>Determines whether starting the action from the given source status is legal (fail-closed: any unknown input is illegal).</en>
        /// </lang>
        /// </summary>
        /// <param name="fromStatus"><l><zh-CN>当前状态。</zh-CN><en>The current status.</en></l></param>
        /// <param name="actionKey"><l><zh-CN>动作键。</zh-CN><en>The action key.</en></l></param>
        /// <returns><l><zh-CN>合法为 <c>true</c>。</zh-CN><en><c>true</c> when legal.</en></l></returns>
        public static bool IsLegalTransition(string fromStatus, string actionKey)
        {
            if (string.IsNullOrEmpty(fromStatus) || string.IsNullOrEmpty(actionKey))
            {
                return false;
            }

            foreach (PortalBusinessApplicationTransition transition in All)
            {
                if (!string.Equals(transition.ActionKey, actionKey, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string allowed in transition.FromStatuses)
                {
                    if (string.Equals(allowed, fromStatus, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取某动作的合法来源状态集合。</zh-CN>
        ///   <en>Gets the legal source statuses of an action.</en>
        /// </lang>
        /// </summary>
        /// <param name="actionKey"><l><zh-CN>动作键。</zh-CN><en>The action key.</en></l></param>
        /// <returns><l><zh-CN>来源状态列表；未知动作为空列表。</zh-CN><en>The source-status list; empty for unknown actions.</en></l></returns>
        public static IList<string> AllowedFromStatuses(string actionKey)
        {
            foreach (PortalBusinessApplicationTransition transition in All)
            {
                if (string.Equals(transition.ActionKey, actionKey, StringComparison.Ordinal))
                {
                    return transition.FromStatuses;
                }
            }

            return new List<string>().AsReadOnly();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>由迁移表生成后台审核 SQL 的状态谓词（仅含走审核路径的动作），使写入守卫与迁移表同源、消除漂移。</zh-CN>
        ///   <en>Generates the administration review SQL status predicate from the transition table (review-path actions only), keeping the write guard and the table from one source and eliminating drift.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>谓词逐条比较 <c>@ActionKey</c> 并要求当前状态落在该动作的合法来源集合内；因为四个审核动作的来源集合相同，生成结果与显式化之前的手写窗口**语义等价**（有单测守护）。返回文本只由受控常量拼成，不接受调用方输入。</zh-CN>
        ///   <en>Each clause compares <c>@ActionKey</c> and requires the current status to fall inside the action's legal source set. Because the four review actions share the same source set, the generated text is semantically equivalent to the hand-written window it replaces (guarded by a unit test). The returned text is composed only from controlled constants and never from caller input.</en>
        /// </lang>
        /// </remarks>
        /// <returns><l><zh-CN>可拼入 WHERE 子句的谓词文本。</zh-CN><en>Predicate text ready to be embedded in a WHERE clause.</en></l></returns>
        public static string BuildSqlStatusPredicate()
        {
            StringBuilder builder = new StringBuilder();
            foreach (PortalBusinessApplicationTransition transition in All)
            {
                if (!transition.ViaReviewPath)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" OR ");
                }

                builder.Append("(@ActionKey = N'");
                builder.Append(transition.ActionKey.Replace("'", "''"));
                builder.Append("' AND [ApplicationStatus] IN (");

                for (int index = 0; index < transition.FromStatuses.Count; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append('N');
                    builder.Append('\'');
                    builder.Append(transition.FromStatuses[index].Replace("'", "''"));
                    builder.Append('\'');
                }

                builder.Append("))");
            }

            return builder.ToString();
        }
    }
}
