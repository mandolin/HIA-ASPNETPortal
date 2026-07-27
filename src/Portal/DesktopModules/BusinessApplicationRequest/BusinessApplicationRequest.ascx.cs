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
    ///   <zh-CN>抽象业务申请提交模块。</zh-CN>
    ///   <en>Submission module for abstract business applications.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P19.4 用它验证“企业能力模块 - 业务事实 - WorkflowEvent - WorkItem - OperationAudit”的最短闭环，不承载具体科研、井设计或情报字段。</zh-CN>
    ///   <en>P19.4 uses this module to validate the shortest loop across enterprise capability module, business fact, WorkflowEvent, WorkItem, and OperationAudit without carrying specific research, well-design, or intelligence fields.</en>
    /// </lang>
    /// </remarks>
    public partial class BusinessApplicationRequest : PortalModuleControl<BusinessApplicationRequest>
    {
        private const int RecentApplicationLimit = 10;

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户数据访问服务，用于把当前登录名解析为门户用户标识。</zh-CN>
        ///   <en>User data service used to resolve the current sign-in name to a Portal user identifier.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

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
        ///   <zh-CN>轻量待办数据服务，用于创建审核入口投影。</zh-CN>
        ///   <en>Lightweight work-item data service used to create the review-entry projection.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化申请模块并绑定分类和最近申请。</zh-CN>
        ///   <en>Initializes the application module and binds categories plus recent applications.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindCategoryList();
                BindModule();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交当前用户填写的抽象业务申请。</zh-CN>
        ///   <en>Submits the abstract business application entered by the current user.</en>
        /// </lang>
        /// </summary>
        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                ShowMessage("请先登录后再提交业务申请。");
                BindModule();
                return;
            }

            if (!PortalAuthorization.HasAnyPermission(
                PortalPermissionKeys.BusinessApplicationSubmit,
                PortalPermissionKeys.BusinessApplicationAdmin))
            {
                ShowMessage("当前账号没有提交业务申请的权限。");
                BindModule();
                return;
            }

            string title = NormalizeInput(TitleTextBox.Text, 200);
            string summary = NormalizeInput(SummaryTextBox.Text, 500);
            string body = NormalizeInput(BodyTextBox.Text, 4000);
            if (string.IsNullOrWhiteSpace(title))
            {
                ShowMessage("请填写申请标题。");
                return;
            }

            if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(body))
            {
                ShowMessage("请填写摘要或申请说明。");
                return;
            }

            BusinessApplicationResult result = BusinessApplicationDb.SubmitApplication(
                new BusinessApplicationSubmitRequest
                {
                    Title = title,
                    CategoryKey = CategoryList.SelectedValue,
                    Summary = summary,
                    Body = body,
                    ApplicantUserId = userId,
                    ReviewRoleKey = PortalPermissionKeys.BusinessApplicationReview,
                    SubmittedUtc = DateTime.UtcNow,
                    SubmittedBy = GetCurrentUserName()
                });

            if (!result.Succeeded)
            {
                ShowMessage(result.Message);
                BindModule();
                return;
            }

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.BusinessApplicationSubmitted,
                PortalOperationAuditEvents.BusinessApplicationTargetType,
                result.ApplicationId.ToString(CultureInfo.InvariantCulture),
                "Business application submitted. ApplicationCode=" + result.ApplicationCode,
                Context);

            TryEnsureWorkItem(result.ApplicationId, result.ApplicationCode, title, summary);

            TitleTextBox.Text = string.Empty;
            SummaryTextBox.Text = string.Empty;
            BodyTextBox.Text = string.Empty;
            ShowMessage("业务申请已提交，编号：" + result.ApplicationCode);
            BindModule();
        }

        private void BindCategoryList()
        {
            CategoryList.Items.Clear();
            CategoryList.Items.Add(new ListItem("通用申请", "General"));
            CategoryList.Items.Add(new ListItem("资料/内容申请", "Content"));
            CategoryList.Items.Add(new ListItem("资源/运维申请", "Operations"));
        }

        private void BindModule()
        {
            int userId = GetCurrentUserId();
            if (!IsCurrentUserAuthenticated())
            {
                RequestPanel.Visible = false;
                BindRecentApplications(0);
                ShowMessage("请先登录后再提交业务申请。");
                return;
            }

            if (BusinessApplicationDb == null || !BusinessApplicationDb.IsSchemaAvailable())
            {
                RequestPanel.Visible = false;
                BindRecentApplications(0);
                ShowMessage("业务申请模块尚未完成数据库初始化。");
                return;
            }

            bool canSubmit = PortalAuthorization.HasAnyPermission(
                PortalPermissionKeys.BusinessApplicationSubmit,
                PortalPermissionKeys.BusinessApplicationAdmin);
            RequestPanel.Visible = canSubmit;
            if (!canSubmit)
            {
                BindRecentApplications(0);
                ShowMessage("当前账号没有提交业务申请的权限。");
                return;
            }

            if (string.IsNullOrEmpty(MessageLabel.Text))
            {
                MessageLabel.Text = string.Empty;
            }

            BindRecentApplications(userId);
        }

        private void BindRecentApplications(int userId)
        {
            IList<BusinessApplicationInfo> applications = BusinessApplicationDb == null || userId <= 0
                ? new List<BusinessApplicationInfo>()
                : BusinessApplicationDb.GetRecentApplicationsForUser(userId, RecentApplicationLimit);
            RecentApplicationsRepeater.DataSource = applications.Select(application => new BusinessApplicationRecentRow(application)).ToList();
            RecentApplicationsRepeater.DataBind();
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

        private bool IsCurrentUserAuthenticated()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated;
        }

        private string GetCurrentUserName()
        {
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        private void TryEnsureWorkItem(long applicationId, string applicationCode, string title, string summary)
        {
            // <lang>
            //   <zh-CN>待办是审核入口投影，失败时不回滚已经成功提交的业务申请。</zh-CN>
            //   <en>The work item is a review-entry projection; failure does not roll back the already submitted business application.</en>
            // </lang>
            if (WorkItemDb == null || applicationId <= 0)
            {
                return;
            }

            WorkItemDb.EnsureWorkItem(
                new PortalWorkItemCreateRequest
                {
                    BusinessKind = PortalWorkItemBusinessKinds.BusinessApplication,
                    BusinessId = applicationId.ToString(CultureInfo.InvariantCulture),
                    Title = "Business application " + applicationCode + ": " + title,
                    Summary = NormalizeInput(summary, 500),
                    AssignedRoleKey = PortalPermissionKeys.BusinessApplicationReview,
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserName()
                });
        }

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private static string NormalizeInput(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>业务申请模块最近申请展示行。</zh-CN>
    ///   <en>Recent-application display row for the business-application module.</en>
    /// </lang>
    /// </summary>
    public sealed class BusinessApplicationRecentRow
    {
        internal BusinessApplicationRecentRow(BusinessApplicationInfo application)
        {
            ApplicationCode = application.ApplicationCode;
            Title = application.Title;
            ApplicationStatus = application.ApplicationStatus;
            SubmittedUtcText = application.SubmittedUtc.HasValue
                ? application.SubmittedUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(none)";
            ReviewComment = string.IsNullOrWhiteSpace(application.ReviewComment) ? "(none)" : application.ReviewComment;
        }

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
        ///   <zh-CN>最近审核意见。</zh-CN>
        ///   <en>Latest review comment.</en>
        /// </lang>
        /// </summary>
        public string ReviewComment { get; private set; }
    }
}
