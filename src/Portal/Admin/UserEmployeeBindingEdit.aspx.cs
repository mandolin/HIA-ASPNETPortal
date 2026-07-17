using System;
using System.Globalization;
using System.Linq;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户账号与员工单条绑定后台维护页。
    ///
    /// English: Administration page for maintaining one Portal-user to employee binding.
    /// </summary>
    /// <remarks>
    /// 中文：P6.3-S5 只提供单条绑定和结束当前绑定，不做批量导入、外部 HR 同步或在线身份源配置。
    /// 绑定成功或解绑成功后会递增目标用户安全版本，使旧登录票据和角色 Cookie 在后续请求中失效。
    ///
    /// English: P6.3-S5 provides only single-row binding and ending of the current binding, not bulk import,
    /// external HR synchronization, or online identity-source configuration. Successful bind or unbind operations
    /// increment the target user's security version so old auth tickets and role cookies expire on later requests.
    /// </remarks>
    public partial class UserEmployeeBindingEdit : PortalPage<UserEmployeeBindingEdit>
    {
        /// <summary>中文：绑定写入服务。English: Binding write service.</summary>
        [Dependency]
        public IUserEmployeeBindingAdminDb BindingAdminDb { private get; set; }

        /// <summary>中文：员工目录只读服务。English: Employee-directory read service.</summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>中文：员工主数据维护服务。English: Employee master-data maintenance service.</summary>
        [Dependency]
        public IEmployeeDirectoryAdminDb EmployeeAdminDb { private get; set; }

        /// <summary>中文：门户用户服务。English: Portal user service.</summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary>
        /// 中文：授权并加载初始绑定上下文。
        ///
        /// English: Authorizes the request and loads the initial binding context.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeDirectoryBind))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindInitialState();
            }
        }

        /// <summary>
        /// 中文：建立当前有效绑定。
        ///
        /// English: Creates the current active binding.
        /// </summary>
        protected void BindButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeDirectoryBind))
            {
                return;
            }

            int userId;
            if (!TryReadUserId(out userId))
            {
                return;
            }

            try
            {
                EmployeeDirectoryWriteResult result = BindingAdminDb.BindUserToEmployee(new UserEmployeeBindingSaveRequest
                {
                    UserId = userId,
                    EmployeeCode = EmployeeCodeTextBox.Text,
                    Reason = ReasonTextBox.Text,
                    ActorName = GetCurrentActor()
                });

                if (!result.Succeeded)
                {
                    ShowMessage(result.Message, true);
                    RefreshCurrentState(userId, EmployeeCodeTextBox.Text);
                    return;
                }

                UsersDb.IncrementSecurityVersion(userId, "EmployeeBindingChanged");
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.EnterpriseDirectoryCategory,
                    PortalOperationAuditEvents.UserEmployeeBound,
                    PortalOperationAuditEvents.UserEmployeeBindingTargetType,
                    result.EntityId.ToString(CultureInfo.InvariantCulture),
                    "Bound user id " + userId.ToString(CultureInfo.InvariantCulture) + " to employee code.",
                    Context);
                ShowMessage("绑定已保存，目标用户旧会话将在后续请求中失效。", false);
                RefreshCurrentState(userId, EmployeeCodeTextBox.Text);
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.UserEmployeeBindingEdit.Bind",
                    "Binding user to employee failed. UserId=" + userId,
                    exception,
                    Context);
                ShowMessage("绑定失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// 中文：结束当前有效绑定。
        ///
        /// English: Ends the current active binding.
        /// </summary>
        protected void EndBindingButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeDirectoryBind))
            {
                return;
            }

            int bindingId;
            if (!int.TryParse(ActiveBindingId.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out bindingId) ||
                bindingId <= 0)
            {
                ShowMessage("没有可结束的当前有效绑定。", true);
                return;
            }

            IUserEmployeeBindingInfo binding = BindingAdminDb.GetBindingById(bindingId);
            if (binding == null)
            {
                ShowMessage("绑定记录已不存在，请重新打开页面。", true);
                return;
            }

            try
            {
                EmployeeDirectoryWriteResult result = BindingAdminDb.EndBinding(new UserEmployeeBindingEndRequest
                {
                    BindingId = bindingId,
                    Reason = ReasonTextBox.Text,
                    ActorName = GetCurrentActor()
                });

                if (!result.Succeeded)
                {
                    ShowMessage(result.Message, true);
                    RefreshCurrentState(binding.UserId, binding.EmployeeCode);
                    return;
                }

                UsersDb.IncrementSecurityVersion(binding.UserId, "EmployeeBindingChanged");
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.EnterpriseDirectoryCategory,
                    PortalOperationAuditEvents.UserEmployeeUnbound,
                    PortalOperationAuditEvents.UserEmployeeBindingTargetType,
                    bindingId.ToString(CultureInfo.InvariantCulture),
                    "Ended employee binding for user id " + binding.UserId.ToString(CultureInfo.InvariantCulture) + ".",
                    Context);
                ShowMessage("绑定已结束，目标用户旧会话将在后续请求中失效。", false);
                RefreshCurrentState(binding.UserId, binding.EmployeeCode);
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.UserEmployeeBindingEdit.End",
                    "Ending user employee binding failed. BindingId=" + bindingId,
                    exception,
                    Context);
                ShowMessage("解绑失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        private void BindInitialState()
        {
            if (BindingAdminDb == null || EmployeeDirectoryDb == null || EmployeeAdminDb == null || UsersDb == null)
            {
                ShowUnavailable("User-employee binding services are not registered.");
                return;
            }

            if (!BindingAdminDb.IsSchemaAvailable() || !EmployeeDirectoryDb.IsSchemaAvailable())
            {
                ShowUnavailable("P6.3 employee binding schema is unavailable. Run the P6.3 SQL scripts before editing bindings.");
                return;
            }

            int bindingId;
            if (TryReadOptionalPositiveParameter("bindingId", out bindingId) && bindingId > 0)
            {
                IUserEmployeeBindingInfo binding = BindingAdminDb.GetBindingById(bindingId);
                if (binding != null)
                {
                    UserIdTextBox.Text = binding.UserId.ToString(CultureInfo.InvariantCulture);
                    EmployeeCodeTextBox.Text = binding.EmployeeCode;
                    RefreshCurrentState(binding.UserId, binding.EmployeeCode);
                    return;
                }
            }

            int userId;
            TryReadOptionalPositiveParameter("userId", out userId);
            if (userId > 0)
            {
                UserIdTextBox.Text = userId.ToString(CultureInfo.InvariantCulture);
            }

            int employeeId;
            if (TryReadOptionalPositiveParameter("employeeId", out employeeId) && employeeId > 0)
            {
                IEmployeeInfo employee = EmployeeAdminDb.GetEmployeeById(employeeId);
                if (employee != null)
                {
                    EmployeeCodeTextBox.Text = employee.EmployeeCode;
                }
            }

            RefreshCurrentState(userId, EmployeeCodeTextBox.Text);
        }

        private void RefreshCurrentState(int userId, string employeeCode)
        {
            BindUserSummary(userId);
            BindEmployeeSummary(employeeCode);
            BindCurrentBinding(userId, employeeCode);
        }

        private void BindUserSummary(int userId)
        {
            if (userId <= 0)
            {
                UserSummaryText.Text = string.Empty;
                ManageUserLink.Visible = false;
                return;
            }

            IUserItem user = UsersDb.FindUserById(userId);
            if (user == null)
            {
                UserSummaryText.Text = "User not found.";
                ManageUserLink.Visible = false;
                return;
            }

            UserSummaryText.Text = Server.HtmlEncode(user.Name + " / " + user.Email);
            ManageUserLink.NavigateUrl = ResolveUrl("~/Admin/ManageUsers.aspx?userId=" + userId.ToString(CultureInfo.InvariantCulture));
            ManageUserLink.Visible = true;
        }

        private void BindEmployeeSummary(string employeeCode)
        {
            string normalizedCode = Normalize(employeeCode);
            if (string.IsNullOrEmpty(normalizedCode) || EmployeeAdminDb == null || !EmployeeAdminDb.IsSchemaAvailable())
            {
                EmployeeSummaryText.Text = string.Empty;
                return;
            }

            IEmployeeInfo employee = EmployeeDirectoryDb
                .GetEmployees(new EmployeeDirectoryQuery { Keyword = normalizedCode, Take = 10 })
                .FirstOrDefault(item => string.Equals(item.EmployeeCode, normalizedCode, StringComparison.Ordinal));
            EmployeeSummaryText.Text = employee == null
                ? "Employee not found."
                : Server.HtmlEncode(employee.DisplayName + " / " + employee.EmploymentStatus);
        }

        private void BindCurrentBinding(int userId, string employeeCode)
        {
            IUserEmployeeBindingInfo binding = null;
            if (userId > 0)
            {
                binding = EmployeeDirectoryDb.GetActiveBindingByUserId(userId);
            }

            if (binding == null && !string.IsNullOrWhiteSpace(employeeCode))
            {
                binding = EmployeeDirectoryDb.GetActiveBindingByEmployeeCode(employeeCode);
            }

            if (binding == null)
            {
                ActiveBindingId.Value = string.Empty;
                CurrentBindingText.Text = "No active binding.";
                EndBindingButton.Visible = false;
                BindButton.Visible = true;
                return;
            }

            ActiveBindingId.Value = binding.BindingId.ToString(CultureInfo.InvariantCulture);
            CurrentBindingText.Text = Server.HtmlEncode(
                "#" + binding.BindingId.ToString(CultureInfo.InvariantCulture) +
                " User " + binding.UserId.ToString(CultureInfo.InvariantCulture) +
                " / " + binding.UserName +
                " -> " + binding.EmployeeCode +
                " / " + binding.EmployeeDisplayName +
                " (" + binding.BindingStatus + ")");
            EndBindingButton.Visible = true;
            BindButton.Visible = false;
        }

        private bool TryReadUserId(out int userId)
        {
            userId = 0;
            if (PortalNavigationPolicy.TryReadPositiveInt32(UserIdTextBox.Text, out userId))
            {
                return true;
            }

            ShowMessage("请输入有效的 Portal User ID。", true);
            return false;
        }

        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            return string.IsNullOrWhiteSpace(rawValue) ||
                   PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value);
        }

        private void ShowUnavailable(string message)
        {
            ShowMessage(message, true);
            BindButton.Enabled = false;
            EndBindingButton.Enabled = false;
            CurrentBindingText.Text = "Unavailable.";
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageLabel.CssClass = isError ? "NormalRed" : "Normal";
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private string GetCurrentActor()
        {
            return Context.User == null || Context.User.Identity == null ||
                   string.IsNullOrWhiteSpace(Context.User.Identity.Name)
                ? "admin"
                : Context.User.Identity.Name;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
