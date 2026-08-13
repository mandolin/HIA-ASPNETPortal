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
            // <lang>
            //   <zh-CN>仅在首次请求加载固定分类和模块数据，回发时保留用户当前表单状态。</zh-CN>
            //   <en>Load fixed categories and module data only on the first request, preserving the user's form state on postback.</en>
            // </lang>
            if (!IsPostBack)
            {
                // <lang>
                //   <zh-CN>分类列表不依赖数据库，先建立稳定的提交值映射。</zh-CN>
                //   <en>The category list is not database-backed, so establish its stable submission-value mapping first.</en>
                // </lang>
                BindCategoryList();
                // <lang>
                //   <zh-CN>随后执行身份、Schema 和权限门禁，并绑定本人最近申请。</zh-CN>
                //   <en>Then apply identity, schema, and permission gates and bind the current user's recent applications.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>先解析当前用户物理标识，所有提交和审计都必须绑定该身份。</zh-CN>
            //   <en>Resolve the current physical user id first; every submission and audit record must be bound to that identity.</en>
            // </lang>
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                ShowMessage("请先登录后再提交业务申请。");
                BindModule();
                return;
            }

            // <lang>
            //   <zh-CN>提交权限允许普通提交者或后台管理员进入同一创建入口。</zh-CN>
            //   <en>Allow either a regular submitter or an administration user to enter the same creation path.</en>
            // </lang>
            if (!PortalAuthorization.HasAnyPermission(
                PortalPermissionKeys.BusinessApplicationSubmit,
                PortalPermissionKeys.BusinessApplicationAdmin))
            {
                ShowMessage("当前账号没有提交业务申请的权限。");
                BindModule();
                return;
            }

            // <lang>
            //   <zh-CN>所有自由文本先按字段上限归一化，避免把未限制的表单值传给数据层。</zh-CN>
            //   <en>Normalize all free text by field limits before passing any unrestricted form value to the data layer.</en>
            // </lang>
            string title = NormalizeInput(TitleTextBox.Text, 200);
            string summary = NormalizeInput(SummaryTextBox.Text, 500);
            string body = NormalizeInput(BodyTextBox.Text, 4000);
            // <lang>
            //   <zh-CN>标题是申请索引的必填字段，缺失时不创建业务事实。</zh-CN>
            //   <en>The title is required for application indexing; do not create a business fact when it is missing.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(title))
            {
                ShowMessage("请填写申请标题。");
                return;
            }

            // <lang>
            //   <zh-CN>摘要和正文至少提供一项，防止提交只有标题的空申请。</zh-CN>
            //   <en>Require either summary or body to prevent a title-only empty application.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(body))
            {
                ShowMessage("请填写摘要或申请说明。");
                return;
            }

            // <lang>
            //   <zh-CN>提交请求携带归一化字段、当前用户、审核角色和 UTC 时间，形成可审计的数据层输入。</zh-CN>
            //   <en>Build the data-layer request from normalized fields, current user, review role, and UTC time to form an auditable input.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>失败只展示数据层提供的业务消息并重新绑定读模型，不执行审计或待办投影。</zh-CN>
                //   <en>On failure, display the data-layer business message and rebind the read model without recording audit or work-item projection.</en>
                // </lang>
                ShowMessage(result.Message);
                BindModule();
                return;
            }

            // <lang>
            //   <zh-CN>业务事实成功后记录操作审计；审计目标使用稳定申请编号，不写入申请正文。</zh-CN>
            //   <en>Record operation audit after the business fact succeeds; use the stable application id as target and do not write application body content.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.BusinessApplicationSubmitted,
                PortalOperationAuditEvents.BusinessApplicationTargetType,
                result.ApplicationId.ToString(CultureInfo.InvariantCulture),
                "Business application submitted. ApplicationCode=" + result.ApplicationCode,
                Context);

            // <lang>
            //   <zh-CN>待办只是审核入口投影，尝试失败不能撤销已经成功的申请。</zh-CN>
            //   <en>The work item is only a review-entry projection; failure to create it must not undo the successful application.</en>
            // </lang>
            TryEnsureWorkItem(result.ApplicationId, result.ApplicationCode, title, summary);

            // <lang>
            //   <zh-CN>成功后清空本次输入，避免回发或重新绑定时复用旧申请内容。</zh-CN>
            //   <en>Clear this submission's inputs after success so a postback or rebind cannot reuse the old application content.</en>
            // </lang>
            TitleTextBox.Text = string.Empty;
            SummaryTextBox.Text = string.Empty;
            BodyTextBox.Text = string.Empty;
            ShowMessage("业务申请已提交，编号：" + result.ApplicationCode);
            BindModule();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>清空并填充抽象业务申请的固定分类列表，保持提交值与展示文本的稳定映射。</zh-CN>
        ///   <en>Clears and fills the fixed abstract-business-application categories, preserving a stable mapping between submitted values and display text.</en>
        /// </lang>
        /// </summary>
        private void BindCategoryList()
        {
            // <lang>
            //   <zh-CN>先清除旧项，保证重复初始化不会累加相同分类。</zh-CN>
            //   <en>Clear existing items first so repeated initialization cannot accumulate duplicate categories.</en>
            // </lang>
            CategoryList.Items.Clear();
            // <lang>
            //   <zh-CN>分类值是提交契约，展示文本可本地化但值保持稳定。</zh-CN>
            //   <en>Category values are the submission contract; display text may be localized while values remain stable.</en>
            // </lang>
            CategoryList.Items.Add(new ListItem("通用申请", "General"));
            CategoryList.Items.Add(new ListItem("资料/内容申请", "Content"));
            CategoryList.Items.Add(new ListItem("资源/运维申请", "Operations"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前身份、数据表可用性和提交权限决定申请区域可见性，并绑定本人最近申请。</zh-CN>
        ///   <en>Determines request-area visibility from identity, schema availability, and submit permission, then binds the current user's recent applications.</en>
        /// </lang>
        /// </summary>
        private void BindModule()
        {
            // <lang>
            //   <zh-CN>先解析用户标识，即使后续身份检查失败也统一得到零值。</zh-CN>
            //   <en>Resolve the user id first so later identity failures consistently operate on zero.</en>
            // </lang>
            int userId = GetCurrentUserId();
            if (!IsCurrentUserAuthenticated())
            {
                // <lang>
                //   <zh-CN>匿名用户不能看到提交区域，最近申请也以空列表绑定。</zh-CN>
                //   <en>Anonymous users cannot see the submission area, and recent applications are bound as an empty list.</en>
                // </lang>
                RequestPanel.Visible = false;
                BindRecentApplications(0);
                ShowMessage("请先登录后再提交业务申请。");
                return;
            }

            // <lang>
            //   <zh-CN>Schema 不可用时保持页面可渲染但不触达创建查询，明确提示数据库初始化边界。</zh-CN>
            //   <en>When the schema is unavailable, keep the page renderable without issuing creation queries and expose the database-initialization boundary.</en>
            // </lang>
            if (BusinessApplicationDb == null || !BusinessApplicationDb.IsSchemaAvailable())
            {
                RequestPanel.Visible = false;
                BindRecentApplications(0);
                ShowMessage("业务申请模块尚未完成数据库初始化。");
                return;
            }

            // <lang>
            //   <zh-CN>提交区域只对提交权限或管理员权限开放，读取列表仍由当前模块策略控制。</zh-CN>
            //   <en>Expose the submission area only to submit or administrator permission holders; list binding remains controlled by this module's policy.</en>
            // </lang>
            bool canSubmit = PortalAuthorization.HasAnyPermission(
                PortalPermissionKeys.BusinessApplicationSubmit,
                PortalPermissionKeys.BusinessApplicationAdmin);
            RequestPanel.Visible = canSubmit;
            if (!canSubmit)
            {
                // <lang>
                //   <zh-CN>无提交权限时不触达业务创建，仍绑定空最近列表并给出低敏提示。</zh-CN>
                //   <en>Without submit permission, do not reach business creation; bind an empty recent list and show a low-sensitivity message.</en>
                // </lang>
                BindRecentApplications(0);
                ShowMessage("当前账号没有提交业务申请的权限。");
                return;
            }

            if (string.IsNullOrEmpty(MessageLabel.Text))
            {
                // <lang>
                //   <zh-CN>保持空消息为空，避免初始化阶段生成不必要的文本。</zh-CN>
                //   <en>Keep an empty message empty so initialization does not create unnecessary text.</en>
                // </lang>
                MessageLabel.Text = string.Empty;
            }

            BindRecentApplications(userId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定用户的最近申请并转换为展示行；无效用户或服务不可用时绑定空列表。</zh-CN>
        ///   <en>Reads recent applications for the specified user and converts them to display rows; binds an empty list for an invalid user or unavailable service.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>当前用户标识；非正数表示匿名或无效身份。</zh-CN>
        ///   <en>Current user identifier; a non-positive value represents an anonymous or invalid identity.</en>
        /// </l>
        /// </param>
        private void BindRecentApplications(int userId)
        {
            // <lang>
            //   <zh-CN>服务不可用或用户无效时构造空读模型，避免把零值身份传入严格查询。</zh-CN>
            //   <en>Build an empty read model when the service is unavailable or the user is invalid, avoiding strict queries with a zero identity.</en>
            // </lang>
            IList<BusinessApplicationInfo> applications = BusinessApplicationDb == null || userId <= 0
                ? new List<BusinessApplicationInfo>()
                : BusinessApplicationDb.GetRecentApplicationsForUser(userId, RecentApplicationLimit);
            // <lang>
            //   <zh-CN>只把申请读模型转换为展示行，模板层不直接接触数据访问对象。</zh-CN>
            //   <en>Convert application read models into display rows so templates do not consume data-access objects directly.</en>
            // </lang>
            RecentApplicationsRepeater.DataSource = applications.Select(application => new BusinessApplicationRecentRow(application)).ToList();
            // <lang>
            //   <zh-CN>提交数据源后触发 Web Forms 重复器绑定。</zh-CN>
            //   <en>Bind the Web Forms repeater after assigning its data source.</en>
            // </lang>
            RecentApplicationsRepeater.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前已认证登录名解析为物理门户用户标识；任一身份或服务条件缺失时返回零。</zh-CN>
        ///   <en>Resolves the current authenticated sign-in name to a physical Portal user id; returns zero when any identity or service condition is missing.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>有效用户标识，或表示不可用身份的零。</zh-CN>
        ///   <en>Valid user identifier, or zero for an unavailable identity.</en>
        /// </l>
        /// </returns>
        private int GetCurrentUserId()
        {
            // <lang>
            //   <zh-CN>用户名来源于当前认证上下文，不读取表单或查询字符串中的用户标识。</zh-CN>
            //   <en>Take the user name from the authenticated context rather than from form or query-string identifiers.</en>
            // </lang>
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            // <lang>
            //   <zh-CN>按登录名执行既有用户查询；查不到时返回零并拒绝提交。</zh-CN>
            //   <en>Query the existing user by sign-in name; return zero and reject submission when no user is found.</en>
            // </lang>
            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查当前 HTTP 上下文是否具有可用且已认证的用户身份。</zh-CN>
        ///   <en>Checks whether the current HTTP context has an available authenticated user identity.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>身份链完整且已认证时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the identity chain is complete and authenticated.</en>
        /// </l>
        /// </returns>
        private bool IsCurrentUserAuthenticated()
        {
            // <lang>
            //   <zh-CN>逐级检查 Context、User、Identity 和认证状态，避免空对象访问和匿名误判。</zh-CN>
            //   <en>Check Context, User, Identity, and authentication state step by step to avoid null access and anonymous misclassification.</en>
            // </lang>
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前已认证用户名称；匿名或上下文不完整时返回空字符串。</zh-CN>
        ///   <en>Reads the current authenticated user name; returns an empty string for an anonymous or incomplete context.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前用户名称，或空字符串。</zh-CN>
        ///   <en>Current user name, or an empty string.</en>
        /// </l>
        /// </returns>
        private string GetCurrentUserName()
        {
            // <lang>
            //   <zh-CN>仅在认证链完整时读取 Name，否则返回可安全传递的空字符串。</zh-CN>
            //   <en>Read Name only when the authentication chain is complete; otherwise return a safely passable empty string.</en>
            // </lang>
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为已成功提交的业务申请补充审核待办投影；待办失败不回滚申请事实。</zh-CN>
        ///   <en>Adds a review work-item projection for a successfully submitted business application; work-item failure does not roll back the application fact.</en>
        /// </lang>
        /// </summary>
        /// <param name="applicationId">
        /// <l>
        ///   <zh-CN>已提交业务申请标识。</zh-CN>
        ///   <en>Submitted business-application identifier.</en>
        /// </l>
        /// </param>
        /// <param name="applicationCode">
        /// <l>
        ///   <zh-CN>已提交业务申请编号。</zh-CN>
        ///   <en>Submitted business-application code.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>已归一化的申请标题。</zh-CN>
        ///   <en>Normalized application title.</en>
        /// </l>
        /// </param>
        /// <param name="summary">
        /// <l>
        ///   <zh-CN>已归一化的申请摘要。</zh-CN>
        ///   <en>Normalized application summary.</en>
        /// </l>
        /// </param>
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

            // <lang>
            //   <zh-CN>待办请求只使用稳定申请标识、归一化标题/摘要和审核角色，不复制完整正文。</zh-CN>
            //   <en>The work-item request uses only the stable application id, normalized title/summary, and review role; it does not copy the full body.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把页面提示统一进行 HTML 编码后写入消息控件，避免服务端消息被当作标记解释。</zh-CN>
        ///   <en>HTML-encodes a page message before writing it to the message control so server-side text is not interpreted as markup.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>待展示提示，可为 <c>null</c>。</zh-CN>
        ///   <en>Message to display; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            // <lang>
            //   <zh-CN>所有页面提示经过 HtmlEncode，避免业务消息成为客户端标记。</zh-CN>
            //   <en>HTML-encode every page message so business text cannot become client-side markup.</en>
            // </lang>
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>去除输入首尾空白并限制最大字符数，供业务申请标题、摘要和正文共用。</zh-CN>
        ///   <en>Trims input and enforces a maximum character count for shared use by application title, summary, and body.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始表单文本，可为 <c>null</c>。</zh-CN>
        ///   <en>Raw form text; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <param name="maxLength">
        /// <l>
        ///   <zh-CN>允许的最大字符数。</zh-CN>
        ///   <en>Allowed maximum character count.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>归一化且不超过上限的文本。</zh-CN>
        ///   <en>Normalized text no longer than the specified limit.</en>
        /// </l>
        /// </returns>
        private static string NormalizeInput(string value, int maxLength)
        {
            // <lang>
            //   <zh-CN>把 null 归一化为空字符串并裁剪首尾空白，随后按字符上限截断。</zh-CN>
            //   <en>Normalize null to an empty string and trim outer whitespace before applying the character limit.</en>
            // </lang>
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
        /// <summary>
        /// <lang>
        ///   <zh-CN>从业务申请读模型构造最近申请展示行，并把缺失的日期和审核意见转换为低敏占位文本。</zh-CN>
        ///   <en>Builds a recent-application display row from the business-application read model and maps missing date or review-comment values to low-sensitivity placeholders.</en>
        /// </lang>
        /// </summary>
        /// <param name="application">
        /// <l>
        ///   <zh-CN>业务申请读模型。</zh-CN>
        ///   <en>Business-application read model.</en>
        /// </l>
        /// </param>
        internal BusinessApplicationRecentRow(BusinessApplicationInfo application)
        {
            // <lang>
            //   <zh-CN>展示行只复制列表所需字段，不保留数据访问对象引用。</zh-CN>
            //   <en>The display row copies only fields needed by the list and retains no data-access object reference.</en>
            // </lang>
            ApplicationCode = application.ApplicationCode;
            Title = application.Title;
            ApplicationStatus = application.ApplicationStatus;
            // <lang>
            //   <zh-CN>UTC 时间使用固定文化格式；缺失值统一显示低敏占位文本。</zh-CN>
            //   <en>Format UTC time with a fixed culture; map missing values to a low-sensitivity placeholder.</en>
            // </lang>
            SubmittedUtcText = application.SubmittedUtc.HasValue
                ? application.SubmittedUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(none)";
            // <lang>
            //   <zh-CN>审核意见为空时不暴露 null，列表仍保持稳定可渲染。</zh-CN>
            //   <en>Do not expose null for a missing review comment so the list remains stable and renderable.</en>
            // </lang>
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
