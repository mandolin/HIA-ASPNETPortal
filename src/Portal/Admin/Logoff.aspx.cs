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

            // Invalidate the roles token.
            // 使角色令牌无效
            HttpCookie rolesCookie = Response.Cookies["portalroles"];
            if (rolesCookie != null)
            {
                rolesCookie.Value = null;
                // Set the expiration time to yesterday to invalidate the token immediately.
                // 设置过期时间为昨天，立即使令牌失效
                rolesCookie.Expires = DateTime.Now.AddDays(-1);

                //#todo 此处后期需要调整，包括Domain设置。因为需要兼容子站点以及虚拟目录/应用程序
                rolesCookie.Path = "/";
                Response.Cookies.Add(rolesCookie);
            }

            // Redirect the user back to the portal home page.
            // 重定向用户回到门户首页
            Response.Redirect(Request.ApplicationPath);
        }
    }
}