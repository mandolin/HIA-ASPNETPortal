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
    ///   <zh-CN>抽象业务申请后台处理页。</zh-CN>
    ///   <en>Administration page for abstract business applications.</en>
    /// </lang>
    /// </summary>
    public partial class BusinessApplications : PortalPage<BusinessApplications>
    {
        private const int PageSize = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>抽象业务申请数据服务。</zh-CN>
        ///   <en>Abstract business-application data service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IBusinessApplicationDb BusinessApplicationDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>轻量待办数据服务，用于把审核结果同步为待办完成事件。</zh-CN>
        ///   <en>Lightweight work-item data service used to mirror review results into work-item completion events.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户数据访问服务，用于解析当前审核人用户标识。</zh-CN>
        ///   <en>User data service used to resolve the current reviewer user identifier.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化后台页并绑定状态筛选和申请列表。</zh-CN>
        ///   <en>Initializes the administration page and binds the status filter plus application list.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnsureCanViewApplications())
            {
                return;
            }

            if (!IsPostBack)
            {
                BindStatusFilter();
                BindApplications();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前筛选条件重新绑定申请列表。</zh-CN>
        ///   <en>Rebinds applications using the current filter.</en>
        /// </lang>
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            if (!EnsureCanViewApplications())
            {
                return;
            }

            BindApplications();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理列表中的审核命令。</zh-CN>
        ///   <en>Handles review commands from the application list.</en>
        /// </lang>
        /// </summary>
        protected void ApplicationsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string actionKey = Convert.ToString(e.CommandName, CultureInfo.InvariantCulture);
            if (!IsSupportedAction(actionKey))
            {
                MessageLabel.Text = "Unsupported workflow action.";
                BindApplications();
                return;
            }

            if (!EnsureCanHandleApplications())
            {
                return;
            }

            long applicationId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out applicationId))
            {
                MessageLabel.Text = "Invalid application id.";
                return;
            }

            TextBox commentBox = e.Item.FindControl("ReviewCommentTextBox") as TextBox;
            string reviewComment = commentBox == null ? string.Empty : commentBox.Text;
            BusinessApplicationResult result = BusinessApplicationDb.ReviewApplication(
                new BusinessApplicationReviewRequest
                {
                    ApplicationId = applicationId,
                    ActionKey = actionKey,
                    ReviewComment = NormalizeInput(reviewComment, 1000),
                    ReviewedByUserId = GetCurrentUserId(),
                    ReviewedBy = GetCurrentUserName(),
                    ReviewedUtc = DateTime.UtcNow
                });

            if (!result.Succeeded)
            {
                MessageLabel.Text = result.Message;
                BindApplications();
                return;
            }

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.BusinessApplicationReviewed,
                PortalOperationAuditEvents.BusinessApplicationTargetType,
                result.ApplicationId.ToString(CultureInfo.InvariantCulture),
                "Business application reviewed. ApplicationCode=" + result.ApplicationCode + "; ActionKey=" + actionKey,
                Context);

            TryCompleteWorkItem(result.ApplicationId, actionKey, reviewComment);

            MessageLabel.Text = "Business application state updated.";
            BindApplications();
        }

        private void BindStatusFilter()
        {
            StatusFilterList.Items.Clear();
            StatusFilterList.Items.Add(new ListItem("All", string.Empty));
            StatusFilterList.Items.Add(new ListItem(PortalBusinessApplicationStatuses.Submitted, PortalBusinessApplicationStatuses.Submitted));
            StatusFilterList.Items.Add(new ListItem(PortalBusinessApplicationStatuses.InReview, PortalBusinessApplicationStatuses.InReview));
            StatusFilterList.Items.Add(new ListItem(PortalBusinessApplicationStatuses.Returned, PortalBusinessApplicationStatuses.Returned));
            StatusFilterList.Items.Add(new ListItem(PortalBusinessApplicationStatuses.Approved, PortalBusinessApplicationStatuses.Approved));
            StatusFilterList.Items.Add(new ListItem(PortalBusinessApplicationStatuses.Rejected, PortalBusinessApplicationStatuses.Rejected));
            StatusFilterList.Items.Add(new ListItem(PortalBusinessApplicationStatuses.Closed, PortalBusinessApplicationStatuses.Closed));
            StatusFilterList.SelectedValue = PortalBusinessApplicationStatuses.Submitted;
        }

        private void BindApplications()
        {
            if (BusinessApplicationDb == null)
            {
                ShowUnavailable("Business application data service is not registered.");
                return;
            }

            if (!BusinessApplicationDb.IsSchemaAvailable())
            {
                ShowUnavailable("P19.4 business application schema is unavailable. Run PortalBiz_BusinessApplications.sql and PortalBiz_WorkflowEvents.sql.");
                return;
            }

            IList<BusinessApplicationInfo> applications = BusinessApplicationDb.GetAdminApplications(
                StatusFilterList.SelectedValue,
                PageSize);
            ApplicationsRepeater.DataSource = applications.Select(application => new BusinessApplicationAdminRow(application)).ToList();
            ApplicationsRepeater.DataBind();

            ResultLabel.Text = "Showing up to " + PageSize.ToString(CultureInfo.InvariantCulture) +
                               " applications; count: " + applications.Count.ToString(CultureInfo.InvariantCulture) + ".";
        }

        private void ShowUnavailable(string message)
        {
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            ApplicationsRepeater.DataSource = Enumerable.Empty<BusinessApplicationAdminRow>();
            ApplicationsRepeater.DataBind();
        }

        private bool EnsureCanViewApplications()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessApplicationReview,
                PortalPermissionKeys.BusinessApplicationAdmin);
        }

        private bool EnsureCanHandleApplications()
        {
            return PortalAuthorization.EnsureAnyPermission(
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
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated
                ? Context.User.Identity.Name
                : "system";
        }

        private void TryCompleteWorkItem(long applicationId, string actionKey, string reviewComment)
        {
            // <lang>
            //   <zh-CN>待办投影只反映审核入口是否已处理，WorkflowEvent 才是业务流程事实；待办写入失败不回滚申请状态。</zh-CN>
            //   <en>The work-item projection only reflects whether the review entry has been handled, while WorkflowEvent is the business-flow fact; work-item failures do not roll back application state.</en>
            // </lang>
            if (WorkItemDb == null || applicationId <= 0)
            {
                return;
            }

            WorkItemDb.CompleteBusinessWorkItem(
                new PortalWorkItemCompletionRequest
                {
                    BusinessKind = PortalWorkItemBusinessKinds.BusinessApplication,
                    BusinessId = applicationId.ToString(CultureInfo.InvariantCulture),
                    EventType = MapWorkItemEventType(actionKey),
                    TargetStatus = PortalWorkItemStatuses.Completed,
                    ActorUserId = GetCurrentUserId(),
                    ActorName = GetCurrentUserName(),
                    Comment = NormalizeInput(reviewComment, 1000),
                    OccurredUtc = DateTime.UtcNow
                });
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

        private static bool IsSupportedAction(string actionKey)
        {
            return string.Equals(actionKey, PortalWorkflowActions.Approve, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalWorkflowActions.Return, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalWorkflowActions.Reject, StringComparison.Ordinal);
        }

        private static string NormalizeInput(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>抽象业务申请后台展示行。</zh-CN>
    ///   <en>Administration display row for an abstract business application.</en>
    /// </lang>
    /// </summary>
    public sealed class BusinessApplicationAdminRow
    {
        internal BusinessApplicationAdminRow(BusinessApplicationInfo application)
        {
            ApplicationId = application.ApplicationId;
            ApplicationCode = application.ApplicationCode;
            Title = EmptyToNone(application.Title);
            CategoryKey = EmptyToNone(application.CategoryKey);
            Summary = EmptyToNone(application.Summary);
            Body = EmptyToNone(application.Body);
            ApplicantText = application.ApplicantUserId.ToString(CultureInfo.InvariantCulture) + " / " + EmptyToNone(application.ApplicantUserName);
            ApplicationStatus = application.ApplicationStatus;
            SubmittedUtcText = application.SubmittedUtc.HasValue
                ? application.SubmittedUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(none)";
            ReviewText = application.ReviewedUtc.HasValue
                ? application.ReviewedUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) +
                  " / User " + (application.ReviewedByUserId.HasValue ? application.ReviewedByUserId.Value.ToString(CultureInfo.InvariantCulture) : "(none)") +
                  " / " + EmptyToNone(application.ReviewComment)
                : "(not reviewed)";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请主键。</zh-CN>
        ///   <en>Application primary key.</en>
        /// </lang>
        /// </summary>
        public long ApplicationId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请编号。</zh-CN>
        ///   <en>Application code.</en>
        /// </lang>
        /// </summary>
        public string ApplicationCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请标题。</zh-CN>
        ///   <en>Application title.</en>
        /// </lang>
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>分类键。</zh-CN>
        ///   <en>Category key.</en>
        /// </lang>
        /// </summary>
        public string CategoryKey { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>低敏摘要。</zh-CN>
        ///   <en>Low-sensitivity summary.</en>
        /// </lang>
        /// </summary>
        public string Summary { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请说明正文。</zh-CN>
        ///   <en>Application body text.</en>
        /// </lang>
        /// </summary>
        public string Body { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请人展示文本。</zh-CN>
        ///   <en>Applicant display text.</en>
        /// </lang>
        /// </summary>
        public string ApplicantText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>申请状态。</zh-CN>
        ///   <en>Application status.</en>
        /// </lang>
        /// </summary>
        public string ApplicationStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交 UTC 展示文本。</zh-CN>
        ///   <en>Submission UTC display text.</en>
        /// </lang>
        /// </summary>
        public string SubmittedUtcText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近审核展示文本。</zh-CN>
        ///   <en>Latest review display text.</en>
        /// </lang>
        /// </summary>
        public string ReviewText { get; private set; }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
