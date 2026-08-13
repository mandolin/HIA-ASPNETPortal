using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求后台处理页。</zh-CN>
    ///   <en>Administration page for employee-profile correction requests.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P6.4.3 第一版只处理请求状态和管理员备注，不直接修改员工主数据；真实资料修改仍走员工目录维护。审核成功、运营审计和待办同步由不同调用链完成，本页不宣称它们构成跨服务原子事务。</zh-CN>
    ///   <en>The first P6.4.3 version updates only request status and administrator notes. Actual profile changes still go through employee-directory maintenance. Review success, operations audit, and work-item synchronization use separate call paths; this page does not claim a cross-service atomic transaction.</en>
    /// </lang>
    /// </remarks>
    public partial class EmployeeProfileCorrectionRequests : PortalPage<EmployeeProfileCorrectionRequests>
    {
        private const int PageSize = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料更正请求数据服务。</zh-CN>
        ///   <en>Employee-profile correction-request data service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeProfileCorrectionRequestDb CorrectionRequestDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>轻量待办数据服务，用于把资料更正处理同步为待办完成事件。</zh-CN>
        ///   <en>Lightweight work-item data service used to mirror correction reviews into work-item completion events.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化员工资料更正请求后台页。</zh-CN>
        ///   <en>Initializes the employee-profile correction-request administration page.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnsureCanViewRequests())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindStatusFilter();
                BindRequests();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前筛选条件重新绑定请求列表。</zh-CN>
        ///   <en>Rebinds requests using the current filter.</en>
        /// </lang>
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            if (!EnsureCanViewRequests())
            {
                return;
            }

            BindRequests();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理请求列表中的管理员状态命令。</zh-CN>
        ///   <en>Handles administrator status commands from the request list.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>命令状态先经过固定白名单和按状态分支的权限检查，再解析正数请求标识并调用数据服务。审核事实成功后才记录运营审计；待办同步是后续旁路，不能被文档化为与审核写入同一事务。</zh-CN>
        ///   <en>The command status passes a fixed allowlist and status-specific permission check before the positive request identifier is parsed and the data service is called. Operations audit is recorded only after the review fact succeeds; work-item synchronization is a later sidecar and must not be documented as the same transaction as the review write.</en>
        /// </lang>
        /// </remarks>
        protected void RequestsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string targetStatus = Convert.ToString(e.CommandName, CultureInfo.InvariantCulture);
            if (!IsSupportedTargetStatus(targetStatus))
            {
                // <lang>
                //   <zh-CN>命令名来自回发控件，不能直接当作任意状态传入数据层；未知值只刷新列表并返回。</zh-CN>
                //   <en>The command name comes from a postback control and cannot be passed to the data layer as an arbitrary status; unknown values only refresh the list and return.</en>
                // </lang>
                MessageLabel.Text = "Unsupported request status.";
                BindRequests();
                return;
            }

            if (!EnsureCanApplyRequestStatus(targetStatus))
            {
                // <lang>
                //   <zh-CN>关闭状态使用取消权限，其它处理状态使用审核权限；权限门禁在每次命令回发时重新执行。</zh-CN>
                //   <en>The closed status uses the cancel permission while other processing statuses use the review permission; the gate is re-evaluated on every command postback.</en>
                // </lang>
                return;
            }

            if (CorrectionRequestDb == null)
            {
                ShowUnavailable("Employee-profile correction request data service is not registered.");
                return;
            }

            long requestId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out requestId))
            {
                MessageLabel.Text = "Invalid request id.";
                return;
            }

            TextBox noteBox = e.Item.FindControl("ReviewNoteTextBox") as TextBox;
            string reviewNote = noteBox == null ? string.Empty : noteBox.Text;
            EmployeeProfileCorrectionRequestResult result = CorrectionRequestDb.ReviewRequest(
                new EmployeeProfileCorrectionReviewRequest
                {
                    RequestId = requestId,
                    RequestStatus = targetStatus,
                    ReviewNote = NormalizeInput(reviewNote, 1000),
                    ReviewedUtc = DateTime.UtcNow,
                    ReviewedBy = GetCurrentUserName()
                });

            if (!result.Succeeded)
            {
                // <lang>
                //   <zh-CN>数据服务返回失败事实时不记录成功审计或待办完成，重新绑定列表以呈现服务端状态。</zh-CN>
                //   <en>When the data service returns a failed fact, do not record success audit or work-item completion; rebind the list to show the server state.</en>
                // </lang>
                MessageLabel.Text = result.Message;
                BindRequests();
                return;
            }

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.EmployeeProfileCorrectionReviewed,
                PortalOperationAuditEvents.EmployeeProfileCorrectionRequestTargetType,
                result.RequestId.ToString(CultureInfo.InvariantCulture),
                "Employee profile correction reviewed. RequestStatus=" + targetStatus,
                Context);

            // <lang>
            //   <zh-CN>审核写入和运营审计已经完成后才尝试待办旁路；旁路调用是否传播异常遵循其既有实现。</zh-CN>
            //   <en>Attempt the work-item sidecar only after the review write and operations audit complete; whether sidecar exceptions propagate follows its existing implementation.</en>
            // </lang>
            TryCompleteWorkItem(result.RequestId, targetStatus, reviewNote);

            MessageLabel.Text = "Correction request status updated.";
            BindRequests();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化资料更正请求状态筛选项。</zh-CN>
        ///   <en>Initializes employee-profile correction request status filter options.</en>
        /// </lang>
        /// </summary>
        private void BindStatusFilter()
        {
            StatusFilterList.Items.Clear();
            StatusFilterList.Items.Add(new ListItem("All", string.Empty));
            StatusFilterList.Items.Add(new ListItem(EmployeeProfileCorrectionRequestStatuses.Submitted, EmployeeProfileCorrectionRequestStatuses.Submitted));
            StatusFilterList.Items.Add(new ListItem(EmployeeProfileCorrectionRequestStatuses.Reviewed, EmployeeProfileCorrectionRequestStatuses.Reviewed));
            StatusFilterList.Items.Add(new ListItem(EmployeeProfileCorrectionRequestStatuses.Closed, EmployeeProfileCorrectionRequestStatuses.Closed));
            StatusFilterList.Items.Add(new ListItem(EmployeeProfileCorrectionRequestStatuses.Rejected, EmployeeProfileCorrectionRequestStatuses.Rejected));
            StatusFilterList.SelectedValue = EmployeeProfileCorrectionRequestStatuses.Submitted;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前状态筛选下的更正请求并绑定后台列表。</zh-CN>
        ///   <en>Reads correction requests for the current status filter and binds the administration list.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>读取只使用当前状态筛选和固定 PageSize；schema 或依赖不可用时显示受控提示并清空列表。展示行随后由 ASPX 的编码绑定输出，不在此层承担 HTML 或授权。</zh-CN>
        ///   <en>The read uses only the current status filter and fixed PageSize; unavailable schema or dependencies produce a controlled message and an empty list. The display rows are later emitted through encoded ASPX bindings, so this method does not own HTML encoding or authorization.</en>
        /// </lang>
        /// </remarks>
        private void BindRequests()
        {
            if (CorrectionRequestDb == null)
            {
                ShowUnavailable("Employee-profile correction request data service is not registered.");
                return;
            }

            if (!CorrectionRequestDb.IsSchemaAvailable())
            {
                ShowUnavailable("P6.4 employee-profile correction request schema is unavailable. Run PortalBiz_EmployeeProfileCorrectionRequests.sql.");
                return;
            }

            IList<EmployeeProfileCorrectionRequestInfo> requests = CorrectionRequestDb.GetAdminRequests(
                StatusFilterList.SelectedValue,
                PageSize);
            RequestsRepeater.DataSource = requests.Select(request => new EmployeeProfileCorrectionAdminRow(request)).ToList();
            RequestsRepeater.DataBind();

            ResultLabel.Text = "Showing up to " + PageSize.ToString(CultureInfo.InvariantCulture) +
                               " requests; count: " + requests.Count.ToString(CultureInfo.InvariantCulture) + ".";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示请求数据不可用提示，并清空请求列表。</zh-CN>
        ///   <en>Displays request-data unavailable messages and clears the request list.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该回退只清理本页展示状态，不伪造数据可用性、不改变权限，也不写入诊断或数据库。</zh-CN>
        ///   <en>This fallback only clears the page's display state; it does not fake data availability, change permissions, or write diagnostics or database state.</en>
        /// </lang>
        /// </remarks>
        private void ShowUnavailable(string message)
        {
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            RequestsRepeater.DataSource = Enumerable.Empty<EmployeeProfileCorrectionAdminRow>();
            RequestsRepeater.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认当前用户具备查看更正请求的权限。</zh-CN>
        ///   <en>Ensures that the current user can view correction requests.</en>
        /// </lang>
        /// </summary>
        private bool EnsureCanViewRequests()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.EmployeeProfileCorrectionRequestReview,
                PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按目标状态确认当前用户具备处理权限。</zh-CN>
        ///   <en>Ensures that the current user has processing permission for the target status.</en>
        /// </lang>
        /// </summary>
        private bool EnsureCanApplyRequestStatus(string targetStatus)
        {
            if (string.Equals(targetStatus, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal))
            {
                return PortalAuthorization.EnsureAnyPermission(
                    Context,
                    PortalPermissionKeys.EmployeeProfileCorrectionRequestCancel,
                    PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin);
            }

            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.EmployeeProfileCorrectionRequestReview,
                PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断列表命令传入的目标状态是否在本页允许范围内。</zh-CN>
        ///   <en>Determines whether the target status from a list command is allowed by this page.</en>
        /// </lang>
        /// </summary>
        private static bool IsSupportedTargetStatus(string targetStatus)
        {
            return string.Equals(targetStatus, EmployeeProfileCorrectionRequestStatuses.Reviewed, StringComparison.Ordinal) ||
                   string.Equals(targetStatus, EmployeeProfileCorrectionRequestStatuses.Rejected, StringComparison.Ordinal) ||
                   string.Equals(targetStatus, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前登录用户名，无法取得时使用系统占位值。</zh-CN>
        ///   <en>Reads the current signed-in user name, using a system placeholder when unavailable.</en>
        /// </lang>
        /// </summary>
        private string GetCurrentUserName()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated
                ? Context.User.Identity.Name
                : "system";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试把资料更正处理结果同步到轻量待办。</zh-CN>
        ///   <en>Attempts to mirror the profile-correction review result into lightweight work items.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>当前实现仅在待办依赖为空或请求标识非正数时短路；对实际 CompleteBusinessWorkItem 调用的异常保留既有传播行为。因此该方法是旁路调用边界，不是失败隔离或跨服务事务保证。</zh-CN>
        ///   <en>The current implementation short-circuits only when the work-item dependency is absent or the request identifier is non-positive; exceptions from CompleteBusinessWorkItem retain their existing propagation behavior. This method is therefore a sidecar-call boundary, not a failure-isolation or cross-service transaction guarantee.</en>
        /// </lang>
        /// </remarks>
        private void TryCompleteWorkItem(long requestId, string requestStatus, string reviewNote)
        {
            // <lang>
            //   <zh-CN>待办是审核后的旁路增强能力；当前调用不捕获服务异常，是否隔离失败属于后续实现任务，不能把“旁路”误写成“不会传播异常”。</zh-CN>
            //   <en>Work items are a sidecar enhancement after review; this call currently does not catch service exceptions, so failure isolation is a follow-up implementation task and “sidecar” must not be misread as “exceptions never propagate.”</en>
            // </lang>
            if (WorkItemDb == null || requestId <= 0)
            {
                return;
            }

            WorkItemDb.CompleteBusinessWorkItem(
                new PortalWorkItemCompletionRequest
                {
                    BusinessKind = PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest,
                    BusinessId = requestId.ToString(CultureInfo.InvariantCulture),
                    EventType = MapWorkItemEventType(requestStatus),
                    TargetStatus = MapWorkItemStatus(requestStatus),
                    ActorName = GetCurrentUserName(),
                    Comment = NormalizeInput(reviewNote, 1000),
                    OccurredUtc = DateTime.UtcNow
                });
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将资料更正请求状态映射为待办事件类型。</zh-CN>
        ///   <en>Maps a profile-correction request status to a work-item event type.</en>
        /// </lang>
        /// </summary>
        private static string MapWorkItemEventType(string requestStatus)
        {
            if (string.Equals(requestStatus, EmployeeProfileCorrectionRequestStatuses.Rejected, StringComparison.Ordinal))
            {
                return PortalWorkItemEventTypes.Rejected;
            }

            if (string.Equals(requestStatus, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal))
            {
                return PortalWorkItemEventTypes.Cancelled;
            }

            return PortalWorkItemEventTypes.Approved;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将资料更正请求状态映射为待办终态。</zh-CN>
        ///   <en>Maps a profile-correction request status to a final work-item status.</en>
        /// </lang>
        /// </summary>
        private static string MapWorkItemStatus(string requestStatus)
        {
            return string.Equals(requestStatus, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal)
                ? PortalWorkItemStatuses.Cancelled
                : PortalWorkItemStatuses.Completed;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪管理员输入，避免备注字段超过数据库约定长度。</zh-CN>
        ///   <en>Trims administrator input to prevent notes from exceeding the database contract length.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeInput(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求后台展示行。</zh-CN>
    ///   <en>Administration display row for an employee-profile correction request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类型只投影审核列表所需字段和 UTC 展示文本，不执行授权、状态变更或敏感值净化；调用的 ASPX 标记使用 <c>&lt;%#:</c> 编码绑定负责 HTML 输出编码。</zh-CN>
    ///   <en>This type only projects fields and UTC display text needed by the review list; it does not authorize, change status, or sanitize sensitive values. The consuming ASPX markup uses <c>&lt;%#:</c> encoded bindings for HTML output encoding.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileCorrectionAdminRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从资料更正请求数据对象创建后台展示行。</zh-CN>
        ///   <en>Creates an administration display row from a profile-correction request data object.</en>
        /// </lang>
        /// </summary>
        internal EmployeeProfileCorrectionAdminRow(EmployeeProfileCorrectionRequestInfo request)
        {
            RequestId = request.RequestId;
            SubmittedUtcText = request.SubmittedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            EmployeeText = request.EmployeeCode + " / " + request.EmployeeDisplayName;
            UserText = request.UserId.ToString(CultureInfo.InvariantCulture) + " / " + request.UserName;
            FieldName = request.FieldName;
            CurrentValueSnapshot = EmptyToNone(request.CurrentValueSnapshot);
            ProposedValue = EmptyToNone(request.ProposedValue);
            RequestNote = EmptyToNone(request.RequestNote);
            RequestStatus = request.RequestStatus;
            ReviewText = request.ReviewedUtc.HasValue
                ? request.ReviewedUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) +
                  " / " + EmptyToNone(request.ReviewedBy) +
                  " / " + EmptyToNone(request.ReviewNote)
                : "(not reviewed)";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>请求标识。</zh-CN>
        ///   <en>Request identifier.</en>
        /// </lang>
        /// </summary>
        public long RequestId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交时间文本。</zh-CN>
        ///   <en>Submission time text.</en>
        /// </lang>
        /// </summary>
        public string SubmittedUtcText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工文本。</zh-CN>
        ///   <en>Employee text.</en>
        /// </lang>
        /// </summary>
        public string EmployeeText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户文本。</zh-CN>
        ///   <en>User text.</en>
        /// </lang>
        /// </summary>
        public string UserText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>字段名。</zh-CN>
        ///   <en>Field name.</en>
        /// </lang>
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前值快照。</zh-CN>
        ///   <en>Current-value snapshot.</en>
        /// </lang>
        /// </summary>
        public string CurrentValueSnapshot { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>建议值。</zh-CN>
        ///   <en>Proposed value.</en>
        /// </lang>
        /// </summary>
        public string ProposedValue { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工说明。</zh-CN>
        ///   <en>Employee note.</en>
        /// </lang>
        /// </summary>
        public string RequestNote { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>请求状态。</zh-CN>
        ///   <en>Request status.</en>
        /// </lang>
        /// </summary>
        public string RequestStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理信息文本。</zh-CN>
        ///   <en>Review information text.</en>
        /// </lang>
        /// </summary>
        public string ReviewText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空展示值统一转换为页面占位文本。</zh-CN>
        ///   <en>Converts empty display values to the shared page placeholder text.</en>
        /// </lang>
        /// </summary>
        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
