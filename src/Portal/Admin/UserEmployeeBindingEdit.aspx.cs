using System;
using System.Globalization;
using System.Linq;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户账号与员工单条绑定后台维护页。</zh-CN>
    ///   <en>Administration page for maintaining one Portal-user to employee binding.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P6.3-S5 只提供单条绑定和结束当前绑定，不做批量导入、外部 HR 同步或在线身份源配置。绑定/解绑写入、目标用户安全版本失效和运营审计由连续调用组成，不宣称跨服务原子事务；成功递增安全版本后，旧登录票据和角色 Cookie 才会在后续请求中按既有认证链失效。</zh-CN>
    ///   <en>P6.3-S5 provides only single-row binding and ending of the current binding, not bulk import, external HR synchronization, or online identity-source configuration. Bind/unbind persistence, target-user security-version invalidation, and operations audit are sequential calls rather than a claimed cross-service atomic transaction; after a successful version increment, old auth tickets and role cookies expire on later requests through the existing authentication chain.</en>
    /// </lang>
    /// </remarks>
    public partial class UserEmployeeBindingEdit : PortalPage<UserEmployeeBindingEdit>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定写入服务。</zh-CN>
        ///   <en>Binding write service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUserEmployeeBindingAdminDb BindingAdminDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工目录只读服务。</zh-CN>
        ///   <en>Employee-directory read service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工主数据维护服务。</zh-CN>
        ///   <en>Employee master-data maintenance service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryAdminDb EmployeeAdminDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户服务。</zh-CN>
        ///   <en>Portal user service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权并加载初始绑定上下文。</zh-CN>
        ///   <en>Authorizes the request and loads the initial binding context.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>建立当前有效绑定。</zh-CN>
        ///   <en>Creates the current active binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该回发先复核绑定权限和正数用户标识，再调用绑定写入；随后递增安全版本并记录审计。三步不共享本页声明的跨服务事务，异常处理保留已发生的部分完成事实。</zh-CN>
        ///   <en>This postback rechecks binding permission and a positive user identifier before the bind write, then increments the security version and records audit. The three steps do not share a cross-service transaction claimed by this page, and exception handling preserves the fact that earlier steps may already have completed.</en>
        /// </lang>
        /// </remarks>
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
                // <lang>
                //   <zh-CN>绑定写入只接收员工号和操作原因，账号标识来自已校验的后台输入，操作者来自当前登录身份。</zh-CN>
                //   <en>The bind write accepts only the employee code and operation reason; the account identifier comes from validated administration input and the actor comes from the current identity.</en>
                // </lang>
                EmployeeDirectoryWriteResult result = BindingAdminDb.BindUserToEmployee(new UserEmployeeBindingSaveRequest
                {
                    UserId = userId,
                    EmployeeCode = EmployeeCodeTextBox.Text,
                    Reason = ReasonTextBox.Text,
                    ActorName = GetCurrentActor()
                });

                if (!result.Succeeded)
                {
                    // <lang>
                    //   <zh-CN>绑定服务返回失败时不递增安全版本、不记录成功审计，刷新当前状态以反映服务端事实。</zh-CN>
                    //   <en>When the binding service reports failure, do not increment the security version or record success audit; refresh the current state to reflect the server fact.</en>
                    // </lang>
                    ShowMessage(result.Message, true);
                    RefreshCurrentState(userId, EmployeeCodeTextBox.Text);
                    return;
                }

                // <lang>
                //   <zh-CN>绑定成功后递增安全版本，让目标用户旧票据在后续请求自然失效。</zh-CN>
                //   <en>After a successful bind, increment the security version so the target user's old tickets expire naturally on later requests.</en>
                // </lang>
                UsersDb.IncrementSecurityVersion(userId, "EmployeeBindingChanged");
                // <lang>
                //   <zh-CN>安全版本递增与绑定写入已是连续调用；若递增异常，绑定事实可能已经持久化，不能把 catch 后提示误写成回滚完成。</zh-CN>
                //   <en>The security-version increment is a sequential call after binding persistence; if it throws, the binding fact may already be durable, so the catch message must not be read as a completed rollback.</en>
                // </lang>
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
        /// <lang>
        ///   <zh-CN>结束当前有效绑定。</zh-CN>
        ///   <en>Ends the current active binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>解绑先解析隐藏域中的正数绑定标识并重新读取当前记录，再执行结束写入；成功后同样递增用户安全版本并审计。隐藏域不是授权凭据，最终权限仍由回发门禁和数据服务共同约束。</zh-CN>
        ///   <en>Unbinding parses a positive binding id from the hidden field and reloads the current row before ending it; success then increments the user security version and records audit. The hidden field is not an authorization credential; the postback gate and data service together constrain the final operation.</en>
        /// </lang>
        /// </remarks>
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
                // <lang>
                //   <zh-CN>解绑使用当前隐藏域记录的绑定标识，并在写入前重新读取绑定，避免对已删除或已变化记录继续操作。</zh-CN>
                //   <en>Unbinding uses the binding identifier stored in the hidden field and reloads the binding before writing so deleted or changed rows are not processed blindly.</en>
                // </lang>
                EmployeeDirectoryWriteResult result = BindingAdminDb.EndBinding(new UserEmployeeBindingEndRequest
                {
                    BindingId = bindingId,
                    Reason = ReasonTextBox.Text,
                    ActorName = GetCurrentActor()
                });

                if (!result.Succeeded)
                {
                    // <lang>
                    //   <zh-CN>解绑服务失败时保留当前绑定摘要并返回，不递增安全版本或记录成功审计。</zh-CN>
                    //   <en>When the unbind service fails, retain the current binding summary and return without incrementing the security version or recording success audit.</en>
                    // </lang>
                    ShowMessage(result.Message, true);
                    RefreshCurrentState(binding.UserId, binding.EmployeeCode);
                    return;
                }

                // <lang>
                //   <zh-CN>解绑同样递增目标用户安全版本，因为员工身份绑定会影响后续权限和资料上下文。</zh-CN>
                //   <en>Unbinding also increments the target user's security version because employee identity binding affects later permissions and profile context.</en>
                // </lang>
                UsersDb.IncrementSecurityVersion(binding.UserId, "EmployeeBindingChanged");
                // <lang>
                //   <zh-CN>解绑写入、安全版本递增和审计仍是连续步骤；后续异常不代表前一步写入已自动回滚。</zh-CN>
                //   <en>Unbind persistence, security-version increment, and audit remain sequential steps; a later exception does not mean the earlier write was automatically rolled back.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>加载页面首次显示所需的服务状态、请求参数和当前绑定摘要。</zh-CN>
        ///   <en>Loads service state, request parameters, and current binding summaries needed for the first page display.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法不创建或修改绑定；它只根据可选的绑定、用户和员工参数预填后台表单，并在 P6.3 表不可用时禁用写入按钮。</zh-CN>
        ///   <en>This method does not create or modify bindings; it only pre-fills the administration form from optional binding, user, and employee parameters, and disables write buttons when the P6.3 schema is unavailable.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>刷新用户摘要、员工摘要和当前有效绑定摘要。</zh-CN>
        ///   <en>Refreshes the user summary, employee summary, and current active binding summary.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识；小于等于 0 时不显示用户摘要。</zh-CN>
        ///   <en>Portal user identifier; values less than or equal to 0 suppress the user summary.</en>
        /// </l>
        /// </param>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>员工号，可为空。</zh-CN>
        ///   <en>Employee code, optionally empty.</en>
        /// </l>
        /// </param>
        private void RefreshCurrentState(int userId, string employeeCode)
        {
            BindUserSummary(userId);
            BindEmployeeSummary(employeeCode);
            BindCurrentBinding(userId, employeeCode);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示指定门户用户的低敏摘要和用户管理链接。</zh-CN>
        ///   <en>Displays a low-sensitivity summary and management link for the specified Portal user.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </l>
        /// </param>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示指定员工号对应的低敏员工摘要。</zh-CN>
        ///   <en>Displays a low-sensitivity employee summary for the specified employee code.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>员工号，可为空。</zh-CN>
        ///   <en>Employee code, optionally empty.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>查询只使用目录读取接口，不在摘要区暴露更高敏资料；员工不存在时只显示通用提示。</zh-CN>
        ///   <en>The lookup uses only the directory read contract and does not expose more sensitive data in the summary area; a missing employee shows a generic message.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据用户标识或员工号显示当前有效绑定，并切换绑定/解绑按钮状态。</zh-CN>
        ///   <en>Displays the current active binding by user identifier or employee code and toggles bind/unbind button state.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>员工号，可为空。</zh-CN>
        ///   <en>Employee code, optionally empty.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>当前绑定摘要优先按用户再按员工号读取；隐藏域只保存展示记录的绑定标识，结束操作前仍由回发代码重新读取并由数据服务复核。摘要文本统一 HTML 编码。</zh-CN>
        ///   <en>The current binding summary reads by user first and employee code second; the hidden field stores only the displayed row's binding id, which the postback code reloads and the data service rechecks before ending. Summary text is HTML-encoded consistently.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>从后台输入框读取并验证门户用户标识。</zh-CN>
        ///   <en>Reads and validates the Portal user identifier from the administration input.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>解析后的门户用户标识。</zh-CN>
        ///   <en>Parsed Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>输入为正整数时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the input is a positive integer.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取一个可选的正整数请求参数。</zh-CN>
        ///   <en>Reads one optional positive-integer request parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>请求参数名称。</zh-CN>
        ///   <en>Request parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>解析后的值；参数缺失时为 0。</zh-CN>
        ///   <en>Parsed value, or 0 when the parameter is absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或为正整数时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is absent or a positive integer.</en>
        /// </l>
        /// </returns>
        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            return string.IsNullOrWhiteSpace(rawValue) ||
                   PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示服务或数据库结构不可用状态，并禁用写入按钮。</zh-CN>
        ///   <en>Shows a service or schema unavailable state and disables write buttons.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>展示给管理员的安全提示。</zh-CN>
        ///   <en>Safe message displayed to the administrator.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>不可用状态只改变页面提示和按钮可用性，不伪造绑定事实、不执行写入，也不把结构缺失当成权限通过。</zh-CN>
        ///   <en>The unavailable state changes only page messaging and button availability; it does not fake a binding fact, perform writes, or treat a missing schema as authorization.</en>
        /// </lang>
        /// </remarks>
        private void ShowUnavailable(string message)
        {
            ShowMessage(message, true);
            BindButton.Enabled = false;
            EndBindingButton.Enabled = false;
            CurrentBindingText.Text = "Unavailable.";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>输出经过 HTML 编码的后台提示。</zh-CN>
        ///   <en>Outputs an HTML-encoded administration message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>提示文本。</zh-CN>
        ///   <en>Message text.</en>
        /// </l>
        /// </param>
        /// <param name="isError">
        /// <l>
        ///   <zh-CN>是否按错误样式显示。</zh-CN>
        ///   <en>Whether to display the message using the error style.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message, bool isError)
        {
            MessageLabel.CssClass = isError ? "NormalRed" : "Normal";
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前审计操作者名称。</zh-CN>
        ///   <en>Gets the current audit actor name.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前登录身份名称；缺失时使用后台兼容值。</zh-CN>
        ///   <en>Current identity name, or an administration-compatible fallback when absent.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>返回值只作为既有审计字段输入；缺失时的 admin 是兼容占位，不是身份认证、权限授予或操作者真实性证明。</zh-CN>
        ///   <en>The return value is only an input to the existing audit field; the admin fallback is a compatibility placeholder, not authentication, permission granting, or proof of actor authenticity.</en>
        /// </lang>
        /// </remarks>
        private string GetCurrentActor()
        {
            return Context.User == null || Context.User.Identity == null ||
                   string.IsNullOrWhiteSpace(Context.User.Identity.Name)
                ? "admin"
                : Context.User.Identity.Name;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化后台文本输入。</zh-CN>
        ///   <en>Normalizes administration text input.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始输入。</zh-CN>
        ///   <en>Raw input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>去除首尾空白后的文本；空输入返回空字符串。</zh-CN>
        ///   <en>Trimmed text, or an empty string for blank input.</en>
        /// </l>
        /// </returns>
        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
