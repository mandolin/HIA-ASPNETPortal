using System;

namespace ASPNET.StarterKit.Portal
{
        /// <summary>
        /// 显示编辑权限拒绝页面，并接入门户页面公共生命周期。
        /// Displays the edit-access-denied page while joining the shared portal-page lifecycle.
        /// </summary>
        /// <remarks>
        /// 此页只显示既有授权流程的拒绝结果；它不把 UI 编辑提示当作后端写操作授权。
        /// This page only displays the denial result from existing authorization flow; it does not treat a UI edit hint
        /// as authorization for a backend write action.
        /// </remarks>
    public partial class EditAccessDenied : PortalPage<EditAccessDenied>
    {
        /// <summary>
        /// 保持页面进入统一门户生命周期。
        /// Keeps the page in the shared portal-page lifecycle.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
