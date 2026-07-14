using System;
using System.Web.Security;
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
    /// 中文：当前登录仍将输入密码转换为既有无盐 MD5 摘要后交给 <see cref="IUsersDb.Login"/> 比较，
    /// 仅为历史用户表兼容保留。成功登录后由 Forms Authentication 签发身份票据，角色 Cookie 在认证请求阶段单独建立。
    ///
    /// English: Current sign-in still converts the submitted password to the legacy unsalted MD5 digest before
    /// passing it to <see cref="IUsersDb.Login"/> for comparison, solely for historical user-table compatibility.
    /// On success, Forms Authentication issues the identity ticket, while the role cookie is established separately during authentication requests.
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
        }

        /// <summary>
        /// 中文：处理登录提交，验证历史密码摘要和注册审核状态，并在成功后签发 Forms Authentication 身份票据。
        ///
        /// English: Handles sign-in submission, validates the legacy password digest and registration-review status, then issues a Forms Authentication identity ticket on success.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：图像按钮事件数据。English: Image-button event data.</param>
        protected void LoginBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // 中文：仅规范化输入边界，不在此记录用户名、密码或摘要。
            // English: Normalize input boundaries only; do not log user name, password, or digest here.
            var emailOrName = EmailOrName.Text.Trim();

            // 中文：保留历史 MD5 比较路径；未来强哈希迁移需要双格式验证与成功登录后的渐进升级。
            // English: Preserve the legacy MD5 comparison path; a future strong-hash migration requires dual-format verification and gradual upgrade after successful sign-in.
            string userName = UsersDB.Login(emailOrName, PortalSecurity.Encrypt(password.Text));

            if (!string.IsNullOrEmpty(userName))
            {
                // 中文：Forms Authentication 身份票据与 Remember 设置一致；角色票据在 AuthenticateRequest 中按需构造。
                // English: The Forms Authentication identity ticket follows Remember; the role ticket is built on demand during AuthenticateRequest.
                FormsAuthentication.SetAuthCookie(userName, RememberCheckbox.Checked);

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
    }
}
