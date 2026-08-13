using System;
using System.Web;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>公开自主注册页面的 Web Forms code-behind。</zh-CN>
    ///   <en>Web Forms code-behind for the public self-registration page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>只有 <see cref="PortalRegistrationOptions.AllowSelfRegistration"/> 为 <c>true</c> 时才可访问。 当前空邀请码允许非邀请注册；带邀请码时校验启用状态、UTC 到期时间与使用次数。是否要求邀请码是后续独立设置， 不能在此页面隐式改变。需要审核的注册不会自动登录。</zh-CN>
    ///   <en>Access is allowed only when <see cref="PortalRegistrationOptions.AllowSelfRegistration"/> is <c>true</c>. Empty invite codes currently allow non-invite registration; supplied invite codes validate enabled state, UTC expiration, and usage count. Requiring invite codes is a later independent setting and must not be changed implicitly here. Registrations requiring approval do not sign in automatically.</en>
    /// </lang>
    /// </remarks>
    public partial class Register : PortalPage<Register>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用户与注册审核数据访问依赖。</zh-CN>
        ///   <en>User and registration-review data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>页面加载时验证自主注册开关并配置邀请码相关的员工号校验。</zh-CN>
        ///   <en>Validates the self-registration switch on page load and configures employee-code validation for invite registration.</en>
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
            // <lang>
            //   <zh-CN>页面加载先执行注册开关门禁，再按当前邀请码配置控件验证；这里不创建用户或消费密码。</zh-CN>
            //   <en>Gate page loading with the registration switch first, then configure controls from the current invite code; no user is created and no password is consumed here.</en>
            // </lang>
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            // <lang>
            //   <zh-CN>集中配置员工号和密码提交控件，使首次加载与回发使用同一设置语义。</zh-CN>
            //   <en>Configure employee-code and password-submission controls in one place so initial load and postback share the same setting semantics.</en>
            // </lang>
            ConfigureRegistrationForm();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理自主注册提交，创建用户、记录注册审计，并按审核开关决定是否直接登录。</zh-CN>
        ///   <en>Handles self-registration submission, creates the user, records registration audit data, and decides immediate sign-in from the approval switch.</en>
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
        protected void RegisterBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>每次提交重查开关，避免页面初次加载后部署设置发生变化时绕过限制。</zh-CN>
            //   <en>Recheck the switch on every submit so deployment-setting changes after initial page load cannot bypass the restriction.</en>
            // </lang>
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            // <lang>
            //   <zh-CN>只在 Web Forms 基础验证通过后继续创建用户。</zh-CN>
            //   <en>Continue user creation only after base Web Forms validation succeeds.</en>
            // </lang>
            if (Page.IsValid)
            {
                // <lang>
                //   <zh-CN>以下值只在当前请求内使用；用户名、邮箱和员工号去除首尾空白，邀请码由受控属性统一解析。</zh-CN>
                //   <en>Use these values only for the current request; trim name, email, and employee code while resolving the invite code through the controlled property.</en>
                // </lang>
                var userName = Name.Text.Trim();
                var email = Email.Text.Trim();
                string employeeCode = EmployeeCode.Text.Trim();
                string inviteCode = CurrentInviteCode;

                // <lang>
                //   <zh-CN>密码、邀请码和策略消息分别由后续门禁填充，避免未完成分支携带未初始化敏感状态。</zh-CN>
                //   <en>Populate password, invite, and policy messages through their later gates so unfinished branches do not carry uninitialized sensitive state.</en>
                // </lang>
                string submittedPassword;
                string submittedConfirmPassword;
                string inviteMessage;
                string passwordPolicyMessage;

                if (!TryResolveRegistrationPasswords(out submittedPassword, out submittedConfirmPassword))
                {
                    // <lang>
                    //   <zh-CN>加密提交失败时 helper 已写入低敏提示；清理所有密码控件后结束本次请求。</zh-CN>
                    //   <en>When encrypted submission fails, the helper has set a low-sensitivity message; clear every password control and end this request.</en>
                    // </lang>
                    ClearSubmittedPasswordFields();
                    return;
                }

                // <lang>
                //   <zh-CN>在任何后续校验、数据库调用或异常诊断前清除控件中的明文/密文残留。</zh-CN>
                //   <en>Clear clear-text and ciphertext control residue before any later validation, database call, or exception diagnostics.</en>
                // </lang>
                ClearSubmittedPasswordFields();

                if (string.IsNullOrEmpty(submittedPassword) || string.IsNullOrEmpty(submittedConfirmPassword))
                {
                    // <lang>
                    //   <zh-CN>空密码不进入邀请码、策略或用户创建流程。</zh-CN>
                    //   <en>Do not enter invite, policy, or user-creation flows with an empty password.</en>
                    // </lang>
                    Message.Text = "'Password' and 'Confirm Password' must not be left blank.";
                    return;
                }

                if (!string.Equals(submittedPassword, submittedConfirmPassword, StringComparison.Ordinal))
                {
                    // <lang>
                    //   <zh-CN>确认值比较只在当前进程内完成，失败提示不包含任一密码内容。</zh-CN>
                    //   <en>Compare the confirmation value only in process and never include either password in the failure message.</en>
                    // </lang>
                    Message.Text = "Password fields do not match.";
                    return;
                }

                if (!UsersDB.ValidateRegistrationInvite(inviteCode, out inviteMessage))
                {
                    // <lang>
                    //   <zh-CN>邀请码服务负责启用、到期和次数门禁；页面只显示契约返回的低敏消息。</zh-CN>
                    //   <en>The invite service owns enabled, expiry, and usage gates; the page displays only its contract-level low-sensitivity message.</en>
                    // </lang>
                    Message.Text = inviteMessage;
                    return;
                }

                if (!PortalPasswordPolicy.TryValidate(
                    submittedPassword,
                    BuildPasswordPolicyContextTerms(userName, email, employeeCode),
                    out passwordPolicyMessage))
                {
                    // <lang>
                    //   <zh-CN>密码策略失败时只返回策略消息，不记录密码或完整上下文词项。</zh-CN>
                    //   <en>On password-policy failure, return only the policy message and never log the password or full context terms.</en>
                    // </lang>
                    Message.Text = passwordPolicyMessage;
                    return;
                }

                if (PortalRegistrationOptions.IsEmployeeCodeRequired(inviteCode) &&
                    string.IsNullOrWhiteSpace(employeeCode) &&
                    !PortalRegistrationOptions.AllowPendingEmployeeBinding)
                {
                    // <lang>
                    //   <zh-CN>邀请注册的员工号门禁在创建用户前执行，待绑定兼容开关决定是否允许空值。</zh-CN>
                    //   <en>Apply the invited-registration employee-code gate before user creation; the pending-binding switch decides whether blank is allowed.</en>
                    // </lang>
                    Message.Text = "Employee Code is required for invitation registration.";
                    return;
                }

                // <lang>
                //   <zh-CN>数据层返回的用户标识决定注册事务是否成功；页面不自行推断或生成标识。</zh-CN>
                //   <en>The data layer's user id determines whether registration succeeded; the page does not infer or generate the id.</en>
                // </lang>
                int userId;
                try
                {
                    // <lang>
                    //   <zh-CN>只传递一次性密码输入；数据层负责强哈希写入，异常与审计信息不得包含密码或邀请码原文。</zh-CN>
                    //   <en>Pass only one-time password input; the data layer owns strong-hash writes, and exceptions/audits must not contain passwords or raw invite codes.</en>
                    // </lang>
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
                    // <lang>
                    //   <zh-CN>诊断只保留用户名/邮箱等受限标识和事件编号；异常处理绝不拼接密码、邀请码或密文。</zh-CN>
                    //   <en>Diagnostics retain only bounded identifiers such as name and email plus an event id; exception handling never concatenates passwords, invite codes, or ciphertext.</en>
                    // </lang>
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
                    // <lang>
                    //   <zh-CN>只有数据层返回非负用户标识才进入审计和审批/登录分支。</zh-CN>
                    //   <en>Enter audit and approval/sign-in branches only when data access returns a non-negative user id.</en>
                    // </lang>
                    // <lang>
                    //   <zh-CN>记录注册状态变化；审计不可用不阻断已成功的注册事务。</zh-CN>
                    //   <en>Record the registration state change; unavailable auditing does not block a successful registration transaction.</en>
                    // </lang>
                    PortalOperationAudit.Record(
                        PortalOperationAuditEvents.UserLifecycleCategory,
                        PortalOperationAuditEvents.RegistrationSubmitted,
                        PortalOperationAuditEvents.UserTargetType,
                        userId.ToString(),
                        "Self-registration submitted.",
                        Context);

                    if (PortalRegistrationOptions.RequireRegistrationApproval)
                    {
                        // <lang>
                        //   <zh-CN>待审核用户不签发认证票据，管理员批准后才满足登录条件。</zh-CN>
                        //   <en>Pending users receive no authentication ticket and meet sign-in conditions only after administrator approval.</en>
                        // </lang>
                        RegisterBtn.Visible = false;
                        Message.CssClass = "Normal";
                        Message.Text = "Registration submitted. Please wait for administrator approval.";
                        return;
                    }

                    // <lang>
                    //   <zh-CN>无需审核时沿用直接登录兼容行为；安全版本从数据层读取并写入认证票据。</zh-CN>
                    //   <en>When approval is not required, preserve direct sign-in compatibility; obtain the security version from data access and put it in the auth ticket.</en>
                    // </lang>
                    // <lang>
                    //   <zh-CN>关闭审核时保持既有直接登录行为，但身份票据需带当前安全版本。</zh-CN>
                    //   <en>Preserve legacy immediate sign-in behavior when approval is disabled, while carrying the current security version in the identity ticket.</en>
                    // </lang>
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
                    // <lang>
                    //   <zh-CN>负用户标识只产生泛化错误，不暴露重复键、邀请码或 schema 事实。</zh-CN>
                    //   <en>A negative user id produces only a generic error and exposes neither duplicate-key, invite, nor schema facts.</en>
                    // </lang>
                    // <lang>
                    //   <zh-CN>保持对外提示泛化，避免暴露数据库或邀请码校验细节。</zh-CN>
                    //   <en>Keep the user-facing message generic and avoid exposing database or invite-validation details.</en>
                    // </lang>
                    Message.Text = "Registration failed. The user name or email may already exist, or registration metadata is not available.";
                }
            }
        }

        private string CurrentInviteCode
        {
            get
            {
                // <lang>
                //   <zh-CN>空值代表当前允许的非邀请注册，不在这里强制改写为拒绝。</zh-CN>
                //   <en>An empty value represents currently allowed non-invite registration and is not forced to rejection here.</en>
                // </lang>
                // <lang>
                //   <zh-CN>邀请码只从查询字符串读取并在当前请求内裁剪；不写入 ViewState、日志或诊断事件。</zh-CN>
                //   <en>Read the invite code only from the query string and trim it for this request; do not write it to ViewState, logs, or diagnostics.</en>
                // </lang>
                string inviteCode = Request.QueryString["invite"];
                return string.IsNullOrWhiteSpace(inviteCode) ? string.Empty : inviteCode.Trim();
            }
        }

        private void ConfigureRegistrationForm()
        {
            // <lang>
            //   <zh-CN>只有邀请注册且未允许待绑定员工号时，员工号成为本页必填项。</zh-CN>
            //   <en>Employee code becomes required only for invite registration when pending employee binding is not allowed.</en>
            // </lang>
            // <lang>
            //   <zh-CN>把邀请码状态转换为一个布尔门禁，随后同时控制 validator 和提示文本，避免两处状态漂移。</zh-CN>
            //   <en>Convert invite state into one Boolean gate and use it for both the validator and hint so the two controls cannot drift.</en>
            // </lang>
            bool employeeCodeRequired = PortalRegistrationOptions.IsEmployeeCodeRequired(CurrentInviteCode) &&
                                        !PortalRegistrationOptions.AllowPendingEmployeeBinding;

            // <lang>
            //   <zh-CN>控件层只反映门禁，不替代提交回调中的服务端再次校验。</zh-CN>
            //   <en>The controls reflect the gate but never replace the server-side recheck performed by the submit callback.</en>
            // </lang>
            EmployeeCodeRequiredValidator.Enabled = employeeCodeRequired;
            EmployeeCodeRequiredHint.Visible = employeeCodeRequired;
            ConfigurePasswordSubmission();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按加密开关配置注册页密码提交脚本和旧验证器。</zh-CN>
        ///   <en>Configures registration password-submission scripts and legacy validators from the encryption switch.</en>
        /// </lang>
        /// </summary>
        private void ConfigurePasswordSubmission()
        {
            // <lang>
            //   <zh-CN>开关决定是否启用旧浏览器 validator 和客户端加密链；服务端回调仍以同一设置为最终事实。</zh-CN>
            //   <en>The switch controls legacy validators and the client-encryption chain; the server callback still treats the same setting as final.</en>
            // </lang>
            bool encryptedSubmissionRequired = PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired();
            RequiredFieldValidator3.Enabled = !encryptedSubmissionRequired;
            RequiredFieldValidator4.Enabled = !encryptedSubmissionRequired;
            CompareValidator1.Enabled = !encryptedSubmissionRequired;

            if (!encryptedSubmissionRequired)
            {
                // <lang>
                //   <zh-CN>未要求加密时移除客户端 onclick，让旧明文兼容路径保持原有 Web Forms 提交。</zh-CN>
                //   <en>When encryption is not required, remove the client onclick so the legacy plain Web Forms submission path remains intact.</en>
                // </lang>
                RegisterBtn.OnClientClick = string.Empty;
                return;
            }

            // <lang>
            //   <zh-CN>加密路径只注册既有脚本资源；脚本地址由站点解析，不能从请求参数拼接。</zh-CN>
            //   <en>The encrypted path registers only existing script resources; URLs are site-resolved and never composed from request parameters.</en>
            // </lang>
            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Register),
                "JSEncryptIE6",
                ResolveUrl("~/Scripts/Security/jsencrypt-ie6.min.js"));

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Register),
                "PortalLoginPasswordEncryption",
                ResolveUrl("~/Scripts/Security/PortalLoginPasswordEncryption.js"));

            // <lang>
            //   <zh-CN>把字段描述、公钥端点和消息控件 ID 编码为 JavaScript 字符串，避免标记层注入。</zh-CN>
            //   <en>Encode field descriptors, the public-key endpoint, and the message-control id as JavaScript strings to protect the markup boundary.</en>
            // </lang>
            RegisterBtn.OnClientClick = string.Format(
                "return PortalLoginPasswordEncryption.encryptPasswordFields([{0},{1}],'{2}','{3}');",
                BuildPasswordFieldScriptObject(Password.ClientID, EncryptedPassword.ClientID),
                BuildPasswordFieldScriptObject(ConfirmPassword.ClientID, EncryptedConfirmPassword.ClientID),
                HttpUtility.JavaScriptStringEncode(ResolveUrl("~/Security/LoginPasswordKey.ashx")),
                HttpUtility.JavaScriptStringEncode(Message.ClientID));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析注册页提交的密码和确认密码，优先使用加密隐藏字段。</zh-CN>
        ///   <en>Resolves registration password and confirmation values, preferring encrypted hidden fields.</en>
        /// </lang>
        /// </summary>
        /// <param name="submittedPassword">
        /// <l>
        ///   <zh-CN>当前请求内使用的明文密码。</zh-CN>
        ///   <en>Plain password for this request.</en>
        /// </l>
        /// </param>
        /// <param name="submittedConfirmPassword">
        /// <l>
        ///   <zh-CN>当前请求内使用的确认密码。</zh-CN>
        ///   <en>Plain confirmation password for this request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>提交满足当前加密策略时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the submission satisfies the current encryption policy.</en>
        /// </l>
        /// </returns>
        private bool TryResolveRegistrationPasswords(
            out string submittedPassword,
            out string submittedConfirmPassword)
        {
            // <lang>
            //   <zh-CN>先以空字符串初始化输出，保证任一失败分支不会把未定义密码传给调用方。</zh-CN>
            //   <en>Initialize both outputs to empty so every failure path returns no undefined password to the caller.</en>
            // </lang>
            submittedPassword = string.Empty;
            submittedConfirmPassword = string.Empty;

            // <lang>
            //   <zh-CN>隐藏字段是否存在只表示客户端尝试走加密链，不代表密文已经完整或可信。</zh-CN>
            //   <en>Presence of hidden fields only indicates a client attempt at encryption; it does not prove ciphertext completeness or trustworthiness.</en>
            // </lang>
            bool hasEncryptedPassword = !string.IsNullOrWhiteSpace(EncryptedPassword.Value);
            bool hasEncryptedConfirmPassword = !string.IsNullOrWhiteSpace(EncryptedConfirmPassword.Value);

            if (hasEncryptedPassword || hasEncryptedConfirmPassword)
            {
                if (!hasEncryptedPassword || !hasEncryptedConfirmPassword)
                {
                    // <lang>
                    //   <zh-CN>只提交一个密文字段时拒绝整组请求，避免密码和确认值使用不同来源。</zh-CN>
                    //   <en>Reject the whole request when only one ciphertext field is present so password and confirmation cannot come from different sources.</en>
                    // </lang>
                    PortalDiagnostics.Warn(
                        "PasswordSubmissionEncryption",
                        "Registration password submission was incomplete: one encrypted password field was missing.",
                        Context);
                    Message.Text = "密码提交不完整，请刷新页面后重试。";
                    return false;
                }

                // <lang>
                //   <zh-CN>以下输出只在本次解密调用内保存；失败分类和事件编号不包含口令材料。</zh-CN>
                //   <en>Keep these outputs only for this decryption call; failure categories and event ids contain no password material.</en>
                // </lang>
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
                    // <lang>
                    //   <zh-CN>按位置提取密码与确认值；长度不足时回退为空文本，由调用方的空值门禁拒绝。</zh-CN>
                    //   <en>Extract password and confirmation by position; a short result falls back to empty text for the caller's blank-value gate.</en>
                    // </lang>
                    submittedPassword = submittedPasswords.Length > 0 ? submittedPasswords[0] : string.Empty;
                    submittedConfirmPassword = submittedPasswords.Length > 1 ? submittedPasswords[1] : string.Empty;
                    return true;
                }

                // <lang>
                //   <zh-CN>解密失败只显示刷新重试提示，绝不回显密文、异常或事件细节。</zh-CN>
                //   <en>On decryption failure, show only a refresh-and-retry message and never echo ciphertext, exceptions, or event details.</en>
                // </lang>
                Message.Text = "密码提交验证失败，请刷新页面后重试。";
                return false;
            }

            if (PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired())
            {
                // <lang>
                //   <zh-CN>服务端要求加密但客户端没有密文字段时拒绝明文回退，并记录不含敏感值的 warning。</zh-CN>
                //   <en>When the server requires encryption but no ciphertext fields arrived, reject plain-text fallback and record a warning without sensitive values.</en>
                // </lang>
                PortalDiagnostics.Warn(
                    "PasswordSubmissionEncryption",
                    "Registration password was submitted without the required encrypted fields.",
                    Context);
                Message.Text = "密码提交验证失败，请刷新页面后重试。";
                return false;
            }

            // <lang>
            //   <zh-CN>仅在明确允许明文兼容时读取 Password 控件；调用方会在后续流程结束后清除它们。</zh-CN>
            //   <en>Read the Password controls only when plain compatibility is explicitly allowed; the caller clears them before later processing ends.</en>
            // </lang>
            submittedPassword = Password.Text ?? string.Empty;
            submittedConfirmPassword = ConfirmPassword.Text ?? string.Empty;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>清空注册页密码字段，避免异常路径和回显残留。</zh-CN>
        ///   <en>Clears registration password fields to avoid exception-path and echo residue.</en>
        /// </lang>
        /// </summary>
        private void ClearSubmittedPasswordFields()
        {
            // <lang>
            //   <zh-CN>同时清除明文输入和隐藏密文，覆盖校验失败、解密失败和异常路径。</zh-CN>
            //   <en>Clear both plain inputs and hidden ciphertext across validation failures, decryption failures, and exception paths.</en>
            // </lang>
            Password.Text = string.Empty;
            ConfirmPassword.Text = string.Empty;
            EncryptedPassword.Value = string.Empty;
            EncryptedConfirmPassword.Value = string.Empty;
        }

        private static string BuildPasswordFieldScriptObject(string passwordElementId, string encryptedElementId)
        {
            // <lang>
            //   <zh-CN>只生成固定字段名的 JavaScript 对象，并对两个控件 ID 做脚本字符串编码。</zh-CN>
            //   <en>Generate only the fixed-field JavaScript object and encode both control ids as script strings.</en>
            // </lang>
            return string.Format(
                "{{passwordElementId:'{0}',encryptedElementId:'{1}'}}",
                HttpUtility.JavaScriptStringEncode(passwordElementId),
                HttpUtility.JavaScriptStringEncode(encryptedElementId));
        }

        private static string[] BuildPasswordPolicyContextTerms(string userName, string email, string employeeCode)
        {
            // <lang>
            //   <zh-CN>密码策略上下文只使用可能影响规则的非密码文本；返回数组不包含密码、邀请码或密文。</zh-CN>
            //   <en>Password-policy context contains only non-password text that may affect policy rules; the returned array contains no password, invite code, or ciphertext.</en>
            // </lang>
            return new[]
            {
                userName ?? string.Empty,
                email ?? string.Empty,
                employeeCode ?? string.Empty
            };
        }
    }
}
