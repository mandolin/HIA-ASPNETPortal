using System;
using System.Web.UI.HtmlControls;

namespace ASPNET.StarterKit.Portal
{
        /// <summary>
        /// 显示尚未实现目标内容的样例数据链接占位页。
        /// Displays a placeholder page for sample-data links whose target content is not implemented yet.
        /// </summary>
        /// <remarks>
        /// 可选标题来自请求参数，按纯文本显示；此页面不提供原始 HTML 输入或预览语义。
        /// The optional title comes from a request parameter and is displayed as plain text; this page does not provide
        /// raw-HTML input or preview semantics.
        /// </remarks>
    public partial class NotImplemented : PortalPage<NotImplemented>
    {
        /// <summary>
        /// 占位页面中由服务器控制的标题元素。
        /// Server-controlled title element in the placeholder page.
        /// </summary>
        protected HtmlGenericControl title;

        /// <summary>
        /// 读取可选标题并作为纯文本写入占位页。
        /// Reads the optional title and writes it to the placeholder page as plain text.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        /// <remarks>
        /// 使用 <see cref="HtmlContainerControl.InnerText"/> 而非 <c>InnerHtml</c>，避免请求参数成为反射型 HTML 或脚本注入。
        /// Uses <see cref="HtmlContainerControl.InnerText"/> rather than <c>InnerHtml</c> so a request parameter cannot
        /// become reflected HTML or script injection.
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.Params["title"] != null)
            {
                title.InnerText = Request.Params["title"];
            }
        }
    }
}
