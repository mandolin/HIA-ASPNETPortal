using System;

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
            // 中文：统一清理主认证 Cookie 与角色 Cookie，确保虚拟目录 Path 一致。
            // English: Clear the main auth cookie and role cookie through one path-aware service.
            PortalAuthenticationService.SignOut(Response, Request);

            // Redirect the user back to the portal home page.
            // 重定向用户回到门户首页
            Response.Redirect(Request.ApplicationPath);
        }
    }
}
