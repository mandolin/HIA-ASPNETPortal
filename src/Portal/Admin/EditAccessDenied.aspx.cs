using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示编辑权限拒绝页面，并接入门户页面公共生命周期。</zh-CN>
    ///   <en>Displays the edit-access-denied page while joining the shared portal-page lifecycle.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此页只显示既有授权流程的拒绝结果；它不把 UI 编辑提示当作后端写操作授权。</zh-CN>
    ///   <en>This page only displays the denial result from existing authorization flow; it does not treat a UI edit hint as authorization for a backend write action.</en>
    /// </lang>
    /// </remarks>
    public partial class EditAccessDenied : PortalPage<EditAccessDenied>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>保持页面进入统一门户生命周期；该空处理器不把 UI 提示升级为写权限。</zh-CN>
        ///   <en>Keeps the page in the shared portal-page lifecycle without upgrading a UI message into write permission.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l zh-CN="事件源。" en="Event source." />
        /// </param>
        /// <param name="e">
        /// <l zh-CN="事件参数。" en="Event arguments." />
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
