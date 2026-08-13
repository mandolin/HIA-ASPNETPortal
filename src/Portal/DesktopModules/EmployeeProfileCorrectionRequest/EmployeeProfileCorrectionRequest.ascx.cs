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
    ///   <zh-CN>员工资料更正请求业务模块样板。</zh-CN>
    ///   <en>Business-module sample for employee-profile correction requests.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>第一版只允许已登录且拥有 Active 员工绑定的用户提交低敏字段级文本更正请求；请求进入后台处理页， 本模块不直接修改员工主数据。</zh-CN>
    ///   <en>The first version allows only signed-in users with an active employee binding to submit low-sensitivity field-level text correction requests. Requests go to an administration page; this module does not directly modify employee master data.</en>
    /// </lang>
    /// </remarks>
    public partial class EmployeeProfileCorrectionRequest : PortalModuleControl<EmployeeProfileCorrectionRequest>
    {
        // <lang>
        //   <zh-CN>限制前台列表只展示最近少量请求，避免模块在一次回发中加载无界历史记录。</zh-CN>
        //   <en>Limit the front-end list to a small recent window so one postback cannot load unbounded request history.</en>
        // </lang>
        private const int RecentRequestLimit = 10;

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
        ///   <zh-CN>员工资料更正请求模块数据访问服务。</zh-CN>
        ///   <en>Employee-profile correction-request module data service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeProfileCorrectionRequestDb CorrectionRequestDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>轻量待办数据服务，用于把资料更正请求同步为后台待办。</zh-CN>
        ///   <en>Lightweight work-item data service used to mirror correction requests into administration work items.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化员工资料更正请求模块。</zh-CN>
        ///   <en>Initializes the employee-profile correction-request module.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载事件的 Web Forms 控件。</zh-CN>
        ///   <en>Web Forms control that raised the load event.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；本回调不依赖其扩展字段。</zh-CN>
        ///   <en>Page-load event arguments; this callback does not depend on extension fields.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>仅在首次请求建立字段选项和资料快照；回发由控件状态保留用户输入，避免覆盖尚未提交的值。</zh-CN>
            //   <en>Build field options and the profile snapshot only on the first request; postbacks retain control state so unsubmitted values are not overwritten.</en>
            // </lang>
            if (!IsPostBack)
            {
                // <lang>
                //   <zh-CN>先固定可提交字段白名单，再绑定资料和最近请求，保证展示与提交使用同一组字段语义。</zh-CN>
                //   <en>Establish the submit-field allowlist before binding the profile and recent requests so display and submission share one field vocabulary.</en>
                // </lang>
                BindFieldList();
                BindProfile();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交当前绑定员工的资料更正请求。</zh-CN>
        ///   <en>Submits a profile correction request for the current bound employee.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发提交事件的按钮控件。</zh-CN>
        ///   <en>Button control that raised the submit event.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮点击事件参数；本回调不依赖其扩展字段。</zh-CN>
        ///   <en>Button-click event arguments; this callback does not depend on extension fields.</en>
        /// </l>
        /// </param>
        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>身份解析失败时使用零值继续进入资料门禁，由统一不可用提示隐藏具体数据访问原因。</zh-CN>
            //   <en>Use zero when identity resolution fails and let the profile gate produce the shared low-detail unavailable message.</en>
            // </lang>
            int userId = GetCurrentUserId();

            // <lang>
            //   <zh-CN>资料快照由服务按用户绑定关系返回；模块不接受客户端提交的员工标识作为信任来源。</zh-CN>
            //   <en>The service returns the profile snapshot from the user's binding; the module never trusts a client-supplied employee identifier.</en>
            // </lang>
            EmployeeProfileCorrectionProfileView profile = GetCurrentProfile(userId);
            if (profile == null)
            {
                // <lang>
                //   <zh-CN>没有可用在职绑定时停止写入，避免把请求挂到未知员工或未初始化 schema。</zh-CN>
                //   <en>Stop before writes when no active binding is available, preventing a request from attaching to an unknown employee or an uninitialized schema.</en>
                // </lang>
                ShowMessage("当前账号没有可提交更正请求的在职员工资料。");
                return;
            }

            // <lang>
            //   <zh-CN>字段名来自服务器生成的白名单控件；仍保留为独立值，供当前值比较和参数化提交共同使用。</zh-CN>
            //   <en>The field name comes from a server-generated allowlist control and is retained as one value for both current-value comparison and parameterized submission.</en>
            // </lang>
            string fieldName = FieldNameList.SelectedValue;

            // <lang>
            //   <zh-CN>建议值和备注在离开控件边界时统一裁剪；长度分别匹配持久化字段限制。</zh-CN>
            //   <en>Trim and bound the proposed value and note at the control boundary; each limit matches its persistence field.</en>
            // </lang>
            string proposedValue = NormalizeInput(ProposedValueTextBox.Text, 512);
            string requestNote = NormalizeInput(RequestNoteTextBox.Text, 1000);
            if (string.IsNullOrWhiteSpace(proposedValue))
            {
                // <lang>
                //   <zh-CN>空建议没有可审核的业务事实，因此不调用数据访问层。</zh-CN>
                //   <en>An empty proposal has no reviewable business fact, so do not call the data-access layer.</en>
                // </lang>
                ShowMessage("请填写建议值。");
                return;
            }

            // <lang>
            //   <zh-CN>按白名单字段读取当前值，拒绝无变化请求，减少无意义的待办和审计记录。</zh-CN>
            //   <en>Read the current value through the allowlist and reject unchanged requests to avoid meaningless work items and audit records.</en>
            // </lang>
            if (string.Equals(GetCurrentValue(profile, fieldName), proposedValue, StringComparison.Ordinal))
            {
                ShowMessage("建议值与当前值相同，无需提交更正请求。");
                return;
            }

            // <lang>
            //   <zh-CN>提交对象只包含服务已确认的用户、员工和绑定事实，以及裁剪后的低敏文本。</zh-CN>
            //   <en>The submit object contains only service-confirmed user, employee, and binding facts plus bounded low-sensitivity text.</en>
            // </lang>
            EmployeeProfileCorrectionRequestResult result = CorrectionRequestDb.SubmitRequest(
                new EmployeeProfileCorrectionSubmitRequest
                {
                    UserId = userId,
                    EmployeeId = profile.EmployeeId,
                    BindingId = profile.BindingId,
                    FieldName = fieldName,
                    ProposedValue = proposedValue,
                    RequestNote = requestNote,
                    SubmittedUtc = DateTime.UtcNow,
                    SubmittedBy = GetCurrentUserName()
                });

            if (!result.Succeeded)
            {
                // <lang>
                //   <zh-CN>服务失败消息由数据访问契约提供；失败时不写审计、不创建待办，也不清空用户输入。</zh-CN>
                //   <en>The data-access contract supplies the failure message; on failure do not audit, create a work item, or clear user input.</en>
                // </lang>
                ShowMessage(result.Message);
                return;
            }

            // <lang>
            //   <zh-CN>审计记录只写请求编号、员工编号和白名单字段名，不把建议值或备注写入事件正文。</zh-CN>
            //   <en>Audit only the request id, employee id, and allowlisted field name; do not put the proposed value or note in the event text.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.EmployeeProfileCorrectionRequested,
                PortalOperationAuditEvents.EmployeeProfileCorrectionRequestTargetType,
                result.RequestId.ToString(CultureInfo.InvariantCulture),
                "Employee profile correction requested. EmployeeId=" + profile.EmployeeId.ToString(CultureInfo.InvariantCulture) +
                "; FieldName=" + fieldName,
                Context);

            // <lang>
            //   <zh-CN>待办是后台处理入口的补充投影；其失败隔离语义由 TryEnsureWorkItem 保持，不回滚已成功的请求。</zh-CN>
            //   <en>The work item is a supplemental administration projection; TryEnsureWorkItem isolates its failure and does not roll back a successful request.</en>
            // </lang>
            TryEnsureWorkItem(result.RequestId, profile.EmployeeId, fieldName);

            // <lang>
            //   <zh-CN>成功后清除当前输入并重新绑定资料/历史，使页面反映刚提交的事实而不保留旧表单残留。</zh-CN>
            //   <en>Clear the submitted inputs and rebind the profile/history after success so the page reflects the new fact without retaining stale form residue.</en>
            // </lang>
            ProposedValueTextBox.Text = string.Empty;
            RequestNoteTextBox.Text = string.Empty;
            BindProfile();
            ShowMessage("更正请求已提交，等待管理员处理。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化允许用户提交更正的低敏字段列表。</zh-CN>
        ///   <en>Initializes the low-sensitivity fields that users may submit for correction.</en>
        /// </lang>
        /// </summary>
        private void BindFieldList()
        {
            // <lang>
            //   <zh-CN>清空控件状态后重新写入固定选项，避免重复绑定在回发或控件重建时累加条目。</zh-CN>
            //   <en>Clear the control state before writing the fixed options so rebinding cannot accumulate duplicate entries.</en>
            // </lang>
            FieldNameList.Items.Clear();

            // <lang>
            //   <zh-CN>以下四项是当前允许更正的低敏字段；新增字段必须同时审查数据访问、审计和输出边界。</zh-CN>
            //   <en>These four entries are the currently permitted low-sensitivity fields; any new field requires a joint review of data access, audit, and output boundaries.</en>
            // </lang>
            FieldNameList.Items.Add(new ListItem("姓名", "DisplayName"));
            FieldNameList.Items.Add(new ListItem("称呼", "PreferredName"));
            FieldNameList.Items.Add(new ListItem("工作邮箱", "WorkEmail"));
            FieldNameList.Items.Add(new ListItem("组织", "OrganizationDisplayName"));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定当前登录用户的员工资料、表单可见性和最近请求列表。</zh-CN>
        ///   <en>Binds the current signed-in user's employee profile, form visibility, and recent-request list.</en>
        /// </lang>
        /// </summary>
        private void BindProfile()
        {
            // <lang>
            //   <zh-CN>使用与提交相同的当前用户解析路径，保证展示资料与后续写入目标一致。</zh-CN>
            //   <en>Use the same current-user resolution path as submission so displayed data and any later write target the same identity.</en>
            // </lang>
            int userId = GetCurrentUserId();

            // <lang>
            //   <zh-CN>资料读取同时承担 schema/绑定门禁；返回空值时不向页面暴露底层异常或数据库细节。</zh-CN>
            //   <en>Profile loading also enforces schema and binding gates; a null result must not expose storage errors or database details to the page.</en>
            // </lang>
            EmployeeProfileCorrectionProfileView profile = GetCurrentProfile(userId);
            if (profile == null)
            {
                // <lang>
                //   <zh-CN>隐藏提交面板并绑定空集合，避免旧回发数据继续显示为可提交状态。</zh-CN>
                //   <en>Hide the submit panel and bind an empty collection so stale postback data cannot remain visibly submittable.</zh-CN>
                // </lang>
                RequestPanel.Visible = false;
                RecentRequestsRepeater.DataSource = Enumerable.Empty<EmployeeProfileCorrectionRecentRequestRow>();
                RecentRequestsRepeater.DataBind();
                ShowMessage(GetUnavailableMessage(userId));
                return;
            }

            // <lang>
            //   <zh-CN>资料可用时恢复面板，并先清除旧提示，再对每个展示值进行 HTML 编码。</zh-CN>
            //   <en>When the profile is available, restore the panel, clear the old message, and HTML-encode each display value.</en>
            // </lang>
            RequestPanel.Visible = true;
            MessageLabel.Text = string.Empty;
            EmployeeCodeLabel.Text = EncodeDisplay(profile.EmployeeCode);
            DisplayNameLabel.Text = EncodeDisplay(profile.DisplayName);
            PreferredNameLabel.Text = EncodeDisplay(EmptyToNone(profile.PreferredName));
            WorkEmailLabel.Text = EncodeDisplay(EmptyToNone(profile.WorkEmail));
            OrganizationLabel.Text = EncodeDisplay(EmptyToNone(profile.OrganizationDisplayName));
            BindRecentRequests(userId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定当前用户最近提交的资料更正请求。</zh-CN>
        ///   <en>Binds recent profile-correction requests submitted by the current user.</en>
        /// </lang>
        /// </summary>
        private void BindRecentRequests(int userId)
        {
            // <lang>
            //   <zh-CN>schema 不可用时使用空列表；可用时由数据访问层按固定上限返回当前用户自己的请求。</zh-CN>
            //   <en>Use an empty list when the schema is unavailable; otherwise let data access return only the current user's requests under the fixed limit.</en>
            // </lang>
            IList<EmployeeProfileCorrectionRequestInfo> requests = CorrectionRequestDb == null
                ? new List<EmployeeProfileCorrectionRequestInfo>()
                : CorrectionRequestDb.GetRecentRequestsForUser(userId, RecentRequestLimit);

            // <lang>
            //   <zh-CN>把数据访问投影转换为只读展示行，避免 Repeater 直接接触内部数据访问对象。</zh-CN>
            //   <en>Convert the data-access projection into display rows so the Repeater does not bind directly to internal data-access objects.</en>
            // </lang>
            RecentRequestsRepeater.DataSource = requests.Select(request => new EmployeeProfileCorrectionRecentRequestRow(request)).ToList();

            // <lang>
            //   <zh-CN>提交绑定结果，让标记层显示当前用户的最近请求集合。</zh-CN>
            //   <en>Commit the binding result so the markup renders the current user's recent-request collection.</en>
            // </lang>
            RecentRequestsRepeater.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户可提交更正请求的 Active 员工资料。</zh-CN>
        ///   <en>Reads the active employee profile that the current user may submit correction requests for.</en>
        /// </lang>
        /// </summary>
        private EmployeeProfileCorrectionProfileView GetCurrentProfile(int userId)
        {
            // <lang>
            //   <zh-CN>只有数据服务存在、目标 schema 可用且用户标识为正数时才允许读取员工资料。</zh-CN>
            //   <en>Read an employee profile only when the data service exists, the target schema is available, and the user id is positive.</en>
            // </lang>
            if (CorrectionRequestDb == null ||
                !CorrectionRequestDb.IsSchemaAvailable() ||
                userId <= 0)
            {
                return null;
            }

            // <lang>
            //   <zh-CN>实际员工绑定关系由数据访问层决定；模块不在内存中拼接或覆盖员工主数据。</zh-CN>
            //   <en>The data-access layer determines the actual employee binding; the module does not assemble or overwrite employee master data in memory.</en>
            // </lang>
            return CorrectionRequestDb.GetCurrentProfileForUser(userId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前登录名解析为门户用户标识。</zh-CN>
        ///   <en>Resolves the current sign-in name to a portal user identifier.</en>
        /// </lang>
        /// </summary>
        private int GetCurrentUserId()
        {
            // <lang>
            //   <zh-CN>从认证上下文取得登录名，避免接受隐藏字段或查询字符串中的用户标识。</zh-CN>
            //   <en>Obtain the sign-in name from the authenticated context instead of trusting a hidden field or query-string user id.</en>
            // </lang>
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            // <lang>
            //   <zh-CN>按登录名读取唯一用户记录；无法解析时返回零，交由上层统一生成低敏提示。</zh-CN>
            //   <en>Read the unique user record by sign-in name; return zero when it cannot be resolved so the caller can produce the shared low-detail message.</en>
            // </lang>
            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据登录、schema 和用户解析状态生成低敏不可用提示。</zh-CN>
        ///   <en>Builds a low-sensitivity unavailable message from sign-in, schema, and user-resolution state.</en>
        /// </lang>
        /// </summary>
        private string GetUnavailableMessage(int userId)
        {
            // <lang>
            //   <zh-CN>先区分认证缺失，避免把未登录状态误报为 schema 或员工绑定问题。</zh-CN>
            //   <en>Check authentication first so an unauthenticated request is not misreported as a schema or employee-binding problem.</en>
            // </lang>
            if (!IsCurrentUserAuthenticated())
            {
                return "请先登录后再提交员工资料更正请求。";
            }

            // <lang>
            //   <zh-CN>认证存在但功能 schema 不可用时，只报告初始化状态，不泄露异常或连接细节。</zh-CN>
            //   <en>When authentication exists but the feature schema is unavailable, report only initialization state without exposing exceptions or connection details.</en>
            // </lang>
            if (CorrectionRequestDb == null || !CorrectionRequestDb.IsSchemaAvailable())
            {
                return "员工资料更正请求模块尚未完成数据库初始化。";
            }

            // <lang>
            //   <zh-CN>最后区分用户解析失败与没有在职绑定，保持前台提示低敏且可行动。</zh-CN>
            //   <en>Finally distinguish user-resolution failure from the absence of an active binding while keeping the front-end message low sensitivity and actionable.</en>
            // </lang>
            return userId <= 0
                ? "当前登录账号无法解析到门户用户。"
                : "当前账号没有可提交更正请求的在职员工资料。";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求是否已有通过认证的门户身份。</zh-CN>
        ///   <en>Determines whether the current request has an authenticated portal identity.</en>
        /// </lang>
        /// </summary>
        private bool IsCurrentUserAuthenticated()
        {
            // <lang>
            //   <zh-CN>逐级检查上下文、用户、身份和认证标志，兼容模块在无 HttpContext 的静态/测试创建场景。</zh-CN>
            //   <en>Check context, user, identity, and authentication flag step by step so the module remains safe when created without an HttpContext in tests or static tooling.</en>
            // </lang>
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前登录用户名，未登录时返回空文本。</zh-CN>
        ///   <en>Reads the current signed-in user name, returning empty text when not signed in.</en>
        /// </lang>
        /// </summary>
        private string GetCurrentUserName()
        {
            // <lang>
            //   <zh-CN>只有通过认证门禁才读取 Identity.Name；未登录时返回空文本供上层统一处理。</zh-CN>
            //   <en>Read Identity.Name only after the authentication gate; return empty text when signed out for the caller's shared handling.</en>
            // </lang>
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试为已提交请求创建后台待办入口。</zh-CN>
        ///   <en>Attempts to create an administration work-item entry for a submitted request.</en>
        /// </lang>
        /// </summary>
        private void TryEnsureWorkItem(long requestId, int employeeId, string fieldName)
        {
            // <lang>
            //   <zh-CN>待办写入只补充后台处理入口，不能阻断用户已经成功提交的资料更正请求。</zh-CN>
            //   <en>Work-item writes only add an administration entry point and must not block an already submitted profile-correction request.</en>
            // </lang>
            if (WorkItemDb == null || requestId <= 0)
            {
                return;
            }

            // <lang>
            //   <zh-CN>待办投影使用请求编号作为稳定业务键，并只写员工编号、字段名和固定审核权限键。</zh-CN>
            //   <en>The work-item projection uses the request id as a stable business key and writes only the employee id, field name, and fixed review-permission key.</en>
            // </lang>
            WorkItemDb.EnsureWorkItem(
                new PortalWorkItemCreateRequest
                {
                    BusinessKind = PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest,
                    BusinessId = requestId.ToString(CultureInfo.InvariantCulture),
                    Title = "Employee profile correction request #" + requestId.ToString(CultureInfo.InvariantCulture),
                    Summary = "EmployeeId=" + employeeId.ToString(CultureInfo.InvariantCulture) + "; FieldName=" + fieldName,
                    AssignedRoleKey = PortalPermissionKeys.EmployeeProfileCorrectionRequestReview,
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserName()
                });
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把页面提示写入 Label，并在写入前进行 HTML 编码。</zh-CN>
        ///   <en>Writes a page message to the label after HTML encoding.</en>
        /// </lang>
        /// </summary>
        private void ShowMessage(string message)
        {
            // <lang>
            //   <zh-CN>所有模块提示都经过服务器 HTML 编码后进入 Label，避免服务层或兼容消息形成标记注入。</zh-CN>
            //   <en>Encode every module message before assigning it to the Label so service or compatibility text cannot become markup injection.</en>
            // </lang>
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编码员工资料展示文本，避免用户资料直接进入标记层。</zh-CN>
        ///   <en>Encodes employee-profile display text so user data does not enter markup directly.</en>
        /// </lang>
        /// </summary>
        private string EncodeDisplay(string value)
        {
            // <lang>
            //   <zh-CN>资料字段属于用户可变数据，即使来源于数据库也必须在标记输出边界再次编码。</zh-CN>
            //   <en>Profile fields are user-controlled data and must be encoded again at the markup boundary even when they came from the database.</en>
            // </lang>
            return Server.HtmlEncode(value ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按字段白名单读取当前资料值，用于阻止无变化提交。</zh-CN>
        ///   <en>Reads the current profile value by field allowlist to block unchanged submissions.</en>
        /// </lang>
        /// </summary>
        private static string GetCurrentValue(EmployeeProfileCorrectionProfileView profile, string fieldName)
        {
            // <lang>
            //   <zh-CN>缺少资料时不允许通过空对象继续比较；空值会让调用方保持可预测的拒绝路径。</zh-CN>
            //   <en>Do not compare through a missing profile; an empty result keeps the caller on a predictable rejection path.</en>
            // </lang>
            if (profile == null)
            {
                return string.Empty;
            }

            // <lang>
            //   <zh-CN>switch 是提交字段的第二道白名单，未知值返回空文本而不是反射读取任意属性。</zh-CN>
            //   <en>The switch is the second allowlist for submit fields; unknown values return empty text instead of reflecting arbitrary properties.</en>
            // </lang>
            switch (fieldName)
            {
                case "DisplayName":
                    return profile.DisplayName ?? string.Empty;
                case "PreferredName":
                    return profile.PreferredName ?? string.Empty;
                case "WorkEmail":
                    return profile.WorkEmail ?? string.Empty;
                case "OrganizationDisplayName":
                    return profile.OrganizationDisplayName ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空展示值转换为统一占位文本。</zh-CN>
        ///   <en>Converts empty display values to the shared placeholder text.</en>
        /// </lang>
        /// </summary>
        private static string EmptyToNone(string value)
        {
            // <lang>
            //   <zh-CN>空展示值统一替换为固定占位符，避免页面在空值和缺失状态间产生歧义。</zh-CN>
            //   <en>Replace blank display values with one fixed placeholder so the page does not confuse blank and unavailable states.</en>
            // </lang>
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪用户输入，避免提交文本超过持久化字段长度。</zh-CN>
        ///   <en>Trims user input so submitted text does not exceed persistence field lengths.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeInput(string value, int maxLength)
        {
            // <lang>
            //   <zh-CN>先把 null 归为空文本并去除首尾空白，保证持久化和无变化比较使用同一规范化值。</zh-CN>
            //   <en>Normalize null to empty text and trim both ends so persistence and unchanged-value comparison use the same value.</en>
            // </lang>
            string normalized = (value ?? string.Empty).Trim();

            // <lang>
            //   <zh-CN>超长输入只保留字段允许的前缀；长度限制在 UI 与服务边界均应保持一致。</zh-CN>
            //   <en>For oversized input, retain only the permitted prefix; the same length boundary must hold at both UI and service edges.</en>
            // </lang>
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料更正请求模块的最近请求展示行。</zh-CN>
    ///   <en>Recent-request display row for the employee-profile correction-request module.</en>
    /// </lang>
    /// </summary>
    public sealed class EmployeeProfileCorrectionRecentRequestRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从资料更正请求投影创建最近请求展示行。</zh-CN>
        ///   <en>Creates a recent-request display row from a profile-correction request projection.</en>
        /// </lang>
        /// </summary>
        internal EmployeeProfileCorrectionRecentRequestRow(EmployeeProfileCorrectionRequestInfo request)
        {
            // <lang>
            //   <zh-CN>把服务投影转换为专用展示行；时间固定为 UTC 文本，空当前值使用统一占位符，避免标记层承担业务判断。</zh-CN>
            //   <en>Convert the service projection into a dedicated display row; keep time as UTC text, normalize an empty current value, and keep business decisions out of markup.</en>
            // </lang>
            SubmittedUtcText = request.SubmittedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            FieldName = request.FieldName;
            CurrentValueSnapshot = string.IsNullOrWhiteSpace(request.CurrentValueSnapshot) ? "(none)" : request.CurrentValueSnapshot;
            ProposedValue = request.ProposedValue;
            RequestStatus = request.RequestStatus;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交时间文本。</zh-CN>
        ///   <en>Submission time text.</en>
        /// </lang>
        /// </summary>
        public string SubmittedUtcText { get; private set; }

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
        ///   <zh-CN>请求状态。</zh-CN>
        ///   <en>Request status.</en>
        /// </lang>
        /// </summary>
        public string RequestStatus { get; private set; }
    }
}
