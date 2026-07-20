using System;
using System.Web;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：公开自主注册页面的 Web Forms code-behind。
    ///
    /// English: Web Forms code-behind for the public self-registration page.
    /// </summary>
    /// <remarks>
    /// 中文：只有 <see cref="PortalRegistrationOptions.AllowSelfRegistration"/> 为 <c>true</c> 时才可访问。
    /// 当前空邀请码允许非邀请注册；带邀请码时校验启用状态、UTC 到期时间与使用次数。是否要求邀请码是后续独立设置，
    /// 不能在此页面隐式改变。需要审核的注册不会自动登录。
    ///
    /// English: Access is allowed only when <see cref="PortalRegistrationOptions.AllowSelfRegistration"/> is <c>true</c>.
    /// Empty invite codes currently allow non-invite registration; supplied invite codes validate enabled state, UTC
    /// expiration, and usage count. Requiring invite codes is a later independent setting and must not be changed
    /// implicitly here. Registrations requiring approval do not sign in automatically.
    /// </remarks>
    public partial class Register : PortalPage<Register>
    {
        /// <summary>
        /// 中文：用户与注册审核数据访问依赖。
        ///
        /// English: User and registration-review data-access dependency.
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// 中文：页面加载时验证自主注册开关并配置邀请码相关的员工号校验。
        ///
        /// English: Validates the self-registration switch on page load and configures employee-code validation for invite registration.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            ConfigureRegistrationForm();
        }

        // 注册按钮点击事件处理程序
        /// <summary>
        /// 中文：处理自主注册提交，创建用户、记录注册审计，并按审核开关决定是否直接登录。
        ///
        /// English: Handles self-registration submission, creates the user, records registration audit data, and decides immediate sign-in from the approval switch.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void RegisterBtn_Click(object sender, EventArgs e)
        {
            // 中文：每次提交重查开关，避免页面初次加载后部署设置发生变化时绕过限制。
            // English: Recheck the switch on every submit so deployment-setting changes after initial page load cannot bypass the restriction.
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            // 中文：只在 Web Forms 基础验证通过后继续创建用户。
            // English: Continue user creation only after base Web Forms validation succeeds.
            if (Page.IsValid)
            {
                var userName = Name.Text.Trim();
                var email = Email.Text.Trim();
                string employeeCode = EmployeeCode.Text.Trim();
                string inviteCode = CurrentInviteCode;
                string submittedPassword;
                string submittedConfirmPassword;
                string inviteMessage;
                string passwordPolicyMessage;

                if (!TryResolveRegistrationPasswords(out submittedPassword, out submittedConfirmPassword))
                {
                    ClearSubmittedPasswordFields();
                    return;
                }

                ClearSubmittedPasswordFields();

                if (string.IsNullOrEmpty(submittedPassword) || string.IsNullOrEmpty(submittedConfirmPassword))
                {
                    Message.Text = "'Password' and 'Confirm Password' must not be left blank.";
                    return;
                }

                if (!string.Equals(submittedPassword, submittedConfirmPassword, StringComparison.Ordinal))
                {
                    Message.Text = "Password fields do not match.";
                    return;
                }

                if (!UsersDB.ValidateRegistrationInvite(inviteCode, out inviteMessage))
                {
                    Message.Text = inviteMessage;
                    return;
                }

                if (!PortalPasswordPolicy.TryValidate(
                    submittedPassword,
                    BuildPasswordPolicyContextTerms(userName, email, employeeCode),
                    out passwordPolicyMessage))
                {
                    Message.Text = passwordPolicyMessage;
                    return;
                }

                if (PortalRegistrationOptions.IsEmployeeCodeRequired(inviteCode) &&
                    string.IsNullOrWhiteSpace(employeeCode) &&
                    !PortalRegistrationOptions.AllowPendingEmployeeBinding)
                {
                    Message.Text = "Employee Code is required for invitation registration.";
                    return;
                }

                int userId;
                try
                {
                    // 中文：只传递一次性密码输入；数据层负责强哈希写入，异常与审计信息不得包含密码或邀请码原文。
                    // English: Pass only one-time password input; the data layer owns strong-hash writes, and exceptions/audits must not contain passwords or raw invite codes.
                    userId = UsersDB.AddSelfRegisteredUser(
                        userName,
                        email,
                        submittedPassword,
                        employeeCode,
                        inviteCode,
                        PortalRegistrationOptions.RequireRegistrationApproval);
                }
                catch (Exception ex)
                {
                    string eventId = PortalDiagnostics.Error(
                        "Admin.Register.SelfRegistration",
                        "Self-registration failed for userName=" + userName + "; email=" + email,
                        ex,
                        Context);
                    Message.Text = "Registration failed. The system recorded this error. Event ID: " + eventId;
                    return;
                }

                if (userId > -1)
                {
                    // 中文：记录注册状态变化；审计不可用不阻断已成功的注册事务。
                    // English: Record the registration state change; unavailable auditing does not block a successful registration transaction.
                    PortalOperationAudit.Record(
                        PortalOperationAuditEvents.UserLifecycleCategory,
                        PortalOperationAuditEvents.RegistrationSubmitted,
                        PortalOperationAuditEvents.UserTargetType,
                        userId.ToString(),
                        "Self-registration submitted.",
                        Context);

                    if (PortalRegistrationOptions.RequireRegistrationApproval)
                    {
                        // 中文：待审核用户不签发认证票据，管理员批准后才满足登录条件。
                        // English: Pending users receive no authentication ticket and meet sign-in conditions only after administrator approval.
                        RegisterBtn.Visible = false;
                        Message.CssClass = "Normal";
                        Message.Text = "Registration submitted. Please wait for administrator approval.";
                        return;
                    }

                    // 中文：关闭审核时保持既有直接登录行为，但身份票据需带当前安全版本。
                    // English: Preserve legacy immediate sign-in behavior when approval is disabled, while carrying the current security version in the identity ticket.
                    PortalAuthenticationService.SignIn(
                        Response,
                        Request,
                        userName,
                        UsersDB.GetSecurityVersionByUserName(userName),
                        false);
                    Response.Redirect("~/DesktopDefault.aspx");
                }
                else
                {
                    // 中文：保持对外提示泛化，避免暴露数据库或邀请码校验细节。
                    // English: Keep the user-facing message generic and avoid exposing database or invite-validation details.
                    Message.Text = "Registration failed. The user name or email may already exist, or registration metadata is not available.";
                }
            }
        }

        private string CurrentInviteCode
        {
            get
            {
                // 中文：空值代表当前允许的非邀请注册，不在这里强制改写为拒绝。
                // English: An empty value represents currently allowed non-invite registration and is not forced to rejection here.
                string inviteCode = Request.QueryString["invite"];
                return string.IsNullOrWhiteSpace(inviteCode) ? string.Empty : inviteCode.Trim();
            }
        }

        private void ConfigureRegistrationForm()
        {
            // 中文：只有邀请注册且未允许待绑定员工号时，员工号成为本页必填项。
            // English: Employee code becomes required only for invite registration when pending employee binding is not allowed.
            bool employeeCodeRequired = PortalRegistrationOptions.IsEmployeeCodeRequired(CurrentInviteCode) &&
                                        !PortalRegistrationOptions.AllowPendingEmployeeBinding;
            EmployeeCodeRequiredValidator.Enabled = employeeCodeRequired;
            EmployeeCodeRequiredHint.Visible = employeeCodeRequired;
            ConfigurePasswordSubmission();
        }

        /// <summary>
        /// 中文：按加密开关配置注册页密码提交脚本和旧验证器。
        ///
        /// English: Configures registration password-submission scripts and legacy validators from the encryption switch.
        /// </summary>
        private void ConfigurePasswordSubmission()
        {
            bool encryptedSubmissionRequired = PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired();
            RequiredFieldValidator3.Enabled = !encryptedSubmissionRequired;
            RequiredFieldValidator4.Enabled = !encryptedSubmissionRequired;
            CompareValidator1.Enabled = !encryptedSubmissionRequired;

            if (!encryptedSubmissionRequired)
            {
                RegisterBtn.OnClientClick = string.Empty;
                return;
            }

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Register),
                "JSEncryptIE6",
                ResolveUrl("~/Scripts/Security/jsencrypt-ie6.min.js"));

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Register),
                "PortalLoginPasswordEncryption",
                ResolveUrl("~/Scripts/Security/PortalLoginPasswordEncryption.js"));

            RegisterBtn.OnClientClick = string.Format(
                "return PortalLoginPasswordEncryption.encryptPasswordFields([{0},{1}],'{2}','{3}');",
                BuildPasswordFieldScriptObject(Password.ClientID, EncryptedPassword.ClientID),
                BuildPasswordFieldScriptObject(ConfirmPassword.ClientID, EncryptedConfirmPassword.ClientID),
                HttpUtility.JavaScriptStringEncode(ResolveUrl("~/Security/LoginPasswordKey.ashx")),
                HttpUtility.JavaScriptStringEncode(Message.ClientID));
        }

        /// <summary>
        /// 中文：解析注册页提交的密码和确认密码，优先使用加密隐藏字段。
        ///
        /// English: Resolves registration password and confirmation values, preferring encrypted hidden fields.
        /// </summary>
        /// <param name="submittedPassword">中文：当前请求内使用的明文密码。English: Plain password for this request.</param>
        /// <param name="submittedConfirmPassword">中文：当前请求内使用的确认密码。English: Plain confirmation password for this request.</param>
        /// <returns>中文：提交满足当前加密策略时为 <c>true</c>。English: <c>true</c> when the submission satisfies the current encryption policy.</returns>
        private bool TryResolveRegistrationPasswords(
            out string submittedPassword,
            out string submittedConfirmPassword)
        {
            submittedPassword = string.Empty;
            submittedConfirmPassword = string.Empty;

            bool hasEncryptedPassword = !string.IsNullOrWhiteSpace(EncryptedPassword.Value);
            bool hasEncryptedConfirmPassword = !string.IsNullOrWhiteSpace(EncryptedConfirmPassword.Value);

            if (hasEncryptedPassword || hasEncryptedConfirmPassword)
            {
                if (!hasEncryptedPassword || !hasEncryptedConfirmPassword)
                {
                    PortalDiagnostics.Warn(
                        "PasswordSubmissionEncryption",
                        "Registration password submission was incomplete: one encrypted password field was missing.",
                        Context);
                    Message.Text = "密码提交不完整，请刷新页面后重试。";
                    return false;
                }

                string[] submittedPasswords;
                string failureCode;
                string eventId;
                if (PortalPasswordSubmissionCrypto.TryDecryptSubmittedPasswords(
                    Context,
                    new[] { EncryptedPassword.Value, EncryptedConfirmPassword.Value },
                    out submittedPasswords,
                    out failureCode,
                    out eventId))
                {
                    submittedPassword = submittedPasswords.Length > 0 ? submittedPasswords[0] : string.Empty;
                    submittedConfirmPassword = submittedPasswords.Length > 1 ? submittedPasswords[1] : string.Empty;
                    return true;
                }

                Message.Text = "密码提交验证失败，请刷新页面后重试。";
                return false;
            }

            if (PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired())
            {
                PortalDiagnostics.Warn(
                    "PasswordSubmissionEncryption",
                    "Registration password was submitted without the required encrypted fields.",
                    Context);
                Message.Text = "密码提交验证失败，请刷新页面后重试。";
                return false;
            }

            submittedPassword = Password.Text ?? string.Empty;
            submittedConfirmPassword = ConfirmPassword.Text ?? string.Empty;
            return true;
        }

        /// <summary>
        /// 中文：清空注册页密码字段，避免异常路径和回显残留。
        ///
        /// English: Clears registration password fields to avoid exception-path and echo residue.
        /// </summary>
        private void ClearSubmittedPasswordFields()
        {
            Password.Text = string.Empty;
            ConfirmPassword.Text = string.Empty;
            EncryptedPassword.Value = string.Empty;
            EncryptedConfirmPassword.Value = string.Empty;
        }

        private static string BuildPasswordFieldScriptObject(string passwordElementId, string encryptedElementId)
        {
            return string.Format(
                "{{passwordElementId:'{0}',encryptedElementId:'{1}'}}",
                HttpUtility.JavaScriptStringEncode(passwordElementId),
                HttpUtility.JavaScriptStringEncode(encryptedElementId));
        }

        private static string[] BuildPasswordPolicyContextTerms(string userName, string email, string employeeCode)
        {
            return new[]
            {
                userName ?? string.Empty,
                email ?? string.Empty,
                employeeCode ?? string.Empty
            };
        }
    }
}
