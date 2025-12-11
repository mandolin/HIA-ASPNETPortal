using System;
using System.Web.Security;
using Microsoft.Practices.Unity;
using Unity;

// 定义命名空间
namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   Summary description for Register.
    ///   注册类的简要描述。
    /// </summary>
    public partial class Register : PortalPage<Register>
    {
        // 使用依赖注入标记属性定义IUsersDb接口的属性
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        // 注册按钮点击事件处理程序
        protected void RegisterBtn_Click(object sender, EventArgs e)
        {
            // 只有在页面上的所有表单字段都有效的情况下才尝试登录
            if (Page.IsValid)
            {
                // 从表单中获取用户输入的用户名，并去除前后空格
                var userName = Name.Text.Trim();
                // 从表单中获取用户输入的电子邮件地址，并去除前后空格
                var email = Email.Text.Trim();

                // 尝试将新用户添加到门户用户数据库中
                // 如果返回值大于-1，则表示用户成功添加到数据库
                if ((UsersDB.AddUser(userName, email, PortalSecurity.Encrypt(Password.Text))) > -1)
                {
                    // 设置用户的认证名称为userId
                    FormsAuthentication.SetAuthCookie(userName, false);

                    // 重定向浏览器回到首页
                    Response.Redirect("~/DesktopDefault.aspx");
                }
                else
                {
                    // 如果注册失败，则更新Message标签内容
                    Message.Text = "Registration Failed!  <" + "u" + ">" + userName + " or " + email + "<" + "/u" +
                                   "> is already registered." + "<" + "br" + ">" +
                                   "Please register using a different email address.";
                }
            }
        }
    }
}