using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>协同事项与业务申请审核页"待办旁路"的异常隔离契约测试：审核主流程不得因待办服务异常而回滚（与资料更正侧 W-anp-P111 同法处理）。</zh-CN>
    ///   <en>Exception-isolation contract tests for the collaboration-item and business-application review pages' work-item sidecars: the review main flow must not roll back because of a work-item service failure (same treatment as the profile-correction W-anp-P111).</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class CollaborationItemSidecarTests
    {
        /// <summary><lang><zh-CN>待测私有方法（非公开、实例级）。</zh-CN><en>The private, instance-level method under test.</en></lang></summary>
        private static readonly MethodInfo TryCompleteWorkItem = typeof(CollaborationItems)
            .GetMethod("TryCompleteWorkItem", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// <lang><zh-CN>待办服务抛错时，方法应捕获降级，不向上抛出（否则会回滚已成功的处理主流程）。</zh-CN><en>When the work-item service throws, the method must catch and degrade rather than rethrow (which would roll back the already-succeeded handling main flow).</en></lang>
        /// </summary>
        [TestMethod]
        public void TryCompleteWorkItem_DoesNotPropagateWorkItemFailure()
        {
            var page = new CollaborationItems { WorkItemDb = new ThrowingWorkItemDb() };
            TryCompleteWorkItem.Invoke(page, new object[] { 1L, "Approve", "处理备注" });
        }

        /// <summary>
        /// <lang><zh-CN>待办服务正常时，方法应正常完成且恰好投影一次，并携带正确的业务标识。</zh-CN><en>When the work-item service succeeds, the method completes normally, projects exactly once, and carries the correct business identifier.</en></lang>
        /// </summary>
        [TestMethod]
        public void TryCompleteWorkItem_CallsSidecarOnSuccess()
        {
            var fake = new RecordingWorkItemDb();
            var page = new CollaborationItems { WorkItemDb = fake };
            TryCompleteWorkItem.Invoke(page, new object[] { 7L, "Approve", "处理备注" });

            Assert.AreEqual(1, fake.CompleteCallCount, "应通过待办服务投影一次。");
            Assert.AreEqual("7", fake.LastRequest.BusinessId, "投影应携带正确的业务标识。");
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>业务申请审核页"待办旁路"的异常隔离契约测试。</zh-CN>
    ///   <en>Exception-isolation contract test for the business-application review page's work-item sidecar.</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class BusinessApplicationSidecarTests
    {
        /// <summary><lang><zh-CN>待测私有方法（非公开、实例级）。</zh-CN><en>The private, instance-level method under test.</en></lang></summary>
        private static readonly MethodInfo TryCompleteWorkItem = typeof(BusinessApplications)
            .GetMethod("TryCompleteWorkItem", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// <lang><zh-CN>待办服务抛错时，方法应捕获降级，不向上抛出（否则会回滚已成功的审核主流程）。</zh-CN><en>When the work-item service throws, the method must catch and degrade rather than rethrow (which would roll back the already-succeeded review main flow).</en></lang>
        /// </summary>
        [TestMethod]
        public void TryCompleteWorkItem_DoesNotPropagateWorkItemFailure()
        {
            var page = new BusinessApplications { WorkItemDb = new ThrowingWorkItemDb() };
            TryCompleteWorkItem.Invoke(page, new object[] { 1L, "Approve", "审核备注" });
        }

        /// <summary>
        /// <lang><zh-CN>待办服务正常时，方法应正常完成且恰好投影一次，并携带正确的业务标识。</zh-CN><en>When the work-item service succeeds, the method completes normally, projects exactly once, and carries the correct business identifier.</en></lang>
        /// </summary>
        [TestMethod]
        public void TryCompleteWorkItem_CallsSidecarOnSuccess()
        {
            var fake = new RecordingWorkItemDb();
            var page = new BusinessApplications { WorkItemDb = fake };
            TryCompleteWorkItem.Invoke(page, new object[] { 7L, "Approve", "审核备注" });

            Assert.AreEqual(1, fake.CompleteCallCount, "应通过待办服务投影一次。");
            Assert.AreEqual("7", fake.LastRequest.BusinessId, "投影应携带正确的业务标识。");
        }
    }

    /// <summary>
    /// <lang><zh-CN>始终抛错的待办数据访问假实现，用于验证旁路隔离。</zh-CN><en>Work-item data-access fake that always throws, used to verify sidecar isolation.</en></lang>
    /// </summary>
    internal sealed class ThrowingWorkItemDb : IPortalWorkItemDb
    {
        public bool IsSchemaAvailable() => true;

        public PortalWorkItemResult EnsureWorkItem(PortalWorkItemCreateRequest request) =>
            new PortalWorkItemResult(true, 0, "ok");

        public PortalWorkItemResult CompleteBusinessWorkItem(PortalWorkItemCompletionRequest request) =>
            throw new InvalidOperationException("模拟待办服务不可用");

        public IList<PortalWorkItemInfo> GetAdminWorkItems(string status, int take) =>
            new List<PortalWorkItemInfo>();

        public IList<PortalWorkItemEventInfo> GetWorkItemEvents(long workItemId, int take) =>
            new List<PortalWorkItemEventInfo>();

        public PortalWorkItemQueryResult GetWorkItemsForUser(int userId, IEnumerable<string> assignedRoleKeys, string status, int take) =>
            new PortalWorkItemQueryResult(true, new List<PortalWorkItemInfo>());
    }

    /// <summary>
    /// <lang><zh-CN>记录调用的待办数据访问假实现，用于验证正常路径下的投影行为。</zh-CN><en>Work-item data-access fake that records calls, used to verify projection behaviour on the success path.</en></lang>
    /// </summary>
    internal sealed class RecordingWorkItemDb : IPortalWorkItemDb
    {
        /// <summary><lang><zh-CN>CompleteBusinessWorkItem 的调用次数。</zh-CN><en>Call count of CompleteBusinessWorkItem.</en></lang></summary>
        public int CompleteCallCount { get; private set; }

        /// <summary><lang><zh-CN>最近一次投影请求。</zh-CN><en>The most recent projection request.</en></lang></summary>
        public PortalWorkItemCompletionRequest LastRequest { get; private set; }

        public bool IsSchemaAvailable() => true;

        public PortalWorkItemResult EnsureWorkItem(PortalWorkItemCreateRequest request) =>
            new PortalWorkItemResult(true, 0, "ok");

        public PortalWorkItemResult CompleteBusinessWorkItem(PortalWorkItemCompletionRequest request)
        {
            CompleteCallCount++;
            LastRequest = request;
            return new PortalWorkItemResult(true, 0, "ok");
        }

        public IList<PortalWorkItemInfo> GetAdminWorkItems(string status, int take) =>
            new List<PortalWorkItemInfo>();

        public IList<PortalWorkItemEventInfo> GetWorkItemEvents(long workItemId, int take) =>
            new List<PortalWorkItemEventInfo>();

        public PortalWorkItemQueryResult GetWorkItemsForUser(int userId, IEnumerable<string> assignedRoleKeys, string status, int take) =>
            new PortalWorkItemQueryResult(true, new List<PortalWorkItemInfo>());
    }
}
