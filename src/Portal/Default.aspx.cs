using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户根地址的兼容跳转页。</zh-CN>
    ///   <en>Compatibility redirect page for the portal root address.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>当前仍保持旧门户入口行为，把根地址导向桌面首页；移动端入口和统一 Index 首页会在独立导航规划中处理。</zh-CN>
    ///   <en>The current implementation preserves the legacy portal entry behavior and redirects the root address to the desktop home page; mobile entry and a unified Index home page are handled by separate navigation planning.</en>
    /// </lang>
    /// </remarks>
    public partial class CDefault : PortalPage<CDefault>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>处理根页加载并立即跳转到桌面门户首页。</zh-CN>
        ///   <en>Handles root-page loading and immediately redirects to the desktop portal home page.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>Web Forms 生命周期事件来源；根入口分流不读取该值。</zh-CN>
        ///   <en>Web Forms lifecycle event source; root-entry routing does not read this value.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；保留用于匹配 Web Forms 生命周期签名。</zh-CN>
        ///   <en>Page-load event arguments retained to match the Web Forms lifecycle signature.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保留旧 WebForms 门户的根入口跳转，避免在导航重构完成前改变用户可见入口。</zh-CN>
            //   <en>Keep the legacy WebForms root-entry redirect so user-visible entry behavior does not change before navigation redesign is complete.</en>
            // </lang>
            Response.Redirect("DesktopDefault.aspx");
        }
    }
}
