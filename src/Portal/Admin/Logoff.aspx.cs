using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>处理门户用户注销的页面。</zh-CN>
    ///   <en>Page that handles portal user sign-out.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>注销逻辑通过统一认证服务清理主认证 Cookie 和角色 Cookie，避免虚拟目录 Path 不一致导致旧 Cookie 残留。</zh-CN>
    ///   <en>Sign-out uses the shared authentication service to clear both the main auth cookie and the role cookie, preventing stale cookies caused by virtual-directory path differences.</en>
    /// </lang>
    /// </remarks>
    public partial class Logoff : PortalPage<Logoff>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>在页面加载时执行注销并返回门户首页。</zh-CN>
        ///   <en>Signs the user out during page load and returns to the portal home page.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l zh-CN="事件源。" en="Event source." />
        /// </param>
        /// <param name="e">
        /// <l zh-CN="事件数据。" en="Event data." />
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>统一清理主认证 Cookie 与角色 Cookie，确保虚拟目录 Path 一致。</zh-CN>
            //   <en>Clear the main auth cookie and role cookie through one path-aware service.</en>
            // </lang>
            PortalAuthenticationService.SignOut(Response, Request);

            // <lang>
            //   <zh-CN>注销后回到当前应用根路径，兼容根站点和虚拟目录两种部署形态。</zh-CN>
            //   <en>After sign-out, return to the current application root to support both root-site and virtual-directory deployments.</en>
            // </lang>
            Response.Redirect(Request.ApplicationPath);
        }
    }
}
