using System;
using System.Web.Security;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Resources;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class Signin : PortalModuleControl<Signin>
    {
        // 依赖注入用户数据库接口
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        //****************************************************************
        //
        // LoginBtn_Click 事件处理程序：用于验证用户凭证。
        // 使用 UsersDB 进行身份验证，并在成功后设置认证Cookie。
        //
        //****************************************************************

        protected void LoginBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // 清除输入的用户名或邮箱两端的空白字符
            var emailOrName = EmailOrName.Text.Trim();

            // 使用 UsersDB 尝试验证用户凭证
            // 注意：这里假设 Password 属性已经加密或散列处理
            string userName = UsersDB.Login(emailOrName, PortalSecurity.Encrypt(password.Text));

            if (!string.IsNullOrEmpty(userName))
            {
                // 使用 FormsAuthentication.SetAuthCookie 方法设置用户ID到客户端Cookie中
                // 第二个参数表示是否记住用户登录状态
                FormsAuthentication.SetAuthCookie(userName, RememberCheckbox.Checked);

                // 重定向浏览器回到原来的页面
                Response.Redirect(Request.ApplicationPath);
            }
            else
            {
                // 如果验证失败，显示错误消息
                Message.Text = string.Format("<br>{0}<br/>", lang.Signin_LoginFaild);
            }
        }
    }
}