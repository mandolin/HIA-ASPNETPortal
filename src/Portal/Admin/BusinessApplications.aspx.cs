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
            // <lang>
            //   <zh-CN>先执行查看/审核权限门禁，再仅在首次请求绑定筛选项和申请列表，避免回发覆盖筛选状态。</zh-CN>
            //   <en>Apply the view/review gate first, then bind filters and applications only on the first request so postbacks do not overwrite filter state.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>搜索回调只重新读取当前状态筛选，不改变审核权限或领域状态。</zh-CN>
            //   <en>The search callback reloads the current status filter only; it does not change review authorization or domain state.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>审核命令先通过固定动作白名单和处理权限，再校验申请标识与评论长度。</zh-CN>
            //   <en>Review commands pass the fixed action allowlist and handling permission before validating the application identifier and comment length.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>审核评论来自当前列表行控件，随后按固定上限归一化后交给数据服务。</zh-CN>
            //   <en>Read the review comment from the current list-row control and normalize it to the fixed limit before calling the data service.</en>
            // </lang>
            TextBox commentBox = e.Item.FindControl("ReviewCommentTextBox") as TextBox;
            string reviewComment = commentBox == null ? string.Empty : commentBox.Text;
            // <lang>
            //   <zh-CN>审核服务负责申请状态和 WorkflowEvent 事实写入，后台页面不自行推断状态。</zh-CN>
            //   <en>The review service writes application state and WorkflowEvent facts; the page does not infer state locally.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>审核事实成功后记录操作审计，再更新后台待办投影；待办失败不回滚申请状态。</zh-CN>
            //   <en>Record operation audit after review succeeds, then update the work-item projection; work-item failure does not roll back application state.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定后台允许筛选的业务申请状态，并默认定位到已提交状态。</zh-CN>
        ///   <en>Binds administration-allowed business-application statuses and defaults to submitted applications.</en>
        /// </lang>
        /// </summary>
        private void BindStatusFilter()
        {
            // <lang>
            //   <zh-CN>筛选项使用稳定状态键，显示值保持现有后台兼容文本。</zh-CN>
            //   <en>Use stable status keys for filters while preserving the existing administration-compatible display text.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前状态筛选读取业务申请并绑定低敏后台展示行。</zh-CN>
        ///   <en>Loads business applications by the current status filter and binds low-sensitivity administration display rows.</en>
        /// </lang>
        /// </summary>
        private void BindApplications()
        {
            // <lang>
            //   <zh-CN>数据服务缺失或 Schema 不可用时绑定空集合，不继续读取业务申请数据。</zh-CN>
            //   <en>Bind an empty collection when the service or schema is unavailable and do not continue reading application data.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>查询使用固定分页上限，展示行只保留申请审核所需低敏字段。</zh-CN>
            //   <en>Use the fixed page-size limit and keep only the low-sensitivity fields needed for review display.</en>
            // </lang>
            IList<BusinessApplicationInfo> applications = BusinessApplicationDb.GetAdminApplications(
                StatusFilterList.SelectedValue,
                PageSize);
            ApplicationsRepeater.DataSource = applications.Select(application => new BusinessApplicationAdminRow(application)).ToList();
            ApplicationsRepeater.DataBind();

            ResultLabel.Text = "Showing up to " + PageSize.ToString(CultureInfo.InvariantCulture) +
                               " applications; count: " + applications.Count.ToString(CultureInfo.InvariantCulture) + ".";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示后台能力不可用提示并绑定空申请列表。</zh-CN>
        ///   <en>Displays an unavailable-capability message and binds an empty application list.</en>
        /// </lang>
        /// </summary>
        private void ShowUnavailable(string message)
        {
            // <lang>
            //   <zh-CN>不可用路径清空旧结果，避免残留数据继续显示。</zh-CN>
            //   <en>The unavailable path clears the old result so stale data cannot remain visible.</en>
            // </lang>
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            ApplicationsRepeater.DataSource = Enumerable.Empty<BusinessApplicationAdminRow>();
            ApplicationsRepeater.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查业务申请审核或管理员权限以允许查看。</zh-CN>
        ///   <en>Checks business-application review or administrator permission for viewing.</en>
        /// </lang>
        /// </summary>
        private bool EnsureCanViewApplications()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessApplicationReview,
                PortalPermissionKeys.BusinessApplicationAdmin);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查业务申请审核或管理员权限以允许处理动作。</zh-CN>
        ///   <en>Checks business-application review or administrator permission for handling actions.</en>
        /// </lang>
        /// </summary>
        private bool EnsureCanHandleApplications()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessApplicationReview,
                PortalPermissionKeys.BusinessApplicationAdmin);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将当前认证用户名解析为门户用户标识；缺少身份或服务时返回零。</zh-CN>
        ///   <en>Resolves the current authenticated name to a Portal user identifier and returns zero when identity or service is unavailable.</en>
        /// </lang>
        /// </summary>
        private int GetCurrentUserId()
        {
            // <lang>
            //   <zh-CN>审核人标识只通过用户服务解析，不从申请参数或控件值推断。</zh-CN>
            //   <en>Resolve the reviewer identifier only through the user service; never infer it from application parameters or controls.</en>
            // </lang>
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前认证用户名；后台无认证上下文时使用 system 兼容回退。</zh-CN>
        ///   <en>Reads the current authenticated name and uses the existing system fallback when no authenticated context exists.</en>
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
        ///   <zh-CN>把审核结果投影为后台待办完成事件；待办失败不回滚 WorkflowEvent 或申请状态。</zh-CN>
        ///   <en>Projects a review result into a work-item completion event; failure does not roll back the WorkflowEvent or application state.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>将审核动作映射为待办事件类型。</zh-CN>
        ///   <en>Maps a review action to a work-item event type.</en>
        /// </lang>
        /// </summary>
        private static string MapWorkItemEventType(string actionKey)
        {
            // <lang>
            //   <zh-CN>拒绝和退回保持特定事件语义，批准动作回退为批准事件。</zh-CN>
            //   <en>Preserve specific event semantics for reject and return, with approve as the default approval event.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断审核动作是否属于批准、退回或拒绝白名单。</zh-CN>
        ///   <en>Determines whether a review action belongs to the approve, return, or reject allowlist.</en>
        /// </lang>
        /// </summary>
        private static bool IsSupportedAction(string actionKey)
        {
            return string.Equals(actionKey, PortalWorkflowActions.Approve, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalWorkflowActions.Return, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalWorkflowActions.Reject, StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪并限制审核输入长度，null 按空字符串处理。</zh-CN>
        ///   <en>Trims and limits review input length, treating null as an empty string.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeInput(string value, int maxLength)
        {
            // <lang>
            //   <zh-CN>该 helper 只负责输入边界归一化，不承担权限、状态或持久化职责。</zh-CN>
            //   <en>This helper performs input-boundary normalization only; authorization, state, and persistence remain elsewhere.</en>
            // </lang>
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
        /// <summary>
        /// <lang>
        ///   <zh-CN>把业务申请转换为低敏只读后台展示模型。</zh-CN>
        ///   <en>Converts a business application into a low-sensitivity read-only administration display model.</en>
        /// </lang>
        /// </summary>
        internal BusinessApplicationAdminRow(BusinessApplicationInfo application)
        {
            // <lang>
            //   <zh-CN>展示行保留审核所需字段，申请正文和审核意见只作为既有展示文本处理。</zh-CN>
            //   <en>The display row keeps review-required fields while treating body and review comment as existing display text.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空白展示字段统一转换为占位文本。</zh-CN>
        ///   <en>Converts blank display fields to a consistent placeholder.</en>
        /// </lang>
        /// </summary>
        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
