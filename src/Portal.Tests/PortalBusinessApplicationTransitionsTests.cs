using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>业务申请显式迁移表的契约测试，锁定合法迁移、fail-closed 判定，以及生成的 SQL 守卫与显式化之前手写窗口的语义等价性。</zh-CN>
    ///   <en>Contract tests for the business-application explicit transition table, pinning the legal transitions, the fail-closed judgement, and the semantic equivalence between the generated SQL guard and the hand-written window it replaced.</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class PortalBusinessApplicationTransitionsTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>四个审核动作都应能从待审核窗口的两个状态发起。</zh-CN>
        ///   <en>All four review actions must be startable from both statuses inside the review window.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsLegalTransition_AllowsReviewActionsFromReviewWindow()
        {
            string[] reviewActions =
            {
                PortalWorkflowActions.Approve,
                PortalWorkflowActions.Return,
                PortalWorkflowActions.Reject,
                PortalWorkflowActions.Close
            };

            foreach (string action in reviewActions)
            {
                Assert.IsTrue(
                    PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Submitted, action),
                    "审核动作必须允许从 Submitted 发起：" + action);
                Assert.IsTrue(
                    PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.InReview, action),
                    "审核动作必须允许从 InReview 发起：" + action);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>Resubmit 只允许从 Returned 发起，且不参与后台审核谓词。</zh-CN>
        ///   <en>Resubmit is allowed only from Returned and does not participate in the administration review predicate.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsLegalTransition_ResubmitOnlyFromReturned()
        {
            Assert.IsTrue(
                PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Returned, PortalWorkflowActions.Resubmit),
                "Resubmit 必须允许从 Returned 发起。");
            Assert.IsFalse(
                PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Submitted, PortalWorkflowActions.Resubmit),
                "Resubmit 不得从 Submitted 发起。");
            Assert.IsFalse(
                PortalBusinessApplicationTransitions.BuildSqlStatusPredicate().Contains(PortalWorkflowActions.Resubmit),
                "自助动作不得进入后台审核 SQL 谓词。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>终态或非审核窗口状态发起审核动作必须被拒绝（fail-closed）。</zh-CN>
        ///   <en>Starting a review action from a terminal or out-of-window status must be rejected (fail-closed).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsLegalTransition_RejectsOutOfWindowSources()
        {
            Assert.IsFalse(
                PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Approved, PortalWorkflowActions.Approve),
                "已批准状态不得再次批准。");
            Assert.IsFalse(
                PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Draft, PortalWorkflowActions.Close),
                "草稿状态不得直接关闭。");
            Assert.IsFalse(
                PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Returned, PortalWorkflowActions.Approve),
                "退回状态不得直接批准（需先重提）。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>空白或未知入参一律不合法。</zh-CN>
        ///   <en>Blank or unknown inputs are always illegal.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void IsLegalTransition_RejectsBlankAndUnknownInput()
        {
            Assert.IsFalse(PortalBusinessApplicationTransitions.IsLegalTransition(null, PortalWorkflowActions.Approve), "空状态必须被拒绝。");
            Assert.IsFalse(PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Submitted, null), "空动作必须被拒绝。");
            Assert.IsFalse(PortalBusinessApplicationTransitions.IsLegalTransition(PortalBusinessApplicationStatuses.Submitted, "NotAnAction"), "未知动作必须被拒绝。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>动作到目标状态的映射应覆盖全部五个动作，未知动作不命中。</zh-CN>
        ///   <en>The action-to-target mapping must cover all five actions and miss unknown ones.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryGetTargetStatus_CoversAllActions()
        {
            AssertTarget(PortalWorkflowActions.Approve, PortalBusinessApplicationStatuses.Approved);
            AssertTarget(PortalWorkflowActions.Return, PortalBusinessApplicationStatuses.Returned);
            AssertTarget(PortalWorkflowActions.Reject, PortalBusinessApplicationStatuses.Rejected);
            AssertTarget(PortalWorkflowActions.Close, PortalBusinessApplicationStatuses.Closed);
            AssertTarget(PortalWorkflowActions.Resubmit, PortalBusinessApplicationStatuses.Submitted);

            string unknown;
            Assert.IsFalse(
                PortalBusinessApplicationTransitions.TryGetTargetStatus("NotAnAction", out unknown),
                "未知动作不得命中目标状态。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成的 SQL 谓词必须与显式化之前的手写窗口语义等价：每个审核动作都要求状态落在 Submitted/InReview。</zh-CN>
        ///   <en>The generated SQL predicate must be semantically equivalent to the hand-written window it replaced: every review action requires the status to fall inside Submitted/InReview.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void BuildSqlStatusPredicate_MatchesEstablishedWindow()
        {
            string predicate = PortalBusinessApplicationTransitions.BuildSqlStatusPredicate();
            string[] reviewActions =
            {
                PortalWorkflowActions.Approve,
                PortalWorkflowActions.Return,
                PortalWorkflowActions.Reject,
                PortalWorkflowActions.Close
            };

            foreach (string action in reviewActions)
            {
                string expected = "(@ActionKey = N'" + action + "' AND [ApplicationStatus] IN (N'Submitted', N'InReview'))";
                Assert.IsTrue(
                    predicate.Contains(expected),
                    "谓词必须为审核动作生成与既有窗口等价的子句：" + action);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>合法来源集合应与迁移声明一致；未知动作为空集合。</zh-CN>
        ///   <en>Legal source sets must match the declaration, and an unknown action yields an empty set.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void AllowedFromStatuses_MatchesDeclarations()
        {
            IList<string> review = PortalBusinessApplicationTransitions.AllowedFromStatuses(PortalWorkflowActions.Approve);
            Assert.AreEqual(2, review.Count, "审核动作应有两个合法来源。");
            Assert.IsTrue(review.Contains(PortalBusinessApplicationStatuses.Submitted), "合法来源应包含 Submitted。");
            Assert.IsTrue(review.Contains(PortalBusinessApplicationStatuses.InReview), "合法来源应包含 InReview。");

            IList<string> resubmit = PortalBusinessApplicationTransitions.AllowedFromStatuses(PortalWorkflowActions.Resubmit);
            Assert.AreEqual(1, resubmit.Count, "Resubmit 应只有一个合法来源。");
            Assert.IsTrue(resubmit.Contains(PortalBusinessApplicationStatuses.Returned), "Resubmit 的合法来源应为 Returned。");

            Assert.AreEqual(0, PortalBusinessApplicationTransitions.AllowedFromStatuses("NotAnAction").Count, "未知动作的来源集合应为空。");
        }

        /// <summary><lang><zh-CN>断言某动作映射到期望的目标状态。</zh-CN><en>Asserts that an action maps to the expected target status.</en></lang></summary>
        /// <param name="actionKey"><l><zh-CN>动作键。</zh-CN><en>The action key.</en></l></param>
        /// <param name="expectedStatus"><l><zh-CN>期望目标状态。</zh-CN><en>The expected target status.</en></l></param>
        private static void AssertTarget(string actionKey, string expectedStatus)
        {
            string targetStatus;
            bool resolved = PortalBusinessApplicationTransitions.TryGetTargetStatus(actionKey, out targetStatus);

            Assert.IsTrue(resolved, "动作应命中目标状态：" + actionKey);
            Assert.AreEqual(expectedStatus, targetStatus, "目标状态不符：" + actionKey);
        }
    }
}
