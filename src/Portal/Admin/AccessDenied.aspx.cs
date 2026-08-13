using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示标准访问拒绝页面，并接入门户页面公共生命周期。</zh-CN>
    ///   <en>Displays the standard access-denied page while joining the shared portal-page lifecycle.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此页不自行作出授权决定；调用方已完成拒绝判断后才导航到此处。继承 <see cref="PortalPage{T}"/> 仅用于主题选择和依赖初始化。</zh-CN>
    ///   <en>This page makes no authorization decision itself; callers navigate here only after deciding to deny access. Inheriting <see cref="PortalPage{T}"/> is used only for theme selection and dependency initialization.</en>
    /// </lang>
    /// </remarks>
    public partial class AccessDenied : PortalPage<AccessDenied>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>保持页面进入统一门户生命周期；该空处理器不重新作出授权决定。</zh-CN>
        ///   <en>Keeps the page in the shared portal-page lifecycle without making a new authorization decision.</en>
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
