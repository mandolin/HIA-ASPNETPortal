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
    /// 中文：员工资料更正请求后台处理页。
    ///
    /// English: Administration page for employee-profile correction requests.
    /// </summary>
    /// <remarks>
    /// 中文：P6.4.3 第一版只处理请求状态和管理员备注，不直接修改员工主数据；真实资料修改仍走员工目录维护。
    ///
    /// English: The first P6.4.3 version updates only request status and administrator notes. Actual profile changes
    /// still go through employee-directory maintenance.
    /// </remarks>
    public partial class EmployeeProfileCorrectionRequests : PortalPage<EmployeeProfileCorrectionRequests>
    {
        private const int PageSize = 50;

        /// <summary>
        /// 中文：员工资料更正请求数据服务。
        ///
        /// English: Employee-profile correction-request data service.
        /// </summary>
        [Dependency]
        public IEmployeeProfileCorrectionRequestDb CorrectionRequestDb { private get; set; }

        /// <summary>
        /// 中文：轻量待办数据服务，用于把资料更正处理同步为待办完成事件。
        ///
        /// English: Lightweight work-item data service used to mirror correction reviews into work-item completion events.
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// 中文：初始化员工资料更正请求后台页。
        ///
        /// English: Initializes the employee-profile correction-request administration page.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin))
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
        /// 中文：按当前筛选条件重新绑定请求列表。
        ///
        /// English: Rebinds requests using the current filter.
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin))
            {
                return;
            }

            BindRequests();
        }

        /// <summary>
        /// 中文：处理请求列表中的管理员状态命令。
        ///
        /// English: Handles administrator status commands from the request list.
        /// </summary>
        protected void RequestsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin))
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

            string targetStatus = Convert.ToString(e.CommandName, CultureInfo.InvariantCulture);
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
            // 中文 / English: 待办是旁路增强能力，写入失败不应回滚已经完成的审核动作。
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
    /// 中文：员工资料更正请求后台展示行。
    ///
    /// English: Administration display row for an employee-profile correction request.
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

        /// <summary>中文：请求标识。English: Request identifier.</summary>
        public long RequestId { get; private set; }

        /// <summary>中文：提交时间文本。English: Submission time text.</summary>
        public string SubmittedUtcText { get; private set; }

        /// <summary>中文：员工文本。English: Employee text.</summary>
        public string EmployeeText { get; private set; }

        /// <summary>中文：用户文本。English: User text.</summary>
        public string UserText { get; private set; }

        /// <summary>中文：字段名。English: Field name.</summary>
        public string FieldName { get; private set; }

        /// <summary>中文：当前值快照。English: Current-value snapshot.</summary>
        public string CurrentValueSnapshot { get; private set; }

        /// <summary>中文：建议值。English: Proposed value.</summary>
        public string ProposedValue { get; private set; }

        /// <summary>中文：员工说明。English: Employee note.</summary>
        public string RequestNote { get; private set; }

        /// <summary>中文：请求状态。English: Request status.</summary>
        public string RequestStatus { get; private set; }

        /// <summary>中文：处理信息文本。English: Review information text.</summary>
        public string ReviewText { get; private set; }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
