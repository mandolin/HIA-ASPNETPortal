using System;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧后台用户资料、角色和注册审核管理页面。</zh-CN>
    ///   <en>Legacy administration page for user profiles, roles, and registration review.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页面要求 <c>Admins</c> 角色，并只编辑能由数值 userId 规范解析到的用户。用户创建由 <c>Users.ascx</c> 的显式管理员 POST 完成；本页不会因访问地址缺少用户名而写入数据库。角色、资料、注册审核和状态写入与运营审计由连续调用组成，不宣称跨服务原子事务；角色调整和密码重置会递增目标用户安全版本，使旧身份票据和角色 Cookie 在后续请求中失效。</zh-CN>
    ///   <en>The page requires the <c>Admins</c> role and edits only a user canonically resolved by numeric userId. User creation occurs through the explicit administrator POST in <c>Users.ascx</c>; this page never writes to the database merely because an address lacks a user name. Role, profile, registration-review, and status writes are sequential calls with operations audit rather than a claimed cross-service atomic transaction; role changes and password resets increment the target user's security version so older authentication and role cookies are invalidated on later requests.</en>
    /// </lang>
    /// </remarks>
    public partial class ManageUsers : PortalPage<ManageUsers>
    {
        private int tabId;
        private int tabIndex;
        private int userId;
        private IUserItem currentUser;

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户和注册审核数据访问依赖。</zh-CN>
        ///   <en>User and registration-review data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>角色和成员关系数据访问依赖。</zh-CN>
        ///   <en>Role and membership data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工目录只读数据访问依赖，用于展示当前账号员工绑定。</zh-CN>
        ///   <en>Employee-directory read dependency used to display the current user-employee binding.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权并解析规范用户目标；首次请求绑定用户、角色和审核信息。</zh-CN>
        ///   <en>Authorizes and resolves the canonical user target, then binds user, role, and review information on the initial request.</en>
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
        ///   <zh-CN>初始化先解析并授权目标用户，再按角色编辑权限配置密码提交脚本；只有首次请求才绑定页面数据，回发动作仍由各自 handler 再次复核权限。</zh-CN>
        ///   <en>Initialization resolves and authorizes the target user before configuring password-submission scripts for role editing; data binds only on the initial request, while each postback handler rechecks its own permission.</en>
        /// </lang>
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit))
            {
                return;
            }

            ConfigurePasswordSubmission();

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理仅返回门户页的保存按钮事件。</zh-CN>
        ///   <en>Handles the save-button event that only returns to the Portal page.</en>
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
        ///   <zh-CN>该动作只安全返回门户页，不保存用户资料；目标和回跳参数仍由统一初始化与安全 URL 策略约束。</zh-CN>
        ///   <en>This action only returns safely to the Portal and does not save user data; the target and return parameters remain constrained by shared initialization and safe-URL policy.</en>
        /// </lang>
        /// </remarks>
        protected void Save_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将当前用户加入所选的当前门户角色，并记录不含角色名称的运营审计。</zh-CN>
        ///   <en>Adds the current user to a selected role of the current Portal and records an operations audit without the role name.</en>
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
        ///   <zh-CN>所选角色必须来自当前门户角色集合；角色写入成功后才记录运营审计，二者不共享本页声明的跨服务事务。</zh-CN>
        ///   <en>The selected role must come from the current Portal role set; operations audit is recorded only after the role write succeeds, and the two calls do not share a cross-service transaction claimed by this page.</en>
        /// </lang>
        /// </remarks>
        protected void AddRole_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            IRoleItem role;
            if (!TryGetSelectedPortalRole(out role))
            {
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>角色成员写入先于运营审计；审计失败不会把已完成的角色关系写入误报为已回滚。</zh-CN>
                //   <en>Role membership persistence precedes operations audit; an audit failure must not be read as rollback of an already completed role change.</en>
                // </lang>
                RolesDB.AddUserRole(role.RoleId, userId);
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.UserAdministrationCategory,
                    PortalOperationAuditEvents.RoleAdded,
                    PortalOperationAuditEvents.UserTargetType,
                    userId.ToString(),
                    "Added role id " + role.RoleId + " to user.",
                    Context);
                ShowRegistrationMessage("角色已加入当前用户。", false);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.AddRole",
                    "Adding a role to user failed. UserId=" + userId + "; RoleId=" + role.RoleId,
                    exception,
                    Context);
                ShowRegistrationMessage("加入角色失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新当前用户的资料扩展，并在填写密码时重置强哈希凭据；审计不记录密码或资料原文。</zh-CN>
        ///   <en>Updates the current user's profile extension and resets the strong-hash credential when a password is entered; audit entries do not record passwords or raw profile values.</en>
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
        ///   <zh-CN>资料字段先按长度和单行规则归一化，再按当前加密策略解析密码并复核重置权限；资料写入、资料审计和密码审计是顺序步骤，不把异常提示解释为自动回滚。</zh-CN>
        ///   <en>Profile fields are normalized for length and single-line form before password values are resolved under the current encryption policy and reset permission is rechecked; profile write, profile audit, and password audit are sequential steps, and an exception message does not imply automatic rollback.</en>
        /// </lang>
        /// </remarks>
        protected void UpdateUser_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit))
            {
                return;
            }

            string email;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(Email.Text, 256, out email))
            {
                ShowRegistrationMessage("邮箱格式无效，未保存本次修改。", true);
                return;
            }

            string loginName;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(LoginName.Text, 100, out loginName))
            {
                ShowRegistrationMessage("登录名格式无效，未保存本次修改。", true);
                return;
            }

            string displayName;
            if (!PortalAdministrationPolicy.TryNormalizeOptionalSingleLineText(DisplayName.Text, 150, out displayName))
            {
                ShowRegistrationMessage("显示名格式无效，未保存本次修改。", true);
                return;
            }

            string nickname;
            if (!PortalAdministrationPolicy.TryNormalizeOptionalSingleLineText(Nickname.Text, 100, out nickname))
            {
                ShowRegistrationMessage("昵称格式无效，未保存本次修改。", true);
                return;
            }

            bool passwordResetSubmitted = HasPasswordResetSubmission();
            if (passwordResetSubmitted && !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersResetPassword))
            {
                return;
            }

            string password;
            string confirmPassword;
            bool shouldResetPassword;
            if (!TryResolvePasswordResetSubmission(
                passwordResetSubmitted,
                out password,
                out confirmPassword,
                out shouldResetPassword))
            {
                ClearSubmittedPasswordFields();
                return;
            }

            ClearSubmittedPasswordFields();

            if (shouldResetPassword && !string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                ShowRegistrationMessage("两次输入的密码不一致，未保存本次修改。", true);
                return;
            }

            string passwordPolicyMessage;
            if (shouldResetPassword && !PortalPasswordPolicy.TryValidate(
                password,
                BuildPasswordPolicyContextTerms(loginName, displayName, nickname, email),
                out passwordPolicyMessage))
            {
                ShowRegistrationMessage(passwordPolicyMessage, true);
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>数据服务负责资料和凭据持久化；本页随后分别记录资料/密码审计，不在审计调用中传递密码或资料原文。</zh-CN>
                //   <en>The data service owns profile and credential persistence; this page records separate profile/password audits afterward without passing passwords or raw profile values.</en>
                // </lang>
                IUserProfileInfo profileBefore = UsersDB.GetUserProfileInfo(userId);
                UsersDB.UpdateUserProfile(
                    userId,
                    loginName,
                    displayName,
                    nickname,
                    email,
                    shouldResetPassword ? password : string.Empty,
                    GetCurrentActor());
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.UserLifecycleCategory,
                    PortalOperationAuditEvents.ProfileUpdated,
                    PortalOperationAuditEvents.UserTargetType,
                    userId.ToString(),
                    BuildProfileAuditSummary(profileBefore, loginName, displayName, nickname, email),
                    Context);
                if (shouldResetPassword)
                {
                    PortalOperationAudit.Record(
                        PortalOperationAuditEvents.SecurityCredentialsCategory,
                        PortalOperationAuditEvents.PasswordReset,
                        PortalOperationAuditEvents.UserTargetType,
                        userId.ToString(),
                        "User credential reset by administrator.",
                        Context);
                }

                RedirectToCurrentUser();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.UpdateUser",
                    "Updating user profile failed. UserId=" + userId,
                    exception,
                    Context);
                ShowRegistrationMessage("资料更新失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按加密开关配置管理员重置密码提交脚本。</zh-CN>
        ///   <en>Configures administrator password-reset submission scripts from the encryption switch.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>加密开关只决定客户端脚本和服务端提交载体；脚本 URL 经过现有 JavaScript 编码，服务端仍必须在回发中验证密文完整性，不能把前端加密当作授权。</zh-CN>
        ///   <en>The encryption switch controls only the client script and server submission carrier; script URLs use the existing JavaScript encoding, and the server must still validate encrypted fields on postback, so client encryption is not authorization.</en>
        /// </lang>
        /// </remarks>
        private void ConfigurePasswordSubmission()
        {
            if (!PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired())
            {
                UpdateUserBtn.OnClientClick = string.Empty;
                return;
            }

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(ManageUsers),
                "JSEncryptIE6",
                ResolveUrl("~/Scripts/Security/jsencrypt-ie6.min.js"));

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(ManageUsers),
                "PortalLoginPasswordEncryption",
                ResolveUrl("~/Scripts/Security/PortalLoginPasswordEncryption.js"));

            UpdateUserBtn.OnClientClick = string.Format(
                "return PortalLoginPasswordEncryption.encryptPasswordFields([{0},{1}],'{2}','{3}');",
                BuildPasswordFieldScriptObject(Password.ClientID, EncryptedPassword.ClientID),
                BuildPasswordFieldScriptObject(ConfirmPassword.ClientID, EncryptedConfirmPassword.ClientID),
                HttpUtility.JavaScriptStringEncode(ResolveUrl("~/Security/LoginPasswordKey.ashx")),
                HttpUtility.JavaScriptStringEncode(RegistrationMessage.ClientID));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断本次后台用户资料提交是否包含重置密码意图。</zh-CN>
        ///   <en>Determines whether this user-profile submission intends to reset the password.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>任一明文或密文字段非空时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when any plain or encrypted field is present.</en>
        /// </l>
        /// </returns>
        private bool HasPasswordResetSubmission()
        {
            return !string.IsNullOrEmpty(Password.Text) ||
                   !string.IsNullOrEmpty(ConfirmPassword.Text) ||
                   !string.IsNullOrWhiteSpace(EncryptedPassword.Value) ||
                   !string.IsNullOrWhiteSpace(EncryptedConfirmPassword.Value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析管理员重置密码提交，保持“两框任一填写即重置”的旧语义。</zh-CN>
        ///   <en>Resolves administrator password-reset submission while preserving the legacy "either field means reset" semantics.</en>
        /// </lang>
        /// </summary>
        /// <param name="passwordResetSubmitted">
        /// <l>
        ///   <zh-CN>是否存在密码重置提交意图。</zh-CN>
        ///   <en>Whether a password-reset submission was detected.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>当前请求内使用的密码。</zh-CN>
        ///   <en>Password value for this request.</en>
        /// </l>
        /// </param>
        /// <param name="confirmPassword">
        /// <l>
        ///   <zh-CN>当前请求内使用的确认密码。</zh-CN>
        ///   <en>Confirmation password value for this request.</en>
        /// </l>
        /// </param>
        /// <param name="shouldResetPassword">
        /// <l>
        ///   <zh-CN>解密或读取后是否应重置密码。</zh-CN>
        ///   <en>Whether the password should be reset after decrypting or reading values.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>提交满足当前加密策略时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the submission satisfies the current encryption policy.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>有密文时要求两个字段成对存在并通过解密；仅在策略关闭时才回退读取明文控件，失败会记录受控诊断并阻止保存。</zh-CN>
        ///   <en>When encrypted values are present, both fields must be supplied and decrypted successfully; plain controls are read only when policy disables encryption, while failures record controlled diagnostics and stop saving.</en>
        /// </lang>
        /// </remarks>
        private bool TryResolvePasswordResetSubmission(
            bool passwordResetSubmitted,
            out string password,
            out string confirmPassword,
            out bool shouldResetPassword)
        {
            password = string.Empty;
            confirmPassword = string.Empty;
            shouldResetPassword = false;

            if (!passwordResetSubmitted)
            {
                return true;
            }

            bool hasEncryptedPassword = !string.IsNullOrWhiteSpace(EncryptedPassword.Value);
            bool hasEncryptedConfirmPassword = !string.IsNullOrWhiteSpace(EncryptedConfirmPassword.Value);

            if (hasEncryptedPassword || hasEncryptedConfirmPassword)
            {
                if (!hasEncryptedPassword || !hasEncryptedConfirmPassword)
                {
                    PortalDiagnostics.Warn(
                        "PasswordSubmissionEncryption",
                        "Administrator password-reset submission was incomplete: one encrypted password field was missing.",
                        Context);
                    ShowRegistrationMessage("密码提交不完整，未保存本次修改。", true);
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
                    password = submittedPasswords.Length > 0 ? submittedPasswords[0] : string.Empty;
                    confirmPassword = submittedPasswords.Length > 1 ? submittedPasswords[1] : string.Empty;
                    shouldResetPassword = !string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmPassword);
                    return true;
                }

                ShowRegistrationMessage("密码提交验证失败，系统已记录本次错误。事件编号：" + eventId, true);
                return false;
            }

            if (PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired())
            {
                PortalDiagnostics.Warn(
                    "PasswordSubmissionEncryption",
                    "Administrator password reset was submitted without the required encrypted fields.",
                    Context);
                ShowRegistrationMessage("密码提交验证失败，未保存本次修改。", true);
                return false;
            }

            password = Password.Text ?? string.Empty;
            confirmPassword = ConfirmPassword.Text ?? string.Empty;
            shouldResetPassword = !string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmPassword);
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>批准当前用户的注册申请，并记录注册审核操作。</zh-CN>
        ///   <en>Approves the current user's registration and records the registration-review operation.</en>
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
        ///   <zh-CN>批准动作重新复核查看和编辑权限，以规范目标用户为对象；注册状态写入成功后再记录审计，异常不表示两步已被事务回滚。</zh-CN>
        ///   <en>The approval action rechecks view and edit permission against the canonical target user; audit follows a successful registration-state write, and an exception does not mean the two steps were transactionally rolled back.</en>
        /// </lang>
        /// </remarks>
        protected void ApproveRegistration_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit))
            {
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>批准状态写入成功后才记录审核审计；两次调用没有本页声明的跨服务事务。</zh-CN>
                //   <en>Registration approval audit follows a successful state write; the two calls have no cross-service transaction claimed by this page.</en>
                // </lang>
                UsersDB.ApproveUser(userId, GetCurrentActor());
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.UserLifecycleCategory,
                    PortalOperationAuditEvents.RegistrationApproved,
                    PortalOperationAuditEvents.UserTargetType,
                    userId.ToString(),
                    "Registration approved.",
                    Context);
                ShowRegistrationMessage("Registration approved.", false);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.ApproveRegistration",
                    "Approving user registration failed. UserId=" + userId,
                    exception,
                    Context);
                ShowRegistrationMessage("审核失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>拒绝当前待审核用户的注册申请，并记录注册审核操作。</zh-CN>
        ///   <en>Rejects the current pending registration and records the registration-review operation.</en>
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
        ///   <zh-CN>拒绝动作只处理当前待审核注册；状态写入和运营审计为连续调用，失败时保留受控事件编号而不伪造成功事实。</zh-CN>
        ///   <en>The rejection action applies only to the current pending registration; state write and operations audit are sequential calls, and failure keeps a controlled event id without fabricating success.</en>
        /// </lang>
        /// </remarks>
        protected void RejectRegistration_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit))
            {
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>拒绝状态写入成功后才记录审核审计；后续异常不代表拒绝事实已自动回滚。</zh-CN>
                //   <en>Registration rejection audit follows a successful state write; a later exception does not mean the rejection fact was automatically rolled back.</en>
                // </lang>
                UsersDB.RejectUser(userId, GetCurrentActor());
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.UserLifecycleCategory,
                    PortalOperationAuditEvents.RegistrationRejected,
                    PortalOperationAuditEvents.UserTargetType,
                    userId.ToString(),
                    "Registration rejected.",
                    Context);
                ShowRegistrationMessage("Registration rejected.", false);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.RejectRegistration",
                    "Rejecting user registration failed. UserId=" + userId,
                    exception,
                    Context);
                ShowRegistrationMessage("拒绝审核失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>移除当前用户的一个当前门户角色，并记录角色成员关系审计。</zh-CN>
        ///   <en>Removes one current-Portal role from the current user and records a role-membership audit.</en>
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
        ///   <zh-CN>包含命令和 DataList 项索引的事件数据。</zh-CN>
        ///   <en>Event data containing the command and DataList item index.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>仅接受 delete 命令，并从当前门户角色集合重新解析 DataKey；角色移除成功后才审计和刷新列表，控件索引不是授权凭据。</zh-CN>
        ///   <en>Only the delete command is accepted, and the DataKey is resolved again against current Portal roles; audit and list refresh follow a successful removal, while the control index is not an authorization credential.</en>
        /// </lang>
        /// </remarks>
        protected void UserRoles_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) ||
                !string.Equals(e.CommandName, "delete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int roleId;
            if (e.Item == null || e.Item.ItemIndex < 0 || e.Item.ItemIndex >= userRoles.DataKeys.Count ||
                !int.TryParse(userRoles.DataKeys[e.Item.ItemIndex].ToString(), out roleId) || roleId < 0)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            IRoleItem role = FindCurrentPortalRole(roleId);
            if (role == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            // <lang>
            //   <zh-CN>角色移除已通过当前门户角色复核；删除和审计仍是连续调用，DataList 行索引不作为授权依据。</zh-CN>
            //   <en>The role removal has been revalidated against current Portal roles; deletion and audit remain sequential calls, and the DataList row index is not an authorization basis.</en>
            // </lang>
            RolesDB.DeleteUserRole(role.RoleId, userId);
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.UserAdministrationCategory,
                PortalOperationAuditEvents.RoleRemoved,
                PortalOperationAuditEvents.UserTargetType,
                userId.ToString(),
                "Removed role id " + role.RoleId + " from user.",
                Context);
            userRoles.EditItemIndex = -1;
            BindData();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>禁用当前目标用户，并通过安全版本让既有会话在后续请求中失效。</zh-CN>
        ///   <en>Disables the current target user and invalidates existing sessions on later requests through the security version.</en>
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
        protected void DisableUser_Click(object sender, EventArgs e)
        {
            ChangeUserProfileStatus(
                PortalUserProfileStatuses.Disabled,
                PortalOperationAuditEvents.UserDisabled,
                "User account disabled by administrator.",
                "账号已禁用。",
                "禁用账号失败，系统已记录本次错误。事件编号：");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>恢复启用当前目标用户；被拒绝注册的用户仍应通过批准动作恢复。</zh-CN>
        ///   <en>Restores the current target user; rejected registrations should still be restored through the approval action.</en>
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
        protected void RestoreUser_Click(object sender, EventArgs e)
        {
            ChangeUserProfileStatus(
                PortalUserProfileStatuses.Active,
                PortalOperationAuditEvents.UserRestored,
                "User account restored by administrator.",
                "账号已恢复启用。",
                "恢复启用失败，系统已记录本次错误。事件编号：");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>复核后台查看权限并解析规范用户目标及可选门户返回参数。</zh-CN>
        ///   <en>Rechecks administration view permission and resolves the canonical user target and optional Portal return parameters.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>userId 必须为正数且必须能读取到用户；可选 username 只作一致性校验，缺失或不一致均进入受控拒绝路径。</zh-CN>
        ///   <en>userId must be positive and resolve to a user; optional username is only a consistency check, and missing or mismatched targets enter the controlled denial path.</en>
        /// </lang>
        /// </remarks>
        private bool TryInitializeRequest()
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersView))
            {
                return false;
            }

            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["userid"], out userId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            if (!TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                return false;
            }

            currentUser = UsersDB.FindUserById(userId);
            if (currentUser == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            string suppliedUserName = Request.Params["username"];
            if (!string.IsNullOrWhiteSpace(suppliedUserName) &&
                !string.Equals(suppliedUserName, currentUser.Name, StringComparison.Ordinal))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选正数页面参数。</zh-CN>
        ///   <en>Reads an optional positive page parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>解析出的正数，缺失时为零。</zh-CN>
        ///   <en>Parsed positive value, or zero when absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>缺失或合法正数时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when absent or a valid positive number is supplied.</en>
        /// </l>
        /// </returns>
        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选非负页面参数。</zh-CN>
        ///   <en>Reads an optional non-negative page parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>解析出的非负数，缺失时为零。</zh-CN>
        ///   <en>Parsed non-negative value, or zero when absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>缺失或合法非负数时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when absent or a valid non-negative number is supplied.</en>
        /// </l>
        /// </returns>
        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析当前门户角色选择并拒绝不属于当前门户的标识。</zh-CN>
        ///   <en>Resolves the selected current-Portal role and rejects identifiers outside the current Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="role">
        /// <l>
        ///   <zh-CN>解析出的角色。</zh-CN>
        ///   <en>Resolved role.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>选择有效且属于当前门户的角色时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a valid role belonging to the current Portal is selected.</en>
        /// </l>
        /// </returns>
        private bool TryGetSelectedPortalRole(out IRoleItem role)
        {
            role = null;
            if (allRoles.SelectedItem == null)
            {
                ShowRegistrationMessage("请选择一个有效角色。", true);
                return false;
            }

            int roleId;
            if (!PortalNavigationPolicy.TryReadNonNegativeInt32(allRoles.SelectedItem.Value, out roleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            role = FindCurrentPortalRole(roleId);
            if (role != null)
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前门户标识查找角色。</zh-CN>
        ///   <en>Finds a role by the current Portal identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前门户角色，找不到时为 <c>null</c>。</zh-CN>
        ///   <en>Current-Portal role, or <c>null</c> when not found.</en>
        /// </l>
        /// </returns>
        private IRoleItem FindCurrentPortalRole(int roleId)
        {
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            return RolesDB.GetPortalRoles(portalSettings.PortalId)
                .FirstOrDefault(item => item.RoleId == roleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户的资料、注册、员工绑定和角色投影并绑定页面控件。</zh-CN>
        ///   <en>Reads the current user's profile, registration, employee-binding, and role projections and binds the page controls.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法只构建页面投影和按钮可见性，不授予权限、不改变业务状态；展示文本统一经过既有 HTML 编码路径。</zh-CN>
        ///   <en>This method builds page projections and button visibility only; it grants no permission and changes no business state, while display text uses the existing HTML-encoding path.</en>
        /// </lang>
        /// </remarks>
        private void BindData()
        {
            if (currentUser == null)
            {
                return;
            }

            IUserProfileInfo profile = UsersDB.GetUserProfileInfo(currentUser.UserId);
            Email.Text = profile == null || string.IsNullOrWhiteSpace(profile.PreferredEmail)
                ? currentUser.Email
                : profile.PreferredEmail;
            LegacyUserNameText.Text = EncodeDisplay(currentUser.Name);
            LoginName.Text = profile == null ? currentUser.Name : profile.LoginName;
            DisplayName.Text = profile == null ? currentUser.Name : profile.DisplayName;
            Nickname.Text = profile == null ? string.Empty : profile.Nickname;
            ProfileStatusText.Text = EncodeDisplay(profile == null ? PortalUserProfileStatuses.Active : profile.Status);
            ProfileSourceText.Text = EncodeDisplay(profile == null ? "LegacyNoProfileInfo" : profile.Source);
            SetProfileInputsEnabled(profile == null || profile.IsAvailable);
            IUserRegistrationInfo registration = BindRegistrationInfo(currentUser.UserId);
            BindEmployeeBindingInfo(currentUser.UserId);
            BindProfileLifecycleActions(profile, registration);
            TitleText.Text = Server.HtmlEncode("Manage User: " + GetEffectiveDisplayName(profile, currentUser.Name));

            userRoles.DataSource = UsersDB.GetRolesByUser(currentUser.Name);
            userRoles.DataBind();

            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            allRoles.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            allRoles.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定当前用户的注册审核摘要和按钮可见性。</zh-CN>
        ///   <en>Binds registration-review summary and action visibility for the current user.</en>
        /// </lang>
        /// </summary>
        /// <param name="currentUserId">
        /// <l>
        ///   <zh-CN>规范用户标识。</zh-CN>
        ///   <en>Canonical user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>注册审核投影。</zh-CN>
        ///   <en>Registration-review projection.</en>
        /// </l>
        /// </returns>
        private IUserRegistrationInfo BindRegistrationInfo(int currentUserId)
        {
            IUserRegistrationInfo registration = UsersDB.GetRegistrationInfo(currentUserId);
            RegistrationStatus.Text = EncodeDisplay(registration.Status);
            RegistrationSource.Text = EncodeDisplay(registration.Source);
            EmployeeCodeText.Text = EncodeDisplay(EmptyToNone(registration.EmployeeCode));
            InviteCodeText.Text = EncodeDisplay(EmptyToNone(registration.InviteCode));
            RegisteredUtcText.Text = EncodeDisplay(FormatUtc(registration.RegisteredUtc));
            ApprovedUtcText.Text = EncodeDisplay(FormatUtc(registration.ApprovedUtc));
            ApproveRegistrationBtn.Visible =
                string.Equals(registration.Status, PortalUserRegistrationStatuses.PendingApproval, StringComparison.Ordinal) ||
                string.Equals(registration.Status, PortalUserRegistrationStatuses.Rejected, StringComparison.Ordinal);
            RejectRegistrationBtn.Visible =
                string.Equals(registration.Status, PortalUserRegistrationStatuses.PendingApproval, StringComparison.Ordinal);
            return registration;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定当前用户的员工账号摘要和维护页链接。</zh-CN>
        ///   <en>Binds the current user's employee-binding summary and maintenance-page link.</en>
        /// </lang>
        /// </summary>
        /// <param name="currentUserId">
        /// <l>
        ///   <zh-CN>规范用户标识。</zh-CN>
        ///   <en>Canonical user identifier.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>员工绑定读取只影响展示；schema 不可用时显示受控状态，不把缺失依赖解释为无绑定或授权通过。</zh-CN>
        ///   <en>Employee-binding reads affect display only; when the schema is unavailable, a controlled status is shown rather than treating a missing dependency as no binding or granted authorization.</en>
        /// </lang>
        /// </remarks>
        private void BindEmployeeBindingInfo(int currentUserId)
        {
            EmployeeBindingLink.NavigateUrl = ResolveUrl(
                "~/Admin/UserEmployeeBindingEdit.aspx?userId=" + currentUserId.ToString());
            EmployeeBindingLink.Visible = true;

            if (EmployeeDirectoryDb == null || !EmployeeDirectoryDb.IsSchemaAvailable())
            {
                EmployeeBindingText.Text = "P6.3 schema unavailable.";
                return;
            }

            IUserEmployeeBindingInfo binding = EmployeeDirectoryDb.GetActiveBindingByUserId(currentUserId);
            EmployeeBindingText.Text = binding == null
                ? "(none)"
                : EncodeDisplay(binding.EmployeeCode + " / " + binding.EmployeeDisplayName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据资料和注册状态设置禁用/恢复按钮可见性。</zh-CN>
        ///   <en>Sets disable/restore action visibility from profile and registration state.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>按钮可见性只是界面提示；真正状态动作仍在回发时重新授权并复核注册状态。</zh-CN>
        ///   <en>Button visibility is only a UI hint; the actual status action reauthorizes and rechecks registration state on postback.</en>
        /// </lang>
        /// </remarks>
        private void BindProfileLifecycleActions(IUserProfileInfo profile, IUserRegistrationInfo registration)
        {
            DisableUserBtn.Visible = false;
            RestoreUserBtn.Visible = false;
            if (profile == null || !profile.IsAvailable)
            {
                return;
            }

            string status = profile.Status ?? string.Empty;
            bool registrationRejected = registration != null &&
                                        string.Equals(registration.Status, PortalUserRegistrationStatuses.Rejected, StringComparison.Ordinal);
            DisableUserBtn.Visible =
                !IsCurrentTargetSelf() &&
                string.Equals(status, PortalUserProfileStatuses.Active, StringComparison.Ordinal);
            RestoreUserBtn.Visible =
                !registrationRejected &&
                string.Equals(status, PortalUserProfileStatuses.Disabled, StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按受控状态动作更新当前目标用户的资料状态并记录审计。</zh-CN>
        ///   <en>Updates the current target user's profile status through a controlled action and records audit.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>动作重新复核编辑权限，禁止当前操作者禁用自己，并阻止被拒绝注册绕过批准流程；状态写入与审计为连续步骤。</zh-CN>
        ///   <en>The action rechecks edit permission, prevents self-disable, and prevents rejected registrations from bypassing approval; status write and audit remain sequential steps.</en>
        /// </lang>
        /// </remarks>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>目标资料状态。</zh-CN>
        ///   <en>Target profile status.</en>
        /// </l>
        /// </param>
        /// <param name="auditAction">
        /// <l>
        ///   <zh-CN>稳定审计动作键。</zh-CN>
        ///   <en>Stable audit action key.</en>
        /// </l>
        /// </param>
        /// <param name="auditSummary">
        /// <l>
        ///   <zh-CN>不含敏感值的审计摘要。</zh-CN>
        ///   <en>Audit summary without sensitive values.</en>
        /// </l>
        /// </param>
        /// <param name="successMessage">
        /// <l>
        ///   <zh-CN>成功时显示的受控消息。</zh-CN>
        ///   <en>Controlled success message.</en>
        /// </l>
        /// </param>
        /// <param name="failurePrefix">
        /// <l>
        ///   <zh-CN>失败消息前缀。</zh-CN>
        ///   <en>Failure-message prefix.</en>
        /// </l>
        /// </param>
        private void ChangeUserProfileStatus(
            string status,
            string auditAction,
            string auditSummary,
            string successMessage,
            string failurePrefix)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit))
            {
                return;
            }

            if (string.Equals(status, PortalUserProfileStatuses.Disabled, StringComparison.Ordinal) && IsCurrentTargetSelf())
            {
                ShowRegistrationMessage("不能在当前会话中禁用自己的账号。", true);
                return;
            }

            IUserRegistrationInfo registration = UsersDB.GetRegistrationInfo(userId);
            if (registration != null &&
                string.Equals(registration.Status, PortalUserRegistrationStatuses.Rejected, StringComparison.Ordinal))
            {
                ShowRegistrationMessage("该账号的注册申请已拒绝，请先使用批准注册动作恢复。", true);
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>状态写入成功后再记录审计并刷新页面；审计或刷新异常不改变前一步已持久化事实。</zh-CN>
                //   <en>Status persistence is followed by audit and page refresh; an audit or refresh exception does not change the fact that the earlier write may be durable.</en>
                // </lang>
                UsersDB.SetUserProfileStatus(userId, status, auditAction, GetCurrentActor());
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.UserLifecycleCategory,
                    auditAction,
                    PortalOperationAuditEvents.UserTargetType,
                    userId.ToString(),
                    auditSummary,
                    Context);
                ShowRegistrationMessage(successMessage, false);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.ChangeUserProfileStatus",
                    "Changing user profile status failed. UserId=" + userId + "; Status=" + status,
                    exception,
                    Context);
                ShowRegistrationMessage(failurePrefix + eventId, true);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求操作者是否就是目标用户。</zh-CN>
        ///   <en>Determines whether the current request actor is the target user.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>操作者名称与规范目标一致时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the actor name matches the canonical target.</en>
        /// </l>
        /// </returns>
        private bool IsCurrentTargetSelf()
        {
            string actor = Context.User == null || Context.User.Identity == null
                ? string.Empty
                : Context.User.Identity.Name;
            return !string.IsNullOrWhiteSpace(actor) &&
                   currentUser != null &&
                   string.Equals(actor, currentUser.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前目标和分页参数返回用户管理页。</zh-CN>
        ///   <en>Returns to the user-management page with the current target and paging parameters.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>返回地址交由统一安全回跳策略处理，不直接信任请求中的 URL。</zh-CN>
        ///   <en>The return address is handled by the shared safe-return policy rather than trusting a request URL directly.</en>
        /// </lang>
        /// </remarks>
        private void RedirectToCurrentUser()
        {
            string url = ResolveUrl(
                "~/Admin/ManageUsers.aspx?userId=" + userId +
                "&username=" + Uri.EscapeDataString(currentUser.Name ?? string.Empty) +
                "&tabindex=" + tabIndex +
                "&tabid=" + tabId);
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, url);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造当前门户页的受限返回地址。</zh-CN>
        ///   <en>Builds the constrained return address for the current Portal page.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>门户首页或带正数页签参数的内部 URL。</zh-CN>
        ///   <en>An internal URL for the Portal home or positive tab parameters.</en>
        /// </l>
        /// </returns>
        private string BuildPortalReturnUrl()
        {
            if (tabId <= 0 || tabIndex <= 0)
            {
                return ResolveUrl("~/DesktopDefault.aspx");
            }

            return ResolveUrl("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前请求的审计操作者名称。</zh-CN>
        ///   <en>Reads the current request's audit actor name.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>缺失时返回的 admin 仅是既有审计兼容占位，不是身份认证、权限授予或操作者真实性证明。</zh-CN>
        ///   <en>The admin fallback is only an existing audit compatibility placeholder, not authentication, permission granting, or proof of actor authenticity.</en>
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
        ///   <zh-CN>设置用户资料输入控件的可编辑状态。</zh-CN>
        ///   <en>Sets whether user-profile input controls are editable.</en>
        /// </lang>
        /// </summary>
        /// <param name="enabled">
        /// <l>
        ///   <zh-CN>是否允许编辑。</zh-CN>
        ///   <en>Whether editing is enabled.</en>
        /// </l>
        /// </param>
        private void SetProfileInputsEnabled(bool enabled)
        {
            LoginName.Enabled = enabled;
            DisplayName.Enabled = enabled;
            Nickname.Enabled = enabled;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>清空管理员重置密码字段，降低页面回发、异常路径和调试残留风险。</zh-CN>
        ///   <en>Clears administrator password-reset fields to reduce postback, exception-path, and debugging residue.</en>
        /// </lang>
        /// </summary>
        private void ClearSubmittedPasswordFields()
        {
            Password.Text = string.Empty;
            ConfirmPassword.Text = string.Empty;
            EncryptedPassword.Value = string.Empty;
            EncryptedConfirmPassword.Value = string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造前端密码加密脚本所需的元素标识对象。</zh-CN>
        ///   <en>Builds the element-id object required by the client password-encryption script.</en>
        /// </lang>
        /// </summary>
        /// <param name="passwordElementId">
        /// <l>
        ///   <zh-CN>明文输入元素标识。</zh-CN>
        ///   <en>Plain-input element identifier.</en>
        /// </l>
        /// </param>
        /// <param name="encryptedElementId">
        /// <l>
        ///   <zh-CN>密文隐藏字段元素标识。</zh-CN>
        ///   <en>Encrypted hidden-field element identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已做 JavaScript 字符串编码的脚本对象文本。</zh-CN>
        ///   <en>Script-object text with JavaScript string encoding applied.</en>
        /// </l>
        /// </returns>
        private static string BuildPasswordFieldScriptObject(string passwordElementId, string encryptedElementId)
        {
            return string.Format(
                "{{passwordElementId:'{0}',encryptedElementId:'{1}'}}",
                HttpUtility.JavaScriptStringEncode(passwordElementId),
                HttpUtility.JavaScriptStringEncode(encryptedElementId));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造密码策略使用的低敏上下文词集合。</zh-CN>
        ///   <en>Builds the low-sensitivity context terms used by password policy.</en>
        /// </lang>
        /// </summary>
        /// <param name="loginName">
        /// <l>
        ///   <zh-CN>登录名。</zh-CN>
        ///   <en>Login name.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>显示名。</zh-CN>
        ///   <en>Display name.</en>
        /// </l>
        /// </param>
        /// <param name="nickname">
        /// <l>
        ///   <zh-CN>昵称。</zh-CN>
        ///   <en>Nickname.</en>
        /// </l>
        /// </param>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>邮箱。</zh-CN>
        ///   <en>Email address.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>按固定顺序组成且不包含密码的上下文词数组。</zh-CN>
        ///   <en>Ordered context terms that never include a password.</en>
        /// </l>
        /// </returns>
        private static string[] BuildPasswordPolicyContextTerms(
            string loginName,
            string displayName,
            string nickname,
            string email)
        {
            return new[]
            {
                loginName ?? string.Empty,
                displayName ?? string.Empty,
                nickname ?? string.Empty,
                email ?? string.Empty
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据资料前后快照生成不含敏感原文的变更字段摘要。</zh-CN>
        ///   <en>Builds a changed-field summary from profile snapshots without sensitive raw values.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>摘要只列字段名，不记录邮箱、登录名、昵称或密码值；归一化只用于比较，不改变持久化输入。</zh-CN>
        ///   <en>The summary lists field names only and never records email, login name, nickname, or password values; normalization is for comparison and does not alter persisted input.</en>
        /// </lang>
        /// </remarks>
        private string BuildProfileAuditSummary(
            IUserProfileInfo before,
            string loginName,
            string displayName,
            string nickname,
            string email)
        {
            string changedFields = string.Empty;
            AppendChangedField(ref changedFields, before == null ? currentUser.Name : before.LoginName, loginName, "LoginName");
            AppendChangedField(ref changedFields, before == null ? currentUser.Name : before.DisplayName, displayName, "DisplayName");
            AppendChangedField(ref changedFields, before == null ? string.Empty : before.Nickname, nickname, "Nickname");
            AppendChangedField(ref changedFields, before == null ? currentUser.Email : before.PreferredEmail, email, "PreferredEmail");
            return string.IsNullOrEmpty(changedFields)
                ? "Saved user profile without profile field changes."
                : "Updated user profile fields: " + changedFields + ".";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在字段发生有效变化时向审计字段名列表追加名称。</zh-CN>
        ///   <en>Appends a field name to the audit field list when an effective change exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="changedFields">
        /// <l>
        ///   <zh-CN>待更新的字段名列表。</zh-CN>
        ///   <en>Field-name list to update.</en>
        /// </l>
        /// </param>
        /// <param name="oldValue">
        /// <l>
        ///   <zh-CN>旧值，仅用于比较。</zh-CN>
        ///   <en>Previous value used only for comparison.</en>
        /// </l>
        /// </param>
        /// <param name="newValue">
        /// <l>
        ///   <zh-CN>新值，仅用于比较。</zh-CN>
        ///   <en>New value used only for comparison.</en>
        /// </l>
        /// </param>
        /// <param name="fieldName">
        /// <l>
        ///   <zh-CN>稳定字段名。</zh-CN>
        ///   <en>Stable field name.</en>
        /// </l>
        /// </param>
        private static void AppendChangedField(ref string changedFields, string oldValue, string newValue, string fieldName)
        {
            if (string.Equals(Normalize(oldValue), Normalize(newValue), StringComparison.Ordinal))
            {
                return;
            }

            changedFields = string.IsNullOrEmpty(changedFields)
                ? fieldName
                : changedFields + ", " + fieldName;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按显示名、昵称、登录名和回退名称顺序选择页面标题名称。</zh-CN>
        ///   <en>Selects a page-title name in display-name, nickname, login-name, and fallback order.</en>
        /// </lang>
        /// </summary>
        /// <param name="profile">
        /// <l>
        ///   <zh-CN>可为空的用户资料投影。</zh-CN>
        ///   <en>Optional user-profile projection.</en>
        /// </l>
        /// </param>
        /// <param name="fallbackName">
        /// <l>
        ///   <zh-CN>资料没有可用显示值时的回退名称。</zh-CN>
        ///   <en>Fallback name when no profile display value is available.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>用于页面标题的名称。</zh-CN>
        ///   <en>Name used by the page title.</en>
        /// </l>
        /// </returns>
        private static string GetEffectiveDisplayName(IUserProfileInfo profile, string fallbackName)
        {
            if (profile != null)
            {
                if (!string.IsNullOrWhiteSpace(profile.DisplayName))
                {
                    return profile.DisplayName;
                }

                if (!string.IsNullOrWhiteSpace(profile.Nickname))
                {
                    return profile.Nickname;
                }

                if (!string.IsNullOrWhiteSpace(profile.LoginName))
                {
                    return profile.LoginName;
                }
            }

            return fallbackName ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示经过 HTML 编码的注册/用户管理操作消息。</zh-CN>
        ///   <en>Displays an HTML-encoded registration or user-management operation message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>受控消息文本。</zh-CN>
        ///   <en>Controlled message text.</en>
        /// </l>
        /// </param>
        /// <param name="isError">
        /// <l>
        ///   <zh-CN>是否使用错误样式。</zh-CN>
        ///   <en>Whether to use the error style.</en>
        /// </l>
        /// </param>
        private void ShowRegistrationMessage(string message, bool isError)
        {
            // <lang>
            //   <zh-CN>保留主题状态行 class，避免后台提示在回发后退回旧式行内文本。</zh-CN>
            //   <en>Preserve the themed status-line class so postback messages do not fall back to legacy inline text.</en>
            // </lang>
            RegistrationMessage.CssClass = (isError ? "NormalRed" : "Normal") + " portal-status-line";
            RegistrationMessage.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>对页面展示文本执行 HTML 编码。</zh-CN>
        ///   <en>HTML-encodes text for page display.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待展示文本。</zh-CN>
        ///   <en>Text to display.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>编码后的文本，空值转为空字符串。</zh-CN>
        ///   <en>Encoded text, with null converted to an empty string.</en>
        /// </l>
        /// </returns>
        private string EncodeDisplay(string value)
        {
            return Server.HtmlEncode(value ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空白展示值转换为固定占位文本。</zh-CN>
        ///   <en>Converts a blank display value to a fixed placeholder.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始展示值。</zh-CN>
        ///   <en>Original display value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>原值或 <c>(none)</c> 占位。</zh-CN>
        ///   <en>The original value or the <c>(none)</c> placeholder.</en>
        /// </l>
        /// </returns>
        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为字段差异比较去除首尾空白并统一空值。</zh-CN>
        ///   <en>Trims values and normalizes blanks for field-difference comparison.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待比较值。</zh-CN>
        ///   <en>Value to compare.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>比较用规范文本。</zh-CN>
        ///   <en>Canonical comparison text.</en>
        /// </l>
        /// </returns>
        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>格式化必填 UTC 时间展示值。</zh-CN>
        ///   <en>Formats a required UTC timestamp for display.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>时间值。</zh-CN>
        ///   <en>Timestamp value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>UTC 展示文本或 legacy 占位。</zh-CN>
        ///   <en>UTC display text or a legacy placeholder.</en>
        /// </l>
        /// </returns>
        private static string FormatUtc(DateTime value)
        {
            return value == DateTime.MinValue ? "(legacy)" : value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>格式化可空 UTC 时间展示值。</zh-CN>
        ///   <en>Formats an optional UTC timestamp for display.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可空时间值。</zh-CN>
        ///   <en>Optional timestamp value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>UTC 展示文本或 none 占位。</zh-CN>
        ///   <en>UTC display text or a none placeholder.</en>
        /// </l>
        /// </returns>
        private static string FormatUtc(DateTime? value)
        {
            return value.HasValue ? FormatUtc(value.Value) : "(none)";
        }
    }
}
