using System;
using System.Web.UI.HtmlControls;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// Shows a placeholder page for sample data links that do not have real content yet.
    /// 显示样例数据链接的占位页面，用于提示目标内容尚未实现。
    /// </summary>
    public partial class NotImplemented : PortalPage<NotImplemented>
    {
        /// <summary>
        /// Server-side title element rendered by the placeholder page.
        /// 占位页面中由服务端控制的标题元素。
        /// </summary>
        protected HtmlGenericControl title;

        /// <summary>
        /// Loads the optional sample title from the query string.
        /// 从查询字符串读取可选的样例标题。
        /// </summary>
        /// <param name="sender">The event source. 事件源。</param>
        /// <param name="e">The event data. 事件数据。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.Params["title"] != null)
            {
                title.InnerHtml = Request.Params["title"];
            }
        }
    }
}
