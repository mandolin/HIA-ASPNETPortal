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
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
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
        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            int userId = GetCurrentUserId();
            EmployeeProfileCorrectionProfileView profile = GetCurrentProfile(userId);
            if (profile == null)
            {
                ShowMessage("当前账号没有可提交更正请求的在职员工资料。");
                return;
            }

            string fieldName = FieldNameList.SelectedValue;
            string proposedValue = NormalizeInput(ProposedValueTextBox.Text, 512);
            string requestNote = NormalizeInput(RequestNoteTextBox.Text, 1000);
            if (string.IsNullOrWhiteSpace(proposedValue))
            {
                ShowMessage("请填写建议值。");
                return;
            }

            if (string.Equals(GetCurrentValue(profile, fieldName), proposedValue, StringComparison.Ordinal))
            {
                ShowMessage("建议值与当前值相同，无需提交更正请求。");
                return;
            }

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
                ShowMessage(result.Message);
                return;
            }

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.EmployeeProfileCorrectionRequested,
                PortalOperationAuditEvents.EmployeeProfileCorrectionRequestTargetType,
                result.RequestId.ToString(CultureInfo.InvariantCulture),
                "Employee profile correction requested. EmployeeId=" + profile.EmployeeId.ToString(CultureInfo.InvariantCulture) +
                "; FieldName=" + fieldName,
                Context);

            TryEnsureWorkItem(result.RequestId, profile.EmployeeId, fieldName);

            ProposedValueTextBox.Text = string.Empty;
            RequestNoteTextBox.Text = string.Empty;
            BindProfile();
            ShowMessage("更正请求已提交，等待管理员处理。");
        }

        private void BindFieldList()
        {
            FieldNameList.Items.Clear();
            FieldNameList.Items.Add(new ListItem("姓名", "DisplayName"));
            FieldNameList.Items.Add(new ListItem("称呼", "PreferredName"));
            FieldNameList.Items.Add(new ListItem("工作邮箱", "WorkEmail"));
            FieldNameList.Items.Add(new ListItem("组织", "OrganizationDisplayName"));
        }

        private void BindProfile()
        {
            int userId = GetCurrentUserId();
            EmployeeProfileCorrectionProfileView profile = GetCurrentProfile(userId);
            if (profile == null)
            {
                RequestPanel.Visible = false;
                RecentRequestsRepeater.DataSource = Enumerable.Empty<EmployeeProfileCorrectionRecentRequestRow>();
                RecentRequestsRepeater.DataBind();
                ShowMessage(GetUnavailableMessage(userId));
                return;
            }

            RequestPanel.Visible = true;
            MessageLabel.Text = string.Empty;
            EmployeeCodeLabel.Text = EncodeDisplay(profile.EmployeeCode);
            DisplayNameLabel.Text = EncodeDisplay(profile.DisplayName);
            PreferredNameLabel.Text = EncodeDisplay(EmptyToNone(profile.PreferredName));
            WorkEmailLabel.Text = EncodeDisplay(EmptyToNone(profile.WorkEmail));
            OrganizationLabel.Text = EncodeDisplay(EmptyToNone(profile.OrganizationDisplayName));
            BindRecentRequests(userId);
        }

        private void BindRecentRequests(int userId)
        {
            IList<EmployeeProfileCorrectionRequestInfo> requests = CorrectionRequestDb == null
                ? new List<EmployeeProfileCorrectionRequestInfo>()
                : CorrectionRequestDb.GetRecentRequestsForUser(userId, RecentRequestLimit);

            RecentRequestsRepeater.DataSource = requests.Select(request => new EmployeeProfileCorrectionRecentRequestRow(request)).ToList();
            RecentRequestsRepeater.DataBind();
        }

        private EmployeeProfileCorrectionProfileView GetCurrentProfile(int userId)
        {
            if (CorrectionRequestDb == null ||
                !CorrectionRequestDb.IsSchemaAvailable() ||
                userId <= 0)
            {
                return null;
            }

            return CorrectionRequestDb.GetCurrentProfileForUser(userId);
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

        private string GetUnavailableMessage(int userId)
        {
            if (!IsCurrentUserAuthenticated())
            {
                return "请先登录后再提交员工资料更正请求。";
            }

            if (CorrectionRequestDb == null || !CorrectionRequestDb.IsSchemaAvailable())
            {
                return "员工资料更正请求模块尚未完成数据库初始化。";
            }

            return userId <= 0
                ? "当前登录账号无法解析到门户用户。"
                : "当前账号没有可提交更正请求的在职员工资料。";
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

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private string EncodeDisplay(string value)
        {
            return Server.HtmlEncode(value ?? string.Empty);
        }

        private static string GetCurrentValue(EmployeeProfileCorrectionProfileView profile, string fieldName)
        {
            if (profile == null)
            {
                return string.Empty;
            }

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

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        private static string NormalizeInput(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
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
        internal EmployeeProfileCorrectionRecentRequestRow(EmployeeProfileCorrectionRequestInfo request)
        {
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
