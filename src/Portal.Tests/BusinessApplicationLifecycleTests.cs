using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>BusinessApplicationLifecycle 门面契约测试：锁定「历史词汇归一化」「仅 Returned 可重提（Q2）」「state→state 迁移判定（复用既有单一事实源）」。</zh-CN>
    ///   <en>Contract tests for the BusinessApplicationLifecycle facade: legacy-vocabulary normalization, "only Returned is resubmittable" (Q2), and state→state transition judgement (delegating to the existing single source of truth).</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class BusinessApplicationLifecycleTests
    {
        /// <summary><lang><zh-CN>历史值归一化，规范值原样透传，空值返回 null。</zh-CN><en>Legacy values are normalized, canonical values pass through, null returns null.</en></lang></summary>
        [TestMethod]
        public void NormalizeToCanonical_MapsLegacyValuesAndPassesThroughCanonical()
        {
            Assert.AreEqual(
                PortalBusinessApplicationStatuses.Submitted,
                BusinessApplicationLifecycle.NormalizeToCanonical("Pending"),
                "Pending 应归一化为 Submitted（Q1）。");
            Assert.AreEqual(
                PortalBusinessApplicationStatuses.InReview,
                BusinessApplicationLifecycle.NormalizeToCanonical("Reviewed"),
                "Reviewed 应归一化为 InReview（CorrectionRequest 展示统一）。");
            Assert.AreEqual(
                PortalBusinessApplicationStatuses.Returned,
                BusinessApplicationLifecycle.NormalizeToCanonical(PortalBusinessApplicationStatuses.Returned),
                "规范值原样透传。");
            Assert.AreEqual(
                PortalBusinessApplicationStatuses.Approved,
                BusinessApplicationLifecycle.NormalizeToCanonical(PortalBusinessApplicationStatuses.Approved),
                "规范值原样透传。");
            Assert.IsNull(BusinessApplicationLifecycle.NormalizeToCanonical(null), "空值返回 null。");
        }

        /// <summary><lang><zh-CN>仅 Returned 可重提；Rejected 终态不可重提（Q2）。</zh-CN><en>Only Returned is resubmittable; Rejected is terminal (Q2).</en></lang></summary>
        [TestMethod]
        public void IsResubmittable_OnlyReturned()
        {
            Assert.IsTrue(
                BusinessApplicationLifecycle.IsResubmittable(PortalBusinessApplicationStatuses.Returned),
                "Returned 可重提。");
            Assert.IsFalse(
                BusinessApplicationLifecycle.IsResubmittable(PortalBusinessApplicationStatuses.Rejected),
                "Rejected 为终态不可重提（Q2）。");
            Assert.IsFalse(
                BusinessApplicationLifecycle.IsResubmittable(PortalBusinessApplicationStatuses.Submitted),
                "已提交不可重提。");
            Assert.IsFalse(
                BusinessApplicationLifecycle.IsResubmittable(PortalBusinessApplicationStatuses.Approved),
                "已批准不可重提。");
            // 历史 Pending 归一化为 Submitted，同样不可重提
            Assert.IsFalse(
                BusinessApplicationLifecycle.IsResubmittable("Pending"),
                "历史 Pending 归一化为 Submitted，不可重提。");
        }

        /// <summary><lang><zh-CN>state→state 迁移判定复用既有单一事实源：Resubmit 仅 Returned→Submitted 合法，Rejected→Submitted 非法，同态非法，审核动作合法。</zh-CN><en>State→state transition judgement delegates to the existing single source of truth: Resubmit only Returned→Submitted is legal, Rejected→Submitted is illegal, same-state is illegal, review actions are legal.</en></lang></summary>
        [TestMethod]
        public void CanTransition_DelegatesToSingleSourceOfTruth()
        {
            Assert.IsTrue(
                BusinessApplicationLifecycle.CanTransition(PortalBusinessApplicationStatuses.Returned, PortalBusinessApplicationStatuses.Submitted),
                "Resubmit：Returned→Submitted 合法。");
            Assert.IsFalse(
                BusinessApplicationLifecycle.CanTransition(PortalBusinessApplicationStatuses.Rejected, PortalBusinessApplicationStatuses.Submitted),
                "Rejected→Submitted 非法（Q2）。");
            Assert.IsFalse(
                BusinessApplicationLifecycle.CanTransition(PortalBusinessApplicationStatuses.Submitted, PortalBusinessApplicationStatuses.Submitted),
                "同态非迁移。");
            // 历史 Pending 归一化为 Submitted，不可「重提」已提交项
            Assert.IsFalse(
                BusinessApplicationLifecycle.CanTransition("Pending", PortalBusinessApplicationStatuses.Submitted),
                "Pending（→Submitted）不可对自身重提。");
            Assert.IsTrue(
                BusinessApplicationLifecycle.CanTransition(PortalBusinessApplicationStatuses.Submitted, PortalBusinessApplicationStatuses.Approved),
                "审核动作 Submitted→Approved 合法。");
            Assert.IsTrue(
                BusinessApplicationLifecycle.CanTransition(PortalBusinessApplicationStatuses.InReview, PortalBusinessApplicationStatuses.Rejected),
                "审核动作 InReview→Rejected 合法。");
            Assert.IsFalse(
                BusinessApplicationLifecycle.CanTransition(PortalBusinessApplicationStatuses.Closed, PortalBusinessApplicationStatuses.Submitted),
                "终态 Closed 不可重提。");
        }
    }
}
