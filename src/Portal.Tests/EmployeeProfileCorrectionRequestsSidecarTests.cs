using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>资料更正审核页"待办旁路"的异常隔离契约测试：审核主流程不得因待办服务异常而回滚（闭合 C-anp-P19 W-anp-P111）。</zh-CN>
    ///   <en>Exception-isolation contract tests for the profile-correction review page's work-item sidecar: the review main flow must not roll back because of a work-item service failure (closes C-anp-P19 W-anp-P111).</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>通过反射调用私有方法 <c>TryCompleteWorkItem</c>，用假 <c>IPortalWorkItemDb</c> 模拟"待办服务抛错"与"待办服务正常"两种路径，锁定"旁路失败不回滚审核"的契约。</zh-CN>
    ///   <en>Invokes the private <c>TryCompleteWorkItem</c> via reflection with a fake <c>IPortalWorkItemDb</c> that simulates both a throwing and a succeeding work-item service, pinning the "sidecar failure does not roll back the review" contract.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public class EmployeeProfileCorrectionRequestsSidecarTests
    {
        /// <summary>
        /// <lang><zh-CN>待测私有方法（非公开、实例级）。</zh-CN><en>The private, instance-level method under test.</en></lang>
        /// </summary>
        private static readonly MethodInfo TryCompleteWorkItem = typeof(EmployeeProfileCorrectionRequests)
            .GetMethod("TryCompleteWorkItem", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办服务抛错时，方法应捕获降级，不向上抛出（否则会回滚已成功的审核主流程）。</zh-CN>
        ///   <en>When the work-item service throws, the method must catch and degrade rather than rethrow (which would roll back the already-succeeded review main flow).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryCompleteWorkItem_DoesNotPropagateWorkItemFailure()
        {
            var page = new EmployeeProfileCorrectionRequests
            {
                WorkItemDb = new ThrowingWorkItemDb()
            };

            // <lang>
            //   <zh-CN>若旁路未被隔离，反射调用会把内部异常包装成 TargetInvocationException 抛出，此处即判为回滚。</zh-CN>
            //   <en>If the sidecar were not isolated, the reflective call would surface the inner exception wrapped in a TargetInvocationException — that is the rollback signal we must not see.</en>
            // </lang>
            TryCompleteWorkItem.Invoke(page, new object[] { 1L, EmployeeProfileCorrectionRequestStatuses.Reviewed, "审核备注" });
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办服务正常时，方法应正常完成且恰好投影一次，并携带正确的业务标识。</zh-CN>
        ///   <en>When the work-item service succeeds, the method completes normally, projects exactly once, and carries the correct business identifier.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryCompleteWorkItem_CallsSidecarOnSuccess()
        {
            var fake = new RecordingWorkItemDb();
            var page = new EmployeeProfileCorrectionRequests
            {
                WorkItemDb = fake
            };

            TryCompleteWorkItem.Invoke(page, new object[] { 7L, EmployeeProfileCorrectionRequestStatuses.Closed, "关闭备注" });

            Assert.AreEqual(1, fake.CompleteCallCount, "应通过待办服务投影一次。");
            Assert.AreEqual("7", fake.LastRequest.BusinessId, "投影应携带正确的业务标识。");
        }

        /// <summary>
        /// <lang><zh-CN>始终抛错的待办数据访问假实现，用于验证旁路隔离。</zh-CN><en>Work-item data-access fake that always throws, used to verify sidecar isolation.</en></lang>
        /// </summary>
        private sealed class ThrowingWorkItemDb : IPortalWorkItemDb
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
        private sealed class RecordingWorkItemDb : IPortalWorkItemDb
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
}
