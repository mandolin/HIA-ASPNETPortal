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
    ///   <zh-CN>P6.4.3 第一版只处理请求状态和管理员备注，不直接修改员工主数据；真实资料修改仍走员工目录维护。</zh-CN>
    ///   <en>The first P6.4.3 version updates only request status and administrator notes. Actual profile changes still go through employee-directory maintenance.</en>
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
        protected void RequestsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string targetStatus = Convert.ToString(e.CommandName, CultureInfo.InvariantCulture);
            if (!IsSupportedTargetStatus(targetStatus))
            {
                MessageLabel.Text = "Unsupported request status.";
                BindRequests();
                return;
            }

            if (!EnsureCanApplyRequestStatus(targetStatus))
            {
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

            TryCompleteWorkItem(result.RequestId, targetStatus, reviewNote);

            MessageLabel.Text = "Correction request status updated.";
            BindRequests();
        }

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

        private void ShowUnavailable(string message)
        {
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            RequestsRepeater.DataSource = Enumerable.Empty<EmployeeProfileCorrectionAdminRow>();
            RequestsRepeater.DataBind();
        }

        private bool EnsureCanViewRequests()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.EmployeeProfileCorrectionRequestReview,
                PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin);
        }

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

        private static bool IsSupportedTargetStatus(string targetStatus)
        {
            return string.Equals(targetStatus, EmployeeProfileCorrectionRequestStatuses.Reviewed, StringComparison.Ordinal) ||
                   string.Equals(targetStatus, EmployeeProfileCorrectionRequestStatuses.Rejected, StringComparison.Ordinal) ||
                   string.Equals(targetStatus, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal);
        }

        private string GetCurrentUserName()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated
                ? Context.User.Identity.Name
                : "system";
        }

        private void TryCompleteWorkItem(long requestId, string requestStatus, string reviewNote)
        {
            // <lang>
            //   <zh-CN>待办是旁路增强能力，写入失败不应回滚已经完成的资料更正审核动作。</zh-CN>
            //   <en>Work items are a sidecar enhancement; write failures must not roll back the completed profile-correction review.</en>
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

        private static string MapWorkItemStatus(string requestStatus)
        {
            return string.Equals(requestStatus, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal)
                ? PortalWorkItemStatuses.Cancelled
                : PortalWorkItemStatuses.Completed;
        }

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
    public sealed class EmployeeProfileCorrectionAdminRow
    {
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

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
