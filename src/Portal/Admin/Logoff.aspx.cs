using System;
using System.Web;
using System.Web.Security;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// Page class for handling user logoff.
    /// 用于处理用户注销的页面类。
    /// </summary>
    public partial class Logoff : PortalPage<Logoff>
    {
        /// <summary>
        /// Page load event to handle user logoff logic.
        /// 页面加载事件，处理用户注销逻辑。
        /// </summary>
        /// <param name="sender">The source of the event. 事件源</param>
        /// <param name="e">An <see cref="EventArgs"/> containing the event data. 事件数据的<see cref="EventArgs"/>对象。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // Log the user out using the Cookie Authentication system.
            // 使用 Cookie 认证系统 注销用户
            FormsAuthentication.SignOut();

            // 角色 Cookie 的 Path 必须和写入时保持一致，虚拟目录部署时才能真正清除。
            PortalAuthenticationCookies.ExpireRolesCookie(Response, Request);

            // Redirect the user back to the portal home page.
            // 重定向用户回到门户首页
            Response.Redirect(Request.ApplicationPath);
        }
    }
}
