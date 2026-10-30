using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Resources;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang>
    ///   <zh-CN>费用报销后台审核页（D23）：列出待审核的费用报销，支持批准/退回/拒绝并投影工作项。</zh-CN>
    ///   <en>Admin review page for expense reimbursements (D23): lists pending reimbursements, supports approve/return/reject, and projects work items.</en>
    /// </lang></summary>
    public partial class ExpenseReimbursements : PortalPage<ExpenseReimbursements>
    {
        private const int PageSize = 15;

        private static readonly string[] SupportedActions =
            { PortalWorkflowActions.Approve, PortalWorkflowActions.Return, PortalWorkflowActions.Reject };

        [Dependency]
        public IExpenseReimbursementDb ExpenseReimbursementDb { private get; set; }

        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Page.IsPostBack)
            {
                return;
            }

            EnsureCanViewExpenseReimbursements();
            BindStatusFilter();
            BindRequests();
        }

        protected void SearchButton_Click(object sender, EventArgs e)
        {
            EnsureCanViewExpenseReimbursements();
            BindRequests();
        }

        /// <summary><lang>
        ///   <zh-CN>审核命令处理：解析动作键、写入审核结果、记录审计并投影工作项。</zh-CN>
        ///   <en>Review command handler: resolves the action key, writes the review result, records an audit, and projects a work item.</en>
        /// </lang></summary>
        protected void RequestsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!IsSupportedAction(e.CommandName))
            {
                MessageLabel.Text = lang.Admin_BusinessApplications_MessageUnsupportedAction;
                return;
            }

            EnsureCanHandleExpenseReimbursements();
            if (!long.TryParse(e.CommandArgument as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out long requestId) || requestId <= 0)
            {
                MessageLabel.Text = lang.Admin_BusinessApplications_MessageInvalidId;
                return;
            }

            var reviewCommentBox = (TextBox)e.Item.FindControl("ReviewCommentTextBox");
            string reviewComment = reviewCommentBox != null ? NormalizeInput(reviewCommentBox.Text, 1000) : string.Empty;
            int reviewerUserId = GetCurrentUserId();

            ExpenseReimbursementReviewResult review = ExpenseReimbursementDb.ReviewRequest(new ExpenseReimbursementReviewRequest
            {
                RequestId = requestId,
                ActionKey = e.CommandName,
                ReviewComment = reviewComment,
                ReviewedByUserId = reviewerUserId,
                ReviewedBy = GetCurrentUserName(),
                ReviewedUtc = DateTime.UtcNow
            });

            if (!review.Succeeded)
            {
                MessageLabel.Text = lang.Admin_BusinessApplications_MessageServiceNotRegistered;
                return;
            }

            // <lang>
            //   <zh-CN>审计事件暂复用业务申请审核事件类型（拆分 ExpenseReimbursementReviewed 为后续项）。</zh-CN>
            //   <en>The audit event temporarily reuses the business-application review event type (splitting out ExpenseReimbursementReviewed is a follow-up).</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.BusinessApplicationReviewed,
                PortalOperationAuditEvents.BusinessApplicationTargetType,
                requestId.ToString(CultureInfo.InvariantCulture),
                string.Format(CultureInfo.InvariantCulture, "Expense reimbursement reviewed. ActionKey={0}", e.CommandName),
                Context);

            TryCompleteWorkItem(requestId, e.CommandName, reviewComment);
            MessageLabel.Text = lang.Admin_BusinessApplications_MessageStateUpdated;
            BindRequests();
        }

        private void BindStatusFilter()
        {
            if (StatusFilterList.Items.Count > 0)
            {
                return;
            }

            AddStatusOption(ExpenseReimbursementStatuses.Pending, "待审批");
            AddStatusOption(ExpenseReimbursementStatuses.Approved, "已批准");
            AddStatusOption(ExpenseReimbursementStatuses.Rejected, "已拒绝");
            AddStatusOption(ExpenseReimbursementStatuses.Closed, "已关闭");
            StatusFilterList.Items.Insert(0, new ListItem(lang.Admin_Common_AllOption, string.Empty));
            StatusFilterList.SelectedIndex = 0;
        }

        private void AddStatusOption(string status, string text)
        {
            StatusFilterList.Items.Add(new ListItem(text, status));
        }

        private void BindRequests()
        {
            if (ExpenseReimbursementDb == null || !ExpenseReimbursementDb.IsSchemaAvailable())
            {
                ShowUnavailable(lang.Admin_BusinessApplications_MessageSchemaUnavailable);
                return;
            }

            IList<ExpenseReimbursementInfo> requests = ExpenseReimbursementDb.GetAdminRequests(StatusFilterList.SelectedValue, PageSize);
            RequestsRepeater.DataSource = requests.Select(r => new ExpenseReimbursementAdminRow(r)).ToList();
            RequestsRepeater.DataBind();
            EmptyStateRowCount = requests.Count;
            ResultLabel.Text = string.Format(
                CultureInfo.CurrentCulture, lang.Admin_BusinessApplications_MessagePageInfo, PageSize, requests.Count);
        }

        private void ShowUnavailable(string message)
        {
            RequestsRepeater.Visible = false;
            MessageLabel.Text = message;
            ResultLabel.Text = string.Empty;
        }

        // <lang>
        //   <zh-CN>D23 暂复用业务申请查看/处理权限键；专用 ExpenseReimbursementReview/Admin 权限键（含迁移脚本与 ADR）为后续项，避免本周期扩大安全契约变更范围。</zh-CN>
        //   <en>D23 temporarily reuses the business-application view/handle permission keys; dedicated ExpenseReimbursementReview/Admin keys (with migration + ADR) are a follow-up that would otherwise widen the security-contract change scope.</en>
        // </lang>
        private void EnsureCanViewExpenseReimbursements()
        {
            PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessApplicationReview,
                PortalPermissionKeys.BusinessApplicationAdmin);
        }

        private void EnsureCanHandleExpenseReimbursements()
        {
            PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessApplicationReview,
                PortalPermissionKeys.BusinessApplicationAdmin);
        }

        private int GetCurrentUserId()
        {
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        private string GetCurrentUserName()
        {
            var identity = Context?.User?.Identity;
            if (identity != null && identity.IsAuthenticated)
            {
                return identity.Name;
            }

            return "system";
        }

        /// <summary><lang>
        ///   <zh-CN>待办是审核通过后的旁路增强能力：调用失败仅记录诊断日志并降级，不影响"审核已成功、报销状态已更新"这一主流程结果。</zh-CN>
        ///   <en>Work items are a sidecar enhancement after a successful review: a failure is caught and degraded (diagnostic log only) so it never rolls back the main "review succeeded, reimbursement status updated" result.</en>
        /// </lang></summary>
        private void TryCompleteWorkItem(long requestId, string actionKey, string reviewNote)
        {
            if (WorkItemDb == null || requestId <= 0)
            {
                return;
            }

            try
            {
                WorkItemDb.CompleteBusinessWorkItem(
                    new PortalWorkItemCompletionRequest
                    {
                        BusinessKind = PortalWorkItemBusinessKinds.ExpenseReimbursement,
                        BusinessId = requestId.ToString(CultureInfo.InvariantCulture),
                        EventType = MapWorkItemEventType(actionKey),
                        TargetStatus = PortalWorkItemStatuses.Completed,
                        ActorName = GetCurrentUserName(),
                        Comment = NormalizeInput(reviewNote, 1000),
                        OccurredUtc = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "费用报销审核通过后待办投影失败（已降级，不影响审核结果）：" + ex);
            }
        }

        private static string MapWorkItemEventType(string actionKey)
        {
            if (string.Equals(actionKey, PortalWorkflowActions.Reject, StringComparison.Ordinal))
            {
                return PortalWorkItemEventTypes.Rejected;
            }

            if (string.Equals(actionKey, PortalWorkflowActions.Return, StringComparison.Ordinal))
            {
                return PortalWorkItemEventTypes.Commented;
            }

            return PortalWorkItemEventTypes.Approved;
        }

        private static bool IsSupportedAction(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return false;
            }

            return ((IReadOnlyList<string>)SupportedActions).Contains(actionName);
        }

        private static string NormalizeInput(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = value.Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary><lang><zh-CN>列表空态行计数（供 PortalEmptyStateRenderer 使用）。</zh-CN><en>Empty-state row count (used by PortalEmptyStateRenderer).</en></lang></summary>
        protected int EmptyStateRowCount { get; private set; }

        protected string EmptyStateText
        {
            get
            {
                string selected = StatusFilterList.SelectedValue;
                if (string.IsNullOrEmpty(selected))
                {
                    return lang.Admin_BusinessApplications_EmptyNoItems;
                }

                return string.Format(
                    CultureInfo.CurrentCulture, lang.Admin_BusinessApplications_EmptyNoMatches, selected);
            }
        }

        private sealed class ExpenseReimbursementAdminRow
        {
            internal ExpenseReimbursementAdminRow(ExpenseReimbursementInfo request)
            {
                RequestId = request.RequestId;
                RequestCode = request.RequestId.ToString(CultureInfo.InvariantCulture);
                Category = EmptyToNone(request.Category);
                AmountText = request.Amount.ToString(CultureInfo.InvariantCulture);
                Reason = EmptyToNone(request.Reason);
                AttachmentName = EmptyToNone(request.AttachmentName);
                RequestStatus = request.Status;
                ApplicantText = string.Format(
                    CultureInfo.CurrentCulture, lang.Admin_BusinessApplications_ApplicantFormat,
                    request.UserId.ToString(CultureInfo.InvariantCulture), lang.Common_NonePlaceholder);
                SubmittedUtcText = request.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
                ReviewText = request.ReviewedUtc.HasValue
                    ? string.Format(
                        CultureInfo.CurrentCulture, lang.Admin_BusinessApplications_ReviewFormat,
                        request.ReviewedUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
                        request.ReviewedByUserId.HasValue
                            ? request.ReviewedByUserId.Value.ToString(CultureInfo.InvariantCulture)
                            : lang.Common_NonePlaceholder,
                        EmptyToNone(request.ReviewComment))
                    : lang.Admin_BusinessApplications_NotReviewed;
            }

            public long RequestId { get; private set; }
            public string RequestCode { get; private set; }
            public string Category { get; private set; }
            public string AmountText { get; private set; }
            public string Reason { get; private set; }
            public string AttachmentName { get; private set; }
            public string RequestStatus { get; private set; }
            public string ApplicantText { get; private set; }
            public string SubmittedUtcText { get; private set; }
            public string ReviewText { get; private set; }
        }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrEmpty(value) ? lang.Common_NonePlaceholder : value;
        }
    }
}
