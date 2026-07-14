using System;

namespace ASPNET.StarterKit.Portal
{
        /// <summary>
        /// 显示标准访问拒绝页面，并接入门户页面公共生命周期。
        /// Displays the standard access-denied page while joining the shared portal-page lifecycle.
        /// </summary>
        /// <remarks>
        /// 此页不自行作出授权决定；调用方已完成拒绝判断后才导航到此处。继承 <see cref="PortalPage{T}"/> 仅用于
        /// 主题选择和依赖初始化。
        /// This page makes no authorization decision itself; callers navigate here only after deciding to deny access.
        /// Inheriting <see cref="PortalPage{T}"/> is used only for theme selection and dependency initialization.
        /// </remarks>
    public partial class AccessDenied : PortalPage<AccessDenied>
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
