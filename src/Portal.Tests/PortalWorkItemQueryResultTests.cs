using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>待办查询结果类型的契约测试，锁定"成功无命中"与"读取失败"可区分这一语义。</zh-CN>
    ///   <en>Contract tests for the work-item query result type, pinning the semantics that "succeeded with no matches" is distinguishable from "read failure".</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class PortalWorkItemQueryResultTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>失败结果的成功标记必须为假，使前台能呈现失败态而不是空态。</zh-CN>
        ///   <en>A failed result must carry a false success flag so a front end renders the failure state instead of the empty state.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void FailedResult_KeepsSucceededFalse()
        {
            // <lang>
            //   <zh-CN>构造一个失败结果：成功标记为假，列表为空引用并在构造时归一化。</zh-CN>
            //   <en>Construct a failed result: the success flag is false and the null list is normalized during construction.</en>
            // </lang>
            PortalWorkItemQueryResult result = new PortalWorkItemQueryResult(false, null);

            Assert.IsFalse(result.Succeeded, "失败结果必须把成功标记保持为假。");
            Assert.IsNotNull(result.Items, "失败结果的列表也不得为空引用。");
            Assert.AreEqual(0, result.Items.Count, "失败结果不应携带任何待办投影。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>成功但无命中的结果必须为真且列表为空，与读取失败区分开。</zh-CN>
        ///   <en>A successful result with no matches must be true with an empty list, distinguishing it from a read failure.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void SucceededEmptyResult_IsDistinguishableFromFailure()
        {
            // <lang>
            //   <zh-CN>构造一个成功但无命中的结果，用于与失败结果做语义对比。</zh-CN>
            //   <en>Construct a successful result with no matches for semantic comparison against a failed result.</en>
            // </lang>
            PortalWorkItemQueryResult emptyResult = new PortalWorkItemQueryResult(true, new List<PortalWorkItemInfo>());
            PortalWorkItemQueryResult failedResult = new PortalWorkItemQueryResult(false, null);

            Assert.IsTrue(emptyResult.Succeeded, "无命中但成功的结果必须把成功标记保持为真。");
            Assert.AreEqual(0, emptyResult.Items.Count, "无命中的成功结果列表应为空。");
            Assert.AreNotEqual(
                emptyResult.Succeeded,
                failedResult.Succeeded,
                "成功无命中与读取失败必须可通过成功标记区分。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>成功结果必须原样保留调用方传入的投影列表。</zh-CN>
        ///   <en>A successful result must preserve the caller-supplied projection list as-is.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void SucceededResult_PreservesSuppliedItems()
        {
            // <lang>
            //   <zh-CN>准备一条待办投影作为传入列表的唯一元素。</zh-CN>
            //   <en>Prepare one work-item projection as the only element of the supplied list.</en>
            // </lang>
            IList<PortalWorkItemInfo> items = new List<PortalWorkItemInfo> { new PortalWorkItemInfo() };

            PortalWorkItemQueryResult result = new PortalWorkItemQueryResult(true, items);

            Assert.IsTrue(result.Succeeded, "成功结果必须把成功标记保持为真。");
            Assert.AreSame(items, result.Items, "成功结果应原样保留传入的列表实例。");
            Assert.AreEqual(1, result.Items.Count, "成功结果应保留传入列表的元素数量。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>列表为空引用时必须归一化为非空空列表，避免消费方出现空引用判断分支。</zh-CN>
        ///   <en>A null list must be normalized to a non-null empty list so consumers need no null-check branch.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Constructor_NullItems_NormalizesToEmptyList()
        {
            PortalWorkItemQueryResult result = new PortalWorkItemQueryResult(true, null);

            Assert.IsNotNull(result.Items, "空引用列表必须被归一化为非空列表。");
            Assert.AreEqual(0, result.Items.Count, "归一化后的列表应为空。");
        }
    }
}
