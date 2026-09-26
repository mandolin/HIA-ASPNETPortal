using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖协同事项显式合法迁移表的契约测试。</zh-CN>
    ///   <en>Contract tests covering the explicit legal-transition table of collaboration items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只构造内存中的迁移表查询，直接调用 <see cref="PortalCollaborationItemTransitions"/> 的纯方法：不读取配置、不连接数据库、不接触 HTTP 上下文，也不使用任何真实账号或凭据。重点证明"15 条合法迁移逐条成立""代表性非法迁移被拒绝（fail-closed）"以及"生成的 SQL 守卫与既有手写谓词语义等价"。</zh-CN>
    ///   <en>The tests build in-memory table queries and call the pure methods of <see cref="PortalCollaborationItemTransitions"/> directly: they read no configuration, connect to no database, touch no HTTP context, and use no real account or credential. They focus on proving that all 15 legal transitions hold individually, that representative illegal transitions are denied (fail-closed), and that the generated SQL guard is semantically equivalent to the established hand-written predicate.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalCollaborationItemTransitionsTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证表中每条迁移都被 <c>IsLegalTransition</c> 与 <c>TryGetTargetStatus</c> 承认，且解析出的目标状态与表中一致。</zh-CN>
        ///   <en>Verifies that every transition in the table is acknowledged by <c>IsLegalTransition</c> and <c>TryGetTargetStatus</c>, and that the resolved target matches the table.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void AllTransitions_AreLegalAndResolveTarget()
        {
            Assert.IsTrue(PortalCollaborationItemTransitions.All.Count == 15, "既定状态机应有 15 条合法迁移。");

            foreach (CollaborationItemTransition transition in PortalCollaborationItemTransitions.All)
            {
                Assert.IsTrue(
                    PortalCollaborationItemTransitions.IsLegalTransition(transition.FromStatus, transition.ActionKey),
                    string.Format("迁移 ({0} --{1}--> {2}) 应被判定为合法。", transition.FromStatus, transition.ActionKey, transition.ToStatus));

                string resolved;
                bool found = PortalCollaborationItemTransitions.TryGetTargetStatus(transition.FromStatus, transition.ActionKey, out resolved);
                Assert.IsTrue(found, "合法迁移必须能被解析出目标状态。");
                Assert.AreEqual(transition.ToStatus, resolved, "解析出的目标状态必须与表中一致。");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证代表性非法迁移（无对应表行）被 <c>IsLegalTransition</c> 拒绝，且不泄露内部原因。</zh-CN>
        ///   <en>Verifies that representative illegal transitions (no matching table row) are denied by <c>IsLegalTransition</c> without exposing internal reasons.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IllegalTransitions_AreRejected()
        {
            Assert.IsFalse(
                PortalCollaborationItemTransitions.IsLegalTransition(PortalCollaborationItemStatuses.Draft, PortalCollaborationItemActions.Start),
                "Draft 不能直接 Start。");
            Assert.IsFalse(
                PortalCollaborationItemTransitions.IsLegalTransition(PortalCollaborationItemStatuses.Completed, PortalCollaborationItemActions.Submit),
                "Completed 不能重新 Submit。");
            Assert.IsFalse(
                PortalCollaborationItemTransitions.IsLegalTransition(PortalCollaborationItemStatuses.Closed, PortalCollaborationItemActions.Cancel),
                "Closed 不能再 Cancel。");
            Assert.IsFalse(
                PortalCollaborationItemTransitions.IsLegalTransition(PortalCollaborationItemStatuses.InProgress, PortalCollaborationItemActions.Resubmit),
                "InProgress 不能直接 Resubmit（仅 Returned 可）。");

            string resolved;
            Assert.IsFalse(
                PortalCollaborationItemTransitions.TryGetTargetStatus(PortalCollaborationItemStatuses.Draft, PortalCollaborationItemActions.Start, out resolved),
                "非法迁移必须解析失败。");
            Assert.IsNull(resolved, "解析失败时应输出空引用。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证空或未知的状态/动作输入一律被拒绝（fail-closed）。</zh-CN>
        ///   <en>Verifies that empty or unknown status/action inputs are always denied (fail-closed).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void EmptyOrUnknownInputs_AreRejected()
        {
            Assert.IsFalse(PortalCollaborationItemTransitions.IsLegalTransition(string.Empty, PortalCollaborationItemActions.Submit));
            Assert.IsFalse(PortalCollaborationItemTransitions.IsLegalTransition(PortalCollaborationItemStatuses.Draft, string.Empty));
            Assert.IsFalse(PortalCollaborationItemTransitions.IsLegalTransition(PortalCollaborationItemStatuses.Draft, "NotAnAction"));
            Assert.IsFalse(PortalCollaborationItemTransitions.IsLegalTransition("NotAStatus", PortalCollaborationItemActions.Start));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 <c>AllowedFromStatuses</c> 返回正确的当前状态集合，对应既有 SQL 守卫的 IN 列表。</zh-CN>
        ///   <en>Verifies that <c>AllowedFromStatuses</c> returns the correct current-status set, matching the IN lists of the established SQL guard.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void AllowedFromStatuses_ContainExpected()
        {
            CollectionAssert.AreEquivalent(
                new List<string> { PortalCollaborationItemStatuses.Submitted, PortalCollaborationItemStatuses.InProgress },
                new List<string>(PortalCollaborationItemTransitions.AllowedFromStatuses(PortalCollaborationItemActions.Complete)),
                "Complete 应来自 Submitted 或 InProgress。");
            CollectionAssert.AreEquivalent(
                new List<string> { PortalCollaborationItemStatuses.Completed, PortalCollaborationItemStatuses.Rejected, PortalCollaborationItemStatuses.Cancelled },
                new List<string>(PortalCollaborationItemTransitions.AllowedFromStatuses(PortalCollaborationItemActions.Close)),
                "Close 应来自 Completed/Rejected/Cancelled。");
            CollectionAssert.AreEquivalent(
                new List<string> { PortalCollaborationItemStatuses.Returned },
                new List<string>(PortalCollaborationItemTransitions.AllowedFromStatuses(PortalCollaborationItemActions.Resubmit)),
                "Resubmit 仅来自 Returned。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证同一动作的所有迁移目标状态唯一，从而保证动作到目标状态的映射无歧义。</zh-CN>
        ///   <en>Verifies that all transitions of the same action share one target status, keeping the action-to-target mapping unambiguous.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void EachActionHasUniqueTargetStatus()
        {
            var actions = PortalCollaborationItemTransitions.All.Select(transition => transition.ActionKey).Distinct().ToList();
            foreach (string action in actions)
            {
                var targets = PortalCollaborationItemTransitions.All
                    .Where(transition => string.Equals(transition.ActionKey, action, System.StringComparison.Ordinal))
                    .Select(transition => transition.ToStatus)
                    .Distinct()
                    .ToList();
                Assert.AreEqual(1, targets.Count, string.Format("动作 {0} 的目标状态应唯一。", action));
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 <c>BuildSqlStatusPredicate</c> 经空白归一化后与既有手写 SQL 守卫逐字符语义等价，保证重构不回归。</zh-CN>
        ///   <en>Verifies that <c>BuildSqlStatusPredicate</c>, after whitespace normalization, is character-semantically equivalent to the established hand-written SQL guard, preventing regression from the refactor.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void BuildSqlStatusPredicate_MatchesEstablishedGuard()
        {
            const string EstablishedPredicate =
                "(@ActionKey = N'Submit' AND [ItemStatus] = N'Draft') OR " +
                "(@ActionKey = N'Start' AND [ItemStatus] = N'Submitted') OR " +
                "(@ActionKey = N'Complete' AND [ItemStatus] IN (N'Submitted', N'InProgress')) OR " +
                "(@ActionKey = N'Return' AND [ItemStatus] IN (N'Submitted', N'InProgress')) OR " +
                "(@ActionKey = N'Resubmit' AND [ItemStatus] = N'Returned') OR " +
                "(@ActionKey = N'Reject' AND [ItemStatus] IN (N'Submitted', N'InProgress')) OR " +
                "(@ActionKey = N'Cancel' AND [ItemStatus] IN (N'Draft', N'Submitted', N'Returned')) OR " +
                "(@ActionKey = N'Close' AND [ItemStatus] IN (N'Completed', N'Rejected', N'Cancelled'))";

            string generated = NormalizeWhitespace(PortalCollaborationItemTransitions.BuildSqlStatusPredicate());
            string expected = NormalizeWhitespace(EstablishedPredicate);

            Assert.AreEqual(expected, generated, "生成的 SQL 守卫必须与既有手写谓词语义等价。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把所有空白序列折叠为单个空格，用于忽略格式差异地比较 SQL 谓词。</zh-CN>
        ///   <en>Collapses every whitespace run into a single space to compare SQL predicates ignoring formatting differences.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeWhitespace(string value)
        {
            return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
        }
    }
}
