using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// Shows the standard access denied page while keeping shared portal page behavior.
    /// 显示标准访问拒绝页面，并接入门户页面的公共行为。
    /// </summary>
    public partial class AccessDenied : PortalPage<AccessDenied>
    {
        /// <summary>
        /// Keeps the page on the shared portal page lifecycle for theme selection and dependency setup.
        /// 让页面进入统一门户生命周期，以便应用主题选择和依赖初始化。
        /// </summary>
        /// <param name="sender">The event source. 事件源。</param>
        /// <param name="e">The event data. 事件数据。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
