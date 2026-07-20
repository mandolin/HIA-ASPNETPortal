using System;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Resources;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户登录模块的 Web Forms code-behind。
    ///
    /// English: Web Forms code-behind for the Portal sign-in module.
    /// </summary>
    /// <remarks>
    /// 中文：P5.2 起页面把一次性密码输入交给 <see cref="IUsersDb.SignIn"/>，由数据层处理强哈希和旧 MD5
    /// 兼容升级。P10.3 起提交前使用一次性 RSA 公钥把密码写入隐藏字段，服务端解密后仍只在当前请求内传递。
    /// 成功登录后签发带安全版本的 Forms Authentication 票据，角色 Cookie 在认证请求阶段按需建立。
    ///
    /// English: Starting with P5.2, the page sends one-time password input to <see cref="IUsersDb.SignIn"/>, while
    /// the data layer handles strong hashing and legacy MD5 compatibility upgrade. Starting with P10.3, the password
    /// is encrypted into a hidden field with a one-time RSA public key before submit; the server decrypts it and keeps
    /// the plain value only inside the current request. On success, a Forms Authentication ticket with the security
    /// version is issued, and the role cookie is established on demand during authentication requests.
    /// </remarks>
    public partial class Signin : PortalModuleControl<Signin>
    {
        /// <summary>
        /// 中文：用户登录和角色查询数据访问依赖。
        ///
        /// English: User sign-in and role-lookup data-access dependency.
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// 中文：按运行期系统设置决定是否显示自主注册链接。
        ///
        /// English: Determines whether to display the self-registration link from runtime system settings.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 中文：自主注册默认关闭；公开注册链接仅在有效运行期设置显式开启时显示。
            // English: Self-registration is disabled by default; show the public link only when the effective runtime setting explicitly enables it.
            RegisterLink.Visible = PortalRegistrationOptions.AllowSelfRegistration;

            RegisterLoginPasswordEncryptionScripts();
        }

        /// <summary>
        /// 中文：处理登录提交，验证密码、注册审核状态和安全版本，并在成功后签发 Forms Authentication 身份票据。
        ///
        /// English: Handles sign-in submission, validates the password, registration-review status, and security version, then issues a Forms Authentication identity ticket on success.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：按钮事件数据。English: Button event data.</param>
        protected void LoginBtn_Click(Object sender, EventArgs e)
        {
            // 中文：仅规范化输入边界，不在此记录用户名、密码或摘要。
            // English: Normalize input boundaries only; do not log user name, password, or digest here.
            var emailOrName = EmailOrName.Text.Trim();
            string submittedPassword;
            if (!TryResolveSubmittedPassword(out submittedPassword))
            {
                ClearSubmittedPasswordFields();
                Message.Text = string.Format("<br>{0}<br/>", lang.Signin_LoginFaild);
                return;
            }

            // 中文：不在页面层生成摘要；数据层负责强哈希验证、旧 MD5 兼容和迁移。
            // English: Do not generate a digest in the page layer; the data layer owns strong-hash verification, legacy MD5 compatibility, and migration.
            PortalSignInResult signInResult = UsersDB.SignIn(emailOrName, submittedPassword);

            ClearSubmittedPasswordFields();

            if (signInResult.Succeeded)
            {
                // 中文：主身份票据写入安全版本；角色票据在 AuthenticateRequest 中按需构造。
                // English: The main identity ticket carries the security version; the role ticket is built on demand during AuthenticateRequest.
                PortalAuthenticationService.SignIn(
                    Response,
                    Request,
                    signInResult.UserName,
                    signInResult.SecurityVersion,
                    RememberCheckbox.Checked);

                if (signInResult.UpgradedLegacyCredential)
                {
                    PortalOperationAudit.Record(
                        PortalOperationAuditEvents.SecurityCredentialsCategory,
                        PortalOperationAuditEvents.LegacyCredentialUpgraded,
                        PortalOperationAuditEvents.UserTargetType,
                        signInResult.UserId.ToString(),
                        "Legacy credential upgraded after successful sign-in.",
                        Context);
                }

                // 中文：回到应用根路径，沿用既有登录后导航。
                // English: Return to the application root and retain legacy post-sign-in navigation.
                Response.Redirect(Request.ApplicationPath);
            }
            else
            {
                // 中文：使用通用失败提示，不暴露用户是否存在、审核状态或摘要比较细节。
                // English: Use a generic failure message and do not expose user existence, review status, or digest-comparison details.
                Message.Text = string.Format("<br>{0}<br/>", lang.Signin_LoginFaild);
            }
        }

        /// <summary>
        /// 中文：注册 IE6 兼容的登录密码加密脚本和按钮提交前处理。
        ///
        /// English: Registers the IE6-compatible login-password encryption scripts and pre-submit button hook.
        /// </summary>
        private void RegisterLoginPasswordEncryptionScripts()
        {
            if (!PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired())
            {
                SigninBtn.OnClientClick = string.Empty;
                return;
            }

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Signin),
                "JSEncryptIE6",
                ResolveUrl("~/Scripts/Security/jsencrypt-ie6.min.js"));

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Signin),
                "PortalLoginPasswordEncryption",
                ResolveUrl("~/Scripts/Security/PortalLoginPasswordEncryption.js"));

            SigninBtn.OnClientClick = string.Format(
                "return PortalLoginPasswordEncryption.encryptPassword('{0}','{1}','{2}','{3}');",
                HttpUtility.JavaScriptStringEncode(password.ClientID),
                HttpUtility.JavaScriptStringEncode(EncryptedPassword.ClientID),
                HttpUtility.JavaScriptStringEncode(ResolveUrl("~/Security/LoginPasswordKey.ashx")),
                HttpUtility.JavaScriptStringEncode(Message.ClientID));
        }

        /// <summary>
        /// 中文：解析提交密码。优先使用隐藏密文字段；只有配置显式允许时才接受明文回退。
        ///
        /// English: Resolves the submitted password. The encrypted hidden field is preferred; plain fallback is
        /// accepted only when configuration explicitly allows it.
        /// </summary>
        /// <param name="submittedPassword">中文：当前请求内使用的明文密码。English: Plain password used inside the current request.</param>
        /// <returns>中文：密码提交满足当前安全策略时为 <c>true</c>。English: <c>true</c> when the submission satisfies the current security policy.</returns>
        private bool TryResolveSubmittedPassword(out string submittedPassword)
        {
            submittedPassword = string.Empty;

            if (!string.IsNullOrWhiteSpace(EncryptedPassword.Value))
            {
                string failureCode;
                string eventId;
                if (PortalPasswordSubmissionCrypto.TryDecryptSubmittedPassword(
                    Context,
                    EncryptedPassword.Value,
                    out submittedPassword,
                    out failureCode,
                    out eventId))
                {
                    return true;
                }

                // 中文：只展示通用失败提示；详细分类和事件编号留在诊断日志中。
                // English: Show only the generic failure message; detailed category and event id remain in diagnostics logs.
                return false;
            }

            if (PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired())
            {
                PortalDiagnostics.Warn(
                    "LoginPasswordEncryption",
                    "Login password was submitted without the required encrypted field.",
                    Context);

                return false;
            }

            submittedPassword = password.Text;
            return true;
        }

        /// <summary>
        /// 中文：清空页面层的密码提交字段，降低回显、调试和异常路径残留风险。
        ///
        /// English: Clears page-level password submission fields to reduce echo, debugging, and exception-path residue.
        /// </summary>
        private void ClearSubmittedPasswordFields()
        {
            password.Text = string.Empty;
            EncryptedPassword.Value = string.Empty;
        }
    }
}
